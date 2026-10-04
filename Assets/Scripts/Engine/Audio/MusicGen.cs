using System;

namespace Gawi.Engine.Audio
{
    public enum Music
    {
        None = -1,
        AmbReality = 0,
        AmbDream,
        AmbNightmare,
        MusicBox,
        MusicBoxWarped,
        Chase,
        Boss,
        Morning,
        Count
    }

    /// <summary>루프 음악/앰비언스 합성. 전부 창작 (자장가 멜로디 포함).</summary>
    public static class MusicGen
    {
        public static float[] Make(Music id)
        {
            var b = MakeRaw(id);
            SoundBank.Limit(b, 0.9f);
            return b;
        }

        static float[] MakeRaw(Music id)
        {
            switch (id)
            {
                case Music.AmbReality: return AmbReality();
                case Music.AmbDream: return AmbDream();
                case Music.AmbNightmare: return AmbNightmare();
                case Music.MusicBox: return MusicBox(false);
                case Music.MusicBoxWarped: return MusicBox(true);
                case Music.Chase: return Chase(false);
                case Music.Boss: return Chase(true);
                case Music.Morning: return Morning();
            }
            return new float[Dsp.SR];
        }

        static float T(int i) { return (float)i / Dsp.SR; }

        /// <summary>루프 길이에 맞춰 정수 주기가 되도록 주파수를 반올림.</summary>
        static double Q(double f, float len) { return Math.Round(f * len) / len; }

        static float Midi(int n) { return 440f * (float)Math.Pow(2, (n - 69) / 12.0); }

        static float[] AmbReality()
        {
            const float len = 16f, xf = 1.5f;
            var b = Dsp.Buf(len + xf);
            var n = new Noise(101);
            var lp = Biquad.LowPass(180, 0.7f);
            var car = Biquad.BandPass(300, 0.8f);
            double h1 = Q(60, len), h2 = Q(120, len), h3 = Q(180, len);
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float x = Dsp.Sin(h1 * t) * 0.05f + Dsp.Sin(h2 * t) * 0.035f + Dsp.Sin(h3 * t) * 0.012f;
                x += lp.Process(n.Brown()) * 0.25f;
                // 멀리서 차 지나가는 소리
                float carEnv = (float)Math.Exp(-Math.Pow((t - 7.0) / 1.6, 2)) * 0.25f;
                car.SetBandPass(250 + 200 * M.Clamp01((t - 5.5f) / 3f), 0.8f);
                x += car.Process(n.White()) * carEnv;
                b[i] = x;
            }
            var tick = SoundBank.Make(Sfx.ClockTick);
            for (int s = 0; s < (int)(len + xf); s++) Dsp.Mix(b, tick, Dsp.Samples(s + 0.2f), 0.35f);
            return Dsp.MakeLoop(b, Dsp.Samples(len));
        }

        static float[] AmbDream()
        {
            const float len = 24f, xf = 2f;
            var b = Dsp.Buf(len + xf);
            var n = new Noise(202);
            var wind = Biquad.BandPass(500, 2f);
            var lp = Biquad.LowPass(320, 0.8f);
            double f1 = Q(41.2, len), f2 = Q(43.65, len), f3 = Q(82.4, len), f4 = Q(110, len);
            double ph4 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float x = Dsp.Sin(f1 * t) * 0.42f + Dsp.Sin(f2 * t) * 0.3f;
                x += Dsp.Sin(f3 * t) * 0.16f * (0.5f + 0.5f * Dsp.Sin(t / len * 2));
                ph4 += f4 / Dsp.SR;
                x += lp.Process(Dsp.Saw(ph4)) * 0.12f * (0.5f + 0.5f * Dsp.Sin(t / len * 3 + 0.3));
                wind.SetBandPass(480 + 300 * Dsp.Sin(t / len * 2 + 0.1) + 80 * Dsp.Sin(t * 0.37), 2.2f);
                x += wind.Process(n.Pink()) * 0.55f * (0.6f + 0.4f * Dsp.Sin(t / len * 3));
                b[i] = x;
            }
            // 먼 금속성 울림
            var pings = Dsp.Buf(len + xf);
            var pr = new Noise(303);
            float pt = 2f;
            while (pt < len)
            {
                float f = pr.Range(260, 900);
                var note = Dsp.Buf(2.5f);
                double a = 0, c = 0;
                for (int i = 0; i < note.Length; i++)
                {
                    float t = T(i);
                    a += f / Dsp.SR; c += f * 2.71 / Dsp.SR;
                    note[i] = (Dsp.Sin(a) + Dsp.Sin(c) * 0.4f) * Dsp.Env(t, 0.004f, 0.9f);
                }
                Dsp.Mix(pings, note, Dsp.Samples(pt), pr.Range(0.04f, 0.1f));
                pt += pr.Range(3f, 7f);
            }
            var pingsRev = Dsp.Reverb(pings, 0.92f, 0.5f, 0.8f, 0f);
            for (int i = 0; i < b.Length; i++) b[i] += pingsRev[i];
            Dsp.Normalize(b, 0.55f);
            return Dsp.MakeLoop(b, Dsp.Samples(len));
        }

        static float[] AmbNightmare()
        {
            const float len = 16f, xf = 1.5f;
            var b = Dsp.Buf(len + xf);
            var n = new Noise(404);
            var lp = Biquad.LowPass(400, 3f);
            var rumble = Biquad.LowPass(90, 0.7f);
            double s1 = Q(55, len), s2 = Q(55.5, len), s3 = Q(58.27, len), g1 = Q(1567.98, len), g2 = Q(1661.2, len);
            double p1 = 0, p2 = 0, p3 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                p1 += s1 / Dsp.SR; p2 += s2 / Dsp.SR; p3 += s3 / Dsp.SR;
                float saws = Dsp.Saw(p1) + Dsp.Saw(p2) + Dsp.Saw(p3) * 0.7f;
                lp.SetLowPass(220 + 500 * (0.5f + 0.5f * Dsp.Sin(t / len * 2)), 3f);
                float x = lp.Process(saws) * 0.22f;
                x += (Dsp.Sin(g1 * t) + Dsp.Sin(g2 * t)) * 0.025f * (0.5f + 0.5f * Dsp.Sin(t * 0.5));
                x += rumble.Process(n.Brown()) * 0.6f;
                // 박동
                float beat = t % 1f;
                x += Dsp.Sin(48 * t) * Dsp.Env(beat, 0.005f, 0.09f) * 0.35f;
                b[i] = x;
            }
            Dsp.Drive(b, 1.6f);
            Dsp.Normalize(b, 0.6f);
            return Dsp.MakeLoop(b, Dsp.Samples(len));
        }

        // 창작 자장가 (가단조, 3/4박자). [MIDI 음, 박자 수]
        static readonly int[] Melody =
        {
            76,1, 81,1, 83,1,   84,2, 83,1,   81,1, 80,1, 81,1,   76,3,
            77,1, 76,1, 74,1,   72,2, 74,1,   76,1, 72,1, 71,1,   69,3,
            76,1, 81,1, 84,1,   88,2, 86,1,   84,1, 83,1, 81,1,   80,3,
            81,1, 77,1, 74,1,   76,2, 68,1,   69,1, 71,1, 72,1,   69,3,
        };

        // 마디별 베이스 음 (MIDI)
        static readonly int[] Bass = { 45, 45, 40, 45, 50, 41, 40, 45, 45, 48, 41, 40, 38, 40, 41, 45 };

        public static int[] LullabyNotes()
        {
            var r = new int[Melody.Length / 2];
            for (int i = 0; i < r.Length; i++) r[i] = Melody[i * 2];
            return r;
        }

        static float[] MusicBox(bool warped)
        {
            const float bpm = 100f;
            float beat = 60f / bpm;
            int beats = 0;
            for (int i = 1; i < Melody.Length; i += 2) beats += Melody[i];
            float len = beats * beat;
            var b = Dsp.Buf(len + 3f);
            var rng = new Noise(warped ? 666u : 505u);
            float tpos = 0;
            int noteIdx = 0;
            for (int i = 0; i < Melody.Length; i += 2, noteIdx++)
            {
                int note = Melody[i];
                if (warped && (noteIdx % 7 == 5 || noteIdx % 11 == 9)) note -= 1; // 틀린 음
                float dur = Melody[i + 1] * beat;
                var nb = SoundBank.MusicBoxNote(Midi(note), Math.Min(2.5f, dur + 1.2f), 0.7f);
                Dsp.Mix(b, nb, Dsp.Samples(tpos), 0.5f);
                tpos += dur;
            }
            for (int bar = 0; bar < Bass.Length; bar++)
            {
                float t0 = bar * 3 * beat;
                var root = SoundBank.MusicBoxNote(Midi(Bass[bar] + 12), 2.2f, 0.9f);
                Dsp.Mix(b, root, Dsp.Samples(t0), 0.3f);
                var fifth = SoundBank.MusicBoxNote(Midi(Bass[bar] + 19), 1.2f, 0.5f);
                Dsp.Mix(b, fifth, Dsp.Samples(t0 + beat), 0.12f);
                Dsp.Mix(b, fifth, Dsp.Samples(t0 + beat * 2), 0.1f);
            }
            float[] outb;
            if (warped)
            {
                // 늘어진 테이프처럼 속도와 음높이가 출렁인다
                var w = new float[(int)(b.Length / 0.82f)];
                double pos = 0;
                for (int i = 0; i < w.Length; i++)
                {
                    float t = T(i);
                    double rate = 0.82 * (1 + 0.025 * Math.Sin(t * 2 * Math.PI * 0.37) + 0.012 * Math.Sin(t * 2 * Math.PI * 1.9));
                    int p = (int)pos;
                    if (p + 1 >= b.Length) break;
                    float fr = (float)(pos - p);
                    w[i] = b[p] * (1 - fr) + b[p + 1] * fr;
                    pos += rate;
                }
                var lp = Biquad.LowPass(2600, 0.9f);
                lp.Run(w);
                Dsp.BitCrush(w, 48, 1);
                Dsp.Drive(w, 1.4f);
                float wlen = len / 0.82f;
                var rev = Dsp.Reverb(w, 0.85f, 0.45f, 0.5f, 0f);
                outb = Dsp.MakeLoop(rev, Math.Min(rev.Length - Dsp.Samples(0.5f), Dsp.Samples(wlen)));
            }
            else
            {
                var rev = Dsp.Reverb(b, 0.7f, 0.35f, 0.35f, 0f);
                outb = Dsp.MakeLoop(rev, Dsp.Samples(len));
            }
            Dsp.Normalize(outb, 0.55f);
            return outb;
        }

        static float[] Chase(bool boss)
        {
            const float bpm = 150f;
            float step = 60f / bpm / 2f; // 8분음표
            int steps = boss ? 64 : 32;
            float len = steps * step;
            var b = Dsp.Buf(len + 1f);
            var n = new Noise(boss ? 909u : 808u);
            int[] bassPat = { 33, 33, 33, 34, 33, 33, 36, 35, 33, 33, 33, 34, 39, 38, 37, 34 };
            var kick = Kick();
            var hat = Hat(n);
            var snare = Snare(n);
            for (int s = 0; s < steps; s++)
            {
                float t0 = s * step;
                int note = bassPat[s % bassPat.Length] + (boss && (s / 16) % 2 == 1 ? 1 : 0);
                var bass = BassNote(Midi(note), step * 0.9f);
                Dsp.Mix(b, bass, Dsp.Samples(t0), s % 4 == 0 ? 0.55f : 0.4f);
                Dsp.Mix(b, hat, Dsp.Samples(t0), s % 2 == 0 ? 0.18f : 0.1f);
                if (s % 4 == 0) Dsp.Mix(b, kick, Dsp.Samples(t0), 0.8f);
                if (s % 8 == 4) Dsp.Mix(b, snare, Dsp.Samples(t0), 0.45f);
                if (s % 8 == 7)
                {
                    var stab = Stab(new[] { Midi(57), Midi(63), Midi(70) }, step * 1.5f);
                    Dsp.Mix(b, stab, Dsp.Samples(t0), 0.22f);
                }
            }
            // 높은 비명 같은 트레몰로
            double pa = 0, pb = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                pa += Midi(81) * (1 + 0.01 * Math.Sin(t * 5)) / Dsp.SR;
                pb += Midi(82) / Dsp.SR;
                float trem = 0.5f + 0.5f * Dsp.Sin(t * 12);
                b[i] += (Dsp.Saw(pa) + Dsp.Saw(pb)) * 0.025f * trem;
                if (boss) b[i] += Dsp.Sin(Q(41.2, len) * t) * 0.25f;
            }
            if (boss)
            {
                var snip = SoundBank.Make(Sfx.Snip);
                for (int s = 0; s < steps; s += 16) Dsp.Mix(b, snip, Dsp.Samples(s * step + step * 6), 0.35f);
            }
            Dsp.Drive(b, 1.8f);
            Dsp.Normalize(b, 0.7f);
            return Dsp.MakeLoop(b, Dsp.Samples(len));
        }

        static float[] Kick()
        {
            var b = Dsp.Buf(0.3f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += (45 + 120 * Math.Exp(-t / 0.025)) / Dsp.SR;
                b[i] = Dsp.Sin(ph) * Dsp.Env(t, 0.001f, 0.12f);
            }
            return b;
        }

        static float[] Hat(Noise n)
        {
            var b = Dsp.Buf(0.06f);
            var hp = Biquad.HighPass(6000, 0.7f);
            for (int i = 0; i < b.Length; i++) b[i] = hp.Process(n.White()) * Dsp.Env(T(i), 0.0005f, 0.015f);
            return b;
        }

        static float[] Snare(Noise n)
        {
            var b = Dsp.Buf(0.2f);
            var bp = Biquad.BandPass(1800, 0.8f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += 190.0 / Dsp.SR;
                b[i] = bp.Process(n.White()) * Dsp.Env(t, 0.001f, 0.06f) * 1.5f + Dsp.Sin(ph) * Dsp.Env(t, 0.001f, 0.03f) * 0.5f;
            }
            return b;
        }

        static float[] BassNote(float f, float len)
        {
            var b = Dsp.Buf(len);
            var lp = Biquad.LowPass(900, 1.5f);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                ph += f / Dsp.SR;
                float x = Dsp.Square(ph, 0.35f) * 0.7f + Dsp.Saw(ph * 2) * 0.3f;
                b[i] = lp.Process(x) * Dsp.Ahr(t, 0.003f, len * 0.6f, len * 0.35f);
            }
            return b;
        }

        static float[] Stab(float[] fs, float len)
        {
            var b = Dsp.Buf(len);
            var ph = new double[fs.Length];
            for (int i = 0; i < b.Length; i++)
            {
                float t = T(i);
                float x = 0;
                for (int k = 0; k < fs.Length; k++) { ph[k] += fs[k] / Dsp.SR; x += Dsp.Saw(ph[k]); }
                b[i] = x * Dsp.Env(t, 0.002f, len * 0.4f);
            }
            return b;
        }

        static float[] Morning()
        {
            const float len = 14f, xf = 1f;
            var b = Dsp.Buf(len + xf);
            var n = new Noise(1111);
            var lp = Biquad.LowPass(250, 0.7f);
            for (int i = 0; i < b.Length; i++) b[i] = lp.Process(n.Brown()) * 0.15f;
            float t0 = 0.5f;
            while (t0 < len)
            {
                int group = 2 + (int)(n.Range(0, 4));
                float baseF = n.Range(2400, 3800);
                for (int g = 0; g < group; g++)
                {
                    var c = Dsp.Buf(0.14f);
                    double ph = 0;
                    float up = n.Range(-0.4f, 0.6f);
                    for (int i = 0; i < c.Length; i++)
                    {
                        float t = T(i);
                        float k = t / 0.14f;
                        ph += baseF * (1 + up * k + 0.08 * Math.Sin(k * 40)) / Dsp.SR;
                        c[i] = Dsp.Sin(ph) * (float)Math.Sin(k * Math.PI);
                    }
                    Dsp.Mix(b, c, Dsp.Samples(t0 + g * 0.17f), 0.08f);
                }
                t0 += n.Range(1.2f, 3.5f);
            }
            var rev = Dsp.Reverb(b, 0.6f, 0.4f, 0.3f, 0f);
            Dsp.Normalize(rev, 0.4f);
            return Dsp.MakeLoop(rev, Dsp.Samples(len));
        }
    }
}
