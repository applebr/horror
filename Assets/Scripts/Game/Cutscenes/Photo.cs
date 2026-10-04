using System;
using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>
    /// 가족사진 그리기 (절반 해상도). 엄마(왼쪽), 하나(가운데), 아빠(오른쪽).
    /// 각 인물은 있거나, 사람 모양 구멍으로 오려져 있다.
    /// </summary>
    public static class Photo
    {
        public const int Mom = 1, Hana = 2, Dad = 4;

        /// <summary>present: 사진에 남아 있는 사람들. 나머지는 구멍.</summary>
        public static void Draw(Canvas h, float cx, float cy, int present, float t, bool frame = true)
        {
            int w = 96, hh = 70;
            int x0 = (int)(cx - w / 2), y0 = (int)(cy - hh / 2);
            if (frame)
            {
                h.FillRect(x0 - 6, y0 - 6, w + 12, hh + 12, Pal.DarkBrown);
                h.Rect(x0 - 6, y0 - 6, w + 12, hh + 12, Pal.Brown);
                h.Rect(x0 - 4, y0 - 4, w + 8, hh + 8, Pal.Tan);
            }
            // 사진 배경 (빛바랜 사진관 배경)
            h.FillRect(x0, y0, w, hh, Pal.Tan);
            h.DitherRect(x0, y0, w, hh / 2, Pal.Bone, 5);
            h.DitherRect(x0, y0 + hh * 2 / 3, w, hh / 3, Pal.Brown, 7);
            DrawFigure(h, x0 + 24, y0 + hh, 0, (present & Mom) != 0);
            DrawFigure(h, x0 + 72, y0 + hh, 2, (present & Dad) != 0);
            DrawFigure(h, x0 + 48, y0 + hh, 1, (present & Hana) != 0);
            // 빛바램
            for (int i = 0; i < 30; i++)
            {
                int px = x0 + (int)(M.Hash(i, 3, 17) * w), py = y0 + (int)(M.Hash(i, 4, 17) * hh);
                h.Set(px, py, Pal.Bone);
            }
        }

        /// <summary>who: 0 엄마, 1 하나, 2 아빠.</summary>
        static void DrawFigure(Canvas h, int fx, int baseY, int who, bool present)
        {
            if (!present)
            {
                // 사람 모양 구멍 + 오린 자리의 흰 테두리
                Silhouette(h, fx, baseY, who, Pal.Bone, 1);
                Silhouette(h, fx, baseY, who, Pal.Black, 0);
                return;
            }
            switch (who)
            {
                case 0: // 엄마: 긴 머리, 베이지 옷
                    h.FillQuad(fx - 12, baseY, fx + 12, baseY, fx + 9, baseY - 24, fx - 9, baseY - 24, Pal.Bone);
                    h.FillQuad(fx - 11, baseY, fx + 11, baseY, fx + 8, baseY - 23, fx - 8, baseY - 23, Pal.Pink);
                    h.FillEllipse(fx, baseY - 36, 9, 10, Pal.DarkBrown);
                    h.FillRect(fx - 9, baseY - 36, 18, 14, Pal.DarkBrown);
                    h.FillEllipse(fx, baseY - 34, 6, 8, Pal.SkinLight);
                    h.Set(fx - 2, baseY - 35, Pal.Black); h.Set(fx + 2, baseY - 35, Pal.Black);
                    h.HLine(fx - 2, fx + 2, baseY - 31, Pal.Red);
                    break;
                case 1: // 하나: 단발, 원피스
                    h.FillQuad(fx - 8, baseY, fx + 8, baseY, fx + 6, baseY - 16, fx - 6, baseY - 16, Pal.Yellow);
                    h.FillEllipse(fx, baseY - 23, 7, 7, Pal.Ink);
                    h.FillEllipse(fx, baseY - 21, 5, 5.5f, Pal.SkinLight);
                    h.FillRect(fx - 6, baseY - 27, 12, 4, Pal.Ink);
                    h.Set(fx - 2, baseY - 21, Pal.Black); h.Set(fx + 2, baseY - 21, Pal.Black);
                    h.HLine(fx - 1, fx + 1, baseY - 18, Pal.Red);
                    break;
                default: // 아빠: 키 크다, 남색 양복
                    h.FillQuad(fx - 13, baseY, fx + 13, baseY, fx + 10, baseY - 30, fx - 10, baseY - 30, Pal.DarkBlue);
                    h.FillTri(fx - 3, baseY - 30, fx + 3, baseY - 30, fx, baseY - 22, Pal.White);
                    h.FillEllipse(fx, baseY - 40, 7, 9, Pal.Skin);
                    h.FillRect(fx - 7, baseY - 50, 14, 5, Pal.Ink);
                    h.Set(fx - 2, baseY - 41, Pal.Black); h.Set(fx + 2, baseY - 41, Pal.Black);
                    h.HLine(fx - 2, fx + 2, baseY - 36, Pal.SkinShadow);
                    // 하나 어깨 위의 손
                    h.Capsule(fx - 9, baseY - 26, fx - 20, baseY - 16, 1.6f, Pal.DarkBlue);
                    h.FillCircle(fx - 21, baseY - 15, 1.8f, Pal.Skin);
                    break;
            }
        }

        static void Silhouette(Canvas h, int fx, int baseY, int who, byte col, int grow)
        {
            switch (who)
            {
                case 0:
                    h.FillQuad(fx - 12 - grow, baseY, fx + 12 + grow, baseY, fx + 9 + grow, baseY - 24, fx - 9 - grow, baseY - 24, col);
                    h.FillEllipse(fx, baseY - 36, 9 + grow, 10 + grow, col);
                    h.FillRect(fx - 9 - grow, baseY - 36, 18 + grow * 2, 14, col);
                    break;
                case 1:
                    h.FillQuad(fx - 8 - grow, baseY, fx + 8 + grow, baseY, fx + 6 + grow, baseY - 16, fx - 6 - grow, baseY - 16, col);
                    h.FillEllipse(fx, baseY - 23, 7 + grow, 7 + grow, col);
                    h.FillRect(fx - 6 - grow, baseY - 27 - grow, 12 + grow * 2, 6, col);
                    break;
                default:
                    h.FillQuad(fx - 13 - grow, baseY, fx + 13 + grow, baseY, fx + 10 + grow, baseY - 30, fx - 10 - grow, baseY - 30, col);
                    h.FillEllipse(fx, baseY - 40, 7 + grow, 9 + grow, col);
                    h.FillRect(fx - 7 - grow, baseY - 50 - grow, 14 + grow * 2, 7, col);
                    h.Capsule(fx - 9, baseY - 26, fx - 20, baseY - 16, 1.6f + grow, col);
                    break;
            }
        }

        /// <summary>찢어진 조각 하나 (0: 왼쪽 엄마, 1: 가운데 하나, 2: 오른쪽 구멍).</summary>
        public static void DrawPiece(Canvas h, float cx, float cy, int part, float t)
        {
            // 사진 전체를 임시로 그린 뒤 해당 부분만 잘라 낸다
            var tmp = Scratch;
            tmp.Clear(Pal.Transparent);
            Draw(tmp, 48, 35, Mom | Hana, t, false);
            int sx0 = part == 0 ? 0 : (part == 1 ? 33 : 62);
            int sx1 = part == 0 ? 34 : (part == 1 ? 63 : 96);
            float tilt = (part - 1) * 0.08f;
            float w = sx1 - sx0;
            for (int y = 0; y < 70; y++)
                for (int x = sx0; x < sx1; x++)
                {
                    // 톱니 모양 찢긴 가장자리
                    float edgeL = sx0 + (part == 0 ? 0 : M.Hash(0, y, 9 + part) * 3);
                    float edgeR = sx1 - (part == 2 ? 0 : M.Hash(1, y, 9 + part) * 3);
                    if (x < edgeL || x > edgeR) continue;
                    byte p = tmp.Get(x, y);
                    if (p == Pal.Transparent) continue;
                    float lx = x - sx0 - w / 2, ly = y - 35;
                    int dx = (int)(cx + lx * M.Cos(tilt) - ly * M.Sin(tilt));
                    int dy = (int)(cy + lx * M.Sin(tilt) + ly * M.Cos(tilt));
                    h.Set(dx, dy, p);
                    h.Set(dx + 1, dy, p);
                }
        }

        static Canvas scratch;
        static Canvas Scratch { get { if (scratch == null) scratch = new Canvas(96, 70); return scratch; } }
    }
}
