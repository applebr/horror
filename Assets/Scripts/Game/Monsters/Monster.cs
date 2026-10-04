using System;
using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>괴물 공통: 벽에 미끄러지는 이동, 길찾기 추적.</summary>
    public abstract class Monster : Entity
    {
        protected float animT;
        public int Dir;       // 0 아래 1 위 2 왼쪽 3 오른쪽
        public bool Moving;

        protected Monster()
        {
            SetBox(-2, -2, 2, 0);
        }

        public bool MoveBy(float dx, float dy)
        {
            bool any = false;
            if (dx != 0)
            {
                if (!P.BlockedFor(this, X + dx, Y)) { X += dx; any = true; }
                else
                {
                    for (int s = -1; s <= 1; s += 2)
                        if (!P.BlockedFor(this, X, Y + s * 2) && !P.BlockedFor(this, X + dx, Y + s * 2)) { Y += s * Math.Abs(dx); any = true; break; }
                }
            }
            if (dy != 0)
            {
                if (!P.BlockedFor(this, X, Y + dy)) { Y += dy; any = true; }
                else
                {
                    for (int s = -1; s <= 1; s += 2)
                        if (!P.BlockedFor(this, X + s * 2, Y) && !P.BlockedFor(this, X + s * 2, Y + dy)) { X += s * Math.Abs(dy); any = true; break; }
                }
            }
            return any;
        }

        protected void SetDirFrom(float dx, float dy)
        {
            if (Math.Abs(dx) > Math.Abs(dy) * 1.2f) Dir = dx < 0 ? 2 : 3;
            else if (Math.Abs(dy) > 0.01f) Dir = dy < 0 ? 1 : 0;
        }

        /// <summary>목표(대개 플레이어)를 향해 이동. 시야가 트이면 직진, 아니면 거리장.</summary>
        protected bool ChaseTarget(float tx, float ty, float speed, float dt, FlowField flow)
        {
            float dx = tx - X, dy = ty - Y;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < 0.5f) { Moving = false; return false; }
            float mx, my;
            bool direct = d < 10f || P.Room.LineOfSight(X, Y - 2, tx, ty - 2);
            if (direct) { mx = dx / d; my = dy / d; }
            else if (flow == null || !flow.Dir(X, Y - 1, out mx, out my)) { mx = dx / d; my = dy / d; }
            SetDirFrom(mx, my);
            Moving = MoveBy(mx * speed * dt, my * speed * dt);
            if (Moving) animT += dt;
            return Moving;
        }

        public bool TouchesPlayer(float pad)
        {
            var pl = P.Player;
            if (pl == null || pl.Hidden || pl.Dead) return false;
            return Overlaps(pl.L - pad, pl.T - pad - 4, pl.R + pad, pl.B + pad);
        }

        public float DistToPlayer
        {
            get { var pl = P.Player; return M.Dist(X, Y, pl.X, pl.Y); }
        }
    }
}
