using System;

namespace Gawi.Engine
{
    /// <summary>xorshift 기반 결정적 난수.</summary>
    public sealed class Rng
    {
        uint s;

        public Rng(uint seed)
        {
            s = seed == 0 ? 0x9E3779B9u : seed;
        }

        public uint NextU()
        {
            uint x = s;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            s = x;
            return x;
        }

        /// <summary>[0, max)</summary>
        public int Next(int max)
        {
            if (max <= 0) return 0;
            return (int)(NextU() % (uint)max);
        }

        /// <summary>[a, b] 정수</summary>
        public int Range(int a, int b)
        {
            if (b < a) { int t = a; a = b; b = t; }
            return a + Next(b - a + 1);
        }

        public float Float()
        {
            return (NextU() >> 8) * (1f / 16777216f);
        }

        public float Range(float a, float b)
        {
            return a + (b - a) * Float();
        }

        public bool Chance(float p)
        {
            return Float() < p;
        }

        public float Signed()
        {
            return Float() * 2f - 1f;
        }

        public T Pick<T>(T[] arr)
        {
            return arr[Next(arr.Length)];
        }
    }

    public static class M
    {
        public const float PI = 3.14159265f;
        public const float TAU = 6.28318531f;

        public static float Clamp(float v, float a, float b) { return v < a ? a : (v > b ? b : v); }
        public static int Clamp(int v, int a, int b) { return v < a ? a : (v > b ? b : v); }
        public static float Clamp01(float v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
        public static float InvLerp(float a, float b, float v) { return Math.Abs(b - a) < 1e-6f ? 0f : (v - a) / (b - a); }
        public static float Smooth(float t) { t = Clamp01(t); return t * t * (3 - 2 * t); }
        public static float EaseOut(float t) { t = Clamp01(t); return 1 - (1 - t) * (1 - t); }
        public static float EaseIn(float t) { t = Clamp01(t); return t * t; }
        public static float Sin(float a) { return (float)Math.Sin(a); }
        public static float Cos(float a) { return (float)Math.Cos(a); }
        public static float Sqrt(float a) { return a <= 0 ? 0 : (float)Math.Sqrt(a); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Abs(float a) { return a < 0 ? -a : a; }
        public static float Sign(float a) { return a > 0 ? 1 : (a < 0 ? -1 : 0); }
        public static float Pow(float a, float b) { return (float)Math.Pow(a, b); }
        public static float Exp(float a) { return (float)Math.Exp(a); }
        public static int Floor(float a) { return (int)Math.Floor(a); }
        public static int Round(float a) { return (int)Math.Round(a); }
        public static float Frac(float a) { return a - (float)Math.Floor(a); }

        public static float Approach(float cur, float target, float delta)
        {
            if (cur < target) return Math.Min(cur + delta, target);
            return Math.Max(cur - delta, target);
        }

        public static float Dist(float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0, dy = y1 - y0;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static float Dist2(float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0, dy = y1 - y0;
            return dx * dx + dy * dy;
        }

        /// <summary>각도 차이 (-PI..PI)</summary>
        public static float AngleDiff(float a, float b)
        {
            float d = (b - a) % TAU;
            if (d > PI) d -= TAU;
            if (d < -PI) d += TAU;
            return d;
        }

        /// <summary>애니메이션을 n fps로 끊어 손으로 그린 느낌을 낸다.</summary>
        public static float Step(float t, float fps)
        {
            return (float)Math.Floor(t * fps) / fps;
        }

        /// <summary>정수 해시 노이즈 (0..1)</summary>
        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        public static float ValueNoise(float x, float y, int seed)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed);
            float c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Lerp(Lerp(a, b, fx), Lerp(c, d, fx), fy);
        }
    }
}
