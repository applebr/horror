using System;
using System.Collections;
using System.Collections.Generic;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    public enum DeathKind { Scissors, Fingers, Mother, Kids }

    /// <summary>방을 돌아다니는 본 게임 상태.</summary>
    public sealed class PlayState : IState
    {
        public GameCore G;
        public Room Room;
        public Player Player;
        public readonly Canvas World = new Canvas(GameCore.WorldW, GameCore.WorldH);
        public readonly Lighting Light = new Lighting();
        public readonly WorldBlood Blood = new WorldBlood();
        public Hud Hud;
        public HandOverlay Hand;
        public Stalker Stalker;

        readonly Dictionary<string, byte[]> stainCache = new Dictionary<string, byte[]>();
        readonly List<Entity> tmp = new List<Entity>();
        readonly List<Entity> drawList = new List<Entity>();

        public float LucidT;
        public float LucidMax = 26f;
        public bool Lucid { get { return LucidT > 0; } }
        public float SpeedMul = 1f;
        int scriptCount;
        bool transitioning, dying;
        public bool ScriptBusy { get { return scriptCount > 0 || transitioning || dying; } }
        public bool Dying { get { return dying; } }

        /// <summary>입력 대기 중인 대사, 쪽지, 컷신(겹쳐 보이는 것 포함) 동안 세계(괴물)가 멈춘다.</summary>
        public bool WorldPaused
        {
            get
            {
                if (G.Note.Active || transitioning) return true;
                if (G.Dialogue.Active && !G.Dialogue.IsTimed) return true;
                return G.Cutscene != null;
            }
        }
        public bool Transitioning { get { return transitioning; } }
        public string CheckRoom = "d_hana";
        public float CheckX = 40, CheckY = 40;
        public bool NoDeath;
        public Entity InteractTarget;
        public Entity HideSpot;

        // 마지막 소음 (몬스터가 듣는다)
        public float NoiseX, NoiseY, NoiseT = -99, NoiseLoud;
        public string LastExitKey;
        public string PrevRoomId;

        public void Enter(GameCore game)
        {
            G = game;
            Hud = new Hud(this);
            Hand = new HandOverlay(this);
            Stalker = new Stalker(this);
            Player = new Player();
            Player.P = this;
        }

        public void Exit()
        {
            G.Audio.SetLoopLevel(Ch.Heart, 0, 0.3f);
        }

        // ───────── 방 ─────────

        public void StartAt(string roomId, float x, float y)
        {
            CheckRoom = roomId; CheckX = x; CheckY = y;
            LoadRoom(roomId, x, y, 0);
        }

        public void LoadRoom(string id, float x, float y, int dir)
        {
            if (Room != null) stainCache[Room.Def.Id] = Room.Stains;
            PrevRoomId = Room != null ? Room.Def.Id : null;
            var def = Rooms.Get(id);
            Room = new Room(def);
            byte[] st;
            if (stainCache.TryGetValue(id, out st) && st.Length == Room.Stains.Length)
                Buffer.BlockCopy(st, 0, Room.Stains, 0, st.Length);
            Player.P = this;
            Player.X = x; Player.Y = y; Player.Dir = dir;
            Player.PushX = Player.PushY = 0;
            Room.Entities.Add(Player);
            Blood.SetRoom(Room);
            if (def.Setup != null) def.Setup(Room, G);
            foreach (var e in Room.Entities) { e.P = this; }
            foreach (var e in Room.Entities.ToArray()) if (e != Player) e.Added();
            if (def.Ambience != Music.None) G.Audio.SetMusic(Ch.Ambience, def.Ambience, def.AmbienceVol, 1.2f);
            else G.Audio.StopMusic(Ch.Ambience, 1f);
            switch (def.Theme)
            {
                case Theme.Reality: Light.AuraRadius = 22; Light.AuraIntensity = 0.5f; break;
                case Theme.Morning: Light.AuraRadius = 20; Light.AuraIntensity = 0.3f; break;
                case Theme.CutRoom: Light.AuraRadius = 15; Light.AuraIntensity = 0.5f; break;
                default: Light.AuraRadius = 17; Light.AuraIntensity = 0.55f; break;
            }
            G.Flags.Set("visited_" + id);
            Stalker.OnRoomLoaded();
            InteractTarget = null;
        }

        public T Spawn<T>(T e) where T : Entity
        {
            e.P = this;
            Room.Entities.Add(e);
            e.Added();
            return e;
        }

        public void GoTo(string roomId, string spawn)
        {
            var def = Rooms.Get(roomId);
            Pt p;
            if (!def.Spawns.TryGetValue(spawn, out p)) p = new Pt(def.Map[0].Length * 2, def.Map.Length * 2, 0);
            LoadRoom(roomId, p.X, p.Y, p.Dir);
        }

        public Pt SpawnPoint(string roomId, string spawn)
        {
            var def = Rooms.Get(roomId);
            Pt p;
            if (!def.Spawns.TryGetValue(spawn, out p)) p = new Pt(def.Map[0].Length * 2, def.Map.Length * 2, 0);
            return p;
        }

        public IEnumerator Transition(string roomId, string spawn, bool sound = true, float fade = 0.15f)
        {
            transitioning = true;
            if (sound) G.Audio.Play(Sfx.DoorOpen, 0.35f, G.Rng.Range(0.9f, 1.1f));
            yield return G.FadeOut(fade);
            GoTo(roomId, spawn);
            yield return G.FadeIn(fade);
            transitioning = false;
        }

        void CheckExits()
        {
            if (ScriptBusy || Player.Dead || Player.Hidden || Player.Frozen || (G.Dialogue.Active && !G.Dialogue.IsTimed) || G.Note.Active) return;
            int cx = (int)Math.Floor(Player.X / Room.CS), cy = (int)Math.Floor((Player.Y - 1f) / Room.CS);
            var e = Room.ExitAt(cx, cy);
            if (e == null || !e.IsOpen(G, Lucid)) return;
            G.Scripts.Start(ExitRoutine(e), "transition");
        }

        IEnumerator ExitRoutine(ExitDef e)
        {
            transitioning = true;
            LastExitKey = Room.Def.Id + ":" + e.Key;
            if (e.Before != null)
            {
                var b = e.Before(G);
                if (b != null) yield return b;
            }
            if (!e.Silent)
            {
                if (e.Style == ExitStyle.Door) G.Audio.Play(Sfx.DoorOpen, 0.3f, G.Rng.Range(0.9f, 1.15f));
                else if (e.Style == ExitStyle.Seam) G.Audio.Play(Sfx.Rip, 0.6f, 0.8f);
            }
            string from = Room.Def.Id;
            yield return G.FadeOut(0.14f);
            GoTo(e.To, e.Spawn);
            Stalker.OnPlayerChangedRoom(from, e);
            yield return G.FadeIn(0.14f);
            transitioning = false;
        }

        // ───────── 스크립트 ─────────

        /// <summary>플레이어 조작을 막고 실행되는 스크립트.</summary>
        public ScriptRunner.Handle RunScript(IEnumerator script)
        {
            scriptCount++;
            return G.Scripts.Start(Wrap(script), "ps");
        }

        IEnumerator Wrap(IEnumerator script)
        {
            try
            {
                yield return script;
            }
            finally
            {
                scriptCount = Math.Max(0, scriptCount - 1);
            }
        }

        /// <summary>플레이어 조작을 막지 않는 배경 스크립트.</summary>
        public ScriptRunner.Handle RunAmbient(IEnumerator script)
        {
            return G.Scripts.Start(script, "amb");
        }

        public IEnumerator Walk(Entity e, float tx, float ty, float speed)
        {
            var actor = e as Actor;
            if (actor != null) actor.Walking = true;
            while (true)
            {
                float dx = tx - e.X, dy = ty - e.Y;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < 0.6f) break;
                float step = Math.Min(d, speed * GameCore.Dt);
                e.X += dx / d * step;
                e.Y += dy / d * step;
                if (actor != null) actor.FaceDir(dx, dy);
                if (e == Player)
                {
                    Player.Moving = true;
                    Player.AnimT += GameCore.Dt;
                    Player.Face(tx, ty);
                    Player.StepT -= GameCore.Dt;
                    if (Player.StepT <= 0) { Player.StepT = 0.32f; G.Audio.Play(Sfx.Step, 0.2f, G.Rng.Range(0.9f, 1.1f)); }
                }
                yield return null;
            }
            if (actor != null) actor.Walking = false;
            if (e == Player) { Player.Moving = false; Player.AnimT = 0; }
        }

        // ───────── 상호작용 ─────────

        Entity FindTarget()
        {
            if (Player == null) return null;
            float px = Player.X + Player.FaceX * 5f, py = Player.Y - 2f + Player.FaceY * 5f;
            Entity best = null;
            float bestD = float.MaxValue;
            foreach (var e in Room.Entities)
            {
                if (e == Player || !e.Interactable) continue;
                float d = e.DistTo(px, py);
                if (d > e.IRange) continue;
                // 등 뒤의 물건은 제외
                float cx = (e.L + e.R) * 0.5f - Player.X, cy = (e.T + e.B) * 0.5f - (Player.Y - 2);
                float dot = cx * Player.FaceX + cy * Player.FaceY;
                if (dot < -4f && d > 1.5f) continue;
                // 가구 위에 놓인 물건(Layer 1)을 가구보다 먼저 고른다
                float score = d - (e.Layer > 0 ? 6f : 0f);
                if (score < bestD) { bestD = score; best = e; }
            }
            return best;
        }

        public void TryInteract()
        {
            var t = FindTarget();
            if (t != null)
            {
                Player.Face((t.L + t.R) * 0.5f, (t.T + t.B) * 0.5f);
                RunScript(t.OnInteract());
                return;
            }
            // 잠긴 문
            float px = Player.X + Player.FaceX * 6f, py = Player.Y - 2f + Player.FaceY * 6f;
            int cx = (int)Math.Floor(px / Room.CS), cy = (int)Math.Floor(py / Room.CS);
            for (int k = 0; k < 3; k++)
            {
                var e = Room.ExitAt(cx + (int)Player.FaceX * k, cy + (int)Player.FaceY * k);
                if (e != null && !e.IsOpen(G, Lucid) && e.Style != ExitStyle.Seam && e.LockedText != null)
                {
                    G.Audio.Play(Sfx.Lock, 0.3f, 1.2f);
                    RunScript(G.Say(e.LockedSpeaker, e.LockedText));
                    return;
                }
            }
        }

        // ───────── 손가락 세기 / 자각 ─────────

        public void StartRealityCheck()
        {
            if (!Player.CanAct) return;
            if (!G.Flags.Has("can_check")) return;
            RunScript(RealityCheck());
        }

        IEnumerator RealityCheck()
        {
            Player.Counting = true;
            Player.LightOn = false;
            bool dream = Room.Def.Dream;
            int fingers = 5;
            if (dream)
            {
                int n = G.Flags.Inc("checks");
                fingers = 6 + (n >= 4 ? 1 : 0) + (n >= 9 ? 1 : 0);
            }
            if (dream && !G.Flags.Has("first_check"))
            {
                G.Flags.Set("first_check");
                yield return G.PlayCutscene(new FingerCutscene(6, FingerCutscene.Mode.FirstDream));
                Player.Counting = false;
                BecomeLucid(true);
                yield return Story.AfterFirstCheck(G);
                yield break;
            }
            Hand.Begin(fingers, dream);
            while (Hand.Active) yield return null;
            Player.Counting = false;
            if (dream) BecomeLucid(false);
            else if (!Story.OnRealityCheckInReality(G))
                yield return G.Say(Speaker.Think, "…다섯 개. 여긴 진짜야.");
        }

        public void BecomeLucid(bool silent)
        {
            bool was = Lucid;
            LucidT = LucidMax;
            G.Audio.Play(Sfx.Lucid, 0.8f);
            G.Fx.Flash(Pal.White, 2);
            G.Fx.Shake(1, 0.3f);
            if (!was && !silent) Hud.Notice("꿈이야.", 1.6f);
            Stalker.OnLucid();
        }

        // ───────── 숨기 ─────────

        public void Hide(Entity spot)
        {
            HideSpot = spot;
            Player.Hidden = true;
            Player.LightOn = false;
            G.Audio.Play(Sfx.DoorClose, 0.25f, 1.4f);
            Stalker.OnPlayerHid();
        }

        public void Unhide()
        {
            if (!Player.Hidden) return;
            Player.Hidden = false;
            G.Audio.Play(Sfx.DoorOpen, 0.2f, 1.5f);
            HideSpot = null;
        }

        // ───────── 죽음 ─────────

        public void Kill(DeathKind kind)
        {
            if (dying || Player.Dead || NoDeath || G.Flags.Has("god")) return;
            dying = true;
            Player.Dead = true;
            Player.LightOn = false;
            G.Scripts.StopTag("ps");
            G.Scripts.StopTag("amb");
            G.Scripts.StopTag("transition");
            scriptCount = 0;
            transitioning = false;
            G.Dialogue.Close();
            G.Note.Active = false;
            Hand.Cancel();
            G.Scripts.Start(DeathRoutine(kind), "death");
        }

        static readonly string[] respawnLines =
        {
            "…다시 잠들었다.",
            "…또 같은 꿈이다.",
            "…눈을 떴다. 아직 4시 44분.",
            "…깨어날 수 없었다.",
        };

        IEnumerator DeathRoutine(DeathKind kind)
        {
            G.Audio.StopAllMusic(0.05f);
            G.Audio.SetLoopLevel(Ch.Heart, 0, 0.1f);
            yield return G.PlayCutscene(new DeathCutscene(kind));
            int deaths = G.Flags.Inc("deaths");
            G.Fx.Reset();
            G.Fx.Fade = 1f;
            G.Fx.Vignette = 0.5f;
            yield return G.Wait(0.6f);
            yield return G.SayAt(Speaker.Narr, respawnLines[(deaths - 1) % respawnLines.Length], BoxPos.Middle);
            Respawn();
            yield return G.FadeIn(1.0f);
            dying = false;
        }

        void Respawn()
        {
            Stalker.Reset();
            Player.Dead = false;
            Player.Fear = 0;
            Player.Hidden = false;
            Player.Frozen = false;
            Player.Counting = false;
            Player.Hurt = 0;
            Player.ResetLight();
            LucidT = 0;
            HideSpot = null;
            G.Input.Blocked = false;
            Story.BeforeRespawn(G);
            LoadRoom(CheckRoom, CheckX, CheckY, 0);
        }

        public void SetCheckpoint(string roomId, float x, float y, bool save)
        {
            CheckRoom = roomId; CheckX = x; CheckY = y;
            if (save) G.SaveCheckpoint(roomId, x, y);
        }

        // ───────── 충돌 / 소리 ─────────

        public bool BlockedFor(Entity mover, float x, float y)
        {
            float x0 = x + mover.BX0, y0 = y + mover.BY0, x1 = x + mover.BX1, y1 = y + mover.BY1;
            bool isPlayer = mover == Player;
            if (isPlayer)
            {
                if (Room.BoxHitsCells(x0, y0, x1, y1, G, Lucid)) return true;
            }
            else if (Room.BoxHitsCellsMonster(x0, y0, x1, y1)) return true;
            foreach (var e in Room.Entities)
            {
                if (e == mover || !e.Solid || e.Removed || !e.Visible) continue;
                if (e.Overlaps(x0, y0, x1, y1)) return true;
            }
            return false;
        }

        public void Noise(float x, float y, float loud)
        {
            NoiseX = x; NoiseY = y; NoiseT = Room.Time; NoiseLoud = loud;
        }

        public bool StainAt(float x, float y)
        {
            int ix = (int)x, iy = (int)y;
            if (ix < 0 || iy < 0 || ix >= Room.PxW || iy >= Room.PxH) return false;
            return Room.Stains[iy * Room.PxW + ix] != Pal.Transparent;
        }

        // ───────── 갱신 ─────────

        public void Update(float dt)
        {
            if (Room == null) return;
            Room.Time += dt;
            if (LucidT > 0)
            {
                LucidT -= dt;
                if (LucidT <= 0) { LucidT = 0; Hud.Notice("흐릿해졌다…", 1.4f); G.Audio.Play(Sfx.StingSoft, 0.3f, 0.7f); }
            }
            Light.UpdateBeam(this);
            bool paused = WorldPaused;
            tmp.Clear();
            tmp.AddRange(Room.Entities);
            foreach (var e in tmp)
            {
                if (e.Removed) continue;
                e.P = this;
                // 대사/쪽지를 읽는 동안 괴물은 멈춘다 (읽다가 죽는 일이 없도록)
                if (paused && e is Monster) continue;
                e.Update(dt);
            }
            Room.Entities.RemoveAll(e => e.Removed);
            Blood.Update(dt);
            Hand.Update(dt);
            if (!paused) Stalker.Update(dt);
            CheckExits();
            InteractTarget = Player.CanAct ? FindTarget() : null;
            Hud.Update(dt);
            float sy = (Room.OY + Player.Y) * 2f;
            if (sy > 158) G.PreferTopBox = true;
            else if (sy < 132) G.PreferTopBox = false;

            // 공포
            Player.Fear = Math.Max(0, Player.Fear - dt * 0.08f);
            float fearFx = Math.Max(Player.Fear, Stalker.Proximity * 0.6f);
            G.Fx.Fear = M.Approach(G.Fx.Fear, fearFx, dt * 1.5f);
            float heart = Math.Max(Player.Fear, Stalker.Proximity);
            G.Audio.SetLoopLevel(Ch.Heart, heart > 0.15f ? 0.25f + heart * 0.6f : 0f, 0.5f);
            G.Audio.SetPitch(Ch.Heart, 1f + heart * 1.1f);
        }

        // ───────── 그리기 ─────────

        public byte[][] Ramps
        {
            get
            {
                switch (Room.Def.Theme)
                {
                    case Theme.Reality:
                    case Theme.Morning:
                        return Pal.DarkWarm;
                    default:
                        return Lucid ? Pal.DarkBlood : Pal.DarkNight;
                }
            }
        }

        public void Draw(Canvas screen)
        {
            if (Room == null) return;
            Light.Compute(this);
            World.OffX = 0; World.OffY = 0;
            World.ResetClip();
            World.Clear(Pal.Black);
            RoomRenderer.DrawCells(this, World);
            DrawStains(World);
            DrawEntities(World);
            Light.Apply(World, Ramps);
            RoomRenderer.DrawPostLight(this, World);
            int sx, sy;
            G.Fx.GetShake(out sx, out sy);
            screen.BlitCanvas(World, sx * 2, sy * 2, 2);
            foreach (var e in Room.Entities)
                if (!e.Removed && e.Visible) e.DrawScreen(screen, Room.OX * 2 + sx * 2, Room.OY * 2 + sy * 2);
        }

        void DrawStains(Canvas w)
        {
            int ox = Room.OX, oy = Room.OY, pw = Room.PxW, ph = Room.PxH;
            var st = Room.Stains;
            for (int y = 0; y < ph; y++)
            {
                int wy = oy + y;
                if (wy < 0 || wy >= w.H) continue;
                for (int x = 0; x < pw; x++)
                {
                    byte c = st[y * pw + x];
                    if (c == Pal.Transparent) continue;
                    int wx = ox + x;
                    if (wx < 0 || wx >= w.W) continue;
                    w.Px[wy * w.W + wx] = c;
                }
            }
        }

        void DrawEntities(Canvas w)
        {
            int ox = Room.OX, oy = Room.OY;
            for (int layer = -1; layer <= 1; layer++)
            {
                drawList.Clear();
                foreach (var e in Room.Entities)
                    if (!e.Removed && e.Layer == layer && e.Visible) drawList.Add(e);
                // y 정렬 (삽입 정렬: 안정적)
                for (int i = 1; i < drawList.Count; i++)
                {
                    var v = drawList[i];
                    int k = i - 1;
                    while (k >= 0 && drawList[k].Y > v.Y) { drawList[k + 1] = drawList[k]; k--; }
                    drawList[k + 1] = v;
                }
                foreach (var e in drawList) e.Draw(w, ox, oy);
                if (layer == 0) Blood.Draw(w, ox, oy);
            }
        }

        public void DrawUi(Canvas screen)
        {
            if (Room == null) return;
            if (Player.Hidden) DrawHideView(screen);
            Hud.Draw(screen);
            Hand.Draw(screen);
        }

        void DrawHideView(Canvas c)
        {
            // 장롱 문틈으로 보는 시야: 세로 판자 사이로만 보인다
            for (int x = 0; x < c.W; x++)
            {
                int m = x % 40;
                bool slat = m < 30;
                if (!slat) continue;
                for (int y = 0; y < c.H; y++)
                {
                    int i = y * c.W + x;
                    c.Px[i] = (m == 0 || m == 29) ? Pal.Ink : Pal.Black;
                }
            }
            for (int x = 0; x < c.W; x += 40)
                for (int y = 0; y < c.H; y++)
                {
                    if (((y >> 2) + (x >> 3)) % 7 == 0) c.Set(x + 15, y, Pal.DarkBrown);
                }
            G.FontSmall.DrawCentered(c, "숨어 있다 — Z: 나가기", GameCore.W / 2, GameCore.H - 16, Pal.DarkGray);
        }
    }
}
