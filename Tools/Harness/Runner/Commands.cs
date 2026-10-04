using System;
using System.Collections;
using System.IO;
using Gawi.Engine;
using Gawi.Engine.Audio;
using Gawi.Game;

namespace Gawi.Harness
{
    public static partial class Program
    {
        static int RunCommand(string cmd, string[] args)
        {
            switch (cmd)
            {
                case "shots": return Shots();
                case "rooms": return RoomShots();
                case "cutscenes": return CutsceneShots(args.Length > 1 ? args[1] : null);
                case "play": return Playthrough(Array.IndexOf(args, "shots") >= 0, Array.IndexOf(args, "hard") >= 0);
                case "momtest": return MomTest();
                case "bosstest": return BossTest();
                case "fuzz": return Fuzz(args.Length > 1 ? int.Parse(args[1]) : 60);
            }
            Console.WriteLine("usage: engine | audio [wav] | shots | rooms | cutscenes [name] | play [shots] | fuzz [seconds]");
            return 1;
        }

        static Sim NewSim(string sub)
        {
            return new Sim(Path.Combine(Out, sub));
        }

        /// <summary>새 게임 시작 직후 스토리 스크립트를 멈추고 원하는 방으로 이동.</summary>
        static void Warp(Sim s, string room, string spawn, params string[] flags)
        {
            if (!(s.G.State is PlayState))
            {
                s.G.NewGame();
                s.Tick(2);
            }
            s.G.Scripts.StopTag("story");
            s.G.Scripts.StopTag("cutscene");
            s.G.Cutscene = null;
            s.G.Dialogue.Close();
            s.G.Fx.Reset();
            s.G.Fx.Vignette = 0.6f;
            foreach (var f in flags) s.G.Flags.Set(f);
            s.P.Player.HasLight = s.G.Flags.Has("has_light");
            s.P.Player.Frozen = false;
            s.P.Player.Lying = false;
            s.P.GoTo(room, spawn);
            s.Tick(10);
        }

        static int Shots()
        {
            var s = NewSim("shots");
            s.Tick(80);
            s.Shot("00_warning");
            s.Press(Btn.Confirm);
            s.Until(() => s.G.State is TitleState, 30, "title");
            s.Seconds(2.5f);
            s.Shot("01_title");
            s.Press(Btn.Confirm);
            s.Until(() => s.G.State is PlayState, 10, "play");
            s.Seconds(1.5f);
            s.Shot("02_titlecard");
            s.Until(() => s.G.Dialogue.Active, 15, "mom dialogue");
            s.Seconds(1.5f);
            s.Shot("03_mom_talk");
            Console.WriteLine("shots ok");
            return 0;
        }

        static int RoomShots()
        {
            var s = NewSim("rooms");
            s.Tick(5);
            string[] all =
            {
                "r_living:center", "r_hall:from_living", "r_hana:center", "r_bath:door", "r_kitchen:center", "r_mom:door",
                "d_hana:center", "d_hall:from_living", "d_living:center", "d_bath:door", "d_kitchen:from_living", "d_mom:door",
                "d_loop:from_west", "d_stairs1:top", "d_cut:top", "m_hana:center", "m_kitchen:center",
            };
            foreach (var a in all)
            {
                var parts = a.Split(':');
                Warp(s, parts[0], parts[1], "mom_left", "has_light", "first_check", "hall_kid_seen", "kitchen_seen", "bath_seen", "mom_seen", "saw_frame", "boss_done", "end_mom_talked", "dream");
                s.G.Flags.Put("loops", 3);
                s.P.GoTo(parts[0], parts[1]);
                s.Tick(20);
                s.Shot(parts[0]);
                if (parts[0].StartsWith("d_"))
                {
                    s.P.LucidT = 10;
                    s.P.Player.LightOn = true;
                    s.Set(Btn.Light, true);
                    s.Tick(5);
                    s.Shot(parts[0] + "_lucid_light");
                    s.Set(Btn.Light, false);
                    s.P.LucidT = 0;
                    s.Tick(2);
                }
            }
            Console.WriteLine("rooms ok");
            return 0;
        }

        static int CutsceneShots(string only)
        {
            var s = NewSim("cutscenes");
            s.Tick(5);
            Warp(s, "d_hana", "center", "dream");
            Func<Cutscene>[] makers =
            {
                () => new ClockCutscene(),
                () => new ParalysisCutscene(),
                () => new FingerCutscene(6, FingerCutscene.Mode.FirstDream),
                () => new FingerCutscene(5, FingerCutscene.Mode.Ending),
                () => new DeathCutscene(DeathKind.Scissors),
                () => new DeathCutscene(DeathKind.Fingers),
                () => new DeathCutscene(DeathKind.Mother),
                () => new DeathCutscene(DeathKind.Kids),
                () => new PieceCutscene("piece_mom"),
                () => new PhotoCutscene(),
                () => new TvCutscene(),
                () => new BossDeathCutscene(),
                () => new CutDreamCutscene(),
                () => new EndingPhotoCutscene(),
                () => new TitleCard("가 위", "1장 · 404호 — 끝", true),
            };
            int idx = 0;
            foreach (var mk in makers)
            {
                var cs = mk();
                string name = cs.GetType().Name + "_" + (idx++).ToString("00");
                if (only != null && !name.StartsWith(only)) continue;
                s.G.Fx.Reset();
                var h = s.G.Scripts.Start(s.G.PlayCutscene(cs), "test");
                int frame = 0;
                int shot = 0;
                int maxFrames = 60 * 90;
                int every = 45;
                while (!h.Done && frame < maxFrames)
                {
                    if (s.G.Dialogue.Active)
                    {
                        // 대사는 다 나온 다음 한 장 찍고 넘긴다
                        s.Tick(40);
                        s.Shot(string.Format("{0}_{1:00}_say", name, shot++));
                        s.Press(Btn.Confirm);
                        frame += 44;
                        continue;
                    }
                    if (frame % every == 0) s.Shot(string.Format("{0}_{1:00}", name, shot++));
                    if (cs is ParalysisCutscene || cs is FingerCutscene) { if (frame % 20 == 0) s.Set(Btn.Check, frame % 40 == 0); }
                    s.Tick();
                    frame++;
                }
                s.ReleaseAll();
                if (!h.Done) throw new Exception("cutscene did not finish: " + name);
                Console.WriteLine(name + " frames=" + frame + " shots=" + shot);
                s.Tick(10);
            }
            Console.WriteLine("cutscenes ok");
            return 0;
        }
    }
}
