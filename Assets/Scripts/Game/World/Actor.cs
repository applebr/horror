using System;
using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>스크립트로 움직이는 인물 (엄마 등).</summary>
    public sealed class Actor : Entity
    {
        public Sprite[] Down, Up, Side;
        public int Dir;
        public bool Walking;
        float animT;
        public Sprite Override;
        public bool Silhouette;
        public byte SilhouetteColor = Pal.Black;

        public Actor(string name, Sprite[] down, Sprite[] up, Sprite[] side, float x, float y)
        {
            Name = name;
            Down = down; Up = up; Side = side;
            X = x; Y = y;
            SetBox(-3, -3, 3, 0);
            Solid = true;
        }

        public void FaceDir(float dx, float dy)
        {
            if (Math.Abs(dx) > Math.Abs(dy)) Dir = dx < 0 ? 2 : 3;
            else Dir = dy < 0 ? 1 : 0;
        }

        public void FaceTo(Entity e) { FaceDir(e.X - X, e.Y - Y); }

        public override void Update(float dt)
        {
            if (Walking) animT += dt; else animT = 0;
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            Sprite s = Override;
            if (s == null)
            {
                Sprite[] set = Dir == 1 ? Up : (Dir == 0 ? Down : Side);
                if (set == null || set.Length == 0) set = Down;
                int f = Walking && set.Length > 1 ? ((int)(animT * 6)) % set.Length : 0;
                s = set[f];
            }
            int px = ox + (int)Math.Round(X), py = oy + (int)Math.Round(Y);
            if (Silhouette) c.BlitSilhouette(s, px, py, Dir == 2, SilhouetteColor);
            else c.Blit(s, px, py, Dir == 2);
        }
    }
}
