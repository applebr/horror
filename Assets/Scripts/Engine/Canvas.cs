using System;

namespace Gawi.Engine
{
    /// <summary>
    /// 8비트 인덱스 컬러 캔버스. 모든 그래픽은 여기에 그린다.
    /// (0,0)은 왼쪽 위. OffX/OffY는 카메라 흔들림 등에 쓰는 그리기 오프셋.
    /// </summary>
    public sealed class Canvas
    {
        public readonly int W, H;
        public readonly byte[] Px;
        public int OffX, OffY;
        int cx0, cy0, cx1, cy1; // 클립 영역 (cx1, cy1은 제외)

        public static readonly int[] Bayer4 =
        {
            0, 8, 2, 10,
            12, 4, 14, 6,
            3, 11, 1, 9,
            15, 7, 13, 5
        };

        public Canvas(int w, int h)
        {
            W = w; H = h;
            Px = new byte[w * h];
            ResetClip();
        }

        public void ResetClip() { cx0 = 0; cy0 = 0; cx1 = W; cy1 = H; }

        public void SetClip(int x, int y, int w, int h)
        {
            cx0 = Math.Max(0, x); cy0 = Math.Max(0, y);
            cx1 = Math.Min(W, x + w); cy1 = Math.Min(H, y + h);
        }

        public void Clear(byte c)
        {
            for (int i = 0; i < Px.Length; i++) Px[i] = c;
        }

        public void CopyFrom(Canvas o)
        {
            Buffer.BlockCopy(o.Px, 0, Px, 0, Math.Min(Px.Length, o.Px.Length));
        }

        public static bool BayerPass(int x, int y, int level16)
        {
            return level16 > Bayer4[((y & 3) << 2) | (x & 3)];
        }

        public void Set(int x, int y, byte c)
        {
            x += OffX; y += OffY;
            if (x < cx0 || y < cy0 || x >= cx1 || y >= cy1) return;
            Px[y * W + x] = c;
        }

        /// <summary>오프셋/클립을 무시한 원시 읽기.</summary>
        public byte Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return 0;
            return Px[y * W + x];
        }

        public void FillRect(int x, int y, int w, int h, byte c)
        {
            x += OffX; y += OffY;
            int x0 = Math.Max(cx0, x), y0 = Math.Max(cy0, y);
            int x1 = Math.Min(cx1, x + w), y1 = Math.Min(cy1, y + h);
            for (int yy = y0; yy < y1; yy++)
            {
                int row = yy * W;
                for (int xx = x0; xx < x1; xx++) Px[row + xx] = c;
            }
        }

        /// <summary>베이어 디더링으로 일부 픽셀만 채운다. level16: 0(없음)~16(전부).</summary>
        public void DitherRect(int x, int y, int w, int h, byte c, int level16)
        {
            if (level16 <= 0) return;
            x += OffX; y += OffY;
            int x0 = Math.Max(cx0, x), y0 = Math.Max(cy0, y);
            int x1 = Math.Min(cx1, x + w), y1 = Math.Min(cy1, y + h);
            for (int yy = y0; yy < y1; yy++)
            {
                int row = yy * W;
                for (int xx = x0; xx < x1; xx++)
                    if (level16 > Bayer4[((yy & 3) << 2) | (xx & 3)]) Px[row + xx] = c;
            }
        }

        public void Rect(int x, int y, int w, int h, byte c)
        {
            if (w <= 0 || h <= 0) return;
            HLine(x, x + w - 1, y, c);
            HLine(x, x + w - 1, y + h - 1, c);
            VLine(x, y, y + h - 1, c);
            VLine(x + w - 1, y, y + h - 1, c);
        }

        public void HLine(int x0, int x1, int y, byte c)
        {
            if (x1 < x0) { int t = x0; x0 = x1; x1 = t; }
            FillRect(x0, y, x1 - x0 + 1, 1, c);
        }

        public void VLine(int x, int y0, int y1, byte c)
        {
            if (y1 < y0) { int t = y0; y0 = y1; y1 = t; }
            FillRect(x, y0, 1, y1 - y0 + 1, c);
        }

        public void Line(int x0, int y0, int x1, int y1, byte c)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int guard = 0;
            while (guard++ < 4096)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        /// <summary>점선. on/off 픽셀 수 지정.</summary>
        public void DashedLine(int x0, int y0, int x1, int y1, byte c, int on, int off, int phase)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int i = phase;
            int period = Math.Max(1, on + off);
            int guard = 0;
            while (guard++ < 4096)
            {
                if (((i % period) + period) % period < on) Set(x0, y0, c);
                i++;
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void FillCircle(float cx, float cy, float r, byte c)
        {
            if (r <= 0) return;
            int y0 = (int)Math.Floor(cy - r), y1 = (int)Math.Ceiling(cy + r);
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
            {
                float dy = y + 0.5f - cy;
                float d = r2 - dy * dy;
                if (d < 0) continue;
                float dx = (float)Math.Sqrt(d);
                int xa = (int)Math.Ceiling(cx - dx - 0.5f), xb = (int)Math.Floor(cx + dx - 0.5f);
                if (xb >= xa) FillRect(xa, y, xb - xa + 1, 1, c);
            }
        }

        public void DitherCircle(float cx, float cy, float r, byte c, int level16)
        {
            if (r <= 0 || level16 <= 0) return;
            int y0 = (int)Math.Floor(cy - r), y1 = (int)Math.Ceiling(cy + r);
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
            {
                float dy = y + 0.5f - cy;
                float d = r2 - dy * dy;
                if (d < 0) continue;
                float dx = (float)Math.Sqrt(d);
                int xa = (int)Math.Ceiling(cx - dx - 0.5f), xb = (int)Math.Floor(cx + dx - 0.5f);
                if (xb >= xa) DitherRect(xa, y, xb - xa + 1, 1, c, level16);
            }
        }

        public void Circle(float cx, float cy, float r, byte c)
        {
            int n = Math.Max(8, (int)(r * 6.3f));
            for (int i = 0; i < n; i++)
            {
                double a = i * Math.PI * 2 / n;
                Set((int)Math.Round(cx + Math.Cos(a) * r - 0.5), (int)Math.Round(cy + Math.Sin(a) * r - 0.5), c);
            }
        }

        public void FillEllipse(float cx, float cy, float rx, float ry, byte c)
        {
            if (rx <= 0 || ry <= 0) return;
            int y0 = (int)Math.Floor(cy - ry), y1 = (int)Math.Ceiling(cy + ry);
            for (int y = y0; y <= y1; y++)
            {
                float dy = (y + 0.5f - cy) / ry;
                float d = 1 - dy * dy;
                if (d < 0) continue;
                float dx = (float)Math.Sqrt(d) * rx;
                int xa = (int)Math.Ceiling(cx - dx - 0.5f), xb = (int)Math.Floor(cx + dx - 0.5f);
                if (xb >= xa) FillRect(xa, y, xb - xa + 1, 1, c);
            }
        }

        /// <summary>둥근 끝을 가진 두꺼운 선 (캡슐). 손가락, 팔다리 등.</summary>
        public void Capsule(float x0, float y0, float x1, float y1, float r0, float r1, byte c)
        {
            float rmax = Math.Max(r0, r1);
            int minX = (int)Math.Floor(Math.Min(x0, x1) - rmax - 1), maxX = (int)Math.Ceiling(Math.Max(x0, x1) + rmax + 1);
            int minY = (int)Math.Floor(Math.Min(y0, y1) - rmax - 1), maxY = (int)Math.Ceiling(Math.Max(y0, y1) + rmax + 1);
            float vx = x1 - x0, vy = y1 - y0;
            float len2 = vx * vx + vy * vy;
            for (int y = minY; y <= maxY; y++)
            {
                float py = y + 0.5f;
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f;
                    float t = len2 > 0.0001f ? ((px - x0) * vx + (py - y0) * vy) / len2 : 0f;
                    if (t < 0) t = 0; else if (t > 1) t = 1;
                    float qx = x0 + vx * t - px, qy = y0 + vy * t - py;
                    float r = r0 + (r1 - r0) * t;
                    if (qx * qx + qy * qy <= r * r) Set(x, y, c);
                }
            }
        }

        public void Capsule(float x0, float y0, float x1, float y1, float r, byte c)
        {
            Capsule(x0, y0, x1, y1, r, r, c);
        }

        static readonly float[] polyBuf = new float[128];

        /// <summary>스캔라인 다각형 채우기 (짝홀 규칙).</summary>
        public void FillPoly(float[] xs, float[] ys, int n, byte c)
        {
            if (n < 3) return;
            float minY = ys[0], maxY = ys[0];
            for (int i = 1; i < n; i++) { if (ys[i] < minY) minY = ys[i]; if (ys[i] > maxY) maxY = ys[i]; }
            int y0 = (int)Math.Floor(minY), y1 = (int)Math.Ceiling(maxY);
            for (int y = y0; y <= y1; y++)
            {
                float py = y + 0.5f;
                int cnt = 0;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    float yi = ys[i], yj = ys[j];
                    if ((yi <= py && yj > py) || (yj <= py && yi > py))
                    {
                        float x = xs[i] + (py - yi) / (yj - yi) * (xs[j] - xs[i]);
                        if (cnt < polyBuf.Length) polyBuf[cnt++] = x;
                    }
                }
                // 삽입 정렬 (교점 수가 적다)
                for (int i = 1; i < cnt; i++)
                {
                    float v = polyBuf[i]; int k = i - 1;
                    while (k >= 0 && polyBuf[k] > v) { polyBuf[k + 1] = polyBuf[k]; k--; }
                    polyBuf[k + 1] = v;
                }
                for (int i = 0; i + 1 < cnt; i += 2)
                {
                    int xa = (int)Math.Ceiling(polyBuf[i] - 0.5f), xb = (int)Math.Floor(polyBuf[i + 1] - 0.5f);
                    if (xb >= xa) FillRect(xa, y, xb - xa + 1, 1, c);
                }
            }
        }

        static readonly float[] triX = new float[3], triY = new float[3];
        public void FillTri(float ax, float ay, float bx, float by, float cx, float cy, byte c)
        {
            triX[0] = ax; triY[0] = ay; triX[1] = bx; triY[1] = by; triX[2] = cx; triY[2] = cy;
            FillPoly(triX, triY, 3, c);
        }

        static readonly float[] quadX = new float[4], quadY = new float[4];
        public void FillQuad(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy, byte c)
        {
            quadX[0] = ax; quadY[0] = ay; quadX[1] = bx; quadY[1] = by;
            quadX[2] = cx; quadY[2] = cy; quadX[3] = dx; quadY[3] = dy;
            FillPoly(quadX, quadY, 4, c);
        }

        // ───────────── 스프라이트 ─────────────

        public void Blit(Sprite s, int x, int y, bool flipX = false)
        {
            if (s == null) return;
            x -= s.OriginX; y -= s.OriginY;
            for (int sy = 0; sy < s.H; sy++)
            {
                int dy = y + sy + OffY;
                if (dy < cy0 || dy >= cy1) continue;
                int row = sy * s.W;
                for (int sx = 0; sx < s.W; sx++)
                {
                    byte c = s.Px[row + (flipX ? s.W - 1 - sx : sx)];
                    if (c == Pal.Transparent) continue;
                    int dx = x + sx + OffX;
                    if (dx < cx0 || dx >= cx1) continue;
                    Px[dy * W + dx] = c;
                }
            }
        }

        public void BlitRemap(Sprite s, int x, int y, bool flipX, byte[] remap)
        {
            if (s == null) return;
            x -= s.OriginX; y -= s.OriginY;
            for (int sy = 0; sy < s.H; sy++)
            {
                int dy = y + sy + OffY;
                if (dy < cy0 || dy >= cy1) continue;
                int row = sy * s.W;
                for (int sx = 0; sx < s.W; sx++)
                {
                    byte c = s.Px[row + (flipX ? s.W - 1 - sx : sx)];
                    if (c == Pal.Transparent) continue;
                    int dx = x + sx + OffX;
                    if (dx < cx0 || dx >= cx1) continue;
                    Px[dy * W + dx] = remap[c];
                }
            }
        }

        public void BlitSilhouette(Sprite s, int x, int y, bool flipX, byte color)
        {
            if (s == null) return;
            x -= s.OriginX; y -= s.OriginY;
            for (int sy = 0; sy < s.H; sy++)
            {
                int dy = y + sy + OffY;
                if (dy < cy0 || dy >= cy1) continue;
                int row = sy * s.W;
                for (int sx = 0; sx < s.W; sx++)
                {
                    byte c = s.Px[row + (flipX ? s.W - 1 - sx : sx)];
                    if (c == Pal.Transparent) continue;
                    int dx = x + sx + OffX;
                    if (dx < cx0 || dx >= cx1) continue;
                    Px[dy * W + dx] = color;
                }
            }
        }

        /// <summary>정수 배율 확대 스프라이트.</summary>
        public void BlitScaled(Sprite s, int x, int y, int scale, bool flipX = false)
        {
            if (s == null || scale < 1) return;
            x -= s.OriginX * scale; y -= s.OriginY * scale;
            for (int sy = 0; sy < s.H; sy++)
            {
                int row = sy * s.W;
                for (int sx = 0; sx < s.W; sx++)
                {
                    byte c = s.Px[row + (flipX ? s.W - 1 - sx : sx)];
                    if (c == Pal.Transparent) continue;
                    FillRect(x + sx * scale, y + sy * scale, scale, scale, c);
                }
            }
        }

        /// <summary>실수 배율 확대 (최근접). 컷신에서 다가오는 형체 등에 사용.</summary>
        public void BlitStretch(Sprite s, float x, float y, float sw, float sh, bool flipX = false)
        {
            if (s == null || sw < 1 || sh < 1) return;
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            int x1 = (int)Math.Ceiling(x + sw), y1 = (int)Math.Ceiling(y + sh);
            for (int dy = y0; dy < y1; dy++)
            {
                int sy = (int)((dy + 0.5f - y) / sh * s.H);
                if (sy < 0 || sy >= s.H) continue;
                for (int dx = x0; dx < x1; dx++)
                {
                    int sx = (int)((dx + 0.5f - x) / sw * s.W);
                    if (sx < 0 || sx >= s.W) continue;
                    byte c = s.Px[sy * s.W + (flipX ? s.W - 1 - sx : sx)];
                    if (c == Pal.Transparent) continue;
                    Set(dx, dy, c);
                }
            }
        }

        /// <summary>다른 캔버스를 정수 배율로 그대로 복사 (월드 → 화면).</summary>
        public void BlitCanvas(Canvas src, int dx, int dy, int scale)
        {
            dx += OffX; dy += OffY;
            for (int y = 0; y < src.H; y++)
            {
                for (int sy = 0; sy < scale; sy++)
                {
                    int ty = dy + y * scale + sy;
                    if (ty < cy0 || ty >= cy1) continue;
                    int srow = y * src.W, trow = ty * W;
                    for (int x = 0; x < src.W; x++)
                    {
                        byte c = src.Px[srow + x];
                        int tx = dx + x * scale;
                        for (int sx = 0; sx < scale; sx++, tx++)
                        {
                            if (tx < cx0 || tx >= cx1) continue;
                            Px[trow + tx] = c;
                        }
                    }
                }
            }
        }

        /// <summary>투명색을 건너뛰며 다른 캔버스를 덮어 그린다.</summary>
        public void BlitCanvasKeyed(Canvas src, int dx, int dy, byte key)
        {
            for (int y = 0; y < src.H; y++)
            {
                int ty = dy + y + OffY;
                if (ty < cy0 || ty >= cy1) continue;
                for (int x = 0; x < src.W; x++)
                {
                    byte c = src.Px[y * src.W + x];
                    if (c == key) continue;
                    int tx = dx + x + OffX;
                    if (tx < cx0 || tx >= cx1) continue;
                    Px[ty * W + tx] = c;
                }
            }
        }

        public void Remap(byte[] map)
        {
            for (int i = 0; i < Px.Length; i++) Px[i] = map[Px[i]];
        }

        public void RemapRect(int x, int y, int w, int h, byte[] map)
        {
            int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
            int x1 = Math.Min(W, x + w), y1 = Math.Min(H, y + h);
            for (int yy = y0; yy < y1; yy++)
            {
                int row = yy * W;
                for (int xx = x0; xx < x1; xx++) Px[row + xx] = map[Px[row + xx]];
            }
        }

        /// <summary>디더링으로 서서히 어둡게 (0~16 단계).</summary>
        public void DitherDarken(int x, int y, int w, int h, byte[] map, int level16)
        {
            int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
            int x1 = Math.Min(W, x + w), y1 = Math.Min(H, y + h);
            for (int yy = y0; yy < y1; yy++)
            {
                int row = yy * W;
                for (int xx = x0; xx < x1; xx++)
                    if (level16 > Bayer4[((yy & 3) << 2) | (xx & 3)]) Px[row + xx] = map[Px[row + xx]];
            }
        }
    }
}
