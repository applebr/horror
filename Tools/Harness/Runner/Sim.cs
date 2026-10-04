using System;
using System.Collections.Generic;
using System.IO;
using Gawi.Engine;
using Gawi.Engine.Audio;
using Gawi.Game;

namespace Gawi.Harness
{
    /// <summary>오디오 호출을 기록하는 백엔드 (소리 이벤트 검사용).</summary>
    public sealed class RecordingAudio : IAudioBackend
    {
        int next = 1;
        public int OneShots;
        public readonly Dictionary<int, string> Names = new Dictionary<int, string>();
        public readonly List<string> Log = new List<string>();
        public int CreateClip(string name, float[] samples, int sampleRate)
        {
            foreach (var s in samples)
                if (float.IsNaN(s) || float.IsInfinity(s)) throw new Exception("NaN in clip " + name);
            Names[next] = name;
            return next++;
        }
        public void ReleaseClip(int clip) { }
        public void PlayOneShot(int clip, float volume, float pitch, float pan)
        {
            OneShots++;
            if (Log.Count < 5000) Log.Add(Names[clip]);
            if (float.IsNaN(volume) || float.IsNaN(pitch) || float.IsNaN(pan)) throw new Exception("NaN audio param");
        }
        public void SetLoop(int channel, int clip, float volume, float pitch) { }
        public void SetLoopVolume(int channel, float volume) { if (float.IsNaN(volume)) throw new Exception("NaN loop vol"); }
        public void SetLoopPitch(int channel, float pitch) { }
        public void StopAllOneShots() { }
    }

    /// <summary>헤드리스로 게임을 돌리는 도구.</summary>
    public sealed class Sim
    {
        public readonly GameCore G;
        public readonly RecordingAudio Audio = new RecordingAudio();
        public readonly MemorySaveStore Store = new MemorySaveStore();
        public int Frames;
        public string OutDir;
        readonly byte[] rgba = new byte[GameCore.W * GameCore.H * 4];
        public bool Verbose;
        /// <summary>"C 키" 안내가 나오면 C를 누른다.</summary>
        public bool PressCForPrompts = true;

        public Sim(string outDir)
        {
            OutDir = outDir;
            Directory.CreateDirectory(outDir);
            G = new GameCore(Program.LoadFont("galmuri11"), Program.LoadFont("galmuri9"), Program.LoadFont("galmuri7"),
                Audio, Store, new NullPlatform());
        }

        public PlayState P { get { return G.Play; } }

        object lastDeathCs;
        public void Tick(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                G.Tick(GameCore.Dt);
                Frames++;
                var dc = G.Cutscene as DeathCutscene;
                if (dc != null && dc != lastDeathCs)
                {
                    lastDeathCs = dc;
                    Console.WriteLine(string.Format("   !! 죽음: {0} in {1} at ({2:0},{3:0}) t={4:0.0}s", dc.Kind, P != null && P.Room != null ? P.Room.Def.Id : "-", P != null ? P.Player.X : 0, P != null ? P.Player.Y : 0, Frames / 60f));
                    if (Verbose) Shot("death_" + Frames);
                }
            }
        }

        public void Seconds(float s) { Tick((int)(s * 60)); }

        public void Set(Btn b, bool down) { G.Input.SetRaw(b, down); }

        public void Press(Btn b)
        {
            Set(b, true); Tick(2); Set(b, false); Tick(2);
        }

        public void Hold(Btn b, float seconds)
        {
            Set(b, true); Seconds(seconds); Set(b, false); Tick(1);
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < (int)Btn.Count; i++) Set((Btn)i, false);
        }

        public void Shot(string name, int scale = 2)
        {
            G.Render();
            G.Present(rgba);
            ImageIO.SaveRgbaFlipped(Path.Combine(OutDir, name + ".png"), rgba, GameCore.W, GameCore.H, scale);
            if (Verbose) Console.WriteLine("  shot " + name + " @" + Frames);
        }

        /// <summary>조건이 될 때까지 진행. 실패하면 예외.</summary>
        public void Until(Func<bool> cond, float maxSeconds, string what)
        {
            int max = (int)(maxSeconds * 60);
            for (int i = 0; i < max; i++)
            {
                if (cond()) return;
                Tick();
            }
            if (!cond())
            {
                Shot("FAIL_" + Safe(what));
                throw new Exception("Timeout waiting for: " + what + " (room=" + (P != null && P.Room != null ? P.Room.Def.Id : "-") + ", dialogue=" + G.Dialogue.Active + ")");
            }
        }

        static string Safe(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) && c < 128 ? c : '_');
            return sb.ToString();
        }

        /// <summary>대화/쪽지가 열려 있으면 넘긴다. 선택지면 choice 번째.</summary>
        public void Advance(int choice = 0)
        {
            int guard = 0;
            while ((G.Dialogue.Active || G.Note.Active) && guard++ < 80)
            {
                if (G.Dialogue.Active && G.Dialogue.CurrentSpeaker == Speaker.Sys && G.Dialogue.Text.Contains("C 키") && !G.Dialogue.Text.Contains("세기\n"))
                {
                    if (PressCForPrompts) { Press(Btn.Check); continue; }
                    return;
                }
                Tick(3);
                if (G.Dialogue.Active && IsChoice())
                {
                    // 선택지가 나올 때까지 글자를 다 보이게
                    Press(Btn.Confirm);
                    for (int i = 0; i < choice; i++) Press(Btn.Down);
                    Press(Btn.Confirm);
                    continue;
                }
                Press(Btn.Confirm);
            }
        }

        /// <summary>대화 한 줄만 넘긴다.</summary>
        public void AdvanceOne(int choice = 0)
        {
            Tick(3);
            if (PressCForPrompts && G.Dialogue.Active && G.Dialogue.CurrentSpeaker == Speaker.Sys && G.Dialogue.Text.Contains("C 키"))
            {
                Press(Btn.Check);
                return;
            }
            if (G.Dialogue.Active && IsChoice())
            {
                Press(Btn.Confirm);
                for (int i = 0; i < choice; i++) Press(Btn.Down);
                Press(Btn.Confirm);
                return;
            }
            if (G.Dialogue.Active || G.Note.Active) Press(Btn.Confirm);
        }

        bool IsChoice()
        {
            var f = typeof(Dialogue).GetField("options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f.GetValue(G.Dialogue) != null;
        }

        /// <summary>대화를 한 줄씩 넘기며 조건이 될 때까지 진행 (컷신 포함).</summary>
        public void AdvanceUntil(Func<bool> cond, float maxSeconds, string what, int choice = 0)
        {
            int max = (int)(maxSeconds * 60);
            for (int i = 0; i < max; i++)
            {
                if (cond()) return;
                if (G.Dialogue.Active || G.Note.Active) { AdvanceOne(choice); i += 8; }
                else Tick();
            }
            if (!cond())
            {
                Shot("FAIL_" + Safe(what));
                throw new Exception("Timeout: " + what + " (room=" + (P != null && P.Room != null ? P.Room.Def.Id : "-") + ")");
            }
        }

        // ───────── 자동 이동 ─────────

        /// <summary>BFS로 목표 지점까지 걸어간다 (실제 입력 사용).</summary>
        public bool WalkTo(float tx, float ty, float maxSeconds = 20f, float tol = 2.5f)
        {
            var ps = P;
            int max = (int)(maxSeconds * 60);
            var roomAtStart = ps.Room;
            var path = new List<int>();
            int repath = 0;
            float lastX = ps.Player.X, lastY = ps.Player.Y;
            int stuck = 0;
            for (int i = 0; i < max; i++)
            {
                if (ps.Room != roomAtStart) { ReleaseAll(); return true; }
                if ((G.Dialogue.Active && !G.Dialogue.IsTimed) || G.Note.Active) { ReleaseAll(); AdvanceOne(); continue; }
                var pl = ps.Player;
                float dx = tx - pl.X, dy = ty - pl.Y;
                if (Math.Abs(dx) <= tol && Math.Abs(dy) <= tol) { ReleaseAll(); Tick(); return true; }
                if (repath-- <= 0 || path.Count == 0)
                {
                    path = FindPath(pl.X, pl.Y - 1, tx, ty - 1);
                    repath = 20;
                }
                float wx = tx, wy = ty;
                // 다음 웨이포인트
                while (path.Count > 1)
                {
                    int c = path[0];
                    float cx = (c % ps.Room.CW) * Room.CS + 2, cy = (c / ps.Room.CW) * Room.CS + 3;
                    if (Math.Abs(cx - pl.X) < 1.6f && Math.Abs(cy - pl.Y) < 1.6f) path.RemoveAt(0);
                    else break;
                }
                if (path.Count > 1)
                {
                    int c = path[0];
                    wx = (c % ps.Room.CW) * Room.CS + 2; wy = (c / ps.Room.CW) * Room.CS + 3;
                }
                ReleaseAll();
                float ddx = wx - pl.X, ddy = wy - pl.Y;
                if (ddx < -0.8f) Set(Btn.Left, true);
                else if (ddx > 0.8f) Set(Btn.Right, true);
                if (ddy < -0.8f) Set(Btn.Up, true);
                else if (ddy > 0.8f) Set(Btn.Down, true);
                Tick();
                if (Math.Abs(pl.X - lastX) < 0.01f && Math.Abs(pl.Y - lastY) < 0.01f && !pl.Frozen && !ps.ScriptBusy) stuck++;
                else stuck = 0;
                lastX = pl.X; lastY = pl.Y;
                if (stuck > 90) { ReleaseAll(); return false; }
            }
            ReleaseAll();
            return false;
        }

        List<int> FindPath(float fx, float fy, float tx, float ty)
        {
            var room = P.Room;
            int w = room.CW, h = room.CH;
            var block = new bool[w * h];
            for (int cy = 0; cy < h; cy++)
                for (int cx = 0; cx < w; cx++)
                {
                    bool b = room.IsSolid(cx, cy, G, P.Lucid);
                    // 플레이어 상자(6px) 때문에 벽 옆 칸도 피한다 (옆으로만)
                    block[cy * w + cx] = b;
                }
            foreach (var e in room.Entities)
            {
                if (!e.Solid || e.Removed || e == P.Player) continue;
                int x0 = (int)Math.Floor((e.L - 3) / Room.CS), x1 = (int)Math.Floor((e.R + 2.99f) / Room.CS);
                int y0 = (int)Math.Floor((e.T - 0) / Room.CS), y1 = (int)Math.Floor((e.B + 3) / Room.CS);
                for (int cy = y0; cy <= y1; cy++)
                    for (int cx = x0; cx <= x1; cx++)
                        if (cx >= 0 && cy >= 0 && cx < w && cy < h) block[cy * w + cx] = true;
            }
            // 벽에 붙은 칸 (가로로 플레이어가 끼는 곳) 비용을 높이는 대신 막는다
            var soft = new bool[w * h];
            for (int cy = 0; cy < h; cy++)
                for (int cx = 0; cx < w; cx++)
                {
                    if (block[cy * w + cx]) continue;
                    bool l = cx > 0 && block[cy * w + cx - 1], r = cx < w - 1 && block[cy * w + cx + 1];
                    bool up = cy > 0 && block[(cy - 1) * w + cx];
                    if ((l && r) || up) continue;
                    if (l || r) soft[cy * w + cx] = true;
                }
            int s = (int)(fy / Room.CS) * w + (int)(fx / Room.CS);
            int t = (int)(ty / Room.CS) * w + (int)(tx / Room.CS);
            var prev = new int[w * h];
            var dist = new int[w * h];
            for (int i = 0; i < prev.Length; i++) { prev[i] = -1; dist[i] = int.MaxValue; }
            var q = new SortedSet<long>();
            if (s < 0 || s >= w * h) return new List<int>();
            dist[s] = 0;
            q.Add(s);
            while (q.Count > 0)
            {
                long top = q.Min; q.Remove(top);
                int c = (int)(top & 0xFFFFF);
                int d = (int)(top >> 20);
                if (d > dist[c]) continue;
                if (c == t) break;
                int cx = c % w, cy = c / w;
                int[] nx = { cx - 1, cx + 1, cx, cx };
                int[] ny = { cy, cy, cy - 1, cy + 1 };
                for (int k = 0; k < 4; k++)
                {
                    if (nx[k] < 0 || ny[k] < 0 || nx[k] >= w || ny[k] >= h) continue;
                    int n = ny[k] * w + nx[k];
                    if (block[n] && n != t) continue;
                    int nd = d + 10 + (soft[n] ? 25 : 0);
                    if (nd < dist[n]) { dist[n] = nd; prev[n] = c; q.Add(((long)nd << 20) | (uint)n); }
                }
            }
            var path = new List<int>();
            if (prev[t] == -1 && t != s) return path;
            for (int c = t; c != -1 && c != s; c = prev[c]) path.Add(c);
            path.Reverse();
            return path;
        }

        /// <summary>대상 엔티티 앞에 가서 조사한다. 가구 위 물건이면 가구 앞에 선다.</summary>
        public void InteractWith(string name, int choice = 0, float maxSeconds = 25f)
        {
            var e = P.Room.Find(name);
            if (e == null) throw new Exception("No entity " + name + " in " + P.Room.Def.Id);
            float cx = (e.L + e.R) * 0.5f, cy = (e.T + e.B) * 0.5f;
            // 후보 위치: 아래(위를 봄), 왼쪽, 오른쪽, 위 — 거리를 늘려 가며 갈 수 있는 곳
            float[] dxs = { 0, -1, 1, 0 };
            float[] dys = { 1, 0, 0, -1 };
            float bx = 0, by = 0; bool found = false; int dirIdx = 0;
            for (int dist = 4; dist <= 16 && !found; dist += 2)
                for (int k = 0; k < 4 && !found; k++)
                {
                    float tx = dxs[k] == 0 ? cx : (dxs[k] < 0 ? e.L - dist : e.R + dist);
                    float ty = dys[k] == 0 ? e.B + 1 : (dys[k] > 0 ? e.B + dist : e.T - 1);
                    if (P.BlockedFor(P.Player, tx, ty)) continue;
                    var path = FindPath(P.Player.X, P.Player.Y - 1, tx, ty - 1);
                    bool same = (int)(P.Player.X / Room.CS) == (int)(tx / Room.CS) && (int)((P.Player.Y - 1) / Room.CS) == (int)((ty - 1) / Room.CS);
                    if (path.Count == 0 && !same) continue;
                    // 그 자리에서 조사 범위 안인가?
                    float px = tx + (dxs[k] == 0 ? 0 : -dxs[k]) * 5f, py = ty - 2f + (dys[k] == 0 ? 0 : -dys[k]) * 5f;
                    if (e.DistTo(px, py) > e.IRange) continue;
                    bx = tx; by = ty; found = true; dirIdx = k;
                }
            if (!found || !WalkTo(bx, by, maxSeconds, 1.2f))
            {
                Shot("FAIL_reach_" + Safe(name));
                throw new Exception("Cannot reach " + name + " in " + P.Room.Def.Id + " player=" + P.Player.X + "," + P.Player.Y);
            }
            Btn face = dirIdx == 0 ? Btn.Up : (dirIdx == 1 ? Btn.Right : (dirIdx == 2 ? Btn.Left : Btn.Down));
            Set(face, true); Tick(1); Set(face, false); Tick(1);
            Press(Btn.Confirm);
            Tick(4);
            if (G.Dialogue.Active || G.Note.Active) Advance(choice);
        }

        public void ExitVia(char key, float maxSeconds = 25f)
        {
            var room = P.Room;
            string id = room.Def.Id;
            var roomObj = room;
            // 출구 칸 하나를 찾는다
            for (int cy = 0; cy < room.CH; cy++)
                for (int cx = 0; cx < room.CW; cx++)
                {
                    if (room.KeyAt(cx, cy) != key) continue;
                    // 가운데 칸 우선
                    if (room.KeyAt(cx + 1, cy) == key && room.KeyAt(cx - 1, cy) != key) continue;
                    float tx = cx * Room.CS + 2, ty = cy * Room.CS + 3;
                    WalkTo(tx, ty, maxSeconds, 1.2f);
                    Until(() => P.Room != roomObj || P.Dying, 3f, "exit " + key + " from " + id);
                    Tick(30);
                    return;
                }
            throw new Exception("No exit " + key + " in " + id);
        }
    }
}
