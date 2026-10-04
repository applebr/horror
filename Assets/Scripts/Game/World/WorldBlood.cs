using System;
using System.Collections.Generic;
using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>
    /// 월드 피 입자. 높이(Z)를 가진 입자가 날아가 바닥에 떨어지면 얼룩으로 남는다.
    /// 웅덩이는 천천히 번진다.
    /// </summary>
    public sealed class WorldBlood
    {
        struct Part
        {
            public float X, Y, Z, VX, VY, VZ;
            public byte C;
            public bool Big;
            public float Life;
        }

        struct Pool
        {
            public float X, Y, R, Max, Rate;
            public byte C;
        }

        readonly List<Part> parts = new List<Part>();
        readonly List<Pool> pools = new List<Pool>();
        readonly Rng rng = new Rng(31337);
        Room room;

        public void SetRoom(Room r)
        {
            room = r;
            parts.Clear();
            pools.Clear();
        }

        public int Count { get { return parts.Count; } }

        public void Burst(float x, float y, float z, int count, float speed, float up)
        {
            for (int i = 0; i < count; i++)
            {
                float a = rng.Range(0, M.TAU);
                float s = rng.Range(0.2f, 1f) * speed;
                parts.Add(new Part
                {
                    X = x, Y = y, Z = z,
                    VX = M.Cos(a) * s, VY = M.Sin(a) * s * 0.7f,
                    VZ = rng.Range(0.3f, 1f) * up,
                    C = rng.Chance(0.6f) ? Pal.Red : (rng.Chance(0.5f) ? Pal.BloodBright : Pal.DarkRed),
                    Big = rng.Chance(0.25f),
                    Life = 4f
                });
            }
        }

        public void Spray(float x, float y, float z, float dx, float dy, int count, float speed, float spread)
        {
            float baseA = M.Atan2(dy, dx);
            for (int i = 0; i < count; i++)
            {
                float a = baseA + rng.Range(-spread, spread);
                float s = rng.Range(0.4f, 1f) * speed;
                parts.Add(new Part
                {
                    X = x, Y = y, Z = z,
                    VX = M.Cos(a) * s, VY = M.Sin(a) * s,
                    VZ = rng.Range(-5f, 25f),
                    C = rng.Chance(0.6f) ? Pal.Red : Pal.BloodBright,
                    Big = rng.Chance(0.2f),
                    Life = 4f
                });
            }
        }

        public void Drip(float x, float y, float z)
        {
            parts.Add(new Part { X = x, Y = y, Z = z, VZ = 0, C = Pal.Red, Life = 4f });
        }

        public void GrowPool(float x, float y, float maxR, float rate, byte c = Pal.DarkRed)
        {
            pools.Add(new Pool { X = x, Y = y, R = 0.5f, Max = maxR, Rate = rate, C = c });
        }

        public void InstantPool(float x, float y, float r, byte c = Pal.DarkRed)
        {
            if (room == null) return;
            for (int yy = (int)(y - r); yy <= (int)(y + r); yy++)
                for (int xx = (int)(x - r * 1.3f); xx <= (int)(x + r * 1.3f); xx++)
                {
                    float dx = (xx - x) / 1.3f, dy = yy - y;
                    float rr = r * (0.8f + 0.3f * M.Hash(xx, yy, 11));
                    if (dx * dx + dy * dy <= rr * rr) room.AddStain(xx, yy, c);
                }
        }

        public void Footprint(float x, float y)
        {
            if (room == null) return;
            int ix = (int)x + rng.Range(-1, 1), iy = (int)y;
            room.AddStain(ix, iy, Pal.Maroon);
            room.AddStain(ix + 1, iy, Pal.Maroon);
        }

        public void Update(float dt)
        {
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i];
                p.VZ -= 160f * dt;
                p.X += p.VX * dt;
                p.Y += p.VY * dt;
                p.Z += p.VZ * dt;
                p.Life -= dt;
                if (p.Z <= 0 || p.Life <= 0)
                {
                    if (room != null)
                    {
                        room.AddStain((int)p.X, (int)p.Y, p.C == Pal.BloodBright ? Pal.Red : p.C);
                        if (p.Big)
                        {
                            room.AddStain((int)p.X + 1, (int)p.Y, Pal.DarkRed);
                            room.AddStain((int)p.X, (int)p.Y + 1, Pal.DarkRed);
                        }
                    }
                    parts.RemoveAt(i);
                    continue;
                }
                parts[i] = p;
            }
            for (int i = pools.Count - 1; i >= 0; i--)
            {
                var pl = pools[i];
                pl.R = Math.Min(pl.Max, pl.R + pl.Rate * dt);
                if (room != null)
                {
                    int r = (int)Math.Ceiling(pl.R);
                    for (int yy = -r; yy <= r; yy++)
                        for (int xx = (int)(-r * 1.4f); xx <= (int)(r * 1.4f); xx++)
                        {
                            float dx = xx / 1.4f;
                            float rr = pl.R * (0.75f + 0.35f * M.Hash((int)pl.X + xx, (int)pl.Y + yy, 5));
                            if (dx * dx + yy * yy <= rr * rr)
                                room.AddStain((int)pl.X + xx, (int)pl.Y + yy, pl.C);
                        }
                }
                if (pl.R >= pl.Max) { pools.RemoveAt(i); continue; }
                pools[i] = pl;
            }
        }

        public void Draw(Canvas c, int ox, int oy)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                int x = ox + (int)p.X, y = oy + (int)(p.Y - p.Z);
                c.Set(x, y, p.C);
                if (p.Big) c.Set(x, y + 1, p.C);
            }
        }
    }
}
