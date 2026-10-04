using System;
using System.Collections.Generic;

namespace Gawi.Engine.Audio
{
    public enum VoiceKind
    {
        Whisper,
        Demon,
        Mother,
        Kids,
        Tv
    }

    public sealed class VoiceLine
    {
        public float[] Samples;
        /// <summary>각 글자가 화면에 나타나야 하는 시각(초). 길이 = 텍스트 길이.</summary>
        public float[] CharTimes;
        public float Duration;
    }

    /// <summary>
    /// 한글 음절을 초성/중성/종성으로 분해해 포먼트 합성으로 "말하는" 소리를 만든다.
    /// 속삭임, 악마 목소리, 엄마인 척하는 것, 아이들 합창 등.
    /// </summary>
    public static class Voice
    {
        // 중성 21개: ㅏ ㅐ ㅑ ㅒ ㅓ ㅔ ㅕ ㅖ ㅗ ㅘ ㅙ ㅚ ㅛ ㅜ ㅝ ㅞ ㅟ ㅠ ㅡ ㅢ ㅣ
        // 각 모음: 시작 모음, 끝 모음 (단모음은 같음)
        static readonly int[,] VowelGlide =
        {
            {0,0},{1,1},{6,0},{6,1},{2,2},{3,3},{6,2},{6,3},{4,4},{5,0},{5,1},{5,3},{6,4},{5,5},{5,2},{5,3},{5,6},{6,5},{7,7},{7,6},{6,6}
        };

        // 기본 모음 포먼트 F1, F2, F3 : a ae eo e o u i eu
        static readonly float[,] Formants =
        {
            {760, 1300, 2500}, // 0 ㅏ
            {600, 1800, 2600}, // 1 ㅐ
            {580, 1000, 2450}, // 2 ㅓ
            {480, 1900, 2600}, // 3 ㅔ
            {420, 820, 2450},  // 4 ㅗ
            {320, 760, 2350},  // 5 ㅜ
            {280, 2250, 3000}, // 6 ㅣ
            {340, 1400, 2500}, // 7 ㅡ
        };

        enum Onset { None, StopK, StopT, StopP, Affric, Sib, Hieut, Nasal, Liquid }
        enum Coda { None, Stop, Nasal, Liquid }

        // 초성 19개: ㄱ ㄲ ㄴ ㄷ ㄸ ㄹ ㅁ ㅂ ㅃ ㅅ ㅆ ㅇ ㅈ ㅉ ㅊ ㅋ ㅌ ㅍ ㅎ
        static readonly Onset[] OnsetType =
        {
            Onset.StopK, Onset.StopK, Onset.Nasal, Onset.StopT, Onset.StopT, Onset.Liquid, Onset.Nasal,
            Onset.StopP, Onset.StopP, Onset.Sib, Onset.Sib, Onset.None, Onset.Affric, Onset.Affric,
            Onset.Affric, Onset.StopK, Onset.StopT, Onset.StopP, Onset.Hieut
        };
        static readonly bool[] OnsetAspirated =
        {
            false, false, false, false, false, false, false, false, false, false, true, false, false, false,
            true, true, true, true, false
        };

        // 종성 28개 → 대표음
        static readonly Coda[] CodaType =
        {
            Coda.None, Coda.Stop, Coda.Stop, Coda.Stop, Coda.Nasal, Coda.Nasal, Coda.Nasal, Coda.Stop,
            Coda.Liquid, Coda.Stop, Coda.Nasal, Coda.Liquid, Coda.Liquid, Coda.Liquid, Coda.Stop, Coda.Liquid,
            Coda.Nasal, Coda.Stop, Coda.Stop, Coda.Stop, Coda.Stop, Coda.Nasal, Coda.Stop, Coda.Stop,
            Coda.Stop, Coda.Stop, Coda.Stop, Coda.Stop
        };

        sealed class Params
        {
            public float F0 = 120, Rate = 1f, Breath = 0.1f, FormantScale = 1f;
            public float VibRate = 5f, VibDepth = 0.02f, Jitter = 0.03f;
            public float Drive = 1f, Sub = 0f, Ring = 0f;
            public bool Tv;
            public float DropChance = 0f;
        }

        static Params Preset(VoiceKind k)
        {
            var p = new Params();
            switch (k)
            {
                case VoiceKind.Whisper:
                    p.F0 = 150; p.Breath = 1f; p.Rate = 1.05f; p.FormantScale = 1.05f; break;
                case VoiceKind.Demon:
                    p.F0 = 54; p.Breath = 0.28f; p.Rate = 1.35f; p.FormantScale = 0.82f;
                    p.Jitter = 0.08f; p.Drive = 2.2f; p.Sub = 0.55f; p.Ring = 0.3f; break;
                case VoiceKind.Mother:
                    p.F0 = 215; p.Breath = 0.4f; p.Rate = 1.15f; p.FormantScale = 1.15f;
                    p.VibRate = 6.5f; p.VibDepth = 0.07f; p.Jitter = 0.05f; p.DropChance = 0.18f; p.Drive = 1.3f; break;
                case VoiceKind.Kids:
                    p.F0 = 300; p.Breath = 0.6f; p.Rate = 1.0f; p.FormantScale = 1.3f; p.VibDepth = 0.03f; break;
                case VoiceKind.Tv:
                    p.F0 = 60; p.Breath = 0.35f; p.Rate = 1.3f; p.FormantScale = 0.85f;
                    p.Jitter = 0.1f; p.Drive = 2.5f; p.Sub = 0.4f; p.Tv = true; break;
            }
            return p;
        }

        struct Seg
        {
            public float Dur;
            public float F1a, F2a, F3a, F1b, F2b, F3b;
            public float Voice, Noise, Hiss; // 유성음 / 형성 소음 / 치찰음 크기
            public float Amp;
            public float PitchMul;
            public float HissFreq;
        }

        public static VoiceLine Speak(string text, VoiceKind kind, uint seed, int[] melody = null)
        {
            if (kind == VoiceKind.Kids)
            {
                var a = SpeakOne(text, VoiceKind.Kids, seed, melody, 1.0f, 0f);
                var b = SpeakOne(text, VoiceKind.Kids, seed + 7, melody, 1.13f, 0.03f);
                var c = SpeakOne(text, VoiceKind.Kids, seed + 13, melody, 0.9f, 0.055f);
                int len = Math.Max(a.Samples.Length, Math.Max(b.Samples.Length, c.Samples.Length));
                var mix = new float[len];
                Dsp.Mix(mix, a.Samples, 0, 0.5f);
                Dsp.Mix(mix, b.Samples, 0, 0.4f);
                Dsp.Mix(mix, c.Samples, 0, 0.4f);
                Dsp.Normalize(mix, 0.7f);
                a.Samples = Dsp.Reverb(mix, 0.7f, 0.4f, 0.35f, 0.6f);
                a.Duration = (float)a.Samples.Length / Dsp.SR;
                return a;
            }
            var v = SpeakOne(text, kind, seed, melody, 1f, 0f);
            float room = kind == VoiceKind.Demon ? 0.8f : 0.55f;
            v.Samples = Dsp.Reverb(v.Samples, room, 0.4f, kind == VoiceKind.Demon ? 0.45f : 0.3f, 0.7f);
            v.Duration = (float)v.Samples.Length / Dsp.SR;
            return v;
        }

        static void SetVowel(ref Seg s, int v, float fs, bool start)
        {
            if (start)
            {
                s.F1a = Formants[v, 0] * fs; s.F2a = Formants[v, 1] * fs; s.F3a = Formants[v, 2] * fs;
            }
            else
            {
                s.F1b = Formants[v, 0] * fs; s.F2b = Formants[v, 1] * fs; s.F3b = Formants[v, 2] * fs;
            }
        }

        static VoiceLine SpeakOne(string text, VoiceKind kind, uint seed, int[] melody, float pitchMul, float delay)
        {
            var p = Preset(kind);
            var rng = new Noise(seed * 7919 + 17);
            var segs = new List<Seg>();
            var charTimes = new float[text.Length];
            float time = delay;
            float fs = p.FormantScale;
            int syl = 0;
            int totalSyl = 0;
            foreach (char ch in text) if (ch >= 0xAC00 && ch <= 0xD7A3) totalSyl++;

            for (int ci = 0; ci < text.Length; ci++)
            {
                char ch = text[ci];
                charTimes[ci] = time;
                if (ch >= 0xAC00 && ch <= 0xD7A3)
                {
                    int code = ch - 0xAC00;
                    int ini = code / 588, med = (code % 588) / 28, fin = code % 28;
                    int va = VowelGlide[med, 0], vb = VowelGlide[med, 1];
                    float r = p.Rate * rng.Range(0.88f, 1.12f);
                    // 문장 끝 음절은 늘인다
                    bool last = ci + 1 >= text.Length || !(text[ci + 1] >= 0xAC00 && text[ci + 1] <= 0xD7A3);
                    float pm = 1f - 0.12f * (totalSyl > 1 ? (float)syl / (totalSyl - 1) : 0f);
                    if (melody != null && melody.Length > 0)
                        pm = (float)Math.Pow(2, (melody[syl % melody.Length] - 69) / 12.0) * 440f / p.F0;
                    if (p.DropChance > 0 && rng.Range(0, 1) < p.DropChance) pm *= rng.Range(0.55f, 0.75f);
                    // ? 로 끝나면 올린다
                    if (last && ci + 1 < text.Length && text[ci + 1] == '?') pm *= 1.25f;
                    pm *= pitchMul;

                    // 초성
                    var on = OnsetType[ini];
                    bool asp = OnsetAspirated[ini];
                    var s0 = new Seg { Amp = 1, PitchMul = pm };
                    SetVowel(ref s0, va, fs, true); SetVowel(ref s0, va, fs, false);
                    switch (on)
                    {
                        case Onset.StopK:
                        case Onset.StopT:
                        case Onset.StopP:
                            {
                                var sil = s0; sil.Dur = 0.025f * r; sil.Amp = 0; segs.Add(sil);
                                var burst = s0; burst.Dur = (asp ? 0.05f : 0.015f) * r;
                                burst.Voice = 0; burst.Noise = 0.2f; burst.Hiss = asp ? 0.5f : 0.8f;
                                burst.HissFreq = on == Onset.StopK ? 2200 : (on == Onset.StopT ? 4200 : 900);
                                segs.Add(burst);
                                break;
                            }
                        case Onset.Affric:
                            {
                                var sil = s0; sil.Dur = 0.02f * r; sil.Amp = 0; segs.Add(sil);
                                var fr = s0; fr.Dur = (asp ? 0.07f : 0.045f) * r; fr.Voice = 0; fr.Hiss = 0.8f; fr.HissFreq = 4800; segs.Add(fr);
                                break;
                            }
                        case Onset.Sib:
                            {
                                var fr = s0; fr.Dur = (asp ? 0.12f : 0.09f) * r; fr.Voice = 0; fr.Hiss = 1f; fr.HissFreq = 6200; segs.Add(fr);
                                break;
                            }
                        case Onset.Hieut:
                            {
                                var h = s0; h.Dur = 0.06f * r; h.Voice = 0; h.Noise = 0.9f; segs.Add(h);
                                break;
                            }
                        case Onset.Nasal:
                            {
                                var nn = s0; nn.Dur = 0.05f * r; nn.Voice = 0.6f; nn.Amp = 0.55f;
                                nn.F1a = nn.F1b = 260 * fs; nn.F2a = nn.F2b = 1100 * fs; nn.F3a = nn.F3b = 2300 * fs;
                                segs.Add(nn);
                                break;
                            }
                        case Onset.Liquid:
                            {
                                var l = s0; l.Dur = 0.035f * r; l.Voice = 0.8f; l.Amp = 0.7f;
                                l.F1a = l.F1b = 380 * fs; l.F2a = l.F2b = 1300 * fs; l.F3a = l.F3b = 1800 * fs;
                                segs.Add(l);
                                break;
                            }
                    }
                    // 중성
                    var vs = s0;
                    vs.Dur = (last ? 0.2f : 0.11f) * r;
                    vs.Voice = 1; vs.Noise = 0; vs.Hiss = 0;
                    SetVowel(ref vs, va, fs, true); SetVowel(ref vs, vb, fs, false);
                    if (va != vb)
                    {
                        // 이중모음: 짧은 활음 + 본 모음
                        var g = vs; g.Dur = 0.05f * r; segs.Add(g);
                        vs.F1a = vs.F1b; vs.F2a = vs.F2b; vs.F3a = vs.F3b;
                    }
                    segs.Add(vs);
                    // 종성
                    var ct = CodaType[fin];
                    if (ct == Coda.Nasal)
                    {
                        var nn = vs; nn.Dur = 0.06f * r; nn.Amp = 0.5f;
                        nn.F1a = nn.F1b = 260 * fs; nn.F2a = nn.F2b = 1000 * fs; nn.F3a = nn.F3b = 2300 * fs;
                        segs.Add(nn);
                    }
                    else if (ct == Coda.Liquid)
                    {
                        var l = vs; l.Dur = 0.05f * r; l.Amp = 0.6f;
                        l.F1a = l.F1b = 380 * fs; l.F2a = l.F2b = 1300 * fs; l.F3a = l.F3b = 1700 * fs;
                        segs.Add(l);
                    }
                    else if (ct == Coda.Stop)
                    {
                        var sil = vs; sil.Dur = 0.04f * r; sil.Amp = 0; segs.Add(sil);
                    }
                    syl++;
                    // 다음 글자 시각 갱신
                    time = delay + TotalDur(segs);
                }
                else
                {
                    float pause = 0f;
                    if (ch == ' ') pause = 0.07f;
                    else if (ch == ',') pause = 0.18f;
                    else if (ch == '.' || ch == '!' || ch == '?') pause = 0.3f;
                    else if (ch == '…') pause = 0.45f;
                    else if (ch == '\n') pause = 0.2f;
                    else if (ch == '-' || ch == '~') pause = 0.12f;
                    else pause = 0.05f;
                    pause *= p.Rate;
                    var s = new Seg { Dur = pause, Amp = 0, PitchMul = 1 };
                    SetVowel(ref s, 7, fs, true); SetVowel(ref s, 7, fs, false);
                    segs.Add(s);
                    time = delay + TotalDur(segs);
                }
            }

            float total = TotalDur(segs) + delay + 0.1f;
            var buf = new float[Dsp.Samples(total)];
            Render(segs, buf, Dsp.Samples(delay), p, rng);
            var line = new VoiceLine { Samples = buf, CharTimes = charTimes, Duration = total };
            return line;
        }

        static float TotalDur(List<Seg> segs)
        {
            float t = 0;
            for (int i = 0; i < segs.Count; i++) t += segs[i].Dur;
            return t;
        }

        static void Render(List<Seg> segs, float[] buf, int start, Params p, Noise rng)
        {
            var f1 = Biquad.BandPass(500, 5);
            var f2 = Biquad.BandPass(1500, 10);
            var f3 = Biquad.BandPass(2500, 14);
            var hiss = Biquad.BandPass(5000, 2);
            var tvBand = Biquad.BandPass(1200, 0.6f);
            double phase = 0, subPhase = 0, ringPhase = 0;
            float jit = 0;
            float curAmp = 0, curVoice = 0, curNoise = 0, curHiss = 0;
            float curF1 = segs.Count > 0 ? segs[0].F1a : 500, curF2 = segs.Count > 0 ? segs[0].F2a : 1500, curF3 = segs.Count > 0 ? segs[0].F3a : 2500;
            float curPitch = 1;
            int pos = start;
            float tGlobal = 0;
            for (int si = 0; si < segs.Count; si++)
            {
                var s = segs[si];
                int n = Dsp.Samples(s.Dur);
                if (s.Hiss > 0) hiss.SetBandPass(s.HissFreq > 0 ? s.HissFreq : 5000, 2f);
                for (int i = 0; i < n && pos < buf.Length; i++, pos++)
                {
                    float k = (float)i / Math.Max(1, n);
                    float tf1 = s.F1a + (s.F1b - s.F1a) * k;
                    float tf2 = s.F2a + (s.F2b - s.F2a) * k;
                    float tf3 = s.F3a + (s.F3b - s.F3a) * k;
                    curF1 += (tf1 - curF1) * 0.004f;
                    curF2 += (tf2 - curF2) * 0.004f;
                    curF3 += (tf3 - curF3) * 0.004f;
                    curAmp += (s.Amp - curAmp) * 0.01f;
                    curVoice += (s.Voice - curVoice) * 0.01f;
                    curNoise += (s.Noise - curNoise) * 0.02f;
                    curHiss += (s.Hiss - curHiss) * 0.03f;
                    curPitch += (s.PitchMul - curPitch) * 0.002f;
                    if ((pos & 255) == 0)
                    {
                        jit = rng.Range(-p.Jitter, p.Jitter);
                        f1.SetBandPass(curF1, curF1 / 90f);
                        f2.SetBandPass(curF2, curF2 / 120f);
                        f3.SetBandPass(curF3, curF3 / 180f);
                    }
                    tGlobal += 1f / Dsp.SR;
                    float f0 = p.F0 * curPitch * (1 + p.VibDepth * Dsp.Sin(tGlobal * p.VibRate) + jit);
                    phase += f0 / Dsp.SR;
                    float glottal = Dsp.Saw(phase) * 0.8f + Dsp.Sin(phase) * 0.4f;
                    if (p.Sub > 0)
                    {
                        subPhase += f0 * 0.5 / Dsp.SR;
                        glottal += Dsp.Saw(subPhase) * p.Sub;
                    }
                    float wn = rng.White();
                    float voiced = curVoice * (1 - p.Breath);
                    float breath = curVoice * p.Breath + curNoise;
                    float src = glottal * voiced + wn * breath * 0.9f;
                    float y = f1.Process(src) * 1.0f + f2.Process(src) * 0.6f + f3.Process(src) * 0.28f;
                    if (curHiss > 0.001f) y += hiss.Process(wn) * curHiss * 0.5f;
                    if (p.Ring > 0)
                    {
                        ringPhase += 31.0 / Dsp.SR;
                        y *= 1 - p.Ring + p.Ring * Dsp.Sin(ringPhase);
                    }
                    y *= curAmp * 2.2f;
                    if (p.Drive > 1) y = (float)Math.Tanh(y * p.Drive) / (float)Math.Tanh(p.Drive);
                    if (p.Tv) y = tvBand.Process(y) * 2f + wn * 0.04f;
                    buf[pos] += y;
                }
            }
            Dsp.Normalize(buf, 0.85f);
        }
    }
}
