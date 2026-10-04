using System;
using System.Collections;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>검은 화면에 글자 두 줄.</summary>
    public sealed class TitleCard : Cutscene
    {
        readonly string a, b;
        readonly bool big;
        float alpha;

        public TitleCard(string line1, string line2, bool bigTitle) { a = line1; b = line2; big = bigTitle; }

        public override IEnumerator Run()
        {
            alpha = 0;
            if (big) g.Audio.Play(Sfx.Snip, 0.7f, 0.8f);
            yield return new Tween(1.0f, k => alpha = k);
            yield return g.Wait(big ? 3.5f : 2.2f);
            yield return new Tween(0.8f, k => alpha = 1 - k);
            yield return g.Wait(0.3f);
        }

        public override void Draw(Canvas c)
        {
            c.Clear(Pal.Black);
            if (alpha <= 0) return;
            byte col = alpha > 0.66f ? Pal.Bone : (alpha > 0.33f ? Pal.Gray : Pal.DarkGray);
            if (big)
            {
                int scale = 5;
                int w = Art.BigTextWidth(g.Font, a, scale);
                Art.BigText(c, g.Font, a, (GameCore.W - w) / 2, 70, scale, col);
                g.Font.DrawCentered(c, b, GameCore.W / 2, 150, alpha > 0.5f ? Pal.DarkRed : Pal.Maroon);
            }
            else
            {
                g.Font.DrawCentered(c, a, GameCore.W / 2, 100, col);
                g.Font.DrawCentered(c, b, GameCore.W / 2, 122, col);
            }
        }
    }

    /// <summary>탁상시계: 11:59 → 4:44.</summary>
    public sealed class ClockCutscene : Cutscene
    {
        string time = "11:59";
        bool flicker;

        public override IEnumerator Run()
        {
            g.Audio.Play(Sfx.ClockTick, 0.5f);
            yield return g.Wait(1.2f);
            g.Audio.Play(Sfx.ClockTick, 0.5f);
            yield return g.Wait(1.0f);
            string[] seq = { "12:00", "1:13", "2:22", "3:33", "4:04", "4:40", "4:44" };
            foreach (var s in seq)
            {
                time = s;
                g.Audio.Play(Sfx.ClockTick, 0.4f, 1.3f);
                yield return g.Wait(0.18f);
            }
            g.Audio.Play(Sfx.StingSoft, 0.6f);
            for (int i = 0; i < 6; i++) { flicker = !flicker; yield return g.Wait(0.12f); }
            flicker = false;
            yield return g.Wait(1.5f);
            g.Audio.Play(Sfx.Static, 0.5f);
            g.Fx.StaticFlicker = 1f;
            yield return g.Wait(0.4f);
        }

        public override void Draw(Canvas c)
        {
            c.Clear(Pal.Black);
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            // 시계 몸통
            h.FillRect(44, 44, 72, 34, Pal.Ink);
            h.Rect(44, 44, 72, 34, Pal.DarkGray);
            h.FillRect(48, 48, 64, 26, Pal.Black);
            Art.EndHalf(c);
            if (!flicker) SevenSeg.Draw(c, time, 106, 104, 4, time == "4:44" ? Pal.BloodBright : Pal.Red, Pal.Maroon);
        }
    }

    /// <summary>7세그먼트 숫자.</summary>
    public static class SevenSeg
    {
        static readonly int[] segs = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };

        public static void Draw(Canvas c, string s, int x, int y, int scale, byte on, byte off)
        {
            int cx = x;
            foreach (char ch in s)
            {
                if (ch == ':')
                {
                    c.FillRect(cx + scale, y + scale * 3, scale * 2, scale * 2, on);
                    c.FillRect(cx + scale, y + scale * 8, scale * 2, scale * 2, on);
                    cx += scale * 4;
                    continue;
                }
                int d = ch - '0';
                int m = d >= 0 && d <= 9 ? segs[d] : 0;
                int w = scale * 6, hh = scale * 12, t = scale * 2;
                Seg(c, cx, y, w, hh, t, (m & 1) != 0 ? on : off, 0);
                Seg(c, cx, y, w, hh, t, (m & 2) != 0 ? on : off, 1);
                Seg(c, cx, y, w, hh, t, (m & 4) != 0 ? on : off, 2);
                Seg(c, cx, y, w, hh, t, (m & 8) != 0 ? on : off, 3);
                Seg(c, cx, y, w, hh, t, (m & 16) != 0 ? on : off, 4);
                Seg(c, cx, y, w, hh, t, (m & 32) != 0 ? on : off, 5);
                Seg(c, cx, y, w, hh, t, (m & 64) != 0 ? on : off, 6);
                cx += w + scale * 2;
            }
        }

        static void Seg(Canvas c, int x, int y, int w, int h, int t, byte col, int i)
        {
            int half = h / 2;
            switch (i)
            {
                case 0: c.FillRect(x + t, y, w - 2 * t, t, col); break;
                case 1: c.FillRect(x + w - t, y + t, t, half - t, col); break;
                case 2: c.FillRect(x + w - t, y + half + 1, t, half - t, col); break;
                case 3: c.FillRect(x + t, y + h - t + 1, w - 2 * t, t, col); break;
                case 4: c.FillRect(x, y + half + 1, t, half - t, col); break;
                case 5: c.FillRect(x, y + t, t, half - t, col); break;
                case 6: c.FillRect(x + t, y + half - t / 2, w - 2 * t, t, col); break;
            }
        }
    }

    /// <summary>
    /// 가위눌림. 베개에서 내려다보는 시점: 열리는 방문, 천장에 닿는 형체,
    /// 깜빡일 때마다 가까워진다.
    /// </summary>
    public sealed class ParalysisCutscene : Cutscene
    {
        float view;        // 장면 밝기 0..1
        float door;        // 문 열림 0..1
        float hand;        // 문틀을 잡은 손가락
        float figZ = -1;   // 형체 거리 (음수 = 없음)
        float lean;        // 침대 위로 몸을 숙임
        float face;        // 얼굴 클로즈업 0..1
        float handUp;      // 하나의 손이 올라옴
        int counted = -1;
        bool blink;

        public override IEnumerator Run()
        {
            g.Audio.StopAllMusic(0.1f);
            view = 0;
            yield return g.SayTimed(Speaker.Think, "……", 1.4f);
            g.Audio.SetLoopLevel(Ch.Heart, 0.25f, 2f);
            g.Audio.SetPitch(Ch.Heart, 0.8f);
            yield return new Tween(2.5f, k => view = k);
            yield return g.Say(Speaker.Think, "눈이 떠졌다.");
            yield return g.Say(Speaker.Think, "……몸이 움직이지 않는다.");
            yield return g.Wait(2.0f);
            g.Audio.Play(Sfx.DoorOpen, 0.6f, 0.55f);
            yield return new Tween(3.0f, k => door = k * 0.3f);
            yield return g.Wait(1.5f);
            yield return new Tween(1.2f, k => hand = k);
            yield return g.Say(Speaker.Think, "누가… 있어.");
            g.Audio.Play(Sfx.DoorOpen, 0.7f, 0.5f);
            figZ = 0f;
            yield return new Tween(1.6f, k => door = 0.3f + k * 0.7f);
            hand = 0;
            yield return g.Wait(0.6f);
            g.Audio.Play(Sfx.Snip, 0.8f, 0.9f);
            yield return g.SayTimed(Speaker.Narr, "싹둑.", 1.0f);
            g.Audio.Play(Sfx.Snip, 0.9f, 0.85f);
            yield return g.SayTimed(Speaker.Narr, "싹둑.", 1.0f);
            g.Audio.SetLoopLevel(Ch.Heart, 0.5f, 1f);
            g.Audio.SetPitch(Ch.Heart, 1.2f);
            float[] steps = { 0.3f, 0.55f, 0.8f, 1.0f };
            foreach (var z in steps)
            {
                yield return g.Wait(1.1f);
                blink = true;
                g.Audio.Play(Sfx.Thump, 0.6f, 0.7f);
                yield return g.Wait(0.12f);
                figZ = z;
                blink = false;
                g.Fx.Shake(1, 0.2f);
            }
            yield return g.Say(Speaker.Think, "눈을 감으면… 안 돼.");
            g.Audio.Play(Sfx.Breath, 0.7f, 0.8f);
            yield return new Tween(2.0f, k => lean = k);
            g.Audio.SetPitch(Ch.Heart, 1.7f);
            yield return g.Say(Speaker.Think, "손가락을… 세어야 해.");
            g.Dialogue.Open(Speaker.Sys, "C 키!", 4f);
            float wait = 0;
            while (!g.Input.Pressed(Btn.Check) && wait < 4f) { wait += GameCore.Dt; yield return null; }
            g.Input.Consume(Btn.Check);
            g.Dialogue.Close();
            yield return new Tween(0.8f, k => handUp = k);
            for (int i = 0; i < 3; i++)
            {
                counted = i;
                g.Audio.Play(Sfx.Blip, 0.4f, 1.4f + i * 0.06f);
                yield return g.SayTimed(Speaker.Hana, HandOverlay.Numbers[i] + "…", 0.9f);
            }
            // 얼굴이 덮친다
            g.Audio.Play(Sfx.Sting, 1f);
            g.Fx.Shake(5, 0.8f);
            yield return new Tween(0.35f, k => face = k);
            yield return g.Say(Speaker.Gawi, "꼭꼭 숨어라.");
            yield return g.Say(Speaker.Gawi, "머리카락 보일라.");
            g.Audio.SetLoopLevel(Ch.Heart, 0, 0.05f);
            g.Audio.Play(Sfx.SnipWet, 1f, 0.9f);
            g.Fx.Flash(Pal.White, 3);
            view = 0; face = 0; figZ = -1;
            yield return g.Wait(3.0f);
        }

        public override void Draw(Canvas c)
        {
            c.Clear(Pal.Black);
            if (view <= 0.01f && face <= 0) return;
            if (face > 0.5f)
            {
                Art.DrawGawiFace(c, g.Time, 2f + face * 2);
                return;
            }
            if (blink) return;
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            // 천장 (원근)
            h.FillQuad(0, 0, 160, 0, 122, 32, 38, 32, Pal.Ink);
            h.Line(0, 0, 38, 32, Pal.Night);
            h.Line(159, 0, 122, 32, Pal.Night);
            // 옆벽
            h.FillQuad(0, 0, 38, 32, 38, 84, 0, 120, Pal.Night);
            h.FillQuad(160, 0, 122, 32, 122, 84, 160, 120, Pal.Night);
            // 뒷벽 + 벽지 줄무늬
            h.FillRect(38, 32, 84, 52, Pal.Night);
            for (int x = 40; x < 122; x += 6) h.VLine(x, 33, 83, Pal.Ink);
            h.HLine(38, 121, 32, Pal.DarkBlue);
            h.HLine(38, 121, 83, Pal.Ink);
            // 왼쪽 창에서 들어오는 달빛
            h.FillQuad(6, 22, 28, 32, 28, 62, 6, 60, Pal.DarkBlue);
            h.FillQuad(8, 25, 26, 34, 26, 59, 8, 57, Pal.Blue);
            h.VLine(17, 28, 58, Pal.DarkBlue);
            h.DitherRect(30, 60, 70, 24, Pal.DarkBlue, 4);
            // 문
            int dx0 = 68, dx1 = 92, dy0 = 40, dy1 = 84;
            h.FillRect(dx0 - 3, dy0 - 3, dx1 - dx0 + 6, dy1 - dy0 + 3, Pal.DarkBrown);
            h.FillRect(dx0, dy0, dx1 - dx0, dy1 - dy0, Pal.Black);
            float openW = (dx1 - dx0) * (1 - door);
            h.FillQuad(dx0, dy0, dx0 + openW, dy0 + door * 3, dx0 + openW, dy1 - door * 3, dx0, dy1, Pal.Brown);
            if (openW > 6)
            {
                h.Rect(dx0 + 3, dy0 + 4, (int)openW - 6, 14, Pal.DarkBrown);
                h.Rect(dx0 + 3, dy0 + 22, (int)openW - 6, 16, Pal.DarkBrown);
                h.Set((int)(dx0 + openW - 3), (dy0 + dy1) / 2 + 2, Pal.Yellow);
            }
            // 문틀을 잡은 긴 손가락
            if (hand > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    int fy = dy0 + 12 + i * 3;
                    int len = (int)(hand * (5 + i * 1.5f));
                    h.HLine(dx1 - len + 2, dx1 + 1, fy, Pal.Bone);
                    h.Set(dx1 - len + 2, fy, Pal.LightGray);
                }
            }
            // 형체
            if (figZ >= 0)
            {
                float sc = M.Lerp(0.5f, 1.55f, figZ);
                float gx = M.Lerp(80, 82, figZ);
                float gy = M.Lerp(84, 152, figZ) + lean * 8;
                float t = M.Step(T, 12);
                Art.DrawTallFigure(h, gx, gy, sc, t, Pal.Black);
            }
            // 이불 (발치) + 발 모양 언덕
            h.FillQuad(0, 120, 160, 120, 152, 92, 8, 92, Pal.DarkBlue);
            h.FillEllipse(62, 95, 13, 5, Pal.Blue);
            h.FillEllipse(98, 95, 13, 5, Pal.Blue);
            h.Line(20, 108, 70, 100, Pal.Night);
            h.Line(100, 101, 140, 112, Pal.Night);
            h.HLine(8, 151, 92, Pal.Blue);
            // 숙여서 내려오는 얼굴 + 이불을 짚은 손
            if (lean > 0)
            {
                float fy = M.Lerp(-30, 44, lean);
                float fr = 12 + lean * 12;
                Art.DrawPaperFace(h, 82, fy, fr, fr * 1.25f, T, 0.3f + lean * 0.5f, 0);
                for (int s2 = -1; s2 <= 1; s2 += 2)
                {
                    float hx = 80 + s2 * (26 + lean * 12), hy = 98;
                    float shx = 80 + s2 * 18, shy = -6;
                    float ex = 80 + s2 * (48 + lean * 6), ey = 40 + lean * 10;
                    h.Capsule(shx - 1, shy, ex - 1, ey, 2.4f, 2.0f, Pal.DarkGray);
                    h.Capsule(shx, shy, ex, ey, 2.4f, 2.0f, Pal.Black);
                    h.Capsule(ex - 1, ey, hx - 1, hy - 6, 2.0f, 1.6f, Pal.DarkGray);
                    h.Capsule(ex, ey, hx, hy - 6, 2.0f, 1.6f, Pal.Black);
                    for (int i = -2; i <= 2; i++)
                        h.Line((int)hx, (int)(hy - 6), (int)(hx + i * 2.5f), (int)(hy + 1 + Math.Abs(i)), Pal.Bone);
                }
            }
            // 하나의 손
            if (handUp > 0)
            {
                var pose = new HandPose { Highlight = counted, Flex = 0.3f, Tremble = 1.2f };
                Art.DrawHand(h, 80, 150 - handUp * 46, 0.75f, pose, g.Time);
            }
            float dark = 1f - view;
            if (dark > 0) ScreenFx.ApplyFade(h, dark);
            Art.EndHalf(c);
        }
    }

    /// <summary>손가락 세기 클로즈업.</summary>
    public sealed class FingerCutscene : Cutscene
    {
        public enum Mode { FirstDream, Ending }
        readonly int count;
        readonly Mode mode;
        float rise;
        HandPose pose;
        string label;
        byte labelColor = Pal.White;
        float fade = 1;

        public FingerCutscene(int fingers, Mode m)
        {
            count = fingers;
            mode = m;
            pose.Highlight = -1;
        }

        public override IEnumerator Run()
        {
            bool end = mode == Mode.Ending;
            pose.Pale = end;
            pose.Tremble = end ? 1.4f : 0.5f;
            if (!end) g.Audio.SetMusic(Ch.Layer, Music.AmbNightmare, 0.4f, 1f);
            yield return new Tween(0.7f, k => fade = 1 - k);
            yield return new Tween(0.9f, k => rise = M.EaseOut(k));
            yield return g.Wait(0.5f);
            for (int i = 0; i < 5; i++)
            {
                pose.Highlight = i;
                label = HandOverlay.Numbers[i];
                labelColor = Pal.White;
                g.Audio.Play(Sfx.Blip, 0.45f, 1.4f + i * 0.06f);
                yield return new Tween(end ? 0.75f : 0.5f, k => pose.Flex = M.Sin(k * M.PI) * 0.45f);
            }
            pose.Highlight = -1;
            pose.Flex = 0;
            label = HandOverlay.Numbers[4] + ".";
            yield return g.Wait(end ? 2.2f : 1.0f);
            if (end)
            {
                g.Audio.SetLoopLevel(Ch.Heart, 0, 0.5f);
                yield return g.Wait(1.0f);
                yield return new Tween(1.2f, k => fade = k);
                yield break;
            }
            // 여섯 번째
            g.Audio.Play(Sfx.Breath, 0.4f, 1.4f);
            pose.Extra = count - 5;
            yield return new Tween(1.3f, k => { pose.Growth = k * 0.3f; pose.Tremble = 0.5f + k * 1.5f; });
            g.Audio.Play(Sfx.Crack, 1f);
            g.Audio.Play(Sfx.Gore, 0.8f);
            g.Fx.Shake(3, 0.4f);
            g.Fx.Blood.Splash(120, 110, 0.18f);
            yield return new Tween(0.3f, k => pose.Growth = 0.3f + k * 0.15f);
            g.Audio.Play(Sfx.Splat, 0.7f);
            label = HandOverlay.Numbers[Math.Min(7, count - 1)] + "…";
            labelColor = Pal.BloodBright;
            yield return new Tween(1.5f, k => { pose.Growth = 0.45f + k * 0.55f; pose.Blood = k * 0.6f; });
            g.Audio.Play(Sfx.StingSoft, 0.8f);
            yield return new Tween(1.4f, k => pose.Blood = 0.6f + k * 0.4f);
            yield return g.Wait(1.2f);
            g.Audio.StopMusic(Ch.Layer, 1f);
            g.Fx.StaticFlicker = 0.7f;
            g.Audio.Play(Sfx.Static, 0.5f);
            yield return new Tween(0.5f, k => fade = k);
            g.Fx.Blood.StartDissolve();
        }

        public override void Draw(Canvas c)
        {
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            // 손 뒤의 희미한 빛 (계단식 원)
            bool end = mode == Mode.Ending;
            h.FillCircle(82, 62, 66, end ? Pal.DarkBrown : Pal.Ink);
            h.FillCircle(82, 62, 50, end ? Pal.Brown : Pal.Night);
            h.FillCircle(82, 62, 34, end ? Pal.Tan : Pal.DarkBlue);
            float cy = M.Lerp(170, 74, rise);
            Art.DrawHand(h, 82, cy, 1.45f, pose, g.Time);
            if (pose.Blood > 0.3f)
            {
                // 손목 아래로 떨어진 피 웅덩이
                float pr = (pose.Blood - 0.3f) * 30;
                h.FillEllipse(72, 119, pr * 1.6f, pr * 0.35f + 1, Pal.DarkRed);
                h.FillEllipse(72, 118, pr, pr * 0.2f + 0.5f, Pal.Red);
            }
            if (fade > 0) ScreenFx.ApplyFade(h, fade);
            Art.EndHalf(c);
        }

        public override void DrawUi(Canvas c)
        {
            if (label != null && fade < 0.8f)
            {
                int scale = 3;
                Art.BigText(c, g.Font, label, 16 + 2, 16 + 2, scale, Pal.Black);
                Art.BigText(c, g.Font, label, 16, 16, scale, labelColor);
            }
        }
    }

    /// <summary>사진 조각을 주웠을 때.</summary>
    public sealed class PieceCutscene : Cutscene
    {
        readonly string flag;
        float a;

        public PieceCutscene(string pieceFlag) { flag = pieceFlag; Opaque = false; }

        public override IEnumerator Run()
        {
            yield return new Tween(0.3f, k => a = k);
            yield return g.Wait(2.0f);
            yield return new Tween(0.3f, k => a = 1 - k);
        }

        public override void Draw(Canvas c)
        {
            c.DitherDarken(0, 0, c.W, c.H, Pal.DarkNight[4], (int)(a * 12));
            if (a < 0.3f) return;
            Art.BeginHalf(Pal.Transparent);
            var h = Art.Half;
            int part = flag == "piece_bath" ? 0 : (flag == "piece_kitchen" ? 1 : 2);
            Photo.DrawPiece(h, 80, 56, part, g.Time);
            c.BlitCanvasKeyed(ScaleUp(h), 0, 0, Pal.Transparent);
        }

        static Canvas big;
        static Canvas ScaleUp(Canvas h)
        {
            if (big == null) big = new Canvas(320, 240);
            big.Clear(Pal.Transparent);
            big.BlitCanvas(h, 0, 0, 2);
            return big;
        }

        public override void DrawUi(Canvas c)
        {
            if (a < 0.6f) return;
            int n = Story.Pieces(g);
            g.Font.DrawOutlined(c, "가족사진 조각 (" + n + "/3)", GameCore.W / 2 - g.Font.Measure("가족사진 조각 (" + n + "/3)") / 2, 196, Pal.PaleYellow, Pal.Black);
        }
    }
}
