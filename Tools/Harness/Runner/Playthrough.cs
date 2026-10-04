using System;
using System.Diagnostics;
using Gawi.Engine;
using Gawi.Engine.Audio;
using Gawi.Game;

namespace Gawi.Harness
{
    public static partial class Program
    {
        /// <summary>
        /// 처음부터 크레딧까지 실제 입력으로 자동 진행한다.
        /// god=true면 죽지 않음 (진행 로직 검사), false면 실제 난이도로 시도.
        /// </summary>
        static int Playthrough(bool shots, bool hard)
        {
            var sw = Stopwatch.StartNew();
            var s = NewSim("play");
            s.Verbose = true;
            int step = 0;
            Action<string> log = m => Console.WriteLine(string.Format("[{0,6:0.0}s game {1,6:0.0}s] {2}", sw.Elapsed.TotalSeconds, s.Frames / 60f, m));
            Action<string> shot = n => { if (shots) s.Shot(string.Format("{0:00}_{1}", step++, n)); };

            // 경고 → 타이틀 → 새 게임
            s.Seconds(1.5f);
            s.Press(Btn.Confirm);
            s.Until(() => s.G.State is TitleState, 30, "title");
            s.Seconds(2.5f);
            shot("title");
            s.Press(Btn.Confirm);
            s.Until(() => s.G.State is PlayState, 10, "play state");
            if (!hard) s.G.Flags.Set("god");
            log(hard ? "새 게임 (죽을 수 있음)" : "새 게임 (무적)");

            // 프롤로그: 엄마 대사 → 손가락 세기 튜토리얼
            s.AdvanceUntil(() => s.G.Dialogue.Active && s.G.Flags.Has("can_check"), 120, "C 안내");
            shot("prologue_c");
            s.Seconds(0.5f);
            s.Press(Btn.Check);
            s.AdvanceUntil(() => s.G.Flags.Has("mom_left") && !s.P.Player.Frozen && !s.G.Dialogue.Active, 120, "엄마 퇴장");
            shot("alone");
            log("엄마가 나감");

            // 이 닦기
            s.ExitVia('1');                     // 거실 → 복도
            s.ExitVia('2');                     // 복도 → 욕실
            s.InteractWith("sink");
            s.AdvanceUntil(() => s.G.Flags.Has("brushed") && !s.P.Player.Frozen, 30, "양치");
            log("이 닦음");
            s.ExitVia('1');                     // 욕실 → 복도
            s.ExitVia('3');                     // 복도 → 하나 방
            s.InteractWith("bed", 0);
            log("잠들기");
            // 시계 + 가위눌림 컷신: C 안내가 나오면 누른다
            int guard = 0;
            while (!(s.P.Room.Def.Id == "d_hana" && !s.P.Player.Frozen && s.G.Cutscene == null) && guard++ < 60 * 200)
            {
                if (s.G.Cutscene is ParalysisCutscene && s.G.Dialogue.Active)
                {
                    s.Press(Btn.Check);
                }
                if (s.G.Dialogue.Active) s.Advance();
                else s.Tick();
                if (shots && guard % 600 == 0) shot("sleep_" + guard);
            }
            if (s.P.Room.Def.Id != "d_hana") throw new Exception("꿈이 시작되지 않음");
            shot("dream_wake");
            log("꿈 시작");

            // 손전등 + 첫 손가락 세기
            s.InteractWith("flashlight");
            if (!s.G.Flags.Has("has_light")) throw new Exception("손전등 못 주움");
            s.Press(Btn.Check);
            s.AdvanceUntil(() => s.G.Flags.Has("first_check") && s.G.Cutscene == null && !s.P.Player.Frozen && !s.P.ScriptBusy, 60, "첫 손가락 세기");
            shot("lucid_first");
            log("자각");
            s.ExitVia('1');                     // 하나 방 → 복도
            s.AdvanceUntil(() => s.G.Flags.Has("hall_kid_seen") || s.P.Player.X > 70, 10, "복도");
            // 복도 이벤트 영역으로 걸어간다
            s.WalkTo(Cx(18), Cy(7));
            s.AdvanceUntil(() => s.G.Flags.Has("hall_kid_seen") && !s.P.Player.Frozen, 20, "복도의 아이");
            log("복도의 얼굴 없는 아이");

            // 거실: 빈 액자
            s.ExitVia('4');
            s.InteractWith("frame");
            s.AdvanceUntil(() => !s.G.Dialogue.Active, 10, "액자");
            if (!s.G.Flags.Has("saw_frame")) throw new Exception("액자 확인 실패");
            shot("living_frame");
            log("빈 액자 확인");

            // 1) 욕실
            s.ExitVia('1');                     // 거실 → 복도
            s.ExitVia('2');                     // 복도 → 욕실
            s.Advance();
            BecomeLucid(s);
            s.InteractWith("tub", 0);
            s.AdvanceUntil(() => s.G.Flags.Has("piece_bath") && !s.P.Player.Frozen && !s.P.ScriptBusy, 40, "욕조");
            shot("bath_fingers");
            log("조각 1 (욕실), 손가락 떼");
            // 손가락을 손전등으로 태워 본다
            BurnCrawlers(s, 8f);
            log("태운 손가락: " + s.G.Flags.Get("fingers_burned"));
            s.ExitVia('1');
            s.ExitVia('4');                     // 복도 → 거실

            // 2) 부엌 (칼질 소리에 맞춰 잠입)
            s.ExitVia('2');                     // 거실 → 부엌
            s.AdvanceUntil(() => s.G.Flags.Has("kitchen_seen") && !s.P.Player.Frozen, 20, "부엌 입장");
            shot("kitchen");
            var mother = s.P.Room.Find<MotherThing>("mother");
            if (mother == null) throw new Exception("엄마인 척하는 것이 없음");
            bool wasGod = s.G.Flags.Has("god");
            s.G.Flags.Clear("god");
            Sneak(s, ref mother, 96, 50);
            log("sneak1 deaths=" + s.G.Flags.Get("deaths") + " room=" + s.P.Room.Def.Id + " pos=" + s.P.Player.X + "," + s.P.Player.Y);
            Sneak(s, ref mother, 30, 50);
            log("sneak2 deaths=" + s.G.Flags.Get("deaths") + " room=" + s.P.Room.Def.Id + " pos=" + s.P.Player.X + "," + s.P.Player.Y);
            Sneak(s, ref mother, 14, 40);
            log("sneak3 deaths=" + s.G.Flags.Get("deaths") + " room=" + s.P.Room.Def.Id + " pos=" + s.P.Player.X + "," + s.P.Player.Y + " mstate=" + mother.State);
            // 칼질 중일 때 위를 보고 조사
            for (int i = 0; i < 600 && !s.G.Flags.Has("piece_kitchen"); i++)
            {
                if (mother.State == MotherThing.St.Chop && !s.G.Dialogue.Active)
                {
                    s.Set(Btn.Up, true); s.Tick(1); s.Set(Btn.Up, false);
                    s.Press(Btn.Confirm);
                    s.Tick(10);
                    break;
                }
                s.Tick();
            }
            if (wasGod) s.G.Flags.Set("god");
            s.AdvanceUntil(() => s.G.Flags.Has("piece_kitchen") && !s.P.Player.Frozen && !s.G.Dialogue.Active, 30, "부엌 조각");
            shot("kitchen_hunt");
            log("조각 2 (부엌), 사냥 시작 (잠입 중 사망 " + s.G.Flags.Get("deaths") + "회)");
            s.ExitVia('1');

            // 3) 끝없는 복도 → 안방
            s.ExitVia('1');                     // 거실 → 복도
            s.ExitVia('5');                     // 복도 → 끝없는 복도
            s.ExitVia('5');                     // 한 바퀴
            s.ExitVia('5');                     // 두 바퀴 (쪽지 등장)
            log("복도 반복: " + s.G.Flags.Get("loops"));
            BecomeLucid(s);
            s.ExitVia('7');                     // 절취선 → 안방
            s.AdvanceUntil(() => s.G.Flags.Has("mom_seen") && !s.P.Player.Frozen, 20, "안방 입장");
            shot("mom_room");
            WalkDefensive(s, 46, 31, 40, 16);
            s.InteractWith("jewel");
            s.AdvanceUntil(() => s.G.Flags.Has("piece_mom") && !s.G.Dialogue.Active && !s.P.ScriptBusy, 30, "안방 조각");
            shot("gawi_appears");
            log("조각 3 (안방), 가위 등장 (사망 " + s.G.Flags.Get("deaths") + ")");
            // 자각하고 손전등으로 쫓아낸다
            RepelGawi(s, 30);
            log("가위를 쫓아냄? 남은 가위=" + (s.P.Stalker.Active != null) + " 사망 " + s.G.Flags.Get("deaths"));
            if (s.P.Room.Def.Id == "d_mom") s.ExitVia('1');                     // 안방 → 복도
            s.ExitVia('4');                     // 복도 → 거실

            // 사진 맞추기 → TV
            s.InteractWith("frame");
            s.AdvanceUntil(() => s.G.Flags.Has("photo_done") && s.G.Cutscene == null && !s.P.Player.Frozen && !s.G.Dialogue.Active, 120, "사진/TV");
            shot("photo_done");
            log("사진 완성, 현관 열림");

            // 계단 추격 (죽으면 다시)
            s.ExitVia('3');
            for (int guardStairs = 0; guardStairs < 40; guardStairs++)
            {
                if (s.P.Room.Def.Id == "d_cut") break;
                if (s.P.Dying || s.P.Player.Frozen || (s.G.Dialogue.Active && !s.G.Dialogue.IsTimed)) { s.AdvanceOne(); s.Tick(); continue; }
                if (s.P.Room.Def.Id.StartsWith("d_stairs"))
                {
                    shot(s.P.Room.Def.Id);
                    s.ExitVia('1');
                }
                else s.Tick();
            }
            if (s.P.Room.Def.Id != "d_cut") throw new Exception("계단을 통과하지 못함");
            log("계단 통과 (사망 " + s.G.Flags.Get("deaths") + ")");

            // 보스
            s.AdvanceUntil(() => s.G.Flags.Has("boss") && !s.P.Player.Frozen && !s.G.Dialogue.Active, 60, "보스 시작");
            shot("boss");
            FightBoss(s, shot);
            log("보스 격파");
            s.AdvanceUntil(() => s.P.Room != null && s.P.Room.Def.Id == "m_hana" && !s.P.Player.Frozen && !s.G.Dialogue.Active, 120, "엔딩 시작");
            shot("morning");
            log("아침");

            // 엔딩: 부엌으로
            s.ExitVia('1');                     // 하나 방 → 복도
            s.ExitVia('4');                     // 복도 → 거실
            s.ExitVia('2');                     // 거실 → 부엌
            s.WalkTo(84, 52);                   // 엄마 쪽으로
            guard = 0;
            while (!(s.G.State is CreditsState) && guard++ < 60 * 200)
            {
                if (s.G.Dialogue.Active)
                {
                    if (s.G.Flags.Has("end_mom_talked")) s.Press(Btn.Check);
                    s.Advance();
                }
                else s.Tick();
                if (shots && guard % 300 == 0) shot("ending_" + guard);
            }
            if (!(s.G.State is CreditsState)) throw new Exception("크레딧에 도달하지 못함");
            log("크레딧");
            s.Seconds(5);
            shot("credits");
            s.Set(Btn.Confirm, true);
            s.Until(() => s.G.State is TitleState, 120, "타이틀 복귀");
            s.ReleaseAll();
            log(string.Format("완료! 게임 시간 {0:0}분 {1:0}초, 사망 {2}회, 효과음 {3}회", s.Frames / 3600, (s.Frames / 60) % 60, s.G.Flags.Get("deaths"), s.Audio.OneShots));
            var hist = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var n in s.Audio.Log) { int c; hist.TryGetValue(n, out c); hist[n] = c + 1; }
            var keys = new System.Collections.Generic.List<string>(hist.Keys);
            keys.Sort((a, b) => hist[b].CompareTo(hist[a]));
            Console.WriteLine("  소리: " + string.Join(", ", keys.ConvertAll(k => k.Replace("sfx_", "") + "×" + hist[k]).ToArray()));
            return 0;
        }

        /// <summary>목표 쪽으로 8방향 조준 (방향키를 한 틱 눌렀다 뗀다). 손전등은 그대로.</summary>
        static void Aim(Sim s, float dx, float dy)
        {
            var pl = s.P.Player;
            float a = (float)Math.Atan2(dy, dx);
            int oct = (int)Math.Round(a / (Math.PI / 4));
            float ax = (float)Math.Cos(oct * Math.PI / 4), ay = (float)Math.Sin(oct * Math.PI / 4);
            for (int b = 0; b < 4; b++) s.Set((Btn)b, false);
            if (pl.Moving) return; // 멈춘 뒤에 돌아본다
            if (Math.Abs(pl.AimX - ax) > 0.2f || Math.Abs(pl.AimY - ay) > 0.2f)
            {
                if (ax < -0.3f) s.Set(Btn.Left, true);
                if (ax > 0.3f) s.Set(Btn.Right, true);
                if (ay < -0.3f) s.Set(Btn.Up, true);
                if (ay > 0.3f) s.Set(Btn.Down, true);
            }
        }

        static float Cx(float cx) { return cx * Room.CS + Room.CS * 0.5f; }
        static float Cy(float cy) { return cy * Room.CS + Room.CS - 0.5f; }

        static void BecomeLucid(Sim s)
        {
            if (s.P.Lucid && s.P.LucidT > 15) return;
            s.Press(Btn.Check);
            s.AdvanceUntil(() => s.P.Lucid && !s.P.Player.Counting && !s.P.ScriptBusy, 20, "자각");
        }

        static void BurnCrawlers(Sim s, float seconds)
        {
            int max = (int)(seconds * 60);
            s.ReleaseAll();
            for (int i = 0; i < max; i++)
            {
                Crawler target = null;
                float best = 999;
                foreach (var e in s.P.Room.Entities)
                {
                    var c = e as Crawler;
                    if (c == null || c.Removed || c.Delay > 0) continue;
                    float d = M.Dist(c.X, c.Y, s.P.Player.X, s.P.Player.Y);
                    if (d < best) { best = d; target = c; }
                }
                if (target == null) break;
                float dx = target.X - s.P.Player.X, dy = target.Y - s.P.Player.ChestY;
                Aim(s, dx, dy);
                s.Set(Btn.Light, s.P.Player.Battery > 0.02f);
                s.Tick();
            }
            s.ReleaseAll();
            s.Tick();
        }

        /// <summary>칼질 소리가 날 때만 움직인다.</summary>
        static void Sneak(Sim s, ref MotherThing m, float tx, float ty)
        {
            for (int i = 0; i < 60 * 90; i++)
            {
                if (s.P.Dying)
                {
                    s.ReleaseAll();
                    s.AdvanceUntil(() => !s.P.Dying && s.P.Room.Def.Id == "d_living", 40, "부엌 사망 후 부활");
                    s.ExitVia('2');
                    m = s.P.Room.Find<MotherThing>("mother");
                    continue;
                }
                if (s.G.Dialogue.Active) { s.Advance(); continue; }
                var pl = s.P.Player;
                if (Math.Abs(pl.X - tx) < 2.5f && Math.Abs(pl.Y - ty) < 2.5f) { s.ReleaseAll(); return; }
                if (m.State != MotherThing.St.Chop)
                {
                    s.ReleaseAll();
                    s.Tick();
                    continue;
                }
                // 한 걸음씩 걷기 (WalkTo를 아주 짧게)
                s.WalkTo(tx, ty, 0.08f, 2.5f);
            }
            throw new Exception("부엌 잠입 실패");
        }

        /// <summary>가까운 괴물에게 손전등을 비추며 목표로 걸어간다.</summary>
        static bool WalkDefensive(Sim s, float tx, float ty, float maxSeconds, float kidRange = 34)
        {
            for (int i = 0; i < maxSeconds * 60; i++)
            {
                if (s.P.Dying) return false;
                if (s.G.Dialogue.Active && !s.G.Dialogue.IsTimed) { s.ReleaseAll(); s.AdvanceOne(); continue; }
                var pl = s.P.Player;
                if (Math.Abs(pl.X - tx) < 2.5f && Math.Abs(pl.Y - ty) < 2.5f) { s.ReleaseAll(); return true; }
                Entity threat = null;
                float best = 999;
                foreach (var e in s.P.Room.Entities)
                {
                    if (e.Removed) continue;
                    float range = e is FacelessKid ? kidRange : (e is Crawler ? 26 : 0);
                    if (range <= 0) continue;
                    float d = M.Dist(e.X, e.Y, pl.X, pl.Y);
                    if (d < range && d < best) { best = d; threat = e; }
                }
                if (threat != null && pl.Battery > 0.1f)
                {
                    float dx = threat.X - pl.X, dy = (threat.Y - 6) - pl.ChestY;
                    Aim(s, dx, dy);
                    s.Set(Btn.Light, true);
                    s.Tick();
                    continue;
                }
                s.Set(Btn.Light, false);
                s.WalkTo(tx, ty, 0.1f, 2.5f);
            }
            s.ReleaseAll();
            return false;
        }

        static void RepelGawi(Sim s, float maxSeconds)
        {
            for (int i = 0; i < maxSeconds * 60; i++)
            {
                if (s.P.Dying) { s.ReleaseAll(); s.AdvanceUntil(() => !s.P.Dying, 30, "부활"); return; }
                if (s.G.Dialogue.Active && !s.G.Dialogue.IsTimed) { s.ReleaseAll(); s.AdvanceOne(); continue; }
                var g = s.P.Stalker.Active;
                if (g == null || g.Removed) { s.ReleaseAll(); return; }
                if (!s.P.Lucid) { s.ReleaseAll(); s.Press(Btn.Check); s.Seconds(0.2f); continue; }
                var pl = s.P.Player;
                float dx = g.X - pl.X, dy = (g.Y - 10) - pl.ChestY;
                Aim(s, dx, dy);
                s.Set(Btn.Light, pl.Battery > 0.02f);
                s.Tick();
            }
            s.ReleaseAll();
        }

        static void FightBoss(Sim s, Action<string> shot)
        {
            int frames = 0;
            int lastShot = 0;
            while (!s.G.Flags.Has("boss_done") && frames < 60 * 240)
            {
                frames++;
                if (s.G.Dialogue.Active && !s.G.Dialogue.IsTimed) { s.ReleaseAll(); s.Advance(); continue; }
                if (s.P.Dying) { s.ReleaseAll(); s.Tick(); continue; }
                ScissorMan boss = null;
                foreach (var e in s.P.Room.Entities) if (e is ScissorMan && !e.Removed) boss = (ScissorMan)e;
                if (boss == null) { s.ReleaseAll(); s.Tick(); continue; }
                var pl = s.P.Player;
                if (!s.P.Lucid || s.P.LucidT < 1.0f)
                {
                    // 멀리 도망친 뒤에 손가락을 센다
                    float bd = M.Dist(boss.X, boss.Y, pl.X, pl.Y);
                    if (bd < 70 && boss.State != ScissorMan.St.Stunned)
                    {
                        s.ReleaseAll();
                        float ax = pl.X - boss.X, ay = pl.Y - boss.Y;
                        float tx = M.Clamp(pl.X + ax * 2, 10, 150), ty = M.Clamp(pl.Y + ay * 2, 20, 110);
                        s.WalkTo(tx, ty, 0.15f, 3f);
                        continue;
                    }
                    s.ReleaseAll();
                    s.Press(Btn.Check);
                    s.Seconds(0.3f);
                    while (s.P.Player.Counting && !s.P.Dying) s.Tick();
                    continue;
                }
                float dx = boss.X - pl.X, dy = (boss.Y - 10) - pl.ChestY;
                float dist = M.Dist(boss.X, boss.Y, pl.X, pl.Y);
                // 사람처럼: 가까이 올 때까지 배터리를 아끼고, 배터리가 없으면 도망
                if (pl.Battery < 0.3f && !pl.LightOn && dist < 60)
                {
                    s.ReleaseAll();
                    float ax = pl.X - boss.X, ay = pl.Y - boss.Y;
                    s.WalkTo(M.Clamp(pl.X + ax * 2, 10, 150), M.Clamp(pl.Y + ay * 2, 20, 110), 0.15f, 3f);
                    continue;
                }
                Aim(s, dx, dy);
                s.Set(Btn.Light, pl.Battery > 0.02f && (dist < 58 || boss.State == ScissorMan.St.Recoil));
                s.Tick();
                if (frames - lastShot > 600) { lastShot = frames; shot("boss_fight"); }
            }
            s.ReleaseAll();
            if (!s.G.Flags.Has("boss_done")) throw new Exception("보스를 이기지 못함");
        }

        static int Fuzz(int seconds)
        {
            var s = NewSim("fuzz");
            var rng = new Random(1234);
            string[] rooms = { "d_hall", "d_living", "d_bath", "d_kitchen", "d_mom", "d_loop", "d_stairs2", "d_cut", "r_living", "m_hana" };
            int total = 0;
            foreach (var room in rooms)
            {
                Warp(s, room, room == "d_stairs2" ? "top" : (room == "d_cut" ? "top" : Rooms.Get(room).Spawns.Keys.GetEnumerator().Current ?? "center"), "dream", "has_light", "first_check", "stalker_on", "can_check");
                if (s.P.Room.Def.Spawns.Count > 0)
                {
                    foreach (var kv in s.P.Room.Def.Spawns) { s.P.GoTo(room, kv.Key); break; }
                }
                s.P.Player.HasLight = true;
                for (int f = 0; f < seconds * 60; f++)
                {
                    if (f % 12 == 0)
                    {
                        s.ReleaseAll();
                        int b = rng.Next(9);
                        s.Set((Btn)b, rng.NextDouble() < 0.7);
                        if (rng.NextDouble() < 0.3) s.Set((Btn)rng.Next(4), true);
                    }
                    s.Tick();
                    total++;
                    if (s.G.Dialogue.Active && rng.NextDouble() < 0.2) s.Press(Btn.Confirm);
                    if (s.G.TopOverlay != null) { s.Press(Btn.Cancel); }
                    if (!(s.G.State is PlayState)) break;
                }
                Console.WriteLine("fuzz " + room + " ok, deaths=" + s.G.Flags.Get("deaths") + " now in " + (s.P != null && s.P.Room != null ? s.P.Room.Def.Id : "-"));
            }
            Console.WriteLine("fuzz ok, frames=" + total);
            return 0;
        }
    }
}
