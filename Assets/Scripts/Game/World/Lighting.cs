using System;
using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>
    /// 월드 캔버스(160×120) 픽셀별 밝기 계산.
    /// 주변광 + 플레이어 주변의 작은 빛 + 벽에 가려지는 손전등 원뿔 + 고정 광원.
    /// 결과는 베이어 디더링된 어둠 단계로 팔레트를 리맵한다.
    /// </summary>
    public sealed class Lighting
    {
        const int W = GameCore.WorldW, H = GameCore.WorldH;
        public readonly float[] L = new float[W * H];
        readonly Canvas beamMask = new Canvas(W, H);
        public const float BeamLen = 72f;
        public const float BeamHalf = 0.5f;
        const int Rays = 56;
        readonly float[] polyX = new float[Rays + 2];
        readonly float[] polyY = new float[Rays + 2];
        public float AuraRadius = 17f;
        public float AuraIntensity = 0.55f;
        public float BeamIntensity = 1.05f;

        public float BeamOX, BeamOY, BeamDX, BeamDY;
        public bool BeamActive;

        public void Compute(PlayState ps)
        {
            var room = ps.Room;
            var pl = ps.Player;
            int ox = room.OX, oy = room.OY;
            int rw = room.PxW, rh = room.PxH;
            float amb = room.Ambient;
            for (int i = 0; i < L.Length; i++) L[i] = 0;
            for (int y = Math.Max(0, oy); y < Math.Min(H, oy + rh); y++)
                for (int x = Math.Max(0, ox); x < Math.Min(W, ox + rw); x++) L[y * W + x] = amb;

            // 고정 광원
            float t = room.Time;
            foreach (var li in room.Lights)
            {
                if (!li.On) continue;
                float inten = li.I;
                if (li.Flicker > 0) inten *= 1f - li.Flicker * (0.5f + 0.5f * M.ValueNoise(t * 9f, li.X, 3));
                AddRadial(ox + li.X, oy + li.Y, li.R, inten);
            }

            if (pl != null && !pl.Hidden)
            {
                float px = ox + pl.X, py = oy + pl.ChestY;
                AddRadial(px, py, AuraRadius, AuraIntensity);
                UpdateBeam(ps);
                if (BeamActive) CastBeam(ps, BeamOX, BeamOY, BeamDX, BeamDY);
            }
            else BeamActive = false;
        }

        /// <summary>손전등 원뿔의 위치/방향 (게임 로직용, 매 틱 갱신).</summary>
        public void UpdateBeam(PlayState ps)
        {
            var pl = ps.Player;
            BeamActive = pl != null && pl.Beam;
            if (!BeamActive) return;
            float dx = pl.AimX, dy = pl.AimY;
            BeamOX = pl.X + dx * 2; BeamOY = pl.ChestY + dy * 2;
            BeamDX = dx; BeamDY = dy;
        }

        void AddRadial(float cx, float cy, float r, float inten)
        {
            int x0 = Math.Max(0, (int)(cx - r)), x1 = Math.Min(W - 1, (int)(cx + r));
            int y0 = Math.Max(0, (int)(cy - r)), y1 = Math.Min(H - 1, (int)(cy + r));
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
            {
                float dy = y + 0.5f - cy;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx;
                    float d2 = dx * dx + dy * dy;
                    if (d2 >= r2) continue;
                    float k = 1f - (float)Math.Sqrt(d2) / r;
                    L[y * W + x] += inten * k * k * (3 - 2 * k);
                }
            }
        }

        void CastBeam(PlayState ps, float bx, float by, float dx, float dy)
        {
            var room = ps.Room;
            int ox = room.OX, oy = room.OY;
            float baseA = M.Atan2(dy, dx);
            polyX[0] = ox + bx; polyY[0] = oy + by;
            for (int i = 0; i <= Rays; i++)
            {
                float a = baseA - BeamHalf + 2 * BeamHalf * i / Rays;
                float rx = M.Cos(a), ry = M.Sin(a);
                float len = BeamLen;
                for (float s = 2; s <= BeamLen; s += 1.5f)
                {
                    float qx = bx + rx * s, qy = by + ry * s;
                    int cx = (int)Math.Floor(qx / Room.CS), cy = (int)Math.Floor(qy / Room.CS);
                    if (room.Opaque(cx, cy))
                    {
                        // 벽 표면도 조금 비추도록 반 칸 더
                        len = s + 2.5f;
                        break;
                    }
                }
                polyX[i + 1] = ox + bx + rx * len;
                polyY[i + 1] = oy + by + ry * len;
            }
            beamMask.Clear(0);
            for (int i = 1; i <= Rays; i++)
                beamMask.FillTri(polyX[0], polyY[0], polyX[i], polyY[i], polyX[i + 1], polyY[i + 1], 1);
            float ocx = ox + bx, ocy = oy + by;
            float cosHalf = M.Cos(BeamHalf);
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (beamMask.Px[i] == 0) continue;
                    float vx = x + 0.5f - ocx, vy = y + 0.5f - ocy;
                    float d = (float)Math.Sqrt(vx * vx + vy * vy);
                    if (d < 0.5f) { L[i] += BeamIntensity; continue; }
                    float cosA = (vx * dx + vy * dy) / d;
                    float edge = M.Clamp01((cosA - cosHalf) / (1f - cosHalf) * 2.2f);
                    float fall = 1f - d / BeamLen;
                    L[i] += BeamIntensity * edge * (0.35f + 0.65f * fall);
                }
            }
        }

        /// <summary>방 좌표의 밝기.</summary>
        public float At(PlayState ps, float rx, float ry)
        {
            int x = ps.Room.OX + (int)rx, y = ps.Room.OY + (int)ry;
            if (x < 0 || y < 0 || x >= W || y >= H) return 0;
            return L[y * W + x];
        }

        /// <summary>손전등이 그 지점을 비추는가 (원뿔 + 벽 가림).</summary>
        public bool InBeam(PlayState ps, float rx, float ry)
        {
            if (!BeamActive) return false;
            float vx = rx - BeamOX, vy = ry - BeamOY;
            float d = (float)Math.Sqrt(vx * vx + vy * vy);
            if (d > BeamLen + 4) return false;
            if (d < 6) return true;
            float cosA = (vx * BeamDX + vy * BeamDY) / d;
            if (cosA < M.Cos(BeamHalf + 0.08f)) return false;
            return ps.Room.LineOfSight(BeamOX, BeamOY, rx, ry);
        }

        public void Apply(Canvas c, byte[][] ramps)
        {
            int last = Pal.Levels - 1;
            for (int y = 0; y < H; y++)
            {
                int row = y * W;
                for (int x = 0; x < W; x++)
                {
                    int i = row + x;
                    float l = L[i];
                    if (l >= 1f) continue;
                    if (l <= 0f) { c.Px[i] = ramps[last][c.Px[i]]; continue; }
                    float dark = (1f - l) * last;
                    int lv = (int)dark;
                    if ((dark - lv) * 16f > Canvas.Bayer4[((y & 3) << 2) | (x & 3)]) lv++;
                    if (lv > last) lv = last;
                    if (lv > 0) c.Px[i] = ramps[lv][c.Px[i]];
                }
            }
        }
    }
}
