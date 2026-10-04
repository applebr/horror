using System;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>잘린 손가락. 떼로 기어 온다. 손전등 빛에 타 죽는다.</summary>
    public sealed class Crawler : Monster
    {
        float vx, vy;
        float speed;
        float burn;
        float wiggle;
        float jitterA;
        float jitterT;
        float bounce;
        public float Delay;     // 처음 움직이기까지
        float skitterT;

        public Crawler(float x, float y, float delay)
        {
            Name = "finger";
            X = x; Y = y;
            Delay = delay;
            SetBox(-1.5f, -1.5f, 1.5f, 0);
        }

        public override void Added()
        {
            speed = G.Rng.Range(28f, 44f);
            wiggle = G.Rng.Range(0, 6);
            jitterA = G.Rng.Range(-1f, 1f);
        }

        public override void Update(float dt)
        {
            wiggle += dt * 14f;
            var pl = P.Player;
            if (Delay > 0) { Delay -= dt; return; }
            if (pl.Dead) return;

            if (P.Light.InBeam(P, X, Y - 1))
            {
                burn += dt;
                if (G.Rng.Chance(dt * 20)) P.Blood.Burst(X, Y, 2, 1, 10, 10);
                if (burn > 0.28f)
                {
                    Removed = true;
                    P.Blood.Burst(X, Y, 2, 10, 26, 30);
                    P.Blood.InstantPool(X, Y, 1.5f, Pal.Red);
                    G.Audio.PlayAt(Sfx.Splat, X, Y, pl.X, pl.Y, 0.6f, G.Rng.Range(1.1f, 1.5f));
                    G.Flags.Inc("fingers_burned");
                    return;
                }
            }
            else burn = Math.Max(0, burn - dt);

            jitterT -= dt;
            if (jitterT <= 0) { jitterT = G.Rng.Range(0.15f, 0.5f); jitterA = G.Rng.Range(-1.1f, 1.1f); }
            if (bounce > 0)
            {
                bounce -= dt;
                MoveBy(vx * dt, vy * dt);
                return;
            }
            float dx = pl.X - X, dy = (pl.Y - 2) - Y;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            float mx, my;
            if (d < 30f || P.Room.LineOfSight(X, Y, pl.X, pl.Y - 2)) { mx = dx / Math.Max(0.1f, d); my = dy / Math.Max(0.1f, d); }
            else if (!P.Stalker.Flow.Dir(X, Y, out mx, out my)) { mx = dx / Math.Max(0.1f, d); my = dy / Math.Max(0.1f, d); }
            // 꿈틀거리며 비틀어 간다
            float a = M.Atan2(my, mx) + jitterA * 0.7f;
            vx = M.Cos(a) * speed; vy = M.Sin(a) * speed;
            Moving = MoveBy(vx * dt, vy * dt);
            skitterT -= dt;
            if (skitterT <= 0 && d < 90)
            {
                skitterT = G.Rng.Range(0.6f, 1.4f);
                G.Audio.PlayAt(Sfx.Skitter, X, Y, pl.X, pl.Y, 0.35f, G.Rng.Range(0.9f, 1.3f), 100f);
            }
            if (d < 5f && !pl.Hidden && pl.Hurt <= 0)
            {
                // 물었다
                pl.Fear = Math.Min(1f, pl.Fear + 0.34f);
                pl.Hurt = 0.6f;
                pl.PushX = -dx / Math.Max(0.1f, d) * -60f;
                pl.PushY = -dy / Math.Max(0.1f, d) * -60f;
                G.Audio.Play(Sfx.Gore, 0.45f, 1.5f);
                G.Fx.Shake(1.5f, 0.2f);
                P.Blood.Burst(pl.X, pl.Y - 3, 4, 4, 18, 18);
                bounce = 0.35f;
                vx = -vx * 1.4f; vy = -vy * 1.4f;
                if (pl.Fear >= 0.999f) P.Kill(DeathKind.Fingers);
            }
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            if (Delay > 0 && ((int)(Delay * 10)) % 2 == 0 && Delay < 0.5f) return;
            float a = M.Atan2(vy, vx);
            if (vx == 0 && vy == 0) a = M.PI * 0.5f;
            float cx = ox + X, cy = oy + Y - 1;
            float ux = M.Cos(a), uy = M.Sin(a);
            float px = -uy, py = ux;
            float w = M.Sin(wiggle) * 0.8f;
            // 꼬리(잘린 단면) → 마디 → 손톱
            for (int i = -3; i <= 3; i++)
            {
                float bend = w * (1 - Math.Abs(i) / 3f);
                int x = (int)Math.Round(cx + ux * i * 0.8f + px * bend);
                int y = (int)Math.Round(cy + uy * i * 0.8f + py * bend);
                byte col;
                if (i <= -3) col = Pal.Red;
                else if (i == -2) col = Pal.DarkRed;
                else if (i == 0) col = Pal.SkinShadow;
                else if (i >= 3) col = Pal.Bone;
                else col = burn > 0.1f ? Pal.Flesh : Pal.Skin;
                c.Set(x, y, col);
                if (i > -3 && i < 3) c.Set(x, y + 1, burn > 0.1f ? Pal.Red : Pal.SkinShadow);
            }
        }
    }
}
