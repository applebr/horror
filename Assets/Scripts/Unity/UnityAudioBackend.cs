using System.Collections.Generic;
using UnityEngine;
using Gawi.Engine.Audio;

namespace Gawi.UnityLayer
{
    /// <summary>
    /// 게임이 합성한 float[] 소리를 AudioClip으로 만들어 재생한다.
    /// 효과음은 AudioSource 풀, 음악/앰비언스는 채널별 루프 소스.
    /// (WebGL에서도 동작하도록 OnAudioFilterRead는 쓰지 않는다)
    /// </summary>
    public sealed class UnityAudioBackend : MonoBehaviour, IAudioBackend
    {
        const int Voices = 20;
        readonly List<AudioClip> clips = new List<AudioClip>();
        AudioSource[] oneShots;
        float[] startTimes;
        AudioSource[] loops;

        void Awake()
        {
            oneShots = new AudioSource[Voices];
            startTimes = new float[Voices];
            for (int i = 0; i < Voices; i++) oneShots[i] = MakeSource("sfx_" + i, false);
            loops = new AudioSource[(int)Ch.Count];
            for (int i = 0; i < loops.Length; i++) loops[i] = MakeSource("loop_" + (Ch)i, true);
        }

        AudioSource MakeSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            s.dopplerLevel = 0f;
            s.priority = loop ? 64 : 128;
            return s;
        }

        AudioClip Get(int id)
        {
            int i = id - 1;
            if (i < 0 || i >= clips.Count) return null;
            return clips[i];
        }

        public int CreateClip(string name, float[] samples, int sampleRate)
        {
            var clip = AudioClip.Create(name, Mathf.Max(1, samples.Length), 1, sampleRate, false);
            clip.SetData(samples, 0);
            clips.Add(clip);
            return clips.Count;
        }

        public void ReleaseClip(int id)
        {
            var c = Get(id);
            if (c == null) return;
            foreach (var s in oneShots) if (s.clip == c) { s.Stop(); s.clip = null; }
            clips[id - 1] = null;
            Destroy(c);
        }

        public void PlayOneShot(int id, float volume, float pitch, float pan)
        {
            var c = Get(id);
            if (c == null || volume <= 0.001f || oneShots == null) return;
            // 빈 소스, 없으면 가장 오래된 소스
            int pick = -1;
            float oldest = float.MaxValue;
            for (int i = 0; i < oneShots.Length; i++)
            {
                if (!oneShots[i].isPlaying) { pick = i; break; }
                if (startTimes[i] < oldest) { oldest = startTimes[i]; pick = i; }
            }
            var src = oneShots[pick];
            src.Stop();
            src.clip = c;
            src.volume = Mathf.Clamp01(volume);
            src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            src.panStereo = Mathf.Clamp(pan, -1f, 1f);
            src.Play();
            startTimes[pick] = Time.unscaledTime;
        }

        public void SetLoop(int channel, int id, float volume, float pitch)
        {
            if (loops == null || channel < 0 || channel >= loops.Length) return;
            var s = loops[channel];
            var c = id > 0 ? Get(id) : null;
            if (c == null)
            {
                s.Stop();
                s.clip = null;
                return;
            }
            s.volume = Mathf.Clamp01(volume);
            s.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            if (s.clip != c)
            {
                s.clip = c;
                s.loop = true;
                s.Play();
            }
            else if (!s.isPlaying) s.Play();
        }

        public void SetLoopVolume(int channel, float volume)
        {
            if (loops == null || channel < 0 || channel >= loops.Length) return;
            loops[channel].volume = Mathf.Clamp01(volume);
        }

        public void SetLoopPitch(int channel, float pitch)
        {
            if (loops == null || channel < 0 || channel >= loops.Length) return;
            loops[channel].pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        }

        public void StopAllOneShots()
        {
            if (oneShots == null) return;
            foreach (var s in oneShots) s.Stop();
        }

        void OnDestroy()
        {
            foreach (var c in clips) if (c != null) Destroy(c);
            clips.Clear();
        }
    }
}
