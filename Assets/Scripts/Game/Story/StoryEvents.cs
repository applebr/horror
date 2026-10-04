using System;
using System.Collections;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    public static partial class Story
    {
        // ═════════════════ 프롤로그 ═════════════════

        public static IEnumerator Prologue(GameCore g)
        {
            CurrentGame = g;
            var ps = PS(g);
            g.Fx.Fade = 1f;
            g.Fx.Vignette = 0.45f;
            var sp = ps.SpawnPoint("r_living", "start");
            ps.StartAt("r_living", sp.X, sp.Y);
            ps.Player.Dir = 3;
            ps.Player.Frozen = true;
            yield return g.Wait(0.8f);
            yield return g.PlayCutscene(new TitleCard("1998년 4월 4일", "밤 11시 44분", false));
            yield return g.FadeIn(1.4f);
            var mom = ps.Room.Find<Actor>("mom");
            if (mom == null) { ps.Player.Frozen = false; yield break; }
            mom.Dir = 2;
            yield return g.Wait(0.5f);
            yield return g.Say(Speaker.Mom, "하나야, 엄마 이제 나가 봐야 돼.");
            yield return g.Say(Speaker.Mom, "오늘도 밤새 일하고, 아침에 올게.");
            yield return g.Say(Speaker.Hana, "…또?");
            yield return g.Say(Speaker.Mom, "미안해. 대신 내일은 엄마랑 놀이터 가자.");
            yield return g.Say(Speaker.Mom, "문 꼭 잠그고, 누가 와도 열어 주면 안 돼. 알았지?");
            yield return g.Say(Speaker.Hana, "응.");
            yield return g.Say(Speaker.Mom, "그리고… 또 가위 눌리면 어떻게 하라고 했지?");
            yield return g.Say(Speaker.Hana, "가위? 자르는 가위?");
            yield return g.Say(Speaker.Mom, "아니. 자다가 눈은 떠지는데 몸이 안 움직이는 거.");
            yield return g.Say(Speaker.Mom, "옛날 사람들은 귀신이 꾹 누르는 거라고 했대.");
            yield return g.Say(Speaker.Mom, "그럴 땐 손가락을 세어 봐. 꿈속에서는 손가락 개수가 이상하대.");
            yield return g.Say(Speaker.Mom, "꿈인 걸 알면, 하나도 안 무서워. 다 하나 마음대로 할 수 있거든.");
            yield return g.Say(Speaker.Mom, "자, 지금 한번 세어 볼래?");
            g.Flags.Set("can_check");
            yield return WaitForCheck(g, "C 키를 눌러 손가락을 세어 보자.");
            g.Input.Consume(Btn.Check);
            g.Dialogue.Close();
            ps.Player.Counting = true;
            ps.Hand.Begin(5, false);
            while (ps.Hand.Active) yield return null;
            ps.Player.Counting = false;
            yield return g.Say(Speaker.Hana, "하나, 둘, 셋, 넷, 다섯. …다섯 개.");
            yield return g.Say(Speaker.Mom, "그렇지. 여긴 진짜야. 잘했어, 우리 딸.");
            yield return g.Say(Speaker.Mom, "엄마 간다. 잘 자.");
            // 엄마가 나간다
            mom.Dir = 0;
            yield return ps.Walk(mom, Cx(31), Cy(26.5f), 22f);
            g.Audio.Play(Sfx.DoorOpen, 0.6f, 0.9f);
            yield return g.Wait(0.3f);
            mom.Removed = true;
            var shoes = ps.Room.Find("momshoes"); if (shoes != null) shoes.Removed = true;
            var bag = ps.Room.Find("bag"); if (bag != null) bag.Removed = true;
            g.Audio.Play(Sfx.DoorClose, 0.8f);
            yield return g.Wait(0.5f);
            g.Audio.Play(Sfx.Lock, 0.7f);
            g.Flags.Set("mom_left");
            yield return g.Wait(1.6f);
            g.Audio.Play(Sfx.Ding, 0.4f);
            yield return g.Wait(1.0f);
            yield return g.Say(Speaker.Think, "……");
            yield return g.Say(Speaker.Think, "조용하다.");
            ps.SetCheckpoint("r_living", sp.X, sp.Y, false);
            ps.Hud.Objective("잘 준비를 하자: 이 닦기");
            ps.Player.Frozen = false;
        }

        /// <summary>C 키를 누를 때까지 안내를 띄워 둔다 (Z로 닫혀도 다시 연다).</summary>
        public static IEnumerator WaitForCheck(GameCore g, string prompt)
        {
            while (true)
            {
                if (g.Input.Pressed(Btn.Check)) break;
                if (!g.Dialogue.Active) g.Dialogue.Open(Speaker.Sys, prompt);
                yield return null;
            }
        }

        static IEnumerator BedReal(GameCore g)
        {
            var ps = PS(g);
            if (!g.Flags.Has("mom_left")) yield break;
            if (!g.Flags.Has("brushed"))
            {
                yield return g.Say(Speaker.Think, "이 닦고 자야지. 엄마가 그랬어.");
                yield break;
            }
            int choice = 1;
            yield return g.Ask("잘까?", new[] { "잔다", "아직" }, i => choice = i);
            if (choice != 0) yield break;
            yield return GoToSleep(g);
        }

        static IEnumerator GoToSleep(GameCore g)
        {
            var ps = PS(g);
            ps.Player.Frozen = true;
            yield return g.FadeOut(0.5f);
            ps.Player.Lying = true;
            ps.Player.X = 12; ps.Player.Y = 36;
            var head = new Prop("hana_bed", Sprites.HanaInBed, 12, 21) { Layer = 1 };
            ps.Spawn(head);
            yield return g.FadeIn(0.5f);
            yield return g.Say(Speaker.Hana, "…잘 자, 하나야.");
            g.Audio.Play(Sfx.Click, 0.7f);
            g.Flags.Set("lights_off");
            ps.Room.Ambient = 0.25f;
            foreach (var l in ps.Room.Lights) l.On = false;
            g.Audio.StopAllMusic(3f);
            yield return g.Wait(2.0f);
            yield return g.FadeOut(2.5f);
            yield return g.Wait(1.0f);
            yield return g.PlayCutscene(new ClockCutscene());
            yield return g.PlayCutscene(new ParalysisCutscene());
            yield return BeginDream(g);
        }

        static IEnumerator BeginDream(GameCore g)
        {
            var ps = PS(g);
            g.Flags.Set("dream");
            g.Fx.Reset();
            g.Fx.Fade = 1f;
            g.Fx.Vignette = 0.6f;
            ps.Player.Lying = true;
            ps.LoadRoom("d_hana", 12, 36, 0);
            ps.Player.Frozen = true;
            var head = new Prop("hana_bed", Sprites.HanaInBed, 12, 21) { Layer = 1 };
            ps.Spawn(head);
            var bedSpawn = ps.SpawnPoint("d_hana", "bed");
            ps.SetCheckpoint("d_hana", bedSpawn.X, bedSpawn.Y, true);
            yield return g.Wait(1.5f);
            g.Audio.Play(Sfx.Gasp, 0.7f);
            yield return g.FadeIn(2.5f);
            yield return g.Wait(0.8f);
            yield return g.Say(Speaker.Hana, "……엄마?");
            yield return g.Wait(1.2f);
            yield return g.Say(Speaker.Think, "꿈… 이었나?");
            head.Removed = true;
            ps.Player.Lying = false;
            ps.Player.X = bedSpawn.X; ps.Player.Y = bedSpawn.Y; ps.Player.Dir = 0;
            g.Audio.Play(Sfx.Step, 0.3f);
            yield return g.Wait(0.4f);
            yield return g.Say(Speaker.Think, "시계가 4시 44분에 멈춰 있다.");
            ps.Player.Frozen = false;
        }

        /// <summary>이어하기 직후.</summary>
        public static IEnumerator OnContinue(GameCore g)
        {
            CurrentGame = g;
            var ps = PS(g);
            ps.Player.HasLight = g.Flags.Has("has_light");
            g.Fx.Fade = 1f;
            g.Fx.Vignette = g.Flags.Has("dream") ? 0.6f : 0.45f;
            yield return g.Wait(0.3f);
            yield return g.FadeIn(1.2f);
            string obj = Objective(g);
            if (obj != null) ps.Hud.Objective(obj);
        }

        // ═════════════════ 자각 ═════════════════

        public static IEnumerator AfterFirstCheck(GameCore g)
        {
            var ps = PS(g);
            ps.Player.Frozen = true;
            yield return g.Say(Speaker.Hana, "여섯… 여섯 개야.");
            yield return g.Say(Speaker.Think, "이건… 꿈이야.");
            g.Audio.Play(Sfx.StingSoft, 0.6f);
            yield return g.Wait(0.5f);
            yield return g.Say(Speaker.Think, "벽에… 글씨가.");
            yield return g.Say(Speaker.Think, "꿈인 걸 알면… 하나도 안 무서워. 엄마가 그랬어.");
            yield return g.Say(Speaker.Sys, "자각한 동안에는 숨겨진 것이 보인다. 자각은 시간이 지나면 흐려진다. (왼쪽 위 눈)");
            if (g.Flags.Has("has_light"))
                yield return g.Say(Speaker.Sys, "자각한 채로 비추는 손전등은 악몽을 밀어낸다.");
            else
                yield return g.Say(Speaker.Think, "책상 위에… 손전등이 있었지.");
            ps.Player.Frozen = false;
        }

        /// <summary>현실에서 손가락을 셌을 때. true면 기본 대사를 생략.</summary>
        public static bool OnRealityCheckInReality(GameCore g)
        {
            if (g.Flags.Has("ending")) return true;
            return false;
        }

        public static void BeforeRespawn(GameCore g)
        {
            var ps = PS(g);
            ps.Player.HasLight = g.Flags.Has("has_light");
            ps.Player.Lying = false;
            ps.Player.RechargeMul = 1f;
            ps.LucidMax = 26f;
            g.Audio.StopMusic(Ch.Layer, 0.5f);
            g.Flags.Clear("chase_scripted");
            g.Fx.Vignette = 0.6f;
            if (g.Flags.Has("boss") && !g.Flags.Has("boss_done")) g.Flags.Clear("boss");
        }
    }

    /// <summary>욕실의 핏물 욕조.</summary>
    public sealed class BathtubProp : Prop
    {
        public float Level = 1f;
        float t;

        public BathtubProp(float x, float y) : base("tub", Sprites.Bathtub, x, y)
        {
            SetBox(-7, -19, 7, 1);
            Solid = true;
            OnInteract = Interact;
            IRange = 7;
        }

        public override void Added()
        {
            if (G.Flags.Has("bath_drained")) Level = 0f;
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            t += dt;
        }

        IEnumerator Interact()
        {
            var g = G;
            if (Level <= 0.01f)
            {
                yield return g.Say(Speaker.Think, g.Flags.Has("piece_bath") ? "욕조 바닥에 손가락 몇 개가 아직 꿈틀거린다." : "…");
                yield break;
            }
            if (!P.Lucid)
            {
                yield return g.Say(Speaker.Think, "욕조에 빨간 물이 가득하다. 바닥이 보이지 않는다.");
                yield return g.Say(Speaker.Think, "…비린내가 난다.");
                if (g.Flags.Has("first_check") && g.Flags.Inc("tub_looks") >= 2)
                    yield return g.Say(Speaker.Think, "자각하면… 뭔가 보일지도 몰라.");
                yield break;
            }
            yield return g.Say(Speaker.Think, "핏물 속에… 마개 줄이 보인다.");
            int choice = 1;
            yield return g.Ask("줄을 당길까?", new[] { "당긴다", "그만둔다" }, i => choice = i);
            if (choice != 0) yield break;
            yield return Drain();
        }

        IEnumerator Drain()
        {
            var g = G;
            var ps = P;
            ps.Player.Frozen = true;
            g.Audio.Play(Sfx.Gurgle, 0.9f);
            yield return new Tween(3.2f, k => Level = 1f - k);
            g.Flags.Set("bath_drained");
            yield return g.Wait(0.4f);
            yield return g.Say(Speaker.Think, "바닥에… 손가락이… 잔뜩.");
            yield return g.Say(Speaker.Think, "그 사이에 사진 조각이 끼어 있다.");
            yield return Story.TakeBathPiece(g);
            yield return g.Wait(0.3f);
            g.Audio.Play(Sfx.Skitter, 0.8f);
            yield return g.Wait(0.4f);
            g.Audio.Play(Sfx.Skitter, 0.9f, 0.8f);
            yield return g.Say(Speaker.Think, "…움직여!");
            for (int i = 0; i < 9; i++)
            {
                float cy = 15 + (i % 5) * 4;
                ps.Spawn(new Crawler(23 + (i / 5) * 3, cy, i * 0.25f));
            }
            g.Audio.SetMusic(Ch.Layer, Music.Chase, 0.55f, 0.3f);
            ps.RunAmbient(Story.WatchCrawlers(g));
            ps.Player.Frozen = false;
            yield return g.Say(Speaker.Sys, "손전등(X)으로 비추면 손가락이 타 버린다!");
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            int px = ox + (int)X, py = oy + (int)Y;
            c.Blit(Sprites.Bathtub, px, py);
            // 안쪽 영역: x-5..x+4, y-17..y-4
            int x0 = px - 5, x1 = px + 4, y0 = py - 17, y1 = py - 4;
            int h = y1 - y0 + 1;
            int top = y0 + (int)Math.Round((1 - Level) * h);
            c.FillRect(x0, y0, x1 - x0 + 1, h, Pal.LightGray);
            if (Level < 0.35f)
            {
                c.Blit(Sprites.FingerPile, px, y1);
                c.Blit(Sprites.FingerPile, px - 1, y1 - 5);
                if (!G.Flags.Has("piece_bath") && ((int)(t * 3)) % 3 == 0) c.Set(px + 2, y1 - 3, Pal.White);
            }
            for (int y = top; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float rip = M.Sin(t * 2 + x * 0.9f + y * 0.4f);
                    byte col = y == top ? Pal.Red : (rip > 0.75f ? Pal.Red : Pal.DarkRed);
                    c.Set(x, y, col);
                }
            }
            if (P.Lucid && Level > 0.5f)
            {
                // 마개 줄 (자각 상태에서만 보임)
                int cx = px + 3;
                for (int y = py - 18; y < py - 9; y++) c.Set(cx + ((y & 1) == 0 ? 0 : 1), y, Pal.LightGray);
                c.Set(cx, py - 19, Pal.White);
            }
        }
    }

    public static partial class Story
    {
        public static IEnumerator TakeBathPiece(GameCore g)
        {
            yield return GetPiece(g, "piece_bath");
        }

        public static IEnumerator WatchCrawlers(GameCore g)
        {
            var ps = PS(g);
            float t = 0;
            while (!ps.Dying)
            {
                t += GameCore.Dt;
                bool any = false;
                foreach (var e in ps.Room.Entities) if (e is Crawler && !e.Removed) { any = true; break; }
                if (!any || ps.Room.Def.Id != "d_bath") break;
                yield return null;
            }
            g.Audio.StopMusic(Ch.Layer, 2f);
            if (!ps.Dying && ps.Room.Def.Id == "d_bath" && g.Flags.Get("fingers_burned") >= 5)
                yield return null;
        }
    }
}
