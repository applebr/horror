using System.Collections.Generic;

namespace Gawi.Engine
{
    /// <summary>키-값 저장소. Unity에서는 PlayerPrefs, 테스트에서는 메모리.</summary>
    public interface ISaveStore
    {
        string Get(string key, string fallback);
        void Set(string key, string value);
        void Delete(string key);
        void Flush();
    }

    public sealed class MemorySaveStore : ISaveStore
    {
        readonly Dictionary<string, string> d = new Dictionary<string, string>();
        public string Get(string key, string fallback) { string v; return d.TryGetValue(key, out v) ? v : fallback; }
        public void Set(string key, string value) { d[key] = value; }
        public void Delete(string key) { d.Remove(key); }
        public void Flush() { }
    }

    /// <summary>호스트(Unity)에게 요청하는 플랫폼 기능.</summary>
    public interface IPlatform
    {
        bool Fullscreen { get; set; }
        void Quit();
        bool CanQuit { get; }
    }

    public sealed class NullPlatform : IPlatform
    {
        public bool Fullscreen { get; set; }
        public bool QuitRequested;
        public void Quit() { QuitRequested = true; }
        public bool CanQuit { get { return true; } }
    }
}
