using System;

namespace Gawi.Engine.Audio
{
    public enum Sfx
    {
        Step,
        StepWet,
        DoorOpen,
        DoorClose,
        Lock,
        Snip,
        SnipWet,
        Heartbeat,
        Static,
        Sting,
        StingSoft,
        Scream,
        Gore,
        Drip,
        Click,
        Blip,
        Pickup,
        Lucid,
        Chop,
        TvOn,
        ClockTick,
        MenuMove,
        MenuSelect,
        Crack,
        Skitter,
        Gasp,
        Thump,
        Whoosh,
        Scrape,
        Ding,
        Breath,
        Rip,
        Splat,
        Gurgle,
        Count
    }

    /// <summary>효과음 합성. 모든 소리는 코드로 만든다 (외부 음원 없음).</summary>
    public static class SoundBank
    {
        public static float[] Make(Sfx id)
        {
            var b = MakeRaw(id);
            Limit(b, 0.95f);
            return b;
        }

        /// <summary>리버브 등으로 커진 피크를 안전 범위로 줄인다.</summary>
        public static void Limit(float[] b, float peak)
        {
            float m = 0;
            for (int i = 0; i < b.Length; i++) { float a = Math.Abs(b[i]); if (a > m) m = a; }
            if (m > peak) Dsp.Gain(b, peak / m);
        }

        static float[] MakeRaw(Sfx id)
        {
            switch (id)
            {
                case Sfx.Step: return Step(11, false);
                case Sfx.StepWet: return Step(23, true);
                case Sfx.DoorOpen: return DoorOpen();
                case Sfx.DoorClose: return DoorClose();
                case Sfx.Lock: return Lock();
                case Sfx.Snip: return Snip(false);
                case Sfx.SnipWet: return Snip(true);
                case Sfx.Heartbeat: return Heartbeat();
                case Sfx.Static: return StaticBurst();
                case Sfx.Sting: return Sting();
                case Sfx.StingSoft: return StingSoft();
                case Sfx.Scream: return Scream();
                case Sfx.Gore: return Gore();
                case Sfx.Drip: return Drip();
                case Sfx.Click: return Click();
                case Sfx.Blip: return Blip();
                case Sfx.Pickup: return Pickup();
                case Sfx.Lucid: return Lucid();
                case Sfx.Chop: return Chop();
                case Sfx.TvOn: return TvOn();
                case Sfx.ClockTick: return ClockTick();
                case Sfx.MenuMove: return Tone(520, 0.035f, 0.012f, 0.35f);
                case Sfx.MenuSelect: return MenuSelect();
                case Sfx.Crack: return Crack();
                case Sfx.Skitter: return Skitter();
                case Sfx.Gasp: return Gasp();
                case Sfx.Thump: return Thump();
                case Sfx.Whoosh: return Whoosh();
                case Sfx.Scrape: return Scrape();
                case Sfx.Ding: return Ding();
                case Sfx.Breath: return Breath();
                case Sfx.Rip: return Rip();
                case Sfx.Splat: return Splat();
                case Sfx.Gurgle: return Gurgle();
            }
            return new float[1];
        }

        static float T(int i) { return (float)i / Dsp.SR; }

        static float[] Step(uint seed, bool wet)
        {
            var b = Dsp.Buf(wet ? 0.22f : 0.1f);
            var n = new Noise(seed);
            var lp = Biquad.LowPass(1100, 0.8f);
            var bp = Biquad.BandPass(420, 3f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float x = lp.Process(n.White()) * Dsp.Env(t, 0.002f, 0.016f) * 0.9f;
                ph += (70 + 60 * Math.Exp(-t / 0.01)) / Dsp.SR;
                x += Dsp.Sin(ph) * Dsp.Env(t, 0.001f, 0.03f) * 0.7f;
                if (wet)
                {
                    float f = 600 - 1800 * t;
                    bp.SetBandPass(Math.Max(150, f), 4f);
                    x += bp.Process(n.White()) * Dsp.Env(t - 0.015f, 0.01f, 0.06f) * 1.6f * (0.6f + 0.4f * Dsp.Sin(t * 37));
                }
                b[i] = x;
            }
            Dsp.Normalize(b, 0.8f);
            return b;
        }

        static float[] DoorOpen()
        {
            var b = Dsp.Buf(1.25f);
            var n = new Noise(5);
            var r1 = Biquad.BandPass(820, 14f);
            var r2 = Biquad.BandPass(1650, 18f);
            var r3 = Biquad.BandPass(2900, 10f);
            float next = 0.05f;
            float rate = 30;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float imp = 0;
                if (t >= next && t < 1.1f)
                {
                    imp = n.Range(0.4f, 1f);
                    rate = 22 + 40 * (0.5f + 0.5f * Dsp.Sin(t * 0.55)) + n.Range(-6, 6);
                    next = t + 1f / Math.Max(8, rate);
                }
                float env = Dsp.Ahr(t, 0.08f, 0.75f, 0.3f);
                float x = r1.Process(imp) * 1.0f + r2.Process(imp) * 0.7f + r3.Process(imp * 0.5f) * 0.4f;
                // 걸쇠 소리
                if (t < 0.02f) x += n.White() * Dsp.Env(t, 0.0005f, 0.004f) * 0.5f;
                b[i] = x * env;
            }
            Dsp.Normalize(b, 0.7f);
            return Dsp.Reverb(b, 0.4f, 0.4f, 0.25f, 0.4f);
        }

        static float[] DoorClose()
        {
            var b = Dsp.Buf(0.6f);
            var n = new Noise(9);
            var lp = Biquad.LowPass(380, 0.9f);
            var hp = Biquad.HighPass(2500, 0.7f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += (42 + 30 * Math.Exp(-t / 0.05)) / Dsp.SR;
                float x = Dsp.Sin(ph) * Dsp.Env(t, 0.002f, 0.12f) * 1.1f;
                x += lp.Process(n.White()) * Dsp.Env(t, 0.001f, 0.06f) * 1.4f;
                x += hp.Process(n.White()) * Dsp.Env(t - 0.07f, 0.0005f, 0.006f) * 0.5f;
                b[i] = x;
            }
            Dsp.Normalize(b, 0.9f);
            return Dsp.Reverb(b, 0.5f, 0.3f, 0.3f, 0.5f);
        }

        static float[] MetalClick(float seconds, float f1, float f2, uint seed)
        {
            var b = Dsp.Buf(seconds);
            var n = new Noise(seed);
            var a = Biquad.BandPass(f1, 22f);
            var c = Biquad.BandPass(f2, 26f);
            var hp = Biquad.HighPass(1800, 0.7f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float ex = n.White() * Dsp.Env(t, 0.0003f, 0.003f);
                b[i] = hp.Process(ex) * 0.8f + a.Process(ex) * 4f + c.Process(ex) * 3f;
            }
            return b;
        }

        static float[] Lock()
        {
            var b = Dsp.Buf(0.35f);
            Dsp.Mix(b, MetalClick(0.12f, 2600, 4100, 3), 0, 1f);
            Dsp.Mix(b, MetalClick(0.15f, 2200, 3500, 4), Dsp.Samples(0.09f), 1.2f);
            Dsp.Normalize(b, 0.8f);
            return Dsp.Reverb(b, 0.3f, 0.5f, 0.2f, 0.3f);
        }

        static float[] Snip(bool wet)
        {
            var b = Dsp.Buf(wet ? 0.7f : 0.45f);
            var n = new Noise(77);
            var bp = Biquad.BandPass(3000, 4f);
            var hp = Biquad.HighPass(3000, 0.7f);
            double p1 = 0, p2 = 0, p3 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float x = 0;
                // 칼날이 미끄러지는 소리
                if (t < 0.2f)
                {
                    float k = t / 0.2f;
                    bp.SetBandPass(2800 + 4200 * k, 5f);
                    float scrape = bp.Process(n.White()) * (0.2f + 0.8f * k * k);
                    x += scrape * 1.4f;
                }
                // 금속 울림
                float ringEnv = t < 0.2f ? (t / 0.2f) * 0.25f : Dsp.Env(t - 0.2f, 0.0005f, 0.11f);
                p1 += 2350.0 / Dsp.SR; p2 += 3710.0 / Dsp.SR; p3 += 5180.0 / Dsp.SR;
                x += (Dsp.Sin(p1) * 0.5f + Dsp.Sin(p2) * 0.35f + Dsp.Sin(p3) * 0.2f) * ringEnv;
                // 싹- 둑 (닫히는 순간)
                x += hp.Process(n.White()) * Dsp.Env(t - 0.2f, 0.0003f, 0.008f) * 1.5f;
                b[i] = x;
            }
            if (wet)
            {
                var g = Gore();
                Dsp.Gain(g, 0.7f);
                Dsp.Mix(b, g, Dsp.Samples(0.19f), 1f);
            }
            Dsp.Normalize(b, 0.95f);
            return Dsp.Reverb(b, 0.55f, 0.35f, 0.3f, 0.6f);
        }

        static float[] Thud(float f0, float f1, float decay, float amp, uint seed, float len)
        {
            var b = Dsp.Buf(len);
            var n = new Noise(seed);
            var lp = Biquad.LowPass(200, 0.7f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += (f1 + (f0 - f1) * Math.Exp(-t / 0.03)) / Dsp.SR;
                b[i] = (Dsp.Sin(ph) * Dsp.Env(t, 0.003f, decay) + lp.Process(n.White()) * Dsp.Env(t, 0.001f, 0.02f) * 0.6f) * amp;
            }
            return b;
        }

        static float[] Heartbeat()
        {
            var b = Dsp.Buf(0.6f);
            Dsp.Mix(b, Thud(70, 38, 0.08f, 1f, 1, 0.3f), 0, 1f);
            Dsp.Mix(b, Thud(62, 34, 0.09f, 0.75f, 2, 0.3f), Dsp.Samples(0.17f), 1f);
            Dsp.Normalize(b, 0.95f);
            return b;
        }

        static float[] StaticBurst()
        {
            var b = Dsp.Buf(0.7f);
            var n = new Noise(31);
            var bp = Biquad.BandPass(2400, 0.4f);
            float flutter = 1;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                if (i % 600 == 0) flutter = n.Range(0.4f, 1f);
                b[i] = (bp.Process(n.White()) * 0.8f + n.White() * 0.25f) * flutter * Dsp.Ahr(t, 0.005f, 0.5f, 0.19f);
            }
            Dsp.Normalize(b, 0.7f);
            return b;
        }

        static float[] Sting()
        {
            var b = Dsp.Buf(2.2f);
            var n = new Noise(13);
            float[] freqs = { 55f, 58.27f, 77.78f, 82.41f, 116.54f, 155.56f, 164.81f, 233.08f, 311.13f, 329.63f, 466.16f };
            var ph = new double[freqs.Length * 2];
            var hp = Biquad.HighPass(1200, 0.7f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float fall = 1f - 0.35f * M.Smooth((t - 0.6f) / 1.4f);
                float x = 0;
                for (int k = 0; k < freqs.Length; k++)
                {
                    ph[k * 2] += freqs[k] * fall * 1.003 / Dsp.SR;
                    ph[k * 2 + 1] += freqs[k] * fall * 0.997 / Dsp.SR;
                    x += (Dsp.Saw(ph[k * 2]) + Dsp.Saw(ph[k * 2 + 1])) * 0.5f;
                }
                x *= Dsp.Env(t, 0.004f, 0.9f) * 0.25f;
                x += hp.Process(n.White()) * Dsp.Env(t, 0.001f, 0.25f) * 0.9f;
                b[i] = x;
            }
            Dsp.Drive(b, 2.6f);
            Dsp.Normalize(b, 0.95f);
            return Dsp.Reverb(b, 0.7f, 0.3f, 0.35f, 1.2f);
        }

        static float[] StingSoft()
        {
            var b = Dsp.Buf(2.5f);
            float[] freqs = { 1760f, 1864.66f, 2489f, 2637f, 1174.66f };
            var ph = new double[freqs.Length];
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float x = 0;
                for (int k = 0; k < freqs.Length; k++)
                {
                    ph[k] += freqs[k] * (1 + 0.004 * Math.Sin(t * (3 + k))) / Dsp.SR;
                    x += Dsp.Sin(ph[k]);
                }
                b[i] = x * Dsp.Ahr(t, 0.6f, 0.6f, 1.2f) * 0.2f;
            }
            Dsp.Normalize(b, 0.5f);
            return Dsp.Reverb(b, 0.8f, 0.2f, 0.5f, 1.5f);
        }

        static float[] Scream()
        {
            var b = Dsp.Buf(1.4f);
            var n = new Noise(41);
            var f1 = Biquad.BandPass(900, 4f);
            var f2 = Biquad.BandPass(1500, 6f);
            var f3 = Biquad.BandPass(2700, 8f);
            double ph = 0;
            float jit = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                if (i % 200 == 0) jit = n.Range(-0.15f, 0.15f);
                float contour = 1f + 0.4f * M.Smooth(t / 0.25f) - 0.5f * M.Smooth((t - 0.7f) / 0.7f);
                float f0 = (620 + 220 * Dsp.Sin(t * 7 * 1.0)) * contour * (1 + jit);
                ph += f0 / Dsp.SR;
                float src = Dsp.Square(ph, 0.3f) + n.White() * 0.5f;
                float x = f1.Process(src) * 1.0f + f2.Process(src) * 0.8f + f3.Process(src) * 0.5f;
                b[i] = x * Dsp.Ahr(t, 0.03f, 0.85f, 0.5f);
            }
            Dsp.Drive(b, 3f);
            Dsp.BitCrush(b, 24, 2);
            Dsp.Normalize(b, 0.85f);
            return Dsp.Reverb(b, 0.6f, 0.3f, 0.3f, 0.8f);
        }

        static float[] Gore()
        {
            var b = Dsp.Buf(0.7f);
            var n = new Noise(57);
            var bp = Biquad.BandPass(500, 3f);
            var hp = Biquad.HighPass(2000, 0.7f);
            var clicks = new float[8];
            for (int k = 0; k < clicks.Length; k++) clicks[k] = n.Range(0f, 0.22f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                bp.SetBandPass(Math.Max(110, 520 - 700 * t), 3f);
                float am = 0.55f + 0.45f * Dsp.Sin(t * (18 + 10 * t));
                float x = bp.Process(n.White()) * Dsp.Env(t, 0.005f, 0.18f) * am * 2.2f;
                float cl = 0;
                for (int k = 0; k < clicks.Length; k++) cl += Dsp.Env(t - clicks[k], 0.0003f, 0.004f);
                x += hp.Process(n.White()) * cl * 0.9f;
                ph += (50 + 40 * Math.Exp(-t / 0.04)) / Dsp.SR;
                x += Dsp.Sin(ph) * Dsp.Env(t, 0.002f, 0.1f) * 0.8f;
                b[i] = x;
            }
            Dsp.Drive(b, 1.8f);
            Dsp.Normalize(b, 0.9f);
            return b;
        }

        static float[] Drip()
        {
            var b = Dsp.Buf(0.12f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += (380 + 1300 * Math.Exp(-t / 0.018)) / Dsp.SR;
                b[i] = Dsp.Sin(ph) * Dsp.Env(t, 0.001f, 0.04f);
            }
            Dsp.Normalize(b, 0.6f);
            return Dsp.Echo(b, 0.11f, 0.35f, 0.4f, 0.4f);
        }

        static float[] Click()
        {
            var b = Dsp.Buf(0.04f);
            var n = new Noise(3);
            var hp = Biquad.HighPass(1500, 0.7f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += 1900.0 / Dsp.SR;
                b[i] = hp.Process(n.White()) * Dsp.Env(t, 0.0003f, 0.004f) + Dsp.Sin(ph) * Dsp.Env(t, 0.0005f, 0.008f) * 0.4f;
            }
            Dsp.Normalize(b, 0.6f);
            return b;
        }

        static float[] Blip()
        {
            var b = Dsp.Buf(0.05f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += 220.0 / Dsp.SR;
                b[i] = (Dsp.Tri(ph) + Dsp.Square(ph, 0.5f) * 0.25f) * Dsp.Env(t, 0.002f, 0.02f);
            }
            Dsp.Normalize(b, 0.5f);
            return b;
        }

        public static float[] Tone(float f, float len, float decay, float peak)
        {
            var b = Dsp.Buf(len);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += f / Dsp.SR;
                b[i] = Dsp.Square(ph, 0.5f) * Dsp.Env(t, 0.001f, decay);
            }
            Dsp.Normalize(b, peak);
            return b;
        }

        static float[] MenuSelect()
        {
            var a = Tone(440, 0.05f, 0.02f, 0.35f);
            var c = Tone(660, 0.08f, 0.03f, 0.35f);
            return Dsp.Concat(a, c);
        }

        /// <summary>오르골 음 하나.</summary>
        public static float[] MusicBoxNote(float f, float len, float decay)
        {
            var b = Dsp.Buf(len);
            double p1 = 0, p2 = 0, p3 = 0;
            var n = new Noise((uint)(f * 10));
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                p1 += f / Dsp.SR;
                p2 += f * 2.0 / Dsp.SR;
                p3 += f * 6.27 / Dsp.SR;
                float x = Dsp.Sin(p1) * Dsp.Env(t, 0.002f, decay);
                x += Dsp.Sin(p2) * Dsp.Env(t, 0.001f, decay * 0.3f) * 0.18f;
                if (f * 6.27f < 10000f) x += Dsp.Sin(p3) * Dsp.Env(t, 0.0005f, 0.05f) * 0.12f;
                x += n.White() * Dsp.Env(t, 0.0002f, 0.002f) * 0.15f;
                b[i] = x;
            }
            return b;
        }

        static float[] Pickup()
        {
            var b = Dsp.Buf(1.3f);
            float[] notes = { 880f, 1046.5f, 1318.5f, 1760f };
            for (int k = 0; k < notes.Length; k++)
                Dsp.Mix(b, MusicBoxNote(notes[k], 1.0f, 0.5f), Dsp.Samples(k * 0.09f), 0.5f);
            Dsp.Normalize(b, 0.6f);
            return Dsp.Reverb(b, 0.6f, 0.3f, 0.35f, 0.8f);
        }

        static float[] Lucid()
        {
            var b = Dsp.Buf(2.0f);
            var n = new Noise(99);
            var bp = Biquad.BandPass(3200, 1.2f);
            double pw = 0, ps = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                pw += 3650.0 / Dsp.SR;
                float bell = (float)Math.Exp(-Math.Pow((t - 0.95) / 0.35, 2));
                float x = Dsp.Sin(pw) * bell * 0.12f;
                ps += (28 + 44 * M.Smooth(t / 1.0f)) / Dsp.SR;
                x += Dsp.Sin(ps) * Dsp.Ahr(t, 0.6f, 0.4f, 0.9f) * 0.7f;
                float swell = t < 0.95f ? (float)Math.Pow(t / 0.95f, 3) : Dsp.Env(t - 0.95f, 0.0001f, 0.05f);
                x += bp.Process(n.White()) * swell * 0.6f;
                b[i] = x;
            }
            Dsp.Normalize(b, 0.8f);
            return Dsp.Reverb(b, 0.75f, 0.25f, 0.4f, 1.0f);
        }

        static float[] Chop()
        {
            var b = Dsp.Buf(0.22f);
            var n = new Noise(19);
            var knock = Biquad.BandPass(950, 10f);
            var knock2 = Biquad.BandPass(1900, 12f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float ex = n.White() * Dsp.Env(t, 0.0002f, 0.003f);
                ph += 140.0 / Dsp.SR;
                b[i] = ex * 0.6f + knock.Process(ex) * 5f + knock2.Process(ex) * 3f + Dsp.Sin(ph) * Dsp.Env(t, 0.001f, 0.04f) * 0.5f;
            }
            Dsp.Normalize(b, 0.85f);
            return Dsp.Reverb(b, 0.35f, 0.4f, 0.2f, 0.3f);
        }

        static float[] TvOn()
        {
            var b = Dsp.Buf(1.4f);
            var n = new Noise(61);
            var bp = Biquad.BandPass(2600, 0.5f);
            double pw = 0, pt = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                pt += (60 + 40 * Math.Exp(-t / 0.02)) / Dsp.SR;
                float x = Dsp.Sin(pt) * Dsp.Env(t, 0.001f, 0.06f) * 0.9f;
                x += n.White() * Dsp.Env(t, 0.0002f, 0.004f) * 0.6f;
                pw += 7800.0 / Dsp.SR;
                x += Dsp.Sin(pw) * 0.05f * M.Clamp01(t / 0.2f);
                x += bp.Process(n.White()) * M.Clamp01((t - 0.1f) / 1.1f) * 0.7f;
                b[i] = x;
            }
            Dsp.FadeEdges(b, 0f, 0.05f);
            Dsp.Normalize(b, 0.75f);
            return b;
        }

        static float[] ClockTick()
        {
            var b = Dsp.Buf(0.03f);
            var n = new Noise(7);
            var bp = Biquad.BandPass(3200, 6f);
            for (int i = 0; i < b.Length; i++)
                b[i] = bp.Process(n.White() * Dsp.Env(T(i), 0.0003f, 0.004f)) * 3f;
            Dsp.Normalize(b, 0.4f);
            return b;
        }

        static float[] Crack()
        {
            var b = Dsp.Buf(0.18f);
            var n = new Noise(71);
            var hp = Biquad.HighPass(1600, 0.7f);
            var times = new float[6];
            for (int k = 0; k < times.Length; k++) times[k] = n.Range(0f, 0.06f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float cl = 0;
                for (int k = 0; k < times.Length; k++) cl += Dsp.Env(t - times[k], 0.0002f, 0.003f);
                ph += (110 + 260 * Math.Exp(-t / 0.015)) / Dsp.SR;
                b[i] = hp.Process(n.White()) * cl + Dsp.Sin(ph) * Dsp.Env(t - 0.03f, 0.001f, 0.02f) * 0.6f;
            }
            Dsp.Normalize(b, 0.85f);
            return b;
        }

        static float[] Skitter()
        {
            var b = Dsp.Buf(0.8f);
            var n = new Noise(83);
            var bp = Biquad.BandPass(3000, 3f);
            float next = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float ex = 0;
                if (t >= next)
                {
                    ex = n.Range(0.3f, 1f);
                    next = t + n.Range(0.012f, 0.045f);
                    bp.SetBandPass(n.Range(1800, 5200), 3f);
                }
                b[i] = bp.Process(ex) * Dsp.Ahr(t, 0.05f, 0.55f, 0.2f) * 3f + ex * 0.2f;
            }
            Dsp.Normalize(b, 0.55f);
            return b;
        }

        static float[] Gasp()
        {
            var b = Dsp.Buf(0.75f);
            var n = new Noise(17);
            var bp = Biquad.BandPass(900, 2f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                bp.SetBandPass(900 + 1600 * M.Clamp01(t / 0.55f), 2.2f);
                float env = t < 0.55f ? (float)Math.Pow(t / 0.55f, 2) : Dsp.Env(t - 0.55f, 0.0001f, 0.05f);
                b[i] = bp.Process(n.White()) * env;
            }
            Dsp.Normalize(b, 0.6f);
            return b;
        }

        static float[] Thump()
        {
            var b = Thud(60, 32, 0.16f, 1f, 5, 0.45f);
            Dsp.Normalize(b, 0.95f);
            return Dsp.Reverb(b, 0.5f, 0.4f, 0.25f, 0.4f);
        }

        static float[] Whoosh()
        {
            var b = Dsp.Buf(0.55f);
            var n = new Noise(29);
            var bp = Biquad.BandPass(400, 1.5f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float k = t / 0.55f;
                bp.SetBandPass(400 + 2200 * (float)Math.Sin(k * Math.PI), 1.5f);
                b[i] = bp.Process(n.White()) * (float)Math.Sin(k * Math.PI);
            }
            Dsp.Normalize(b, 0.6f);
            return b;
        }

        static float[] Scrape()
        {
            var b = Dsp.Buf(1.8f);
            var n = new Noise(37);
            var bp = Biquad.BandPass(2200, 14f);
            var bp2 = Biquad.BandPass(3300, 18f);
            float rough = 1;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                if (i % 450 == 0) rough = n.Range(0.2f, 1f);
                float f = 2200 + 400 * Dsp.Sin(t * 0.7) + 150 * Dsp.Sin(t * 0.13 * 7);
                bp.SetBandPass(f, 14f);
                bp2.SetBandPass(f * 1.49f, 18f);
                float w = n.White();
                b[i] = (bp.Process(w) * 2f + bp2.Process(w) * 1.4f) * rough * Dsp.Ahr(t, 0.2f, 1.2f, 0.4f);
            }
            Dsp.Normalize(b, 0.6f);
            return Dsp.Reverb(b, 0.6f, 0.3f, 0.35f, 0.8f);
        }

        static float[] Ding()
        {
            var b = Dsp.Buf(1.6f);
            float[] notes = { 1318.5f, 1046.5f };
            for (int k = 0; k < 2; k++)
            {
                var nb = Dsp.Buf(1.2f);
                double p1 = 0, p2 = 0;
                for (int i = 0; i < nb.Length; i++)
                {
                    float t = T(i);
                    p1 += notes[k] / Dsp.SR; p2 += notes[k] * 2.76 / Dsp.SR;
                    nb[i] = Dsp.Sin(p1) * Dsp.Env(t, 0.002f, 0.6f) + Dsp.Sin(p2) * Dsp.Env(t, 0.001f, 0.15f) * 0.3f;
                }
                Dsp.Mix(b, nb, Dsp.Samples(k * 0.38f), 0.5f);
            }
            var lp = Biquad.LowPass(2500, 0.7f);
            lp.Run(b);
            Dsp.Normalize(b, 0.4f);
            return Dsp.Reverb(b, 0.85f, 0.4f, 0.55f, 1.2f);
        }

        static float[] Breath()
        {
            var b = Dsp.Buf(2.4f);
            var n = new Noise(43);
            var lp = Biquad.LowPass(900, 1f);
            var bp = Biquad.BandPass(500, 2f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float inhale = t < 1.0f ? (float)Math.Pow(Math.Sin(t / 1.0 * Math.PI), 2) : 0f;
                float exhale = t >= 1.1f && t < 2.3f ? (float)Math.Pow(Math.Sin((t - 1.1) / 1.2 * Math.PI), 1.5) : 0f;
                float flutter = 0.7f + 0.3f * Dsp.Sin(t * 23 + Dsp.Sin(t * 3));
                float w = n.White();
                b[i] = (lp.Process(w) * inhale * 0.8f + bp.Process(w) * exhale * 1.6f) * flutter;
            }
            Dsp.Normalize(b, 0.5f);
            return b;
        }

        static float[] Rip()
        {
            var b = Dsp.Buf(0.55f);
            var n = new Noise(53);
            var bp = Biquad.BandPass(1800, 1f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float rate = 0.25f + 0.75f * (1 - t / 0.55f);
                float ex = n.White() > (1 - rate * 0.3f) ? n.Range(0.5f, 1f) : 0f;
                b[i] = bp.Process(ex + n.White() * 0.15f) * Dsp.Ahr(t, 0.01f, 0.35f, 0.19f) * 2f;
            }
            Dsp.Normalize(b, 0.7f);
            return b;
        }

        static float[] Splat()
        {
            var b = Dsp.Buf(0.35f);
            var n = new Noise(67);
            var lp = Biquad.LowPass(650, 1.2f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += (85 + 60 * Math.Exp(-t / 0.02)) / Dsp.SR;
                b[i] = lp.Process(n.White()) * Dsp.Env(t, 0.001f, 0.07f) * 1.5f + Dsp.Sin(ph) * Dsp.Env(t, 0.001f, 0.05f) * 0.7f;
            }
            Dsp.Drive(b, 1.5f);
            Dsp.Normalize(b, 0.85f);
            return b;
        }

        static float[] Gurgle()
        {
            var b = Dsp.Buf(3.0f);
            var n = new Noise(89);
            var lp = Biquad.LowPass(300, 1.5f);
            float next = 0;
            float bubbleT = -1, bubbleF = 200;
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                if (t >= next)
                {
                    bubbleT = t;
                    bubbleF = n.Range(160, 380);
                    next = t + n.Range(0.03f, 0.16f);
                }
                float bt = t - bubbleT;
                ph += bubbleF * (1 + bt * 12) / Dsp.SR;
                float x = Dsp.Sin(ph) * Dsp.Env(bt, 0.003f, 0.035f) * 0.6f;
                x += lp.Process(n.White()) * (0.5f + 0.5f * Dsp.Sin(t * 9)) * 0.9f;
                b[i] = x * Dsp.Ahr(t, 0.2f, 2.2f, 0.6f);
            }
            Dsp.Normalize(b, 0.7f);
            return Dsp.Reverb(b, 0.5f, 0.4f, 0.3f, 0.5f);
        }
    }
}
