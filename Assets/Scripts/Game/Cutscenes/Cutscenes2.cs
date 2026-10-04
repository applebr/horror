using System;
using System.Collections;
using System.Collections.Generic;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>사망 연출 4종.</summary>
    public sealed class DeathCutscene : Cutscene
    {
        readonly DeathKind kind;
        float k;          // 진행도
        float red;        // 붉은 화면
        bool black;
        string word;
        readonly List<float> fx = new List<float>();
        readonly List<float> fy = new List<float>();
        readonly List<float> fa = new List<float>();

        public DeathCutscene(DeathKind dk) { kind = dk; }
        public DeathKind Kind { get { return kind; } }

        public override IEnumerator Run()
        {
            g.Fx.Shake(3, 0.3f);
            switch (kind)
            {
                case DeathKind.Scissors:
                    g.Audio.Play(Sfx.Sting, 1f);
                    yield return new Tween(0.9f, p => k = M.EaseIn(p) * 0.85f);
                    yield return g.Wait(0.25f);
                    k = 1f;
                    g.Audio.Play(Sfx.SnipWet, 1f);
                    g.Audio.Play(Sfx.Gore, 0.9f, 0.8f);
                    g.Fx.Flash(Pal.White, 2);
                    g.Fx.Shake(6, 0.8f);
                    red = 1f;
                    g.Fx.Blood.Splash(160, 110, 1f);
                    yield return g.Wait(0.5f);
                    g.Fx.Blood.Splash(120, 80, 0.6f);
                    word = "싹둑.";
                    break;
                case DeathKind.Fingers:
                    g.Audio.Play(Sfx.Skitter, 1f);
                    for (int i = 0; i < 260; i++)
                    {
                        float a = g.Rng.Range(0, M.TAU);
                        fx.Add(160 + M.Cos(a) * g.Rng.Range(170, 260));
                        fy.Add(120 + M.Sin(a) * g.Rng.Range(140, 220));
                        fa.Add(g.Rng.Range(0.6f, 1.4f));
                    }
                    for (int s = 0; s < 4; s++) { g.Audio.Play(Sfx.Skitter, 1f, 0.8f + s * 0.15f); yield return new Tween(0.55f, p => k = (s + p) / 4f); }
                    g.Audio.Play(Sfx.Gore, 1f);
                    g.Audio.Play(Sfx.Scream, 0.6f, 1.6f);
                    yield return g.Wait(0.4f);
                    black = true;
                    word = "…";
                    break;
                case DeathKind.Mother:
                    g.Audio.Play(Sfx.Sting, 1f, 0.9f);
                    yield return new Tween(1.2f, p => k = p * 0.5f);
                    g.Audio.Play(Sfx.Rip, 1f, 0.7f);
                    yield return new Tween(0.5f, p => k = 0.5f + p * 0.5f);
                    g.Audio.Play(Sfx.Scream, 1f, 0.7f);
                    g.Audio.Play(Sfx.Gore, 1f);
                    g.Fx.Shake(6, 0.8f);
                    g.Fx.Flash(Pal.Red, 3);
                    red = 1f;
                    g.Fx.Blood.Splash(160, 120, 0.9f);
                    word = "밥 먹자, 하나야.";
                    break;
                case DeathKind.Kids:
                    g.Audio.PlayVoice("찾았다.", VoiceKind.Kids, 1f);
                    yield return new Tween(1.6f, p => k = M.EaseIn(p));
                    g.Audio.Play(Sfx.Static, 1f);
                    g.Fx.StaticFlicker = 1f;
                    black = true;
                    word = "찾았다.";
                    break;
            }
            yield return g.Wait(1.6f);
            if (g.Input.Down(Btn.Confirm)) yield break;
            yield return g.Wait(0.8f);
            g.Fx.Blood.StartDissolve();
        }

        public override void Draw(Canvas c)
        {
            c.Clear(Pal.Black);
            switch (kind)
            {
                case DeathKind.Scissors: DrawScissors(c); break;
                case DeathKind.Fingers: DrawFingers(c); break;
                case DeathKind.Mother:
                    if (red <= 0) Art.DrawMotherFace(c, g.Time, k);
                    else c.Clear(Pal.Maroon);
                    break;
                case DeathKind.Kids:
                    if (!black)
                    {
                        Art.BeginHalf(Pal.Black);
                        var h = Art.Half;
                        float r = 10 + k * 120;
                        h.FillEllipse(80, 60 - r * 0.25f, r * 1.9f, r * 1.9f, Pal.Ink);
                        h.FillEllipse(80, 60 + r * 0.1f, r * 1.45f, r * 1.6f, Pal.SkinLight);
                        h.FillEllipse(80, 60 - r * 0.9f, r * 1.7f, r * 0.9f, Pal.Ink);
                        h.FillEllipse(80, 60, r * 1.1f + 4, r * 1.3f + 4, Pal.DarkRed);
                        h.FillEllipse(80, 60, r * 1.1f, r * 1.3f, Pal.Black);
                        h.Circle(80, 60, r * 1.1f + 1, Pal.DarkRed);
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i * 0.78f;
                            int x = (int)(80 + M.Cos(a) * r * 1.1f), y = (int)(60 + M.Sin(a) * r * 1.3f);
                            h.VLine(x, y, y + (int)(4 + k * 20), Pal.Red);
                        }
                        Art.EndHalf(c);
                    }
                    break;
            }
            if (red > 0 && kind == DeathKind.Scissors) c.Remap(Pal.RedTint);
        }

        void DrawScissors(Canvas c)
        {
            if (k >= 1f) { c.Clear(Pal.Maroon); return; }
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            // 내려다보는 종이 얼굴
            float fr = 13 + k * 9;
            Art.DrawPaperFace(h, 80 + M.Sin(g.Time * 20) * k * 2, 4 + fr * 0.9f, fr, fr * 1.25f, T, 0.6f + k * 0.4f, 0);
            // 목을 향해 닫히는 거대한 날
            float pv = 62 + k * 6;
            float ang = M.Lerp(1.0f, 0.06f, k);
            DrawBlade(h, 80, pv, M.PI * 0.5f + ang, 150);
            DrawBlade(h, 80, pv, M.PI * 0.5f - ang, 150);
            h.Circle(70, pv - 8, 6, Pal.Gray);
            h.Circle(90, pv - 8, 6, Pal.Gray);
            h.Circle(70, pv - 8, 5, Pal.DarkGray);
            h.Circle(90, pv - 8, 5, Pal.DarkGray);
            h.FillCircle(80, pv, 4, Pal.Gray);
            h.FillCircle(80, pv, 2, Pal.White);
            Art.EndHalf(c);
        }

        static void DrawBlade(Canvas h, float px, float py, float a, float len)
        {
            float dx = M.Cos(a), dy = M.Sin(a);
            float nx = -dy, ny = dx;
            float w = 9;
            h.FillQuad(px + nx * w, py + ny * w, px + dx * len + nx * 1, py + dy * len + ny * 1, px + dx * len - nx * 1, py + dy * len - ny * 1, px - nx * w * 0.3f, py - ny * w * 0.3f, Pal.LightGray);
            h.Line((int)(px + nx * w * 0.8f), (int)(py + ny * w * 0.8f), (int)(px + dx * len), (int)(py + dy * len), Pal.White);
            h.Line((int)(px - nx * w * 0.2f), (int)(py - ny * w * 0.2f), (int)(px + dx * len), (int)(py + dy * len), Pal.Gray);
        }

        void DrawFingers(Canvas c)
        {
            if (black) return;
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            int n = fx.Count;
            for (int i = 0; i < n; i++)
            {
                // 가장자리에서 기어 들어오며 점점 커진다 (카메라를 덮친다)
                float p = M.Clamp01(k * fa[i] * 1.25f - M.Hash(i, 9, 3) * 0.3f);
                if (p <= 0) continue;
                float tx = 80 + (M.Hash(i, 0, 3) - 0.5f) * 150, ty = 60 + (M.Hash(i, 1, 3) - 0.5f) * 110;
                float x = M.Lerp(fx[i] * 0.5f, tx, p), y = M.Lerp(fy[i] * 0.5f, ty, p);
                float size = 0.6f + p * p * 5f;
                float a = M.Atan2(ty - fy[i] * 0.5f, tx - fx[i] * 0.5f) + M.Sin(g.Time * 14 + i) * 0.35f;
                float ux = M.Cos(a), uy = M.Sin(a);
                float len = 3 + size * 2.2f;
                float r = Math.Max(0.6f, size * 0.55f);
                float hx = x + ux * len, hy = y + uy * len;
                h.Capsule(x, y, hx, hy, r + 0.6f, r * 0.85f + 0.6f, Pal.SkinShadow);
                h.Capsule(x, y, hx, hy, r, r * 0.85f, Pal.Skin);
                h.FillCircle(x, y, r, Pal.Red);
                h.FillCircle(x, y, r * 0.5f, Pal.DarkRed);
                h.FillCircle(hx - ux * r * 0.4f, hy - uy * r * 0.4f, r * 0.6f, Pal.Bone);
                if (size > 2.5f)
                {
                    float mx = x + ux * len * 0.5f, my = y + uy * len * 0.5f;
                    h.Line((int)(mx - uy * r), (int)(my + ux * r), (int)(mx + uy * r), (int)(my - ux * r), Pal.SkinShadow);
                }
            }
            Art.EndHalf(c);
        }

        public override void DrawUi(Canvas c)
        {
            if (word == null) return;
            var f = g.Font;
            int scale = kind == DeathKind.Scissors ? 4 : 2;
            int w = Art.BigTextWidth(f, word, scale);
            int x = (GameCore.W - w) / 2, y = 100;
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                    if (ox != 0 || oy != 0) Art.BigText(c, f, word, x + ox * 2, y + oy * 2, scale, Pal.Black);
            Art.BigText(c, f, word, x, y, scale, Pal.Bone);
        }
    }

    /// <summary>세 조각을 맞추면 드러나는 가족사진.</summary>
    public sealed class PhotoCutscene : Cutscene
    {
        float join;
        float zoom;

        public override IEnumerator Run()
        {
            g.Audio.Play(Sfx.Pickup, 0.5f, 0.7f);
            yield return new Tween(1.6f, k => join = M.Smooth(k));
            g.Audio.Play(Sfx.Click, 0.5f);
            g.Audio.SetMusic(Ch.Music, Music.MusicBox, 0.4f, 2f);
            yield return g.Wait(0.8f);
            yield return g.Say(Speaker.Think, "엄마… 나… 그리고…");
            yield return g.Say(Speaker.Think, "……");
            yield return new Tween(1.5f, k => zoom = M.Smooth(k));
            yield return g.Say(Speaker.Think, "사람 모양으로 오려진 구멍.");
            yield return g.Say(Speaker.Think, "키가 큰 사람. 내 어깨에 손을 올리고 있던…");
            yield return g.Say(Speaker.Hana, "…아빠.");
            yield return g.Say(Speaker.Think, "아빠가 있었어. 왜 잊어버렸지?");
            yield return g.Say(Speaker.Think, "그림일기 속 키 큰 사람. 바다에서 사진을 찍어 준 사람.");
            yield return g.Say(Speaker.Think, "가위가… 아빠를 오려 갔어.");
            g.Audio.StopMusic(Ch.Music, 1.5f);
        }

        public override void Draw(Canvas c)
        {
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            if (join < 0.999f)
            {
                float sep = (1 - join) * 30;
                Photo.DrawPiece(h, 80 - 31 - sep, 58 - sep * 0.3f, 0, g.Time);
                Photo.DrawPiece(h, 80, 58 + sep * 0.4f, 1, g.Time);
                Photo.DrawPiece(h, 80 + 31 + sep, 58 - sep * 0.2f, 2, g.Time);
            }
            else
            {
                Photo.Draw(h, 80, 58, Photo.Mom | Photo.Hana, g.Time);
                // 오린 선
                h.DashedLine(46, 23, 46, 92, Pal.Bone, 2, 2, 0);
                h.DashedLine(78, 23, 78, 92, Pal.Bone, 2, 2, 0);
            }
            Art.EndHalf(c);
            if (zoom > 0)
            {
                // 구멍 쪽으로 다가가는 느낌: 가장자리부터 어두워진다
                c.DitherDarken(0, 0, (int)(zoom * 110), 240, Pal.DarkNight[3], 12);
            }
        }
    }

    /// <summary>저절로 켜진 TV. 노이즈 → 화면 조정 → 얼굴.</summary>
    public sealed class TvCutscene : Cutscene
    {
        int stage; // 0 노이즈, 1 화면조정, 2 얼굴, 3 꺼짐
        float off;

        public override IEnumerator Run()
        {
            stage = 0;
            g.Audio.SetLoopLevel(Ch.Noise, 0.35f, 0.2f);
            yield return g.Wait(1.2f);
            stage = 1;
            g.Audio.Play(Sfx.TvOn, 0.6f);
            yield return g.Wait(1.6f);
            stage = 2;
            g.Audio.Play(Sfx.Sting, 0.8f, 0.8f);
            g.Fx.Shake(2, 0.3f);
            yield return g.Say(Speaker.Tv, "다음은… 너야.");
            yield return g.Say(Speaker.Tv, "하나, 둘, 셋, 넷, 다섯, 여섯, 일곱, 여덟, 아홉, 열.");
            yield return g.Say(Speaker.Tv, "…찾으러 간다.");
            stage = 3;
            g.Audio.SetLoopLevel(Ch.Noise, 0, 0.1f);
            g.Audio.Play(Sfx.Click, 0.8f, 0.6f);
            yield return new Tween(0.4f, k => off = k);
            yield return g.Wait(0.6f);
        }

        public override void Draw(Canvas c)
        {
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            // TV 테두리
            h.FillRect(10, 6, 140, 108, Pal.DarkGray);
            h.FillRect(14, 10, 132, 96, Pal.Black);
            int sx = 18, sy = 14, sw = 124, sh = 88;
            if (stage == 0 || stage == 2)
            {
                for (int y = sy; y < sy + sh; y++)
                    for (int x = sx; x < sx + sw; x++)
                    {
                        float n = M.Hash(x, y, g.Frame);
                        h.Set(x, y, n < 0.3f ? Pal.Black : (n < 0.6f ? Pal.DarkGray : (n < 0.85f ? Pal.Gray : Pal.LightGray)));
                    }
            }
            if (stage == 1)
            {
                byte[] bars = { Pal.White, Pal.Yellow, Pal.Teal, Pal.Green, Pal.Purple, Pal.Red, Pal.Blue };
                int bw = sw / bars.Length;
                for (int i = 0; i < bars.Length; i++) h.FillRect(sx + i * bw, sy, bw + 1, sh * 2 / 3, bars[i]);
                h.FillRect(sx, sy + sh * 2 / 3, sw, sh / 3, Pal.Ink);
                int gy = (int)(g.Time * 40) % sh;
                h.HLine(sx, sx + sw - 1, sy + gy, Pal.White);
            }
            if (stage == 2)
            {
                // 노이즈 속의 얼굴
                float fx = 80 + M.Sin(g.Time * 9) * 1.5f;
                Art.DrawPaperFace(h, fx, 56, 26, 34, g.Time, 0.6f + 0.4f * Math.Max(0, M.Sin(g.Time * 6)), 0);
                for (int y = sy; y < sy + sh; y += 2)
                    if (M.Hash(y, g.Frame, 2) < 0.25f) h.HLine(sx, sx + sw - 1, y, Pal.DarkGray);
            }
            if (stage == 3)
            {
                int lineH = (int)(sh * (1 - off));
                h.FillRect(sx, sy + (sh - lineH) / 2, sw, Math.Max(1, lineH), Pal.White);
                if (off > 0.9f) h.FillRect(sx, sy, sw, sh, Pal.Black);
            }
            Art.EndHalf(c);
        }
    }

    /// <summary>가위의 최후.</summary>
    public sealed class BossDeathCutscene : Cutscene
    {
        float tear;       // 얼굴 찢어짐
        float collapse;   // 무너짐
        float fallen;     // 가위가 떨어짐
        bool scissorsShot;

        public override IEnumerator Run()
        {
            g.Audio.Play(Sfx.Scream, 1f, 0.7f);
            g.Fx.Shake(4, 1.5f);
            yield return new Tween(1.8f, k => tear = k);
            g.Audio.Play(Sfx.Rip, 1f, 0.6f);
            g.Audio.Play(Sfx.Gore, 1f, 0.7f);
            g.Fx.Blood.Splash(160, 90, 0.7f);
            yield return g.Say(Speaker.Gawi, "가위는… 누군가를…");
            yield return g.Say(Speaker.Gawi, "꼭… 잘라야… 해……");
            g.Audio.Play(Sfx.Crack, 1f, 0.6f);
            yield return new Tween(1.4f, k => collapse = M.EaseIn(k));
            g.Audio.Play(Sfx.Thump, 1f, 0.6f);
            g.Fx.Shake(5, 0.5f);
            yield return g.Wait(0.6f);
            g.Audio.Play(Sfx.Snip, 0.9f, 0.6f);
            g.Audio.Play(Sfx.Thump, 0.6f, 1.4f);
            fallen = 1;
            yield return g.Wait(1.5f);
            g.Fx.Blood.StartDissolve();
            scissorsShot = true;
            yield return g.Wait(1.0f);
            yield return g.Say(Speaker.Think, "…가위가 떨어져 있다.");
            yield return g.Say(Speaker.Think, "누군가를 꼭 잘라야 한다면…");
            yield return g.Say(Speaker.Hana, "이번엔, 내가 자를 거야.");
            yield return g.Say(Speaker.Hana, "이 꿈을.");
        }

        public override void Draw(Canvas c)
        {
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            // 아래에서 비추는 손전등 빛
            h.FillTri(80, 130, 14, -10, 146, -10, Pal.Ink);
            h.FillTri(80, 130, 40, -10, 120, -10, Pal.Night);
            // 바닥과 피 웅덩이
            h.FillRect(0, 100, 160, 20, Pal.Maroon);
            h.FillEllipse(80, 106, 36 + collapse * 20, 5 + collapse * 3, Pal.DarkRed);
            h.FillEllipse(84, 105, 18 + collapse * 10, 2 + collapse * 2, Pal.Red);
            if (scissorsShot)
            {
                h.FillEllipse(80, 100, 50, 10, Pal.DarkRed);
                h.BlitScaled(Sprites.BigScissors, 80, 102, 5);
                if (((int)(g.Time * 2)) % 3 == 0) h.Set(62, 60, Pal.White);
                Art.EndHalf(c);
                return;
            }
            float sq = 1 - collapse;
            float baseY = 104;
            float hgt = 92 * sq + 6;
            float shake = (1 - collapse) * tear * 2;
            float x = 80 + M.Sin(g.Time * 31) * shake;
            byte body = Pal.Black, rim = Pal.Gray;
            // 몸 (아래에서 빛을 받아 테두리가 밝다)
            h.FillQuad(x - 10, baseY - hgt * 0.62f, x + 10, baseY - hgt * 0.62f, x + 8 + collapse * 22, baseY, x - 8 - collapse * 22, baseY, rim);
            h.FillQuad(x - 9, baseY - hgt * 0.62f + 1, x + 9, baseY - hgt * 0.62f + 1, x + 7 + collapse * 21, baseY, x - 7 - collapse * 21, baseY, body);
            h.Capsule(x - 10, baseY - hgt * 0.6f, x - 26 - collapse * 10, baseY - hgt * 0.12f, 2.6f, rim);
            h.Capsule(x - 10, baseY - hgt * 0.6f, x - 26 - collapse * 10, baseY - hgt * 0.12f, 1.8f, body);
            h.Capsule(x + 10, baseY - hgt * 0.6f, x + 28 + collapse * 10, baseY - hgt * 0.18f, 2.6f, rim);
            h.Capsule(x + 10, baseY - hgt * 0.6f, x + 28 + collapse * 10, baseY - hgt * 0.18f, 1.8f, body);
            // 얼굴: 반으로 찢어져 벌어진다
            float fy = baseY - hgt * 0.8f;
            float sp = tear * 10;
            float fr = 15;
            if (tear > 0.15f)
            {
                // 틈 사이: 손가락과 이빨이 엉킨 살덩이
                h.FillEllipse(x, fy, sp + 3, fr * 1.2f * Math.Max(0.35f, sq), Pal.DarkRed);
                for (int i = 0; i < 16; i++)
                {
                    float yy = fy - fr * sq + i * 2.0f * sq;
                    float xx = x + M.Sin(i * 1.7f + g.Time * 7) * sp * 0.5f;
                    float ang = i * 0.9f + g.Time * 3;
                    h.Capsule(xx, yy, xx + M.Cos(ang) * 3, yy + M.Sin(ang) * 2, 0.9f, i % 3 == 0 ? Pal.Bone : Pal.Skin);
                }
            }
            h.SetClip(0, 0, (int)x, 120);
            Art.DrawPaperFace(h, x - sp, fy, fr, fr * 1.2f * Math.Max(0.35f, sq), g.Time, 0.8f, 0);
            h.SetClip((int)x, 0, 160 - (int)x, 120);
            Art.DrawPaperFace(h, x + sp, fy, fr, fr * 1.2f * Math.Max(0.35f, sq), g.Time, 0.8f, 0);
            h.ResetClip();
            if (tear > 0.15f)
            {
                // 찢긴 가장자리 + 쏟아지는 피
                for (int yy = (int)(fy - fr); yy < (int)(fy + fr); yy += 2)
                {
                    h.Set((int)(x - sp) + (yy % 4 == 0 ? 1 : 0), yy, Pal.LightGray);
                    h.Set((int)(x + sp) - (yy % 4 == 0 ? 1 : 0), yy, Pal.LightGray);
                }
                for (int i = 0; i < 7; i++)
                {
                    int dx = (int)(x - sp + i * (sp * 2 / 6f));
                    h.VLine(dx, (int)(fy + fr * 0.6f), (int)(fy + fr * 0.6f + 6 + tear * (10 + i * 5)), Pal.Red);
                }
            }
            // 가위
            if (fallen <= 0)
            {
                float gx = x + 28, gy = baseY - hgt * 0.18f;
                h.FillTri(gx - 1, gy, gx + 1, gy, gx - 4, gy + 22, Pal.LightGray);
                h.FillTri(gx - 1, gy, gx + 1, gy, gx + 5, gy + 22, Pal.White);
                h.Circle(gx - 2, gy - 3, 2, Pal.Gray);
                h.Circle(gx + 2, gy - 3, 2, Pal.Gray);
            }
            else h.BlitScaled(Sprites.BigScissors, 116, 104, 2);
            Art.EndHalf(c);
        }
    }

    /// <summary>하나가 가위로 꿈을 자른다: 절취선을 따라 화면이 갈라진다.</summary>
    public sealed class CutDreamCutscene : Cutscene
    {
        float cut;    // 자른 길이 0..1
        float split;  // 갈라짐 0..1
        float white;
        static byte[] copy;

        float oldVignette;
        public override void Begin(GameCore game) { base.Begin(game); oldVignette = g.Fx.Vignette; g.Fx.Vignette = 0; }
        public override void End() { g.Fx.Vignette = oldVignette; }

        public override IEnumerator Run()
        {
            yield return g.Wait(0.6f);
            for (int i = 0; i < 6; i++)
            {
                g.Audio.Play(Sfx.Snip, 0.8f, 1f + i * 0.05f);
                float from = cut;
                yield return new Tween(0.38f, k => cut = from + k / 6f);
            }
            cut = 1;
            g.Audio.Play(Sfx.Rip, 1f, 0.5f);
            g.Audio.Play(Sfx.Lucid, 1f, 0.8f);
            yield return new Tween(1.6f, k => split = M.EaseIn(k));
            yield return new Tween(1.2f, k => white = k);
            yield return g.Wait(2.0f);
        }

        public override void Draw(Canvas c)
        {
            if (white >= 1f) { c.Clear(Pal.White); return; }
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            h.FillRect(0, 0, 160, 120, Pal.Maroon);
            for (int y = 0; y < 120; y += 3) h.HLine(0, 159, y, Pal.DarkRed);
            h.FillRect(0, 96, 160, 24, Pal.DarkRed);
            // 매달린 사람들 (실루엣)
            for (int i = 0; i < 7; i++)
            {
                float sway = M.Sin(T * 0.8f + i * 1.3f) * 2;
                int x = 10 + i * 24 + (int)sway;
                int y = 52 + (i % 2) * 10;
                h.VLine(10 + i * 24, 0, y - 20, Pal.Black);
                h.BlitSilhouette(Sprites.PaperDoll, x, y, false, Pal.Black);
                h.Set(x, y - 8, Pal.BloodBright);
            }
            // 절취선 (왼쪽 위 → 오른쪽 아래)
            float x0 = 0, y0 = 20, x1 = 160, y1 = 100;
            int phase = (int)(g.Time * 8);
            h.DashedLine((int)x0, (int)y0, (int)x1, (int)y1, Pal.White, 4, 3, phase);
            float cx = M.Lerp(x0, x1, cut), cy = M.Lerp(y0, y1, cut);
            if (cut > 0)
            {
                // 잘린 틈으로 빛이 샌다
                h.Line((int)x0, (int)y0 - 1, (int)cx, (int)cy - 1, Pal.PaleYellow);
                h.Line((int)x0, (int)y0, (int)cx, (int)cy, Pal.White);
                h.Line((int)x0, (int)y0 + 1, (int)cx, (int)cy + 1, Pal.PaleYellow);
            }
            if (split <= 0)
            {
                // 큰 재단 가위와 작은 손
                float open = Math.Max(0, M.Sin(T * 16));
                float ax = cx, ay = cy;
                h.FillTri(ax, ay, ax + 30, ay - 14 - open * 8, ax + 30, ay - 10 - open * 8, Pal.LightGray);
                h.FillTri(ax, ay, ax + 30, ay - 2 + open * 8 - 8, ax + 30, ay + 2 + open * 8 - 8, Pal.White);
                h.Circle(ax + 34, ay - 15 - open * 8, 4, Pal.Gray);
                h.Circle(ax + 34, ay - 1 + open * 8 - 8, 4, Pal.Gray);
                h.FillEllipse(ax + 38, ay - 8, 6, 7, Pal.SkinShadow);
                h.FillEllipse(ax + 38, ay - 9, 5, 6, Pal.Skin);
                h.FillRect((int)ax + 38, (int)ay - 6, 14, 6, Pal.PaleBlue);
            }
            Art.EndHalf(c);
            if (split > 0)
            {
                // 두 조각으로 갈라져 멀어진다
                int d = (int)(split * 140);
                var px = c.Px;
                if (copy == null || copy.Length != px.Length) copy = new byte[px.Length];
                Buffer.BlockCopy(px, 0, copy, 0, px.Length);
                c.Clear(Pal.White);
                for (int y = 0; y < GameCore.H; y++)
                    for (int x = 0; x < GameCore.W; x++)
                    {
                        float lineY = 40 + x * 0.5f;
                        bool above = y < lineY;
                        int sy = above ? y + d : y - d;
                        if (sy < 0 || sy >= GameCore.H) continue;
                        if (above && sy >= lineY) continue;
                        if (!above && sy < lineY) continue;
                        px[y * GameCore.W + x] = copy[sy * GameCore.W + x];
                    }
            }
            if (white > 0) ScreenFxWhite(c, white);
        }

        static void ScreenFxWhite(Canvas c, float w)
        {
            int lvl = (int)(w * 16);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                    if (lvl > Canvas.Bayer4[((y & 3) << 2) | (x & 3)]) c.Px[y * c.W + x] = Pal.White;
        }
    }

    /// <summary>엔딩: 아이 모양 구멍이 난 가족사진.</summary>
    public sealed class EndingPhotoCutscene : Cutscene
    {
        float a;
        float zoom;

        public override IEnumerator Run()
        {
            g.Audio.SetMusic(Ch.Music, Music.MusicBox, 0.45f, 3f);
            yield return new Tween(2.0f, k => a = k);
            yield return g.Wait(1.0f);
            yield return new Tween(3.5f, k => zoom = M.Smooth(k));
            yield return g.SayAt(Speaker.Narr, "거실 벽의 가족사진.", BoxPos.Bottom);
            yield return g.SayAt(Speaker.Narr, "엄마와 아빠 사이에, 작은 아이 모양의 구멍.", BoxPos.Bottom);
            yield return g.Wait(1.0f);
            yield return new Tween(2.0f, k => a = 1 - k);
            g.Audio.StopMusic(Ch.Music, 2f);
            yield return g.Wait(1.0f);
        }

        public override void Draw(Canvas c)
        {
            Art.BeginHalf(Pal.Black);
            var h = Art.Half;
            h.FillRect(0, 0, 160, 120, Pal.Bone);
            h.DitherRect(0, 0, 160, 120, Pal.Tan, 4);
            Photo.Draw(h, 80, 56 + zoom * 6, Photo.Mom | Photo.Dad, g.Time);
            if (a < 1f) ScreenFx.ApplyFade(h, 1 - a);
            Art.EndHalf(c);
        }
    }

    /// <summary>크레딧.</summary>
    public sealed class CreditsState : IState
    {
        GameCore g;
        float t;
        bool done;
        static readonly string[] lines =
        {
            "가 위", "", "", "GAWI", "1장 · 404호", "", "", "",
            "기획 · 프로그래밍 · 그래픽 · 사운드", "404호 작업실", "", "",
            "한글 비트맵 폰트", "갈무리 (Galmuri) — 이민서", "SIL Open Font License 1.1", "", "",
            "영감을 준 게임", "FAITH — Airdorf Games", "", "",
            "모든 그림과 소리는", "코드로 만들었습니다.", "", "",
            "Claude Code와 함께 만듦", "", "", "", "",
            "플레이해 주셔서 감사합니다.", "", "", "", "", "",
            "아무도 기억하지 않는 아이는,", "어디에서 잠들까?",
        };

        public void Enter(GameCore game)
        {
            g = game;
            t = 0;
            g.Fx.Reset();
            g.Audio.StopAllMusic(0.5f);
            g.Audio.SetMusic(Ch.Music, Music.MusicBoxWarped, 0.5f, 2f);
        }

        public void Exit() { }

        public void Update(float dt)
        {
            t += dt;
            float speed = g.Input.Down(Btn.Confirm) ? 60f : 16f;
            scroll += dt * speed;
            if (!done && scroll > lines.Length * 16 + 300)
            {
                done = true;
                g.Scripts.Start(End(), "credits");
            }
            if (!done && t > 3f && g.Input.Pressed(Btn.Cancel))
            {
                done = true;
                g.Scripts.Start(End(), "credits");
            }
        }

        float scroll;

        IEnumerator End()
        {
            g.Audio.StopAllMusic(2f);
            yield return g.FadeOut(2f);
            yield return g.Wait(1f);
            g.SetState(new TitleState());
        }

        public void Draw(Canvas c)
        {
            c.Clear(Pal.Black);
            int y0 = GameCore.H + 10 - (int)scroll;
            for (int i = 0; i < lines.Length; i++)
            {
                int y = y0 + i * 16;
                if (y < -16 || y > GameCore.H) continue;
                string s = lines[i];
                if (s.Length == 0) continue;
                byte col = i < 5 ? Pal.Bone : (i >= lines.Length - 2 ? Pal.BloodBright : Pal.LightGray);
                if (i == 0)
                {
                    int w = Art.BigTextWidth(g.Font, s, 3);
                    Art.BigText(c, g.Font, s, (GameCore.W - w) / 2, y - 10, 3, Pal.Bone);
                    continue;
                }
                g.Font.DrawCentered(c, s, GameCore.W / 2, y, col);
            }
        }

        public void DrawUi(Canvas c) { }
    }
}
