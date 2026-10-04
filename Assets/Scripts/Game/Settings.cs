using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gawi.Engine;

namespace Gawi.Game
{
    public sealed class Settings
    {
        public int Volume = 8;          // 0..10
        public int MusicVolume = 8;     // 0..10
        public bool Crt = true;
        public bool ReduceFlash;
        public int TextSpeed = 1;       // 0 느림, 1 보통, 2 빠름
        public bool Fullscreen;

        public void Load(ISaveStore s, bool platformFullscreen)
        {
            Volume = ParseInt(s.Get("opt_volume", "8"), 8, 0, 10);
            MusicVolume = ParseInt(s.Get("opt_music", "8"), 8, 0, 10);
            Crt = s.Get("opt_crt", "1") == "1";
            ReduceFlash = s.Get("opt_reduceflash", "0") == "1";
            TextSpeed = ParseInt(s.Get("opt_textspeed", "1"), 1, 0, 2);
            // 저장된 값이 없으면 지금 화면 상태를 따른다
            Fullscreen = s.Get("opt_fullscreen", platformFullscreen ? "1" : "0") == "1";
        }

        public void Save(ISaveStore s)
        {
            s.Set("opt_volume", Volume.ToString(CultureInfo.InvariantCulture));
            s.Set("opt_music", MusicVolume.ToString(CultureInfo.InvariantCulture));
            s.Set("opt_crt", Crt ? "1" : "0");
            s.Set("opt_reduceflash", ReduceFlash ? "1" : "0");
            s.Set("opt_textspeed", TextSpeed.ToString(CultureInfo.InvariantCulture));
            s.Set("opt_fullscreen", Fullscreen ? "1" : "0");
            s.Flush();
        }

        static int ParseInt(string v, int def, int min, int max)
        {
            int r;
            if (!int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out r)) r = def;
            return Math.Max(min, Math.Min(max, r));
        }

        public float CharsPerSecond
        {
            get { return TextSpeed == 0 ? 16f : (TextSpeed == 1 ? 30f : 70f); }
        }
    }

    /// <summary>스토리 진행 플래그와 체크포인트.</summary>
    public sealed class Flags
    {
        readonly HashSet<string> set = new HashSet<string>();
        readonly Dictionary<string, int> counters = new Dictionary<string, int>();

        public bool Has(string f) { return set.Contains(f); }
        public void Set(string f) { set.Add(f); }
        public void Clear(string f) { set.Remove(f); }
        public bool SetOnce(string f) { return set.Add(f); }

        public int Get(string k) { int v; return counters.TryGetValue(k, out v) ? v : 0; }
        public void Put(string k, int v) { counters[k] = v; }
        public int Inc(string k) { int v = Get(k) + 1; counters[k] = v; return v; }

        public void Reset() { set.Clear(); counters.Clear(); }

        public string Serialize()
        {
            var sb = new StringBuilder();
            foreach (var f in set) sb.Append(f).Append(';');
            sb.Append('|');
            foreach (var kv in counters) sb.Append(kv.Key).Append('=').Append(kv.Value.ToString(CultureInfo.InvariantCulture)).Append(';');
            return sb.ToString();
        }

        public void Deserialize(string s)
        {
            Reset();
            if (string.IsNullOrEmpty(s)) return;
            var parts = s.Split('|');
            foreach (var f in parts[0].Split(';')) if (f.Length > 0) set.Add(f);
            if (parts.Length > 1)
            {
                foreach (var kv in parts[1].Split(';'))
                {
                    int eq = kv.IndexOf('=');
                    if (eq <= 0) continue;
                    int v;
                    if (int.TryParse(kv.Substring(eq + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                        counters[kv.Substring(0, eq)] = v;
                }
            }
        }
    }
}
