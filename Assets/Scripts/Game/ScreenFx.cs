using System;
using System.Collections;
using System.Collections.Generic;
using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>화면(320×240)에 튀어 흘러내리는 피.</summary>
    public sealed class ScreenBlood
    {
        public readonly int W, H;
        readonly byte[] layer;
        readonly Rng rng = new Rng(777);
        bool any;

        struct Drip { public float X, Y, Vy, Life; public byte C; public int Wd; }
        readonly List<Drip> drips = new List<Drip>();
        float dissolve;

        public ScreenBlood(int w, int h)
        {
            W = w; H = h;
            layer = new byte[w * h];
            Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < layer.Length; i++) layer[i] = Pal.Transparent;
            drips.Clear();
            any = false;
            dissolve = 0;
        }

        public bool Active { get { return any; } }

        void Put(int x, int y, byte c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            layer[y * W + x] = c;
            any = true;
        }

        void Blob(float cx, float cy, float r, byte c)
        {
            int x0 = (int)(cx - r - 1), x1 = (int)(cx + r + 1), y0 = (int)(cy - r - 1), y1 = (int)(cy + r + 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float rr = r * (0.75f + 0.35f * M.Hash(x, y, 3));
                    if (dx * dx + dy * dy <= rr * rr) Put(x, y, c);
                }
        }

        /// <summary>화면에 피가 튄다. amount 0..1</summary>
        public void Splash(float cx, float cy, float amount)
        {
            dissolve = 0;
            int blobs = (int)(6 + amount * 40);
            for (int i = 0; i < blobs; i++)
            {
                float a = rng.Range(0, M.TAU);
                float d = rng.Range(0, 40 + 140 * amount) * (rng.Chance(0.3f) ? 1.6f : 1f);
                float x = cx + M.Cos(a) * d, y = cy + M.Sin(a) * d * 0.8f;
                float r = rng.Range(1.5f, 4f + 10f * amount) * (1 - d / 400f);
                byte c = rng.Chance(0.7f) ? Pal.Red : (rng.Chance(0.5f) ? Pal.DarkRed : Pal.BloodBright);
                Blob(x, y, Math.Max(1, r), c);
                // 작은 튀김
                int specks = rng.Range(2, 7);
                for (int k = 0; k < specks; k++)
                {
                    float sa = a + rng.Range(-0.4f, 0.4f);
                    float sd = r + rng.Range(2, 14);
                    Put((int)(x + M.Cos(sa) * sd), (int)(y + M.Sin(sa) * sd), c);
                }
                if (r > 2.5f && rng.Chance(0.75f))
                {
                    drips.Add(new Drip
                    {
                        X = x + rng.Range(-r * 0.5f, r * 0.5f),
                        Y = y + r * 0.7f,
                        Vy = rng.Range(6f, 22f),
                        Life = rng.Range(1.5f, 6f),
                        C = c,
                        Wd = r > 6 ? 2 : 1
                    });
                }
            }
        }

        public void StartDissolve() { dissolve = 0.0001f; }

        public void Update(float dt)
        {
            for (int i = drips.Count - 1; i >= 0; i--)
            {
                var d = drips[i];
                float oy = d.Y;
                d.Vy = Math.Max(2f, d.Vy * (1 - dt * 0.25f));
                d.Y += d.Vy * dt;
                d.Life -= dt;
                for (int y = (int)oy; y <= (int)d.Y; y++)
                    for (int w = 0; w < d.Wd; w++) Put((int)d.X + w, y, d.C);
                if (rng.Chance(dt * 0.6f)) d.X += rng.Chance(0.5f) ? 1 : -1;
                if (d.Life <= 0 || d.Y >= H) { drips.RemoveAt(i); continue; }
                drips[i] = d;
            }
            if (dissolve > 0 && any)
            {
                dissolve += dt;
                int n = (int)(layer.Length * dt * 0.5f * (0.5f + dissolve));
                for (int k = 0; k < n; k++) layer[rng.Next(layer.Length)] = Pal.Transparent;
                if (dissolve > 4f) Clear();
            }
        }

        public void Draw(Canvas c)
        {
            if (!any) return;
            for (int i = 0; i < layer.Length; i++)
                if (layer[i] != Pal.Transparent) c.Px[i] = layer[i];
        }
    }

    /// <summary>흔들림, 플래시, 페이드, 노이즈, 서브리미널 등 화면 전체 효과.</summary>
    public sealed class ScreenFx
    {
        readonly GameCore g;
        readonly Rng rng = new Rng(4242);
        public readonly ScreenBlood Blood;

        float shakeAmp, shakeTime, shakeDur;
        int flashFrames;
        byte flashColor;
        public float Fade;            // 0 = 보임, 1 = 완전히 검정
        public float Static;          // 0..1 노이즈
        public float StaticFlicker;   // 순간 노이즈 (감쇠)
        public float Fear;            // 0..1 붉은 비네트
        public float Glitch;          // 0..1 가로줄 어긋남
        public float Vignette = 0.5f; // 상시 비네트 강도
        public byte[] Tint;           // 팔레트 색조 (null = 원래)
        Action<Canvas> subliminal;
        int subliminalFrames;
        public bool Invert;

        public ScreenFx(GameCore game)
        {
            g = game;
            Blood = new ScreenBlood(GameCore.W, GameCore.H);
        }

        public void Shake(float amp, float time)
        {
            if (g.Settings.ReduceFlash) amp *= 0.5f;
            if (amp >= shakeAmp * (shakeDur > 0 ? shakeTime / shakeDur : 0))
            {
                shakeAmp = amp; shakeTime = time; shakeDur = time;
            }
        }

        public void Flash(byte color, int frames)
        {
            if (g.Settings.ReduceFlash)
            {
                // 밝은 플래시 대신 짧고 어두운 붉은 색조
                flashColor = Pal.Maroon;
                flashFrames = Math.Min(frames, 2) + 1;
                return;
            }
            flashColor = color;
            flashFrames = frames + 1;
        }

        /// <summary>1~2 프레임 동안만 다른 그림을 보여준다.</summary>
        public void Subliminal(Action<Canvas> draw, int frames)
        {
            if (g.Settings.ReduceFlash) frames = Math.Min(frames, 1);
            subliminal = draw;
            subliminalFrames = frames + 1;
        }

        public void Reset()
        {
            shakeAmp = shakeTime = 0;
            flashFrames = 0;
            Fade = 0; Static = 0; StaticFlicker = 0; Fear = 0; Glitch = 0;
            Tint = null; subliminal = null; subliminalFrames = 0; Invert = false;
            Blood.Clear();
        }

        public void Update(float dt)
        {
            if (shakeTime > 0) shakeTime -= dt;
            // 플래시/서브리미널은 렌더 횟수가 아니라 시간(틱)으로 줄어든다
            if (flashFrames > 0) flashFrames--;
            if (subliminalFrames > 0) { subliminalFrames--; if (subliminalFrames <= 0) subliminal = null; }
            StaticFlicker = Math.Max(0, StaticFlicker - dt * 2.5f);
            Blood.Update(dt);
        }

        public void GetShake(out int x, out int y)
        {
            if (shakeTime <= 0 || shakeDur <= 0) { x = 0; y = 0; return; }
            float a = shakeAmp * (shakeTime / shakeDur);
            x = (int)Math.Round(rng.Signed() * a);
            y = (int)Math.Round(rng.Signed() * a);
        }

        public IEnumerator FadeTo(float target, float seconds)
        {
            float start = Fade;
            return FadeRoutine(start, target, seconds);
        }

        IEnumerator FadeRoutine(float start, float target, float seconds)
        {
            yield return new Tween(seconds, p => Fade = M.Lerp(start, target, p));
        }

        /// <summary>렌더 마지막 단계: 화면 캔버스에 후처리 적용.</summary>
        public void Apply(Canvas c)
        {
            if (subliminalFrames > 0 && subliminal != null) subliminal(c);
            Blood.Draw(c);
            if (Glitch > 0.01f) ApplyGlitch(c);
            float st = Math.Max(Static, StaticFlicker);
            if (st > 0.01f) ApplyStatic(c, st);
            if (Fear > 0.01f) ApplyFear(c);
            if (Vignette > 0.01f) ApplyVignette(c);
            if (Fade > 0.001f) ApplyFade(c, Fade);
            if (flashFrames > 0)
            {
                if (flashColor == Pal.Maroon) c.Remap(Pal.RedTint);
                else c.Clear(flashColor);
            }
        }

        void ApplyStatic(Canvas c, float amount)
        {
            int f = g.Frame;
            int threshold = (int)(amount * 255);
            for (int i = 0; i < c.Px.Length; i++)
            {
                uint h = (uint)(i * 2654435761u + f * 40503u);
                h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
                if ((h & 255) < threshold)
                {
                    uint v = (h >> 8) & 3;
                    c.Px[i] = v == 0 ? Pal.Black : (v == 1 ? Pal.Gray : (v == 2 ? Pal.LightGray : Pal.White));
                }
            }
        }

        void ApplyGlitch(Canvas c)
        {
            int rows = (int)(Glitch * 12);
            for (int k = 0; k < rows; k++)
            {
                int y = rng.Next(c.H);
                int h = rng.Range(1, 6);
                int sh = rng.Range(-12, 12);
                for (int yy = y; yy < Math.Min(c.H, y + h); yy++)
                {
                    int row = yy * c.W;
                    if (sh > 0)
                        for (int x = c.W - 1; x >= sh; x--) c.Px[row + x] = c.Px[row + x - sh];
                    else if (sh < 0)
                        for (int x = 0; x < c.W + sh; x++) c.Px[row + x] = c.Px[row + x - sh];
                }
            }
        }

        void ApplyFear(Canvas c)
        {
            // 가장자리부터 붉게 물든다
            float pulse = 0.85f + 0.15f * M.Sin(g.Time * 8f);
            for (int y = 0; y < c.H; y++)
            {
                float dy = Math.Abs(y - c.H * 0.5f) / (c.H * 0.5f);
                for (int x = 0; x < c.W; x++)
                {
                    float dx = Math.Abs(x - c.W * 0.5f) / (c.W * 0.5f);
                    float d = Math.Max(dx, dy);
                    float k = (d - (1f - Fear * 0.7f)) / 0.3f * pulse;
                    if (k <= 0) continue;
                    int lvl = (int)(k * 16);
                    if (lvl > Canvas.Bayer4[((y & 3) << 2) | (x & 3)])
                    {
                        int i = y * c.W + x;
                        c.Px[i] = Pal.RedTint[c.Px[i]];
                        if (k > 0.8f) c.Px[i] = Pal.DarkNight[2][c.Px[i]];
                    }
                }
            }
        }

        void ApplyVignette(Canvas c)
        {
            for (int y = 0; y < c.H; y++)
            {
                float dy = (y - c.H * 0.5f) / (c.H * 0.5f);
                for (int x = 0; x < c.W; x++)
                {
                    float dx = (x - c.W * 0.5f) / (c.W * 0.5f);
                    float d = dx * dx * 0.7f + dy * dy;
                    float k = (d - 0.55f) * Vignette * 1.4f;
                    if (k <= 0) continue;
                    float lv = k * 4f;
                    int li = (int)lv;
                    if ((lv - li) * 16 > Canvas.Bayer4[((y & 3) << 2) | (x & 3)]) li++;
                    if (li <= 0) continue;
                    if (li > Pal.Levels - 1) li = Pal.Levels - 1;
                    int i = y * c.W + x;
                    c.Px[i] = Pal.DarkNight[li][c.Px[i]];
                }
            }
        }

        public static void ApplyFade(Canvas c, float fade)
        {
            if (fade >= 0.999f) { c.Clear(Pal.Black); return; }
            float lv = fade * (Pal.Levels - 1);
            int baseL = (int)lv;
            int frac = (int)((lv - baseL) * 16);
            var lo = Pal.DarkNight[Math.Min(Pal.Levels - 1, baseL)];
            var hi = Pal.DarkNight[Math.Min(Pal.Levels - 1, baseL + 1)];
            for (int y = 0; y < c.H; y++)
            {
                int row = y * c.W;
                for (int x = 0; x < c.W; x++)
                {
                    int i = row + x;
                    c.Px[i] = frac > Canvas.Bayer4[((y & 3) << 2) | (x & 3)] ? hi[c.Px[i]] : lo[c.Px[i]];
                }
            }
        }
    }
}
