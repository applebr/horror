using System;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>
    /// 가위의 출몰을 관리한다. 일정 시간마다 다가오는 소리 → 방에 들어옴 → 추적.
    /// 플레이어가 방을 옮기면 몇 초 뒤 같은 문으로 따라 들어온다.
    /// </summary>
    public sealed class Stalker
    {
        readonly PlayState p;
        public readonly FlowField Flow = new FlowField();
        public ScissorMan Active;
        public float Proximity;
        float cooldown = 50f;
        float comingT = -1;
        ExitDef comingExit;
        float followT = -1;
        string followSpawn;
        float huntT;
        float flowT;

        public Stalker(PlayState ps) { p = ps; }

        GameCore G { get { return p.G; } }

        public bool Enabled { get { return G.Flags.Has("stalker_on") && !G.Flags.Has("stalker_off"); } }

        int Difficulty
        {
            get
            {
                int d = 0;
                if (G.Flags.Has("piece_bath")) d++;
                if (G.Flags.Has("piece_kitchen")) d++;
                if (G.Flags.Has("piece_mom")) d++;
                return d;
            }
        }

        float NewCooldown()
        {
            int d = Difficulty;
            float baseT = d <= 1 ? 70f : (d == 2 ? 52f : 40f);
            return baseT + G.Rng.Range(-8f, 12f);
        }

        bool RoomOk
        {
            get
            {
                var def = p.Room.Def;
                return def.Dream && def.StalkerAllowed && !def.Safe;
            }
        }

        public void Reset()
        {
            if (Active != null) Active.Remove();
            Active = null;
            comingT = -1;
            followT = -1;
            Proximity = 0;
            cooldown = Math.Max(cooldown, 35f);
            G.Audio.SetLoopLevel(Ch.Noise, 0, 0.3f);
        }

        public void OnRoomLoaded()
        {
            Active = null;
            comingT = -1;
            flowT = 0;
            if (!RoomOk) followT = -1;
        }

        public void OnPlayerChangedRoom(string from, ExitDef via)
        {
            if (!Enabled) return;
            bool wasHunting = followT == -2;
            if (wasHunting && RoomOk)
            {
                // 같은 문으로 따라 들어온다
                followT = Math.Max(1.6f, 3.2f - Difficulty * 0.4f);
                followSpawn = via.Spawn;
            }
            else followT = -1;
        }

        public void OnLucid()
        {
            if (!Enabled || Active != null) return;
            // 자각한 생각을 듣는다
            cooldown = Math.Max(4f, cooldown - 15f);
        }

        public void OnPlayerHid()
        {
            if (Active == null) return;
            var pl = p.Player;
            bool seen = p.Room.LineOfSight(Active.X, Active.Y - 12, pl.X, pl.Y - 4) && M.Dist(Active.X, Active.Y, pl.X, pl.Y) < 70f;
            if (seen) Active.SetState(ScissorMan.St.Grab);
            else Active.SetState(ScissorMan.St.Search);
        }

        public void OnLeft()
        {
            Active = null;
            followT = -1;
            cooldown = NewCooldown();
        }

        /// <summary>스크립트용: 지정 위치에 즉시 나타나 추적 시작.</summary>
        public ScissorMan Appear(float x, float y)
        {
            if (Active != null && !Active.Removed) { Active.X = x; Active.Y = y; return Active; }
            var s = new ScissorMan { X = x, Y = y };
            s.Speed = Math.Min(29f, 24f + Difficulty * 2f);
            p.Spawn(s);
            Active = s;
            huntT = 0;
            followT = -2;
            G.Audio.Play(Sfx.Snip, 0.8f);
            return s;
        }

        public void Update(float dt)
        {
            var pl = p.Player;
            if (Active != null && Active.Removed) { Active = null; if (followT == -2) followT = -1; cooldown = NewCooldown(); }

            // 거리장 갱신 (괴물 공용)
            flowT -= dt;
            if (flowT <= 0)
            {
                flowT = 0.25f;
                Flow.Build(p, pl.X, pl.Y - 1);
            }

            float prox = 0;
            if (Active != null)
            {
                float d = M.Dist(Active.X, Active.Y, pl.X, pl.Y);
                prox = M.Clamp01(1f - d / 110f);
                huntT += dt;
                followT = -2; // 추적 중 표시
                // 너무 오래 못 잡으면 물러난다
                if (huntT > 45f && Active.State == ScissorMan.St.Stalk && !G.Flags.Has("chase_scripted"))
                    Active.SetState(ScissorMan.St.Leave);
            }

            if (!Enabled || !RoomOk || p.Dying || G.Flags.Has("chase_scripted"))
            {
                Proximity = M.Approach(Proximity, prox, dt * 2f);
                if (comingT >= 0 && !RoomOk) comingT = -1;
                return;
            }

            // 따라 들어오기
            if (followT > 0)
            {
                followT -= dt;
                prox = Math.Max(prox, 0.55f);
                if (followT <= 0)
                {
                    followT = -1;
                    var sp = p.SpawnPoint(p.Room.Def.Id, followSpawn);
                    Appear(sp.X, sp.Y);
                    G.Audio.Play(Sfx.DoorOpen, 0.6f, 0.7f);
                    G.Fx.StaticFlicker = 0.3f;
                }
            }
            else if (Active == null && comingT < 0)
            {
                if (!p.ScriptBusy) cooldown -= dt;
                if (cooldown <= 0)
                {
                    // 다가오는 중: 출구 하나를 고른다
                    comingExit = PickExit();
                    if (comingExit != null) { comingT = 5.5f - Difficulty * 0.6f; }
                    else cooldown = 10f;
                }
            }
            else if (comingT >= 0)
            {
                float before = comingT;
                comingT -= dt;
                prox = Math.Max(prox, M.Clamp01(1f - comingT / 5f) * 0.5f);
                // 멀리서 가위 소리
                if ((int)(before * 1.3f) != (int)(comingT * 1.3f))
                {
                    G.Audio.Play(Sfx.Snip, 0.25f + 0.4f * (1 - comingT / 5f), 0.85f, ExitPan(comingExit));
                    G.Fx.StaticFlicker = 0.12f;
                }
                if (comingT <= 0)
                {
                    comingT = -1;
                    float ex, ey;
                    if (ExitPos(comingExit, out ex, out ey))
                    {
                        Appear(ex, ey);
                        G.Audio.Play(Sfx.DoorOpen, 0.7f, 0.65f);
                        G.Audio.Play(Sfx.StingSoft, 0.5f);
                    }
                    else cooldown = 10f;
                }
            }
            Proximity = M.Approach(Proximity, prox, dt * 2f);
            G.Audio.SetLoopLevel(Ch.Noise, Proximity > 0.5f ? (Proximity - 0.5f) * 0.25f : 0f, 0.4f);
        }

        ExitDef PickExit()
        {
            var room = p.Room;
            ExitDef best = null;
            float bestScore = -1;
            foreach (var kv in room.Def.Exits)
            {
                var e = kv.Value;
                if (e.Style == ExitStyle.Seam || e.Style == ExitStyle.Hidden) continue;
                float ex, ey;
                if (!ExitPos(e, out ex, out ey)) continue;
                float d = M.Dist(ex, ey, p.Player.X, p.Player.Y);
                float score = d + G.Rng.Range(0, 40);
                if (d < 24) continue;
                if (score > bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        bool ExitPos(ExitDef e, out float x, out float y)
        {
            var room = p.Room;
            // 출구 칸 옆의 바닥
            for (int cy = 0; cy < room.CH; cy++)
                for (int cx = 0; cx < room.CW; cx++)
                {
                    if (room.KeyAt(cx, cy) != e.Key) continue;
                    int[] ox = { 0, 0, -1, 1 }, oy = { 1, -1, 0, 0 };
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = cx + ox[k] * 2, ny = cy + oy[k] * 2;
                        if (room.IsFloorish(nx, ny) && room.IsFloorish(cx + ox[k], cy + oy[k]))
                        {
                            x = nx * Room.CS + 2; y = ny * Room.CS + 3;
                            return true;
                        }
                    }
                }
            x = y = 0;
            return false;
        }

        float ExitPan(ExitDef e)
        {
            float x, y;
            if (e == null || !ExitPos(e, out x, out y)) return 0;
            return M.Clamp((x - p.Player.X) / 60f, -0.8f, 0.8f);
        }
    }
}
