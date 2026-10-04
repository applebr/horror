using System;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>
    /// 부엌의 "엄마". 등을 돌린 채 칼질한다. 칼질 소리가 멈추면 듣고 있다.
    /// 그때 움직이면 돌아본다.
    /// </summary>
    public sealed class MotherThing : Monster
    {
        public enum St { Chop, SlowChop, Listen, Turn, Hunt, Idle, Scripted }
        public St State = St.Chop;
        float stateT;
        float chopT;
        int chopsLeft;
        int frame;
        public float HearRange = 100f;
        public Action OnCaught;
        public float KitchenMinX = -999, KitchenMaxX = 999, KitchenMinY = -999, KitchenMaxY = 999;
        float stepT;
        float headTilt;
        float caughtGrace;

        public MotherThing(float x, float y)
        {
            Name = "mother";
            X = x; Y = y;
            SetBox(-3, -2, 3, 0);
            Solid = false;
        }

        public override void Added()
        {
            chopsLeft = G.Rng.Range(5, 9);
        }

        public bool Listening { get { return State == St.Listen; } }

        public void SetState(St s) { State = s; stateT = 0; }

        public override void Update(float dt)
        {
            stateT += dt;
            var pl = P.Player;
            switch (State)
            {
                case St.Chop:
                case St.SlowChop:
                    {
                        chopT -= dt;
                        float interval = State == St.Chop ? 0.42f : 0.75f;
                        if (chopT <= 0)
                        {
                            chopT = interval;
                            frame = 1;
                            G.Audio.PlayAt(Sfx.Chop, X, Y, pl.X, pl.Y, 0.9f, G.Rng.Range(0.95f, 1.05f), 220f);
                            if (G.Rng.Chance(0.3f)) P.Blood.Burst(X + 3, Y - 9, 9, 2, 10, 12);
                            chopsLeft--;
                            if (chopsLeft <= 0)
                            {
                                if (State == St.Chop) { SetState(St.SlowChop); chopsLeft = 2; }
                                else { SetState(St.Listen); chopsLeft = G.Rng.Range(4, 9); headTilt = 0; caughtGrace = 0.25f; }
                            }
                        }
                        if (chopT < interval - 0.12f) frame = 0;
                        break;
                    }
                case St.Listen:
                    {
                        frame = 0;
                        headTilt = Math.Min(1f, stateT * 2f);
                        caughtGrace -= dt;
                        float d = DistToPlayer;
                        bool heard = pl.Moving && !pl.Hidden && d < HearRange && caughtGrace <= 0;
                        if (heard || (d < 7f && !pl.Hidden))
                        {
                            SetState(St.Turn);
                            G.Audio.Play(Sfx.Sting, 0.9f);
                            G.Fx.Shake(3, 0.4f);
                            if (OnCaught != null) OnCaught();
                            break;
                        }
                        if (stateT > 2.1f)
                        {
                            SetState(St.Chop);
                            chopT = 0.3f;
                        }
                        break;
                    }
                case St.Turn:
                    Moving = false;
                    break;
                case St.Hunt:
                    {
                        // 처음 1.8초는 관절을 꺾으며 일어난다 (도망칠 틈)
                        if (stateT < 1.8f)
                        {
                            Moving = false;
                            if (G.Rng.Chance(dt * 6)) G.Audio.PlayAt(Sfx.Crack, X, Y, pl.X, pl.Y, 0.5f, G.Rng.Range(0.5f, 0.8f));
                            break;
                        }
                        float tx = M.Clamp(pl.X, KitchenMinX, KitchenMaxX);
                        float ty = M.Clamp(pl.Y, KitchenMinY, KitchenMaxY);
                        ChaseTarget(tx, ty, 21f, dt, P.Stalker.Flow);
                        X = M.Clamp(X, KitchenMinX, KitchenMaxX);
                        Y = M.Clamp(Y, KitchenMinY, KitchenMaxY);
                        stepT -= dt;
                        if (stepT <= 0) { stepT = 0.25f; G.Audio.PlayAt(Sfx.Crack, X, Y, pl.X, pl.Y, 0.3f, G.Rng.Range(0.6f, 0.9f)); }
                        if (TouchesPlayer(1f)) P.Kill(DeathKind.Mother);
                        break;
                    }
            }
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            int px = ox + (int)Math.Round(X), py = oy + (int)Math.Round(Y);
            switch (State)
            {
                case St.Turn:
                case St.Idle:
                    c.Blit(Sprites.MotherFront, px, py);
                    break;
                case St.Hunt:
                    c.Blit(Sprites.MotherCrawl[((int)(animT * 5)) % 2], px, py, Dir == 2);
                    break;
                case St.Scripted:
                    c.Blit(Moving ? Sprites.MotherCrawl[((int)(animT * 5)) % 2] : Sprites.MotherFront, px, py);
                    break;
                default:
                    {
                        var s = Sprites.MotherBack[frame];
                        c.Blit(s, px, py);
                        if (State == St.Listen && headTilt > 0.5f)
                        {
                            // 고개를 살짝 기울인다 (듣는 중)
                            c.Set(px + 3, py - 17, Pal.DarkBrown);
                            c.Set(px - 4, py - 15, Pal.Black);
                        }
                        break;
                    }
            }
        }
    }

    /// <summary>얼굴이 오려진 아이들. 빛 안에서는 멈춘다.</summary>
    public sealed class FacelessKid : Monster
    {
        public bool Boy;
        public float Speed = 15f;
        public bool Hunter = true;   // false면 그 자리에서 고개만 돌림
        public float LitT;
        float swayT;
        float glitchT;
        float whisperT;
        public bool Frozen;
        public string ChantLine = "꼭꼭 숨어라, 머리카락 보일라.";

        public FacelessKid(float x, float y, bool boy)
        {
            Name = "kid";
            X = x; Y = y;
            Boy = boy;
            SetBox(-2, -2, 2, 0);
        }

        public override void Added()
        {
            swayT = G.Rng.Range(0, 5);
            glitchT = G.Rng.Range(2f, 4f);
            whisperT = G.Rng.Range(3f, 9f);
        }

        public override void Update(float dt)
        {
            swayT += dt;
            var pl = P.Player;
            bool lit = P.Light.InBeam(P, X, Y - 6);
            if (lit) LitT += dt; else LitT = Math.Max(0, LitT - dt * 2f);
            Frozen = lit;
            if (pl.Dead) return;
            whisperT -= dt;
            if (whisperT <= 0 && DistToPlayer < 90f)
            {
                whisperT = G.Rng.Range(9f, 16f);
                if (!G.Dialogue.Active) G.Audio.PlayVoice(ChantLine, Engine.Audio.VoiceKind.Kids, 0.35f);
            }
            if (!Hunter || lit)
            {
                Moving = false;
                if (lit && LitT > 1.0f)
                {
                    // 빛을 오래 받으면 뒤로 밀린다
                    float dx = X - pl.X, dy = Y - pl.Y;
                    float d = Math.Max(0.1f, (float)Math.Sqrt(dx * dx + dy * dy));
                    MoveBy(dx / d * 10f * dt, dy / d * 10f * dt);
                }
                return;
            }
            ChaseTarget(pl.X, pl.Y, Speed, dt, P.Stalker.Flow);
            // 어둠 속에서 순간 이동하듯 다가온다
            glitchT -= dt;
            if (glitchT <= 0)
            {
                glitchT = G.Rng.Range(2.5f, 5f);
                float dx = pl.X - X, dy = pl.Y - Y;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d > 20 && !P.Light.InBeam(P, X, Y - 6))
                {
                    float jump = Math.Min(8f, d - 14f);
                    if (!P.BlockedFor(this, X + dx / d * jump, Y + dy / d * jump))
                    {
                        X += dx / d * jump; Y += dy / d * jump;
                        G.Fx.StaticFlicker = Math.Max(G.Fx.StaticFlicker, 0.15f);
                        G.Audio.PlayAt(Sfx.Static, X, Y, pl.X, pl.Y, 0.25f, 1.4f);
                    }
                }
            }
            if (TouchesPlayer(0.5f)) P.Kill(DeathKind.Kids);
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            var set = Boy ? Sprites.KidBoy : Sprites.KidGirl;
            int f = ((int)(swayT * 1.5f)) % 2;
            int px = ox + (int)Math.Round(X), py = oy + (int)Math.Round(Y);
            if (Frozen && G.Rng.Chance(0.4f)) px += G.Rng.Range(-1, 1);
            c.Blit(set[f], px, py);
            if (LitT > 0.3f)
            {
                // 얼굴 구멍에서 피가 흐른다
                int len = Math.Min(5, (int)(LitT * 4));
                for (int i = 0; i < len; i++) c.Set(px, py - (Boy ? 8 : 9) + i, Pal.Red);
            }
        }
    }
}
