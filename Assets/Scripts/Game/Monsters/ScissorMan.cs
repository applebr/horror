using System;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>가위. 천장에 머리가 닿을 듯한 마른 형체, 커다란 재단 가위.</summary>
    public sealed class ScissorMan : Monster
    {
        public enum St { Stalk, Lunge, Recoil, Search, Leave, Grab, Vanish, Scripted, Stunned }
        public St State = St.Stalk;
        public float Speed = 24f;
        public bool CanKill = true;
        public bool CanLunge = true;
        public bool Repellable = true;
        public float Exposure;          // 자각 손전등을 맞은 누적 시간
        public float RepelTime = 1.8f;
        public Action OnRepelled;       // 쫓겨났을 때
        public Action<float> OnBeamTick;// 보스전 등: 빛을 맞는 중
        public float Scissor;           // 0 닫힘 .. 1 열림
        public bool FaceLock;           // 스크립트용: 방향 고정
        float stateT;
        float lungeCd = 1.5f;
        float lungeDx, lungeDy;
        float snipT = 1f, stepT;
        float snipAnim;
        float searchT;
        float tx, ty;
        bool hintShown;
        Light selfLight;
        public float Alpha = 1f;        // 사라질 때
        public bool Glitchy;
        public float SpeedMul = 1f;
        public float LungeCooldownMin = 2.0f, LungeCooldownMax = 3.2f;

        public ScissorMan()
        {
            Name = "gawi";
            SetBox(-2, -2, 2, 0);
        }

        public override void Added()
        {
            selfLight = new Light(X, Y - 20, 11, 0.28f);
            P.Room.Lights.Add(selfLight);
        }

        public void SetState(St s)
        {
            State = s;
            stateT = 0;
        }

        public void Snip(float vol = 1f)
        {
            snipAnim = 0.35f;
            var pl = P.Player;
            G.Audio.PlayAt(Sfx.Snip, X, Y, pl.X, pl.Y, vol, G.Rng.Range(0.9f, 1.05f), 200f);
        }

        public override void Update(float dt)
        {
            stateT += dt;
            if (selfLight != null) { selfLight.X = X; selfLight.Y = Y - 22; }
            snipAnim = Math.Max(0, snipAnim - dt);
            Scissor = snipAnim > 0 ? (snipAnim > 0.2f ? 1f : snipAnim / 0.2f) : 0.15f;
            var pl = P.Player;
            if (State == St.Scripted) { if (Moving) animT += dt; return; }
            if (State == St.Vanish)
            {
                Alpha = Math.Max(0, 1 - stateT / 0.8f);
                if (stateT > 0.8f) Remove();
                return;
            }

            // 소리
            snipT -= dt;
            if (snipT <= 0)
            {
                snipT = G.Rng.Range(1.4f, 3.0f);
                Snip(0.9f);
            }
            if (Moving)
            {
                stepT -= dt;
                if (stepT <= 0)
                {
                    stepT = 0.55f;
                    G.Audio.PlayAt(Sfx.Thump, X, Y, pl.X, pl.Y, 0.35f, G.Rng.Range(0.6f, 0.75f), 160f);
                }
            }

            // 손전등
            bool lit = P.Light.InBeam(P, X, Y - 14) || P.Light.InBeam(P, X, Y - 4);
            if (lit && Repellable)
            {
                if (P.Lucid)
                {
                    // 가까이서 비출수록 빨리 쓰러진다 (위험과 보상)
                    float dd = DistToPlayer;
                    Exposure += dt * (dd < 35f ? 1.6f : (dd < 55f ? 1.1f : 0.6f));
                    if (OnBeamTick != null) OnBeamTick(dt);
                    if (State != St.Recoil) { SetState(St.Recoil); G.Audio.Play(Sfx.Scream, 0.35f, 1.4f); }
                    if (Exposure >= RepelTime)
                    {
                        Exposure = 0;
                        G.Audio.Play(Sfx.Scream, 0.7f, 0.9f);
                        G.Fx.Shake(2, 0.4f);
                        P.Blood.Burst(X, Y, 14, 14, 30, 30);
                        if (OnRepelled != null) OnRepelled();
                        else SetState(St.Vanish);
                        return;
                    }
                }
                else if (!hintShown && !G.Flags.Has("hint_beam"))
                {
                    hintShown = true;
                    G.Flags.Set("hint_beam");
                    P.Hud.Notice("빛이 통하지 않아… 꿈인 걸 알아야 해!", 2.8f);
                }
            }
            else if (State == St.Recoil && stateT > 0.25f)
            {
                SetState(St.Stalk);
            }
            if (!lit) Exposure = Math.Max(0, Exposure - dt * 0.2f);

            switch (State)
            {
                case St.Recoil:
                    {
                        // 빛 속에서 비틀거리며 아주 천천히 물러난다 (빛에서 벗어나지 않게)
                        float dx = X - pl.X, dy = Y - pl.Y;
                        float d = Math.Max(0.1f, (float)Math.Sqrt(dx * dx + dy * dy));
                        float back = d < 40f ? 14f : 4f;
                        MoveBy(dx / d * back * dt, dy / d * back * dt);
                        Glitchy = true;
                        Moving = false;
                        break;
                    }
                case St.Stalk:
                    {
                        Glitchy = false;
                        if (pl.Hidden) { SetState(St.Search); searchT = G.Rng.Range(6f, 9f); PickWander(); break; }
                        lungeCd -= dt;
                        float d = DistToPlayer;
                        if (CanLunge && lungeCd <= 0 && d < 30f && d > 8f && P.Room.LineOfSight(X, Y - 3, pl.X, pl.Y - 3))
                        {
                            lungeDx = (pl.X - X) / d; lungeDy = (pl.Y - Y) / d;
                            SetState(St.Lunge);
                            G.Audio.PlayAt(Sfx.Whoosh, X, Y, pl.X, pl.Y, 0.8f, 0.8f);
                            Snip(1f);
                            break;
                        }
                        ChaseTarget(pl.X, pl.Y, Speed * SpeedMul, dt, P.Stalker.Flow);
                        break;
                    }
                case St.Lunge:
                    {
                        Moving = MoveBy(lungeDx * 88f * dt, lungeDy * 88f * dt);
                        SetDirFrom(lungeDx, lungeDy);
                        animT += dt * 2;
                        if (stateT > 0.32f)
                        {
                            lungeCd = G.Rng.Range(LungeCooldownMin, LungeCooldownMax);
                            SetState(St.Stalk);
                        }
                        break;
                    }
                case St.Search:
                    {
                        searchT -= dt;
                        if (!pl.Hidden && P.Room.LineOfSight(X, Y - 10, pl.X, pl.Y - 4)) { SetState(St.Stalk); break; }
                        if (M.Dist(X, Y, tx, ty) < 3 || stateT > 3f) { PickWander(); stateT = 0; }
                        Moving = ChaseTarget(tx, ty, Speed * 0.6f, dt, null);
                        if (searchT <= 0) { SetState(St.Leave); }
                        break;
                    }
                case St.Leave:
                    {
                        Alpha = Math.Max(0, 1 - stateT / 1.5f);
                        if (stateT > 1.5f) { Remove(); P.Stalker.OnLeft(); }
                        break;
                    }
                case St.Grab:
                    {
                        // 숨는 걸 봤다: 숨은 곳으로 간다
                        var spot = P.HideSpot;
                        if (spot == null || !pl.Hidden) { SetState(St.Stalk); break; }
                        float sx = (spot.L + spot.R) * 0.5f, sy = spot.B + 3;
                        ChaseTarget(sx, sy, Speed * 1.1f, dt, P.Stalker.Flow);
                        if (M.Dist(X, Y, sx, sy) < 6f)
                        {
                            P.Unhide();
                            if (CanKill) P.Kill(DeathKind.Scissors);
                        }
                        break;
                    }
                case St.Stunned:
                    Moving = false;
                    if (stateT > 1.2f) SetState(St.Stalk);
                    break;
            }

            if (CanKill && State != St.Leave && State != St.Stunned && TouchesPlayer(1f))
                P.Kill(DeathKind.Scissors);
        }

        void PickWander()
        {
            float x, y;
            if (P.Room.RandomFloor(G.Rng, out x, out y)) { tx = x; ty = y; }
            else { tx = X; ty = Y; }
        }

        public void Remove()
        {
            Removed = true;
            if (selfLight != null) { P.Room.Lights.Remove(selfLight); selfLight = null; }
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            if (Alpha < 1f && ((int)(G.Time * 30)) % 3 != 0 && G.Rng.Float() > Alpha) return;
            int px = ox + (int)Math.Round(X), py = oy + (int)Math.Round(Y);
            if (Glitchy && G.Rng.Chance(0.3f)) px += G.Rng.Range(-2, 2);
            int frame = Moving ? ((int)(animT * 4)) % 2 : 0;
            Sprite s;
            bool flip = false;
            if (Dir == 1) s = Sprites.GawiBack;
            else if (Dir == 2 || Dir == 3) { s = Sprites.GawiSide[frame]; flip = Dir == 2; }
            else s = Sprites.GawiFront[frame];
            c.Blit(s, px, py, flip);
            DrawScissors(c, px, py, flip);
        }

        void DrawScissors(Canvas c, int px, int py, bool flip)
        {
            int hx, hy;
            if (Dir == 2 || Dir == 3)
            {
                hx = px + (flip ? -3 : 3);
                hy = py - 8;
            }
            else if (Dir == 1)
            {
                hx = px - 4; hy = py - 7; // 뒤에서 보면 반대손
            }
            else
            {
                hx = px + 4; hy = py - 6;
            }
            float open = Scissor;
            int len = 10;
            // 손잡이 고리
            c.Set(hx - 1, hy - 2, Pal.Gray); c.Set(hx + 1, hy - 2, Pal.Gray);
            c.Set(hx - 2, hy - 1, Pal.Gray); c.Set(hx + 2, hy - 1, Pal.Gray);
            c.Set(hx, hy, Pal.LightGray);
            // 날
            int sp = (int)Math.Round(open * 4);
            c.Line(hx, hy, hx - sp, hy + len, Pal.LightGray);
            c.Line(hx, hy, hx + sp, hy + len, Pal.White);
            if (G.Rng.Chance(0.05f)) c.Set(hx + sp, hy + len - 2, Pal.White);
        }
    }
}
