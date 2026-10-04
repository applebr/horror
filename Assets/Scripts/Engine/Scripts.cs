using System;
using System.Collections;
using System.Collections.Generic;

namespace Gawi.Engine
{
    /// <summary>yield return 으로 넘기면 끝날 때까지 기다리는 객체.</summary>
    public interface IWait
    {
        /// <summary>매 틱 호출. 끝났으면 true.</summary>
        bool Tick(float dt);
    }

    public sealed class WaitSeconds : IWait
    {
        float t;
        public WaitSeconds(float seconds) { t = seconds; }
        public bool Tick(float dt) { t -= dt; return t <= 0; }
    }

    public sealed class WaitUntil : IWait
    {
        readonly Func<bool> cond;
        public WaitUntil(Func<bool> c) { cond = c; }
        public bool Tick(float dt) { return cond(); }
    }

    public sealed class WaitWhile : IWait
    {
        readonly Func<bool> cond;
        public WaitWhile(Func<bool> c) { cond = c; }
        public bool Tick(float dt) { return !cond(); }
    }

    /// <summary>시간 동안 매 틱 콜백(0..1 진행도)을 부른다.</summary>
    public sealed class Tween : IWait
    {
        readonly float dur;
        readonly Action<float> fn;
        float t;
        bool started;
        public Tween(float duration, Action<float> f) { dur = Math.Max(0.0001f, duration); fn = f; }
        public bool Tick(float dt)
        {
            if (!started) { started = true; fn(0f); return false; }
            t += dt;
            float p = t / dur;
            if (p >= 1f) { fn(1f); return true; }
            fn(p);
            return false;
        }
    }

    /// <summary>
    /// IEnumerator 기반 스크립트(코루틴) 실행기. 컷신과 이벤트를 순서대로 쓰기 위해 사용.
    /// yield return null: 한 틱 대기 / IWait: 끝날 때까지 / IEnumerator: 하위 스크립트 실행.
    /// </summary>
    public sealed class ScriptRunner
    {
        public sealed class Handle
        {
            internal readonly Stack<IEnumerator> Stack = new Stack<IEnumerator>();
            internal IWait Wait;
            public string Tag;
            public bool Done;
            public bool Paused;
        }

        readonly List<Handle> tasks = new List<Handle>();
        readonly List<Handle> added = new List<Handle>();
        bool ticking;

        public Handle Start(IEnumerator routine, string tag = null)
        {
            var h = new Handle { Tag = tag };
            h.Stack.Push(routine);
            if (ticking) added.Add(h); else tasks.Add(h);
            return h;
        }

        public void Stop(Handle h)
        {
            if (h != null) h.Done = true;
        }

        public void StopTag(string tag)
        {
            foreach (var t in tasks) if (t.Tag == tag) t.Done = true;
            foreach (var t in added) if (t.Tag == tag) t.Done = true;
        }

        public void StopAll()
        {
            foreach (var t in tasks) t.Done = true;
            foreach (var t in added) t.Done = true;
        }

        public bool IsRunning(string tag)
        {
            foreach (var t in tasks) if (!t.Done && t.Tag == tag) return true;
            foreach (var t in added) if (!t.Done && t.Tag == tag) return true;
            return false;
        }

        public int Count
        {
            get
            {
                int n = 0;
                foreach (var t in tasks) if (!t.Done) n++;
                return n + added.Count;
            }
        }

        public void Tick(float dt)
        {
            ticking = true;
            try
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    var h = tasks[i];
                    if (h.Done || h.Paused) continue;
                    Step(h, dt);
                }
            }
            finally
            {
                ticking = false;
            }
            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                var t = tasks[i];
                if (!t.Done) continue;
                DisposeStack(t);
                tasks.RemoveAt(i);
            }
            if (added.Count > 0)
            {
                for (int i = added.Count - 1; i >= 0; i--)
                    if (added[i].Done) { DisposeStack(added[i]); added.RemoveAt(i); }
                tasks.AddRange(added);
                added.Clear();
            }
        }

        /// <summary>중간에 멈춘 스크립트의 finally 블록이 실행되도록 정리.</summary>
        static void DisposeStack(Handle h)
        {
            while (h.Stack.Count > 0)
            {
                var e = h.Stack.Pop();
                var d = e as IDisposable;
                if (d != null)
                {
                    try { d.Dispose(); }
                    catch (Exception) { }
                }
            }
        }

        static void Step(Handle h, float dt)
        {
            if (h.Wait != null)
            {
                if (!h.Wait.Tick(dt)) return;
                h.Wait = null;
            }
            int guard = 0;
            while (h.Stack.Count > 0 && !h.Done)
            {
                if (++guard > 10000) throw new InvalidOperationException("Script loop without yield: " + h.Tag);
                var top = h.Stack.Peek();
                if (!top.MoveNext())
                {
                    h.Stack.Pop();
                    continue;
                }
                object y = top.Current;
                if (y == null) return;
                var sub = y as IEnumerator;
                if (sub != null) { h.Stack.Push(sub); continue; }
                var w = y as IWait;
                if (w != null)
                {
                    // 바로 끝나는 대기는 같은 틱에 계속 진행
                    if (w.Tick(0f)) continue;
                    h.Wait = w;
                    return;
                }
                return;
            }
            if (h.Stack.Count == 0) h.Done = true;
        }
    }
}
