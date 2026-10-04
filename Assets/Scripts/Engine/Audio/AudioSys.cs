using System;
using System.Collections.Generic;

namespace Gawi.Engine.Audio
{
    /// <summary>플랫폼(Unity/테스트)이 구현하는 소리 출력.</summary>
    public interface IAudioBackend
    {
        /// <summary>모노 샘플로 클립을 만들고 ID를 돌려준다.</summary>
        int CreateClip(string name, float[] samples, int sampleRate);
        void ReleaseClip(int clip);
        void PlayOneShot(int clip, float volume, float pitch, float pan);
        /// <summary>루프 채널에 클립을 건다. clip &lt; 0 이면 정지.</summary>
        void SetLoop(int channel, int clip, float volume, float pitch);
        void SetLoopVolume(int channel, float volume);
        void SetLoopPitch(int channel, float pitch);
        void StopAllOneShots();
    }

    public sealed class NullAudioBackend : IAudioBackend
    {
        int next = 1;
        public int CreateClip(string name, float[] samples, int sampleRate) { return next++; }
        public void ReleaseClip(int clip) { }
        public void PlayOneShot(int clip, float volume, float pitch, float pan) { }
        public void SetLoop(int channel, int clip, float volume, float pitch) { }
        public void SetLoopVolume(int channel, float volume) { }
        public void SetLoopPitch(int channel, float pitch) { }
        public void StopAllOneShots() { }
    }

    public enum Ch
    {
        Ambience = 0,
        Music = 1,
        Layer = 2,
        Heart = 3,
        Noise = 4,
        Count = 5
    }

    /// <summary>
    /// 게임 쪽 오디오 관리자. 소리 생성(지연/분산), 루프 채널 페이드, 위치 기반 볼륨.
    /// </summary>
    public sealed class AudioSys
    {
        readonly IAudioBackend be;
        readonly int[] sfxClip = new int[(int)Sfx.Count];
        readonly int[] musicClip = new int[(int)Music.Count];
        readonly Queue<int> warmSfx = new Queue<int>();
        readonly Queue<int> warmMusic = new Queue<int>();

        sealed class Chan
        {
            public Music Id = Music.None;
            public int Clip = -1;
            public float Vol, Target, FadeSpeed;
            public float Pitch = 1;
            public Music PendingId = Music.None;
            public float PendingVol;
        }
        readonly Chan[] chans = new Chan[(int)Ch.Count];
        int heartClip = -1;
        int noiseClip = -1;

        // 음성 캐시 (같은 대사를 다시 만들지 않도록)
        readonly Dictionary<string, VoiceLine> voiceCache = new Dictionary<string, VoiceLine>();
        readonly Dictionary<string, int> voiceClips = new Dictionary<string, int>();
        readonly List<string> voiceOrder = new List<string>();

        public float MasterVolume = 0.8f;
        public float SfxVolume = 1f;
        public float MusicVolume = 0.8f;
        public bool Muted;

        public AudioSys(IAudioBackend backend)
        {
            be = backend ?? new NullAudioBackend();
            for (int i = 0; i < sfxClip.Length; i++) { sfxClip[i] = -1; warmSfx.Enqueue(i); }
            for (int i = 0; i < musicClip.Length; i++) { musicClip[i] = -1; warmMusic.Enqueue(i); }
            for (int i = 0; i < chans.Length; i++) chans[i] = new Chan();
        }

        public bool WarmDone { get { return warmSfx.Count == 0 && warmMusic.Count == 0; } }
        public float WarmProgress
        {
            get
            {
                int total = sfxClip.Length + musicClip.Length;
                return 1f - (float)(warmSfx.Count + warmMusic.Count) / total;
            }
        }

        /// <summary>프레임마다 하나씩 소리를 미리 만든다 (멈춤 분산).</summary>
        public void WarmStep()
        {
            if (warmSfx.Count > 0) { EnsureSfx((Sfx)warmSfx.Dequeue()); return; }
            if (warmMusic.Count > 0) { EnsureMusic((Music)warmMusic.Dequeue()); }
        }

        int EnsureSfx(Sfx id)
        {
            int i = (int)id;
            if (sfxClip[i] < 0) sfxClip[i] = be.CreateClip("sfx_" + id, SoundBank.Make(id), Dsp.SR);
            return sfxClip[i];
        }

        int EnsureMusic(Music id)
        {
            if (id == Music.None) return -1;
            int i = (int)id;
            if (musicClip[i] < 0) musicClip[i] = be.CreateClip("mus_" + id, MusicGen.Make(id), Dsp.SR);
            return musicClip[i];
        }

        float SfxGain { get { return Muted ? 0 : MasterVolume * SfxVolume; } }
        float MusGain { get { return Muted ? 0 : MasterVolume * MusicVolume; } }

        public void Play(Sfx id, float vol = 1f, float pitch = 1f, float pan = 0f)
        {
            int c = EnsureSfx(id);
            be.PlayOneShot(c, M.Clamp01(vol) * SfxGain, pitch, M.Clamp(pan, -1, 1));
        }

        /// <summary>월드 위치 기반: 거리에 따라 작아지고 좌우로 퍼진다.</summary>
        public void PlayAt(Sfx id, float x, float y, float lx, float ly, float vol = 1f, float pitch = 1f, float range = 140f)
        {
            float d = M.Dist(x, y, lx, ly);
            float att = M.Clamp01(1f - d / range);
            att *= att;
            if (att < 0.01f) return;
            float pan = M.Clamp((x - lx) / 70f, -0.85f, 0.85f);
            Play(id, vol * att, pitch, pan);
        }

        public void StopOneShots() { be.StopAllOneShots(); }

        /// <summary>채널 음악 전환. 같은 곡이면 볼륨만 페이드.</summary>
        public void SetMusic(Ch ch, Music id, float vol = 1f, float fade = 1f)
        {
            var c = chans[(int)ch];
            if (c.Id == id)
            {
                c.Target = vol;
                c.FadeSpeed = fade <= 0 ? 999f : 1f / fade;
                c.PendingId = Music.None;
                return;
            }
            if (c.Id == Music.None || c.Vol <= 0.001f || fade <= 0)
            {
                Switch(c, (int)ch, id, fade <= 0 ? vol : 0f);
                c.Target = vol;
                c.FadeSpeed = fade <= 0 ? 999f : 1f / fade;
                c.PendingId = Music.None;
                return;
            }
            // 먼저 줄이고 바꾼다
            c.PendingId = id;
            c.PendingVol = vol;
            c.Target = 0;
            c.FadeSpeed = 1f / Math.Max(0.05f, fade * 0.5f);
        }

        public void StopMusic(Ch ch, float fade = 1f)
        {
            var c = chans[(int)ch];
            c.PendingId = Music.None;
            c.Target = 0;
            c.FadeSpeed = fade <= 0 ? 999f : 1f / fade;
            if (fade <= 0) { c.Vol = 0; Switch(c, (int)ch, Music.None, 0); }
        }

        public void StopAllMusic(float fade = 0.5f)
        {
            for (int i = 0; i < chans.Length; i++)
            {
                if (i == (int)Ch.Heart || i == (int)Ch.Noise) { SetLoopLevel((Ch)i, 0, fade); continue; }
                StopMusic((Ch)i, fade);
            }
        }

        void Switch(Chan c, int ch, Music id, float vol)
        {
            c.Id = id;
            c.Clip = EnsureMusic(id);
            c.Vol = vol;
            be.SetLoop(ch, c.Clip, vol * MusGain, c.Pitch);
        }

        public void SetPitch(Ch ch, float pitch)
        {
            var c = chans[(int)ch];
            c.Pitch = pitch;
            be.SetLoopPitch((int)ch, pitch);
        }

        /// <summary>심장 박동 / 노이즈 루프: 클립은 효과음으로 고정, 볼륨만 조절.</summary>
        public void SetLoopLevel(Ch ch, float vol, float fade)
        {
            var c = chans[(int)ch];
            if (c.Clip < 0)
            {
                if (ch == Ch.Heart)
                {
                    if (heartClip < 0)
                    {
                        // 1초 주기 루프 심장 소리 생성
                        var hb = SoundBank.Make(Sfx.Heartbeat);
                        var loop = new float[Dsp.SR];
                        Dsp.Mix(loop, hb, 0, 1f);
                        heartClip = be.CreateClip("loop_heart", loop, Dsp.SR);
                    }
                    c.Clip = heartClip;
                }
                else if (ch == Ch.Noise)
                {
                    if (noiseClip < 0)
                    {
                        var n = new Noise(4242);
                        var loop = new float[Dsp.SR * 2];
                        var bp = Biquad.BandPass(2600, 0.5f);
                        for (int i = 0; i < loop.Length; i++) loop[i] = bp.Process(n.White()) * 0.6f + n.White() * 0.15f;
                        noiseClip = be.CreateClip("loop_noise", loop, Dsp.SR);
                    }
                    c.Clip = noiseClip;
                }
                else return;
                c.Vol = 0;
                be.SetLoop((int)ch, c.Clip, 0, c.Pitch);
            }
            c.Target = vol;
            c.FadeSpeed = fade <= 0 ? 999f : 1f / fade;
        }

        public void Update(float dt)
        {
            for (int i = 0; i < chans.Length; i++)
            {
                var c = chans[i];
                if (c.Clip < 0 && c.PendingId == Music.None) continue;
                float before = c.Vol;
                c.Vol = M.Approach(c.Vol, c.Target, c.FadeSpeed * dt);
                if (c.PendingId != Music.None && c.Vol <= 0.001f)
                {
                    var id = c.PendingId;
                    c.PendingId = Music.None;
                    Switch(c, i, id, 0f);
                    c.Target = c.PendingVol;
                }
                if (Math.Abs(before - c.Vol) > 0.0001f || c.Vol > 0)
                    be.SetLoopVolume(i, c.Vol * MusGain);
            }
        }

        public void RefreshVolumes()
        {
            for (int i = 0; i < chans.Length; i++) be.SetLoopVolume(i, chans[i].Vol * MusGain);
        }

        // ───────── 음성 ─────────

        public VoiceLine GetVoice(string text, VoiceKind kind, int[] melody = null)
        {
            string key = kind + "|" + text;
            VoiceLine v;
            if (voiceCache.TryGetValue(key, out v)) return v;
            v = Voice.Speak(text, kind, (uint)(text.GetHashCode() & 0x7FFFFFFF) + 1, melody);
            voiceCache[key] = v;
            voiceClips[key] = be.CreateClip("voice", v.Samples, Dsp.SR);
            voiceOrder.Add(key);
            // 캐시가 너무 커지면 오래된 것부터 버린다
            if (voiceOrder.Count > 48)
            {
                string old = voiceOrder[0];
                voiceOrder.RemoveAt(0);
                voiceCache.Remove(old);
                int clip;
                if (voiceClips.TryGetValue(old, out clip)) { be.ReleaseClip(clip); voiceClips.Remove(old); }
            }
            return v;
        }

        public void PlayVoice(string text, VoiceKind kind, float vol = 1f, int[] melody = null)
        {
            GetVoice(text, kind, melody);
            string key = kind + "|" + text;
            int clip;
            if (voiceClips.TryGetValue(key, out clip)) be.PlayOneShot(clip, vol * SfxGain, 1f, 0f);
        }
    }
}
