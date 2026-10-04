using System;
using System.Diagnostics;
using System.IO;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Harness
{
    public static partial class Program
    {
        public static string Root;
        public static string Out;

        public static int Main(string[] args)
        {
            Root = FindRoot();
            Out = Path.Combine(Root, "Tools", "Harness", "out");
            Directory.CreateDirectory(Out);
            string cmd = args.Length > 0 ? args[0] : "help";
            try
            {
                switch (cmd)
                {
                    case "engine": return EngineTest();
                    case "audio": return AudioTest(args.Length > 1 && args[1] == "wav");
                    default:
                        return RunCommand(cmd, args);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 2;
            }
        }

        static string FindRoot()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets"))) d = d.Parent;
            if (d == null) throw new Exception("Assets 폴더를 찾을 수 없음");
            return d.FullName;
        }

        public static byte[] LoadFont(string name)
        {
            return File.ReadAllBytes(Path.Combine(Root, "Assets", "Resources", "Fonts", name + ".bytes"));
        }

        static int EngineTest()
        {
            var c = new Canvas(320, 240);
            c.Clear(Pal.Night);
            var f11 = BitmapFont.Load(LoadFont("galmuri11"));
            var f9 = BitmapFont.Load(LoadFont("galmuri9"));
            for (int i = 0; i < Pal.Count; i++) c.FillRect(4 + (i % 16) * 19, 4 + (i / 16) * 19, 17, 17, (byte)i);
            f11.DrawOutlined(c, "꼭꼭 숨어라, 머리카락 보일라…", 8, 50, Pal.BloodBright, Pal.Black);
            var lines = f11.Wrap("하나야, 엄마 이제 나가 봐야 돼. 오늘도 밤새 일하고, 아침에 올게. 문 꼭 잠그고 자.", 200);
            for (int i = 0; i < lines.Count; i++) f11.Draw(c, lines[i], 8, 70 + i * f11.LineStep, Pal.White);
            f9.Draw(c, "작은 글씨 테스트 0123 ABC", 8, 130, Pal.PaleYellow);
            c.FillCircle(250, 150, 20.5f, Pal.Red);
            c.Capsule(200, 200, 280, 170, 5, 2, Pal.Skin);
            c.DitherRect(10, 160, 100, 60, Pal.Blue, 8);
            c.FillTri(150, 230, 190, 160, 230, 230, Pal.Green);
            var spr = Sprite.Parse(new[]
            {
                "..111..",
                ".11111.",
                ".1sss1.",
                ".1s1s1.",
                "..sss..",
                ".ccccc.",
                "s.ccc.s",
                "..ccc..",
                "..c.c..",
                "..s.s..",
            });
            c.BlitScaled(spr, 290, 230, 3);
            ImageIO.SaveCanvas(Path.Combine(Out, "engine_test.png"), c, 2);
            // 어둠 램프 미리보기
            var r = new Canvas(Pal.Count * 10, Pal.Levels * 3 * 10);
            for (int lv = 0; lv < Pal.Levels; lv++)
                for (int i = 0; i < Pal.Count; i++)
                {
                    r.FillRect(i * 10, lv * 10, 10, 10, Pal.DarkNight[lv][i]);
                    r.FillRect(i * 10, (lv + Pal.Levels) * 10, 10, 10, Pal.DarkBlood[lv][i]);
                    r.FillRect(i * 10, (lv + Pal.Levels * 2) * 10, 10, 10, Pal.DarkWarm[lv][i]);
                }
            ImageIO.SaveCanvas(Path.Combine(Out, "ramps.png"), r, 3);
            Console.WriteLine("engine ok");
            return 0;
        }

        static int AudioTest(bool wav)
        {
            var sw = Stopwatch.StartNew();
            int bad = 0;
            for (int i = 0; i < (int)Sfx.Count; i++)
            {
                var t0 = sw.ElapsedMilliseconds;
                var s = SoundBank.Make((Sfx)i);
                bad += Check(((Sfx)i).ToString(), s, sw.ElapsedMilliseconds - t0);
                if (wav) ImageIO.SaveWav(Path.Combine(Out, "wav", "sfx_" + (Sfx)i + ".wav"), s, Dsp.SR);
            }
            for (int i = 0; i < (int)Music.Count; i++)
            {
                var t0 = sw.ElapsedMilliseconds;
                var s = MusicGen.Make((Music)i);
                bad += Check(((Music)i).ToString(), s, sw.ElapsedMilliseconds - t0);
                if (wav) ImageIO.SaveWav(Path.Combine(Out, "wav", "mus_" + (Music)i + ".wav"), s, Dsp.SR);
            }
            string[] lines = { "꼭꼭 숨어라, 머리카락 보일라.", "다음은... 너야.", "하나야... 밥 먹어야지?", "찾았다." };
            VoiceKind[] kinds = { VoiceKind.Kids, VoiceKind.Demon, VoiceKind.Mother, VoiceKind.Whisper };
            for (int i = 0; i < lines.Length; i++)
            {
                var t0 = sw.ElapsedMilliseconds;
                var v = Voice.Speak(lines[i], kinds[i], 1);
                bad += Check("voice_" + kinds[i], v.Samples, sw.ElapsedMilliseconds - t0);
                Console.WriteLine("   chars: " + string.Join(",", Array.ConvertAll(v.CharTimes, x => x.ToString("0.00"))));
                if (wav) ImageIO.SaveWav(Path.Combine(Out, "wav", "voice_" + kinds[i] + ".wav"), v.Samples, Dsp.SR);
            }
            Console.WriteLine("total ms: " + sw.ElapsedMilliseconds + "  bad: " + bad);
            return bad == 0 ? 0 : 1;
        }

        static int Check(string name, float[] s, long ms)
        {
            float peak = 0; double rms = 0; int nan = 0;
            foreach (var v in s)
            {
                if (float.IsNaN(v) || float.IsInfinity(v)) { nan++; continue; }
                peak = Math.Max(peak, Math.Abs(v));
                rms += v * v;
            }
            rms = Math.Sqrt(rms / Math.Max(1, s.Length));
            bool ok = nan == 0 && peak > 0.01f && peak <= 1.0001f;
            Console.WriteLine(string.Format("{0,-18} {1,6:0.00}s peak {2:0.000} rms {3:0.000} {4,5}ms {5}", name, (float)s.Length / Dsp.SR, peak, rms, ms, ok ? "" : "<-- BAD (nan=" + nan + ")"));
            return ok ? 0 : 1;
        }
    }
}
