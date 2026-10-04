using System;

namespace Gawi.Engine.Audio
{
    /// <summary>오프라인 합성용 DSP 도구 모음. 모든 소리는 모노 float[] (−1..1).</summary>
    public static class Dsp
    {
        public const int SR = 22050;
        const int TableBits = 12;
        const int TableSize = 1 << TableBits;
        static readonly float[] sineTable = BuildSine();

        static float[] BuildSine()
        {
            var t = new float[TableSize + 1];
            for (int i = 0; i <= TableSize; i++) t[i] = (float)Math.Sin(i * 2.0 * Math.PI / TableSize);
            return t;
        }

        public static int Samples(float seconds) { return Math.Max(1, (int)(seconds * SR)); }
        public static float[] Buf(float seconds) { return new float[Samples(seconds)]; }

        /// <summary>phase는 주기 단위 (1.0 = 한 바퀴).</summary>
        public static float Sin(double phase)
        {
            double p = phase - Math.Floor(phase);
            double f = p * TableSize;
            int i = (int)f;
            float fr = (float)(f - i);
            return sineTable[i] + (sineTable[i + 1] - sineTable[i]) * fr;
        }

        public static float Saw(double phase) { double p = phase - Math.Floor(phase); return (float)(p * 2.0 - 1.0); }
        public static float Square(double phase, float duty) { double p = phase - Math.Floor(phase); return p < duty ? 1f : -1f; }
        public static float Tri(double phase) { double p = phase - Math.Floor(phase); return (float)(p < 0.5 ? p * 4 - 1 : 3 - p * 4); }

        public static float Env(float t, float a, float d)
        {
            if (t < 0) return 0;
            if (t < a) return t / a;
            return (float)Math.Exp(-(t - a) / Math.Max(0.0001f, d));
        }

        /// <summary>attack, hold, release(선형) 사다리꼴 엔벨로프.</summary>
        public static float Ahr(float t, float a, float h, float r)
        {
            if (t < 0) return 0;
            if (t < a) return t / a;
            if (t < a + h) return 1;
            if (t < a + h + r) return 1 - (t - a - h) / r;
            return 0;
        }

        public static float SoftClip(float x) { return (float)Math.Tanh(x); }

        public static void Normalize(float[] b, float peak)
        {
            float m = 0;
            for (int i = 0; i < b.Length; i++) { float a = Math.Abs(b[i]); if (a > m) m = a; }
            if (m < 1e-6f) return;
            float g = peak / m;
            for (int i = 0; i < b.Length; i++) b[i] *= g;
        }

        public static void Gain(float[] b, float g)
        {
            for (int i = 0; i < b.Length; i++) b[i] *= g;
        }

        public static void Drive(float[] b, float drive)
        {
            for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * drive);
        }

        public static void Mix(float[] dst, float[] src, int offset, float gain)
        {
            for (int i = 0; i < src.Length; i++)
            {
                int j = i + offset;
                if (j < 0) continue;
                if (j >= dst.Length) break;
                dst[j] += src[i] * gain;
            }
        }

        public static void FadeEdges(float[] b, float fadeIn, float fadeOut)
        {
            int fi = Samples(fadeIn), fo = Samples(fadeOut);
            for (int i = 0; i < fi && i < b.Length; i++) b[i] *= (float)i / fi;
            for (int i = 0; i < fo && i < b.Length; i++) b[b.Length - 1 - i] *= (float)i / fo;
        }

        public static void BitCrush(float[] b, int levels, int hold)
        {
            float held = 0;
            for (int i = 0; i < b.Length; i++)
            {
                if (i % Math.Max(1, hold) == 0) held = (float)Math.Round(b[i] * levels) / levels;
                b[i] = held;
            }
        }

        public static float[] Concat(float[] a, float[] b)
        {
            var r = new float[a.Length + b.Length];
            Array.Copy(a, r, a.Length);
            Array.Copy(b, 0, r, a.Length, b.Length);
            return r;
        }

        public static float[] Reverse(float[] a)
        {
            var r = new float[a.Length];
            for (int i = 0; i < a.Length; i++) r[i] = a[a.Length - 1 - i];
            return r;
        }

        public static float[] Pad(float[] a, float seconds)
        {
            var r = new float[a.Length + Samples(seconds)];
            Array.Copy(a, r, a.Length);
            return r;
        }

        /// <summary>
        /// 길이 len짜리 루프를 만든다: src는 len + xfade 길이여야 하고,
        /// 끝부분 xfade 구간을 앞부분에 크로스페이드해서 이음매를 없앤다.
        /// </summary>
        public static float[] MakeLoop(float[] src, int len)
        {
            int xf = src.Length - len;
            var r = new float[len];
            Array.Copy(src, r, len);
            for (int i = 0; i < xf && i < len; i++)
            {
                float t = (float)i / xf;
                r[i] = r[i] * t + src[len + i] * (1 - t);
            }
            return r;
        }

        /// <summary>스테레오가 아닌 간단한 슈뢰더 리버브 (꼬리 포함 길이로 반환).</summary>
        public static float[] Reverb(float[] input, float room, float damp, float wet, float tail)
        {
            var output = new float[input.Length + Samples(tail)];
            int[] combD = { 1116, 1188, 1277, 1356 };
            int[] apD = { 556, 341 };
            // 22050Hz 기준으로 대략 절반 길이
            for (int i = 0; i < combD.Length; i++) combD[i] = combD[i] / 2;
            for (int i = 0; i < apD.Length; i++) apD[i] = apD[i] / 2;
            var combs = new float[combD.Length][];
            var combIdx = new int[combD.Length];
            var combLp = new float[combD.Length];
            for (int i = 0; i < combD.Length; i++) combs[i] = new float[combD[i]];
            var aps = new float[apD.Length][];
            var apIdx = new int[apD.Length];
            for (int i = 0; i < apD.Length; i++) aps[i] = new float[apD[i]];
            float fb = 0.70f + room * 0.28f;
            for (int n = 0; n < output.Length; n++)
            {
                float x = n < input.Length ? input[n] : 0f;
                float acc = 0;
                for (int c = 0; c < combs.Length; c++)
                {
                    var buf = combs[c];
                    float y = buf[combIdx[c]];
                    combLp[c] = y * (1 - damp) + combLp[c] * damp;
                    buf[combIdx[c]] = x * 0.25f + combLp[c] * fb;
                    if (++combIdx[c] >= buf.Length) combIdx[c] = 0;
                    acc += y;
                }
                for (int a = 0; a < aps.Length; a++)
                {
                    var buf = aps[a];
                    float bo = buf[apIdx[a]];
                    float v = acc + bo * 0.5f;
                    buf[apIdx[a]] = v;
                    acc = bo - v * 0.5f;
                    if (++apIdx[a] >= buf.Length) apIdx[a] = 0;
                }
                output[n] = x * (1 - wet * 0.5f) + acc * wet;
            }
            return output;
        }

        public static float[] Echo(float[] input, float delay, float feedback, float wet, float tail)
        {
            var output = new float[input.Length + Samples(tail)];
            var e = new float[output.Length];
            int d = Math.Max(1, Samples(delay));
            for (int i = 0; i < output.Length; i++)
            {
                float x = i < input.Length ? input[i] : 0f;
                if (i >= d)
                {
                    float xd = i - d < input.Length ? input[i - d] : 0f;
                    e[i] = xd + e[i - d] * feedback;
                }
                output[i] = x + e[i] * wet;
            }
            return output;
        }
    }

    /// <summary>RBJ 바이쿼드 필터.</summary>
    public struct Biquad
    {
        float b0, b1, b2, a1, a2, z1, z2;

        public static Biquad LowPass(float f, float q) { var b = new Biquad(); b.SetLowPass(f, q); return b; }
        public static Biquad HighPass(float f, float q) { var b = new Biquad(); b.SetHighPass(f, q); return b; }
        public static Biquad BandPass(float f, float q) { var b = new Biquad(); b.SetBandPass(f, q); return b; }

        static void Coef(float f, float q, out double w, out double alpha, out double cos)
        {
            f = Math.Max(10f, Math.Min(f, Dsp.SR * 0.45f));
            w = 2 * Math.PI * f / Dsp.SR;
            cos = Math.Cos(w);
            alpha = Math.Sin(w) / (2 * Math.Max(0.05f, q));
        }

        public void SetLowPass(float f, float q)
        {
            double w, alpha, cos; Coef(f, q, out w, out alpha, out cos);
            double a0 = 1 + alpha;
            b0 = (float)((1 - cos) / 2 / a0); b1 = (float)((1 - cos) / a0); b2 = b0;
            a1 = (float)(-2 * cos / a0); a2 = (float)((1 - alpha) / a0);
        }

        public void SetHighPass(float f, float q)
        {
            double w, alpha, cos; Coef(f, q, out w, out alpha, out cos);
            double a0 = 1 + alpha;
            b0 = (float)((1 + cos) / 2 / a0); b1 = (float)(-(1 + cos) / a0); b2 = b0;
            a1 = (float)(-2 * cos / a0); a2 = (float)((1 - alpha) / a0);
        }

        /// <summary>피크 게인 0dB 밴드패스.</summary>
        public void SetBandPass(float f, float q)
        {
            double w, alpha, cos; Coef(f, q, out w, out alpha, out cos);
            double a0 = 1 + alpha;
            b0 = (float)(alpha / a0); b1 = 0; b2 = (float)(-alpha / a0);
            a1 = (float)(-2 * cos / a0); a2 = (float)((1 - alpha) / a0);
        }

        public float Process(float x)
        {
            float y = b0 * x + z1;
            z1 = b1 * x - a1 * y + z2;
            z2 = b2 * x - a2 * y;
            if (float.IsNaN(y) || float.IsInfinity(y)) { z1 = z2 = 0; return 0; }
            return y;
        }

        public void Run(float[] b)
        {
            for (int i = 0; i < b.Length; i++) b[i] = Process(b[i]);
        }
    }

    public struct OnePole
    {
        float a, z;
        public static OnePole Make(float cutoff)
        {
            var p = new OnePole();
            p.Set(cutoff);
            return p;
        }
        public void Set(float cutoff)
        {
            a = (float)Math.Exp(-2 * Math.PI * Math.Max(1f, cutoff) / Dsp.SR);
        }
        public float Low(float x) { z = x * (1 - a) + z * a; return z; }
        public float High(float x) { z = x * (1 - a) + z * a; return x - z; }
    }

    /// <summary>합성용 노이즈 소스 (결정적).</summary>
    public sealed class Noise
    {
        uint s;
        float pinkB0, pinkB1, pinkB2;
        float brown;
        public Noise(uint seed) { s = seed == 0 ? 1u : seed; }
        public float White()
        {
            s ^= s << 13; s ^= s >> 17; s ^= s << 5;
            return (s & 0xFFFFFF) / 8388608f - 1f;
        }
        public float Pink()
        {
            float w = White();
            pinkB0 = 0.99765f * pinkB0 + w * 0.0990460f;
            pinkB1 = 0.96300f * pinkB1 + w * 0.2965164f;
            pinkB2 = 0.57000f * pinkB2 + w * 1.0526913f;
            return (pinkB0 + pinkB1 + pinkB2 + w * 0.1848f) * 0.25f;
        }
        public float Brown()
        {
            brown += White() * 0.02f;
            brown *= 0.998f;
            if (brown > 1) brown = 1; if (brown < -1) brown = -1;
            return brown * 3f;
        }
        public float Range(float a, float b) { return a + (b - a) * (White() * 0.5f + 0.5f); }
    }
}
