using System;
using Gawi.Engine;

namespace Gawi.Game
{
    public struct HandPose
    {
        public int Highlight;   // 세는 중인 손가락 (-1 없음)
        public float Flex;      // 그 손가락 굽힘 0..1
        public int Extra;       // 더 자라는 손가락 수
        public float Growth;    // 0..1 자라는 진행
        public float Blood;     // 0..1 흘러내린 피
        public float Tremble;   // 떨림
        public bool Pale;       // 창백한 손 (엔딩)
    }

    /// <summary>
    /// 코드로 그리는 그림들: 손, 타이틀, 얼굴들.
    /// 큰 그림은 160×120 절반 해상도에 그린 뒤 2배로 확대해 큰 픽셀을 유지한다.
    /// </summary>
    public static class Art
    {
        public static readonly Canvas Half = new Canvas(160, 120);

        public static void BeginHalf(byte clear)
        {
            Half.OffX = 0; Half.OffY = 0;
            Half.ResetClip();
            Half.Clear(clear);
        }

        public static void EndHalf(Canvas screen, int shakeX = 0, int shakeY = 0)
        {
            screen.BlitCanvas(Half, shakeX, shakeY, 2);
        }

        // ───────────────── 손 ─────────────────

        static readonly float[] FX = { -13f, -5.5f, 2f, 9.5f, 15.5f };
        static readonly float[] FY = { -11f, -15f, -16.5f, -14f, 3f };
        static readonly float[] FA = { -1.83f, -1.68f, -1.57f, -1.45f, -0.62f };
        static readonly float[] FL = { 19f, 25f, 27f, 24f, 19f };
        static readonly float[] FR = { 3.4f, 3.9f, 4.1f, 3.9f, 4.4f };

        static readonly float[] EX = { -18.5f, -13f, 5.8f };
        static readonly float[] EY = { -1f, 12f, -16.5f };
        static readonly float[] EA = { -2.55f, -2.95f, -1.52f };
        static readonly float[] EL = { 22f, 17f, 23f };
        static readonly float[] ER = { 3.5f, 3.2f, 3.4f };

        public static void DrawHand(Canvas c, float cx, float cy, float s, HandPose pose, float time)
        {
            byte fill = pose.Pale ? Pal.Bone : Pal.Skin;
            byte shade = pose.Pale ? Pal.LightGray : Pal.SkinShadow;
            byte light = pose.Pale ? Pal.White : Pal.SkinLight;
            if (pose.Tremble > 0)
            {
                cx += M.Sin(time * 47f) * pose.Tremble * s;
                cy += M.Cos(time * 39f) * pose.Tremble * s * 0.6f;
            }
            // 손목
            c.FillQuad(cx - 11.5f * s, cy + 9 * s, cx + 11.5f * s, cy + 9 * s, cx + 12.5f * s, cy + 80 * s, cx - 12.5f * s, cy + 80 * s, shade);
            c.FillQuad(cx - 10.5f * s, cy + 9 * s, cx + 10.5f * s, cy + 9 * s, cx + 11.5f * s, cy + 80 * s, cx - 11.5f * s, cy + 80 * s, fill);
            c.Line((int)(cx + 4 * s), (int)(cy + 20 * s), (int)(cx + 5 * s), (int)(cy + 60 * s), shade);
            // 손바닥
            c.FillEllipse(cx, cy, 19.2f * s, 18f * s, shade);
            c.FillEllipse(cx, cy, 18.2f * s, 17f * s, fill);
            c.FillEllipse(cx - 3 * s, cy - 5 * s, 9 * s, 6 * s, light);
            c.FillEllipse(cx - 2 * s, cy - 2 * s, 12 * s, 9 * s, fill);
            // 손금
            PalmLine(c, cx, cy, s, -14, -5, -2, -1, 8, 0, shade);
            PalmLine(c, cx, cy, s, -15, 1, -5, 4, 3, 8, shade);
            PalmLine(c, cx, cy, s, 9, -2, 6, 6, 1, 14, shade);
            // 손가락
            for (int i = 0; i < 5; i++)
            {
                bool hl = i == pose.Highlight;
                float flex = hl ? pose.Flex : 0f;
                Finger(c, cx + FX[i] * s, cy + FY[i] * s, FA[i], FL[i] * s, FR[i] * s, flex, hl, fill, shade, light);
            }
            // 더 자라는 손가락들
            for (int k = 0; k < Math.Min(3, pose.Extra); k++)
            {
                float gk = M.Clamp01(pose.Growth * (1 + k * 0.15f) - k * 0.1f);
                Extra(c, cx + EX[k] * s, cy + EY[k] * s, EA[k], EL[k] * s, ER[k] * s, gk, s, time, k);
            }
            // 흘러내리는 피
            if (pose.Blood > 0 && pose.Extra > 0)
            {
                float bx = cx + EX[0] * s, by = cy + EY[0] * s;
                for (int d = 0; d < 5; d++)
                {
                    float len = pose.Blood * (24 + d * 9) * s;
                    float x = bx + (2 + d * 3.2f) * s;
                    float y0 = by + 2 * s;
                    for (int y = 0; y < (int)len; y++)
                    {
                        float wob = M.Sin((y + d * 13) * 0.08f) * 1.2f * s;
                        int px = (int)(x + wob + y * 0.12f), py = (int)(y0 + y);
                        c.Set(px, py, Pal.Red);
                        if (s > 1.5f) c.Set(px + 1, py, Pal.DarkRed);
                    }
                    c.FillCircle(x + M.Sin((len + d * 13) * 0.08f) * 1.2f * s + len * 0.12f, y0 + len, 1.2f * s, Pal.Red);
                }
            }
        }

        static void PalmLine(Canvas c, float cx, float cy, float s, float x0, float y0, float x1, float y1, float x2, float y2, byte col)
        {
            c.Line((int)(cx + x0 * s), (int)(cy + y0 * s), (int)(cx + x1 * s), (int)(cy + y1 * s), col);
            c.Line((int)(cx + x1 * s), (int)(cy + y1 * s), (int)(cx + x2 * s), (int)(cy + y2 * s), col);
        }

        static void Finger(Canvas c, float bx, float by, float a, float len, float r, float flex, bool hl, byte fill, byte shade, byte light)
        {
            float k = 0.55f;
            float dx = M.Cos(a), dy = M.Sin(a);
            float kx = bx + dx * len * k, ky = by + dy * len * k;
            float a2 = a + flex * 0.5f;
            float len2 = len * (1 - k) * (1 - flex * 0.65f);
            float tx = kx + M.Cos(a2) * len2, ty = ky + M.Sin(a2) * len2;
            byte edge = hl ? Pal.White : shade;
            float o = r < 6 ? 1f : 1.6f;
            c.Capsule(bx, by, kx, ky, r + o, r * 0.95f + o, edge);
            c.Capsule(kx, ky, tx, ty, r * 0.95f + o, r * 0.85f + o, edge);
            c.Capsule(bx, by, kx, ky, r, r * 0.95f, fill);
            c.Capsule(kx, ky, tx, ty, r * 0.95f, r * 0.85f, fill);
            // 빛 받는 쪽
            float pxx = dy, pyy = -dx;
            c.Line((int)(bx + pxx * r * 0.45f), (int)(by + pyy * r * 0.45f), (int)(tx + pxx * r * 0.35f), (int)(ty + pyy * r * 0.35f), light);
            // 마디 주름
            c.Line((int)(kx - pxx * r * 0.6f), (int)(ky - pyy * r * 0.6f), (int)(kx + pxx * r * 0.6f), (int)(ky + pyy * r * 0.6f), shade);
            float mx = bx + dx * len * 0.25f, my = by + dy * len * 0.25f;
            c.Line((int)(mx - pxx * r * 0.5f), (int)(my - pyy * r * 0.5f), (int)(mx + pxx * r * 0.5f), (int)(my + pyy * r * 0.5f), shade);
        }

        static void Extra(Canvas c, float bx, float by, float a, float len, float r, float g, float s, float time, int k)
        {
            if (g <= 0) return;
            float dx = M.Cos(a), dy = M.Sin(a);
            if (g < 0.3f)
            {
                // 살이 부풀어 오른다
                float br = (g / 0.3f) * r * 1.4f;
                c.FillCircle(bx, by, br + 1, Pal.SkinShadow);
                c.FillCircle(bx, by, br, Pal.Flesh);
                c.FillCircle(bx - br * 0.3f, by - br * 0.3f, br * 0.4f, Pal.Pink);
                int cracks = (int)(g / 0.3f * 4);
                for (int i = 0; i < cracks; i++)
                {
                    float ca = i * 1.7f + k;
                    c.Line((int)bx, (int)by, (int)(bx + M.Cos(ca) * br), (int)(by + M.Sin(ca) * br), Pal.Red);
                }
                return;
            }
            float open = M.Clamp01((g - 0.3f) / 0.15f);
            float er = r * (0.9f + open * 0.6f);
            // 찢어진 살: 들쭉날쭉한 고리
            for (int i = 0; i < 28; i++)
            {
                float aa = i * M.TAU / 28;
                float rr = er * (1.35f + M.Hash(i, k, 77) * 0.6f);
                float px = bx + M.Cos(aa) * rr, py = by + M.Sin(aa) * rr * 0.85f;
                c.FillCircle(px, py, Math.Max(1, s * 1.1f), M.Hash(i, k, 78) > 0.5f ? Pal.Flesh : Pal.Pink);
            }
            c.FillEllipse(bx, by, er + 1.5f * s, er * 0.8f + 1.5f * s, Pal.Red);
            c.FillEllipse(bx, by, er, er * 0.75f, Pal.Maroon);
            // 튄 핏방울
            for (int i = 0; i < 10; i++)
            {
                float aa = M.Hash(i, k, 90) * M.TAU, dd = er * (1.8f + M.Hash(i, k, 91) * 3f);
                c.FillCircle(bx + M.Cos(aa) * dd, by + M.Sin(aa) * dd, Math.Max(0.6f, s * (0.5f + M.Hash(i, k, 92))), Pal.Red);
            }
            if (g < 0.45f) return;
            float grow = M.EaseOut((g - 0.45f) / 0.55f);
            float l = len * grow;
            float tx = bx + dx * l, ty = by + dy * l;
            // 축축하고 창백한 새 손가락 (손톱이 반대쪽에 났다)
            c.Capsule(bx, by, tx, ty, r + 1, r * 0.8f + 1, Pal.Flesh);
            c.Capsule(bx, by, tx, ty, r, r * 0.8f, Pal.SkinLight);
            float pxx = dy, pyy = -dx;
            c.Line((int)(bx - pxx * r * 0.3f), (int)(by - pyy * r * 0.3f), (int)(tx - pxx * r * 0.3f), (int)(ty - pyy * r * 0.3f), Pal.Red);
            c.Line((int)(bx + pxx * r * 0.5f), (int)(by + pyy * r * 0.5f), (int)(bx + pxx * r * 0.5f + dx * l * 0.6f), (int)(by + pyy * r * 0.5f + dy * l * 0.6f), Pal.Pink);
            if (grow > 0.6f)
            {
                float nx = tx - dx * r * 0.6f, ny = ty - dy * r * 0.6f;
                c.FillEllipse(nx, ny, r * 0.7f, r * 0.7f, Pal.Maroon);
                c.FillEllipse(nx - dx, ny - dy, r * 0.4f, r * 0.4f, Pal.DarkRed);
            }
            // 여러 마디
            for (int j = 1; j <= 2; j++)
            {
                float jx = bx + dx * l * j / 3f, jy = by + dy * l * j / 3f;
                c.Line((int)(jx - pxx * r * 0.7f), (int)(jy - pyy * r * 0.7f), (int)(jx + pxx * r * 0.7f), (int)(jy + pyy * r * 0.7f), Pal.Flesh);
            }
            // 테두리 피
            c.Circle(bx, by, er + 1, Pal.BloodBright);
        }

        // ───────────────── 큰 글씨 ─────────────────

        static readonly System.Collections.Generic.Dictionary<string, Canvas> bigCache = new System.Collections.Generic.Dictionary<string, Canvas>();

        /// <summary>폰트 픽셀을 scale배 블록으로 찍는다 (타이틀용).</summary>
        public static void BigText(Canvas c, BitmapFont f, string s, int x, int y, int scale, byte color)
        {
            Canvas tmp;
            if (!bigCache.TryGetValue(s, out tmp))
            {
                if (bigCache.Count > 64) bigCache.Clear();
                tmp = new Canvas(f.Measure(s) + 2, f.VisualAscent + f.Descent + 4);
                tmp.Clear(Pal.Transparent);
                f.Draw(tmp, s, 0, 0, 1);
                bigCache[s] = tmp;
            }
            for (int ty = 0; ty < tmp.H; ty++)
                for (int tx = 0; tx < tmp.W; tx++)
                {
                    if (tmp.Px[ty * tmp.W + tx] == Pal.Transparent) continue;
                    c.FillRect(x + tx * scale, y + ty * scale, scale, scale, color);
                }
        }

        public static int BigTextWidth(BitmapFont f, string s, int scale) { return f.Measure(s) * scale; }

        // ───────────────── 타이틀 ─────────────────

        public static void DrawTitle(Canvas c, GameCore g, float t)
        {
            BeginHalf(Pal.Black);
            var h = Half;
            // 방의 벽
            h.FillRect(0, 0, 160, 120, Pal.Ink);
            h.DitherRect(0, 0, 160, 120, Pal.Night, 5);
            // 창문 (빨간 달)
            h.FillRect(104, 18, 34, 30, Pal.Night);
            h.Rect(103, 17, 36, 32, Pal.DarkGray);
            h.VLine(121, 17, 48, Pal.DarkGray);
            h.HLine(103, 138, 33, Pal.DarkGray);
            float moonPulse = 0.5f + 0.5f * M.Sin(t * 0.7f);
            h.FillCircle(128, 26, 4.5f, Pal.DarkRed);
            h.FillCircle(128, 26, 3f, moonPulse > 0.85f ? Pal.BloodBright : Pal.Red);
            h.FillCircle(128, 26, 1.2f, Pal.Black); // 달이 아니라 눈
            // 바닥
            h.FillQuad(0, 84, 160, 84, 160, 120, 0, 120, Pal.DarkBrown);
            h.DitherRect(0, 84, 160, 36, Pal.Black, 6);
            // 침대 (발치에서 본 원근)
            h.FillQuad(30, 76, 112, 76, 128, 112, 14, 112, Pal.Brown);
            h.FillQuad(34, 72, 108, 72, 120, 104, 22, 104, Pal.LightGray);
            float breath = M.Sin(t * 1.3f) * 1.2f;
            h.FillQuad(26, 86 - breath, 116, 86 - breath, 122, 106, 20, 106, Pal.Pink);
            h.DitherRect(20, 96, 102, 10, Pal.SkinShadow, 6);
            // 베개와 아이
            h.FillEllipse(71, 76, 16, 5, Pal.White);
            h.FillEllipse(71, 75, 7, 5, Pal.Ink);
            h.FillEllipse(71, 77, 5, 3.5f, Pal.Skin);
            h.Set(69, 77, Pal.Ink); h.Set(73, 77, Pal.Ink);
            // 형체 (깜빡이며 나타났다 사라진다)
            float vis = M.ValueNoise(t * 0.8f, 3, 9);
            bool show = vis > 0.42f || (t % 7f) < 0.15f;
            if (show)
            {
                float sway = M.Sin(t * 0.6f) * 1.5f;
                DrawTallFigure(h, 40 + sway, 84, 1.0f, t, vis > 0.6f ? Pal.Black : Pal.Ink);
            }
            // 먼지
            for (int i = 0; i < 14; i++)
            {
                float px = (M.Hash(i, 1, 3) * 160 + t * (2 + i % 3)) % 160;
                float py = (M.Hash(i, 2, 3) * 100 + M.Sin(t * 0.5f + i) * 4) % 100;
                h.Set((int)px, (int)py, Pal.DarkGray);
            }
            EndHalf(c);
            // 제목
            var f = g.Font;
            int scale = 5;
            string title = "가 위";
            int w = BigTextWidth(f, title, scale);
            int tx = (GameCore.W - w) / 2, ty = 22;
            BigText(c, f, title, tx + 3, ty + 3, scale, Pal.Black);
            BigText(c, f, title, tx, ty, scale, Pal.Bone);
            // 절취선 + 가위
            int ly = ty + 30;
            int phase = (int)(t * 10);
            c.DashedLine(tx - 14, ly, tx + w + 10, ly, Pal.BloodBright, 5, 3, -phase);
            DrawScissorsIcon(c, tx + w + 12 - (int)(M.Frac(t * 0.2f) * 6), ly - 4, Pal.LightGray);
            // 잘린 자리에서 흐르는 피
            for (int i = 0; i < 9; i++)
            {
                float hx = M.Hash(i, 5, 2);
                int x = tx + (int)(hx * w);
                int len = (int)(4 + M.Hash(i, 6, 2) * 18 + (t * (1 + hx)) % 6);
                c.VLine(x, ly + 1, ly + len, Pal.Red);
                c.Set(x, ly + len + 1, Pal.DarkRed);
            }
            g.FontSmall.DrawCentered(c, "G A W I", GameCore.W / 2, ty + 68, Pal.Gray);
            g.FontSmall.DrawCentered(c, "1장 · 404호", GameCore.W / 2, ty + 82, Pal.DarkRed);
        }

        public static void DrawScissorsIcon(Canvas c, int x, int y, byte col)
        {
            c.Circle(x + 2, y + 2, 2, col);
            c.Circle(x + 2, y + 7, 2, col);
            c.Line(x + 4, y + 3, x + 12, y + 6, col);
            c.Line(x + 4, y + 6, x + 12, y + 3, col);
            c.Set(x + 8, y + 4, Pal.White);
        }

        /// <summary>테두리 빛을 받은 캡슐 (왼쪽에서 빛).</summary>
        static void RimCapsule(Canvas h, float x0, float y0, float x1, float y1, float r0, float r1, byte body, byte rim)
        {
            h.Capsule(x0 - 1, y0, x1 - 1, y1, r0, r1, rim);
            h.Capsule(x0, y0, x1, y1, r0, r1, body);
        }

        /// <summary>천장에 닿는 마른 형체 (가위). 절반 해상도용. 왼쪽 창문빛을 받아 윤곽이 드러난다.</summary>
        public static void DrawTallFigure(Canvas h, float x, float groundY, float sc, float t, byte body)
        {
            byte rim = Pal.DarkGray;
            byte rim2 = Pal.Gray;
            float headY = groundY - 80 * sc;
            float tilt = 6 * sc;
            float sw = M.Sin(t * 0.9f) * 2 * sc;
            // 다리 (가늘고 길다, 무릎이 살짝 꺾임)
            RimCapsule(h, x - 3 * sc, groundY - 36 * sc, x - 5 * sc, groundY - 17 * sc, 1.5f * sc, 1.2f * sc, body, rim);
            RimCapsule(h, x - 5 * sc, groundY - 17 * sc, x - 4 * sc, groundY, 1.2f * sc, 1.0f * sc, body, rim);
            RimCapsule(h, x + 3 * sc, groundY - 36 * sc, x + 5 * sc, groundY - 17 * sc, 1.5f * sc, 1.2f * sc, body, rim);
            RimCapsule(h, x + 5 * sc, groundY - 17 * sc, x + 4 * sc, groundY, 1.2f * sc, 1.0f * sc, body, rim);
            // 몸통 (갈비뼈가 드러난 듯 좁다)
            h.FillQuad(x - 6 * sc - 1, headY + 15 * sc, x + 5 * sc, headY + 15 * sc, x + 4 * sc, groundY - 34 * sc, x - 4 * sc - 1, groundY - 34 * sc, rim);
            h.FillQuad(x - 6 * sc, headY + 15 * sc, x + 6 * sc, headY + 15 * sc, x + 4 * sc, groundY - 34 * sc, x - 4 * sc, groundY - 34 * sc, body);
            for (int i = 0; i < 4; i++)
            {
                float ry = headY + (20 + i * 5) * sc;
                h.Line((int)(x - 4 * sc), (int)ry, (int)(x - 1 * sc), (int)(ry + 1 * sc), rim);
            }
            // 팔: 무릎 아래까지 늘어진다
            RimCapsule(h, x - 6 * sc, headY + 16 * sc, x - 9 * sc + sw * 0.5f, headY + 40 * sc, 1.3f * sc, 1.1f * sc, body, rim2);
            RimCapsule(h, x - 9 * sc + sw * 0.5f, headY + 40 * sc, x - 11 * sc + sw, groundY - 14 * sc, 1.1f * sc, 0.9f * sc, body, rim2);
            RimCapsule(h, x + 6 * sc, headY + 16 * sc, x + 9 * sc - sw * 0.5f, headY + 40 * sc, 1.3f * sc, 1.1f * sc, body, rim);
            RimCapsule(h, x + 9 * sc - sw * 0.5f, headY + 40 * sc, x + 11 * sc - sw, groundY - 16 * sc, 1.1f * sc, 0.9f * sc, body, rim);
            // 긴 손가락 (창백)
            float hx = x - 11 * sc + sw, hy = groundY - 14 * sc;
            for (int i = -2; i <= 2; i++)
                h.Line((int)hx, (int)hy, (int)(hx + i * 1.4f * sc), (int)(hy + (7 + Math.Abs(i)) * sc), Pal.Bone);
            // 가위
            float gx = x + 11 * sc - sw, gy = groundY - 16 * sc;
            float open = 0.35f + 0.65f * Math.Max(0, M.Sin(t * 1.7f));
            h.Circle(gx - 2 * sc, gy - 2.5f * sc, 1.6f * sc, Pal.Gray);
            h.Circle(gx + 2 * sc, gy - 2.5f * sc, 1.6f * sc, Pal.Gray);
            h.FillTri(gx - 0.5f * sc, gy, gx + 0.8f * sc, gy, gx - open * 4.5f * sc, gy + 16 * sc, Pal.LightGray);
            h.FillTri(gx - 0.8f * sc, gy, gx + 0.5f * sc, gy, gx + open * 4.5f * sc, gy + 16 * sc, Pal.White);
            h.Set((int)gx, (int)gy, Pal.DarkGray);
            // 목 (꺾임)과 머리
            RimCapsule(h, x, headY + 15 * sc, x + tilt * 0.6f, headY + 9 * sc, 1.3f * sc, 1.1f * sc, body, rim);
            DrawPaperFace(h, x + tilt, headY + 4 * sc, 5.2f * sc, 6.5f * sc, t, 0.4f, 0.35f);
        }

        /// <summary>
        /// 구겨진 종이 얼굴: 칼로 그은 눈, 톱니처럼 오려 낸 입, 입 안의 살과 이빨.
        /// tiltRad로 기울일 수 있다.
        /// </summary>
        public static void DrawPaperFace(Canvas h, float cx, float cy, float rx, float ry, float t, float mouth, float tiltRad)
        {
            h.FillEllipse(cx + 1, cy + 1, rx, ry, Pal.Gray);
            h.FillEllipse(cx, cy, rx, ry, Pal.Bone);
            if (rx < 4)
            {
                h.Set((int)(cx - rx * 0.4f), (int)(cy - ry * 0.1f), Pal.Black);
                h.Set((int)(cx + rx * 0.4f), (int)(cy - ry * 0.1f), Pal.Black);
                h.HLine((int)(cx - rx * 0.5f), (int)(cx + rx * 0.5f), (int)(cy + ry * 0.45f), Pal.Black);
                h.Set((int)cx, (int)(cy + ry * 0.45f), Pal.Red);
                return;
            }
            // 구김
            int creases = (int)(rx * 1.2f);
            for (int i = 0; i < creases; i++)
            {
                float a = M.Hash(i, 1, 4) * M.TAU;
                float r0 = M.Hash(i, 2, 4) * 0.7f, r1 = r0 + 0.15f + M.Hash(i, 3, 4) * 0.3f;
                h.Line((int)(cx + M.Cos(a) * r0 * rx), (int)(cy + M.Sin(a) * r0 * ry), (int)(cx + M.Cos(a + 0.4f) * r1 * rx), (int)(cy + M.Sin(a + 0.4f) * r1 * ry), Pal.LightGray);
            }
            // 칼로 그은 눈 (가는 틈 + 흘러내리는 피)
            float ey = cy - ry * 0.18f;
            float ew = rx * 0.38f;
            for (int s = -1; s <= 1; s += 2)
            {
                float ex = cx + s * rx * 0.42f;
                h.FillQuad(ex - ew, ey - s * 0.6f, ex + ew, ey + s * 0.6f, ex + ew, ey + s * 0.6f + Math.Max(1, ry * 0.05f), ex - ew, ey - s * 0.6f + Math.Max(1, ry * 0.05f), Pal.Black);
                int dripLen = (int)(ry * (0.15f + 0.1f * M.Sin(t * 0.7f + s)));
                h.VLine((int)ex, (int)ey + 1, (int)ey + 1 + dripLen, Pal.Red);
            }
            // 톱니 입
            int n = 12;
            var xs = mouthXs; var ys = mouthYs;
            float mw = rx * 0.75f, mtop = cy + ry * 0.28f, open = ry * (0.08f + mouth * 0.32f);
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / (n - 1);
                float x = cx - mw + k * mw * 2;
                float smile = (float)Math.Sin(k * Math.PI) * ry * 0.12f;
                xs[i] = x; ys[i] = mtop + smile + ((i % 2) == 0 ? -1 : 0.6f);
                xs[n * 2 - 1 - i] = x; ys[n * 2 - 1 - i] = mtop + smile * 1.6f + open + ((i % 2) == 0 ? 1 : -0.6f);
            }
            h.FillPoly(xs, ys, n * 2, Pal.Black);
            if (open > 2)
            {
                h.FillEllipse(cx, mtop + open * 0.6f + ry * 0.08f, mw * 0.6f, open * 0.3f + 0.5f, Pal.Maroon);
                h.FillEllipse(cx + mw * 0.1f, mtop + open * 0.65f + ry * 0.08f, mw * 0.3f, open * 0.16f + 0.5f, Pal.DarkRed);
                int teeth = Math.Max(3, (int)(mw / 2.2f));
                for (int i = 0; i < teeth; i++)
                {
                    float tx = cx - mw * 0.8f + i * (mw * 1.6f / teeth);
                    float tw = Math.Max(1, mw * 1.6f / teeth - 1);
                    h.FillRect((int)tx, (int)(mtop + 1 + Math.Abs(i - teeth / 2) * 0.2f), (int)tw, (int)Math.Max(1, open * 0.25f), Pal.Bone);
                }
            }
        }

        static readonly float[] mouthXs = new float[24];
        static readonly float[] mouthYs = new float[24];

        // ───────────────── 얼굴들 ─────────────────

        /// <summary>가위의 얼굴: 구겨진 종이, 오려 낸 입. 화면 가득.</summary>
        public static void DrawGawiFace(Canvas c, float t, float intensity)
        {
            BeginHalf(Pal.Black);
            var h = Half;
            float cx = 80 + M.Sin(t * 13f) * intensity, cy = 58;
            DrawPaperFace(h, cx, cy, 50, 62, t, 1f, 0);
            EndHalf(c);
        }

        /// <summary>거울 속 하나: 찢어진 웃음.</summary>
        public static void DrawMirrorFace(Canvas c, float t)
        {
            BeginHalf(Pal.Black);
            var h = Half;
            h.FillEllipse(80, 50, 40, 46, Pal.Ink);
            h.FillEllipse(80, 66, 30, 36, Pal.Skin);
            h.FillRect(44, 20, 72, 30, Pal.Ink);
            h.FillEllipse(66, 62, 6, 4, Pal.Black);
            h.FillEllipse(94, 62, 6, 4, Pal.Black);
            h.Set(66, 62, Pal.White); h.Set(94, 62, Pal.White);
            // 귀까지 찢어진 입
            h.Line(52, 78, 108, 78, Pal.DarkRed);
            h.Line(52, 79, 108, 79, Pal.Red);
            h.Line(52, 78, 46, 70, Pal.Red);
            h.Line(108, 78, 114, 70, Pal.Red);
            for (int x = 54; x < 108; x += 4) { h.Set(x, 77, Pal.Black); h.Set(x + 1, 80, Pal.Black); }
            for (int i = 0; i < 6; i++) h.VLine(56 + i * 10, 80, 84 + (int)(M.Hash(i, 0, 1) * 10), Pal.Red);
            EndHalf(c);
        }

        /// <summary>엄마 사진을 붙인 얼굴이 벗겨지며 드러나는 입.</summary>
        public static void DrawMotherFace(Canvas c, float t, float peel)
        {
            BeginHalf(Pal.Black);
            var h = Half;
            // 살
            h.FillEllipse(80, 62, 40, 52, Pal.Flesh);
            for (int i = 0; i < 60; i++)
            {
                float x = 80 + (M.Hash(i, 1, 8) - 0.5f) * 70, y = 62 + (M.Hash(i, 2, 8) - 0.5f) * 90;
                h.FillCircle(x, y, 1 + M.Hash(i, 3, 8) * 3, M.Hash(i, 4, 8) > 0.5f ? Pal.Red : Pal.DarkRed);
            }
            // 머리카락: 얼굴을 감싸고 어깨까지
            h.FillEllipse(80, 26, 50, 26, Pal.DarkBrown);
            h.FillQuad(30, 26, 46, 26, 44, 110, 22, 110, Pal.DarkBrown);
            h.FillQuad(114, 26, 130, 26, 138, 110, 116, 110, Pal.DarkBrown);
            h.Line(40, 30, 32, 108, Pal.Brown);
            h.Line(120, 30, 128, 108, Pal.Brown);
            h.FillEllipse(80, 14, 30, 8, Pal.Brown);
            // 세로로 갈라진 입
            float open = M.Clamp01(peel * 1.4f);
            if (open > 0)
            {
                h.FillEllipse(80, 66, 6 + open * 9, 10 + open * 30, Pal.Maroon);
                h.FillEllipse(80, 66, 3 + open * 6, 8 + open * 26, Pal.Black);
                for (int i = 0; i < 10; i++)
                {
                    float y = 66 - (8 + open * 26) + i * (16 + open * 52) / 10f;
                    h.FillRect((int)(80 - 5 - open * 7), (int)y, 4, 2, Pal.Bone);
                    h.FillRect((int)(80 + 2 + open * 7), (int)y + 2, 4, 2, Pal.Bone);
                }
            }
            // 테이프로 붙인 사진 (벗겨진다)
            float px = 80 + peel * 70, py = 58 + peel * 40, rot = peel * 1.2f;
            if (peel < 0.98f)
            {
                float w = 32, hh = 38;
                float cs = M.Cos(rot), sn = M.Sin(rot);
                Func<float, float, float> X = (u, v) => px + u * cs - v * sn;
                Func<float, float, float> Y = (u, v) => py + u * sn + v * cs;
                h.FillQuad(X(-w / 2, -hh / 2), Y(-w / 2, -hh / 2), X(w / 2, -hh / 2), Y(w / 2, -hh / 2), X(w / 2, hh / 2), Y(w / 2, hh / 2), X(-w / 2, hh / 2), Y(-w / 2, hh / 2), Pal.Bone);
                // 사진 속 엄마의 웃는 얼굴
                h.FillEllipse(X(0, 2), Y(0, 2), 10, 13, Pal.SkinLight);
                h.FillEllipse(X(0, -10), Y(0, -10), 12, 6, Pal.DarkBrown);
                h.Set((int)X(-4, 0), (int)Y(-4, 0), Pal.Black);
                h.Set((int)X(4, 0), (int)Y(4, 0), Pal.Black);
                h.Line((int)X(-3, 7), (int)Y(-3, 7), (int)X(3, 7), (int)Y(3, 7), Pal.Red);
                // 테이프
                h.FillQuad(X(-w / 2 - 4, -hh / 2 + 2), Y(-w / 2 - 4, -hh / 2 + 2), X(-w / 2 + 8, -hh / 2 - 4), Y(-w / 2 + 8, -hh / 2 - 4), X(-w / 2 + 10, -hh / 2), Y(-w / 2 + 10, -hh / 2), X(-w / 2 - 2, -hh / 2 + 6), Y(-w / 2 - 2, -hh / 2 + 6), Pal.LightGray);
            }
            EndHalf(c);
        }
    }
}
