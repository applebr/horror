using System;
using System.Collections.Generic;
using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>
    /// 플레이어(또는 목표 지점)까지의 셀 거리장. 괴물들이 공유한다.
    /// </summary>
    public sealed class FlowField
    {
        Room room;
        int w, h;
        int[] dist = new int[0];
        bool[] blocked = new bool[0];
        readonly Queue<int> q = new Queue<int>();
        int tx = -1, ty = -1;
        public float Age = 99f;

        public void Build(PlayState ps, float targetX, float targetY)
        {
            room = ps.Room;
            w = room.CW; h = room.CH;
            if (dist.Length != w * h) { dist = new int[w * h]; blocked = new bool[w * h]; }
            for (int i = 0; i < blocked.Length; i++)
            {
                int cx = i % w, cy = i / w;
                blocked[i] = room.IsSolidForMonster(cx, cy);
                dist[i] = int.MaxValue;
            }
            foreach (var e in room.Entities)
            {
                if (!e.Solid || e.Removed || e == ps.Player) continue;
                int x0 = (int)Math.Floor(e.L / Room.CS), x1 = (int)Math.Floor((e.R - 0.01f) / Room.CS);
                int y0 = (int)Math.Floor(e.T / Room.CS), y1 = (int)Math.Floor((e.B - 0.01f) / Room.CS);
                for (int cy = y0; cy <= y1; cy++)
                    for (int cx = x0; cx <= x1; cx++)
                        if (cx >= 0 && cy >= 0 && cx < w && cy < h) blocked[cy * w + cx] = true;
            }
            tx = (int)Math.Floor(targetX / Room.CS);
            ty = (int)Math.Floor(targetY / Room.CS);
            q.Clear();
            if (tx >= 0 && ty >= 0 && tx < w && ty < h)
            {
                dist[ty * w + tx] = 0;
                q.Enqueue(ty * w + tx);
            }
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                int cx = i % w, cy = i / w;
                int d = dist[i] + 1;
                Relax(cx - 1, cy, d); Relax(cx + 1, cy, d);
                Relax(cx, cy - 1, d); Relax(cx, cy + 1, d);
            }
            Age = 0;
        }

        void Relax(int cx, int cy, int d)
        {
            if (cx < 0 || cy < 0 || cx >= w || cy >= h) return;
            int i = cy * w + cx;
            if (blocked[i] || dist[i] <= d) return;
            dist[i] = d;
            q.Enqueue(i);
        }

        public int DistAt(float x, float y)
        {
            int cx = (int)Math.Floor(x / Room.CS), cy = (int)Math.Floor(y / Room.CS);
            if (cx < 0 || cy < 0 || cx >= w || cy >= h) return int.MaxValue;
            return dist[cy * w + cx];
        }

        /// <summary>다음으로 갈 방향 (정규화). 길이 없으면 false.</summary>
        public bool Dir(float x, float y, out float dx, out float dy)
        {
            dx = dy = 0;
            if (room == null) return false;
            int cx = (int)Math.Floor(x / Room.CS), cy = (int)Math.Floor(y / Room.CS);
            if (cx < 0 || cy < 0 || cx >= w || cy >= h) return false;
            int cur = dist[cy * w + cx];
            int bestScore = cur == int.MaxValue ? int.MaxValue : cur * 10;
            int bx = cx, by = cy;
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0) continue;
                    int nx = cx + ox, ny = cy + oy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    int i = ny * w + nx;
                    if (blocked[i]) continue;
                    if (ox != 0 && oy != 0)
                    {
                        // 대각선은 양옆이 열려 있어야
                        if (blocked[cy * w + nx] || blocked[ny * w + cx]) continue;
                    }
                    int d = dist[i];
                    if (d == int.MaxValue) continue;
                    int score = d * 10 + (ox != 0 && oy != 0 ? 4 : 0);
                    if (score < bestScore) { bestScore = score; bx = nx; by = ny; }
                }
            if (bx == cx && by == cy) return false;
            float tx2 = bx * Room.CS + Room.CS * 0.5f, ty2 = by * Room.CS + Room.CS * 0.5f;
            dx = tx2 - x; dy = ty2 - y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return false;
            dx /= len; dy /= len;
            return true;
        }

        public bool Reachable(float x, float y)
        {
            return DistAt(x, y) != int.MaxValue;
        }
    }
}
