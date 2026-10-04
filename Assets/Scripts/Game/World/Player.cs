using System;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    public sealed class Player : Entity
    {
        public int Dir;            // 0 아래, 1 위, 2 왼쪽, 3 오른쪽
        public float Speed = 38f;
        public bool Moving;
        public float AnimT;
        public float StepT;

        public bool HasLight;
        public float RechargeMul = 1f;
        public bool LightOn;
        public float Battery = 1f;
        float rechargeDelay;
        bool depleted;
        float flicker;

        public bool Counting;      // 손가락 세는 중
        public bool Hidden;
        public Entity HideSpot;
        public bool Frozen;        // 스크립트가 붙잡음
        public bool Dead;
        public float Hurt;         // 맞았을 때 깜빡임
        public float Fear;         // 0..1 공포 (가득 차면 죽음)
        public float PushX, PushY; // 밀려남
        public float VisualOffsetY;
        public bool Lying;         // 침대에 누운 상태 (그리기 생략)

        public Player()
        {
            Name = "player";
            // 4px 폭: 셀 하나에 딱 맞아 좁은 문/모서리에 덜 걸린다
            SetBox(-2, -3, 2, 0);
        }

        public float FaceX { get { return Dir == 2 ? -1 : (Dir == 3 ? 1 : 0); } }
        public float FaceY { get { return Dir == 0 ? 1 : (Dir == 1 ? -1 : 0); } }
        /// <summary>손전등 방향 (8방향, 마지막으로 움직인 방향).</summary>
        public float AimX = 0, AimY = 1;
        float moveHold;
        bool wasMoving;
        /// <summary>멈춘 상태에서 이 시간보다 짧게 누르면 제자리에서 돌아보기만 한다.</summary>
        const float TurnDelay = 0.07f;

        /// <summary>손전등이 실제로 비추는 중인가.</summary>
        public bool Beam { get { return HasLight && LightOn && !depleted && flicker <= 0 && !Hidden && !Dead; } }

        public float ChestY { get { return Y - 6; } }

        public bool CanAct
        {
            get { return !Frozen && !Dead && !Counting && !Hidden && !P.ScriptBusy && !(G.Dialogue.Active && !G.Dialogue.IsTimed) && !G.Note.Active && !G.Input.Blocked; }
        }

        public override void Update(float dt)
        {
            var inp = G.Input;
            Hurt = Math.Max(0, Hurt - dt);
            flicker = Math.Max(0, flicker - dt);

            // 손전등
            bool wantLight = HasLight && inp.Down(Btn.Light) && !Hidden && !Dead && !Frozen && !Counting && !(G.Dialogue.Active && !G.Dialogue.IsTimed) && !G.Note.Active;
            if (wantLight && !LightOn && !depleted)
            {
                LightOn = true;
                G.Audio.Play(Sfx.Click, 0.5f, 1.1f);
            }
            else if (!wantLight && LightOn)
            {
                LightOn = false;
                G.Audio.Play(Sfx.Click, 0.4f, 0.8f);
            }
            if (LightOn && !depleted)
            {
                Battery -= dt * 0.11f;
                rechargeDelay = 0.7f;
                if (Battery < 0.15f && G.Rng.Chance(dt * 4f)) flicker = G.Rng.Range(0.03f, 0.1f);
                if (Battery <= 0)
                {
                    Battery = 0;
                    depleted = true;
                    LightOn = false;
                    G.Audio.Play(Sfx.Click, 0.5f, 0.6f);
                    P.Hud.Notice("배터리가 다 됐다…", 1.5f);
                }
            }
            else
            {
                if (rechargeDelay > 0) rechargeDelay -= dt;
                else Battery = Math.Min(1f, Battery + dt * 0.2f * RechargeMul);
                if (depleted && Battery >= 0.3f) depleted = false;
            }

            // 밀려남
            if (Math.Abs(PushX) > 0.1f || Math.Abs(PushY) > 0.1f)
            {
                TryMove(PushX * dt, 0);
                TryMove(0, PushY * dt);
                PushX *= (float)Math.Pow(0.02, dt);
                PushY *= (float)Math.Pow(0.02, dt);
            }

            if (Hidden)
            {
                Moving = false;
                if (!Frozen && !P.ScriptBusy && (inp.Pressed(Btn.Confirm) || inp.Pressed(Btn.Cancel)))
                {
                    inp.Consume(Btn.Confirm);
                    P.Unhide();
                }
                return;
            }

            if (!CanAct)
            {
                Moving = false;
                wasMoving = false;
                moveHold = 0;
                return;
            }

            if (inp.Pressed(Btn.Pause))
            {
                inp.Consume(Btn.Pause);
                G.PushOverlay(new PauseState());
                return;
            }

            float mx = 0, my = 0;
            if (inp.Down(Btn.Left)) mx -= 1;
            if (inp.Down(Btn.Right)) mx += 1;
            if (inp.Down(Btn.Up)) my -= 1;
            if (inp.Down(Btn.Down)) my += 1;
            bool anyDir = mx != 0 || my != 0;
            if (anyDir)
            {
                // 방향: 마지막으로 바뀐 축을 우선
                if (mx != 0 && my == 0) Dir = mx < 0 ? 2 : 3;
                else if (my != 0 && mx == 0) Dir = my < 0 ? 1 : 0;
                else if (mx != 0 && my != 0)
                {
                    if (inp.Pressed(Btn.Left) || inp.Pressed(Btn.Right)) Dir = mx < 0 ? 2 : 3;
                    else if (inp.Pressed(Btn.Up) || inp.Pressed(Btn.Down)) Dir = my < 0 ? 1 : 0;
                }
                float len = (float)Math.Sqrt(mx * mx + my * my);
                AimX = mx / len; AimY = my / len;
                moveHold += dt;
                // 멈춰 있다가 살짝 누르면 제자리에서 돌아보기 (손전등 조준용)
                Moving = wasMoving || moveHold >= TurnDelay;
            }
            else
            {
                moveHold = 0;
                Moving = false;
            }
            wasMoving = Moving;
            if (Moving)
            {
                float len = (float)Math.Sqrt(mx * mx + my * my);
                float sp = Speed * (LightOn ? 0.85f : 1f) * P.SpeedMul;
                float dx = mx / len * sp * dt, dy = my / len * sp * dt;
                bool movedX = TryMove(dx, 0);
                bool movedY = TryMove(0, dy);
                // 모서리 미끄러지기: 막혔을 때 살짝 비켜 간다
                if (!movedX && dx != 0 && dy == 0) Slide(dx, 0, dt);
                if (!movedY && dy != 0 && dx == 0) Slide(0, dy, dt);
                AnimT += dt;
                StepT -= dt;
                if (StepT <= 0)
                {
                    StepT = 0.32f;
                    var cell = P.Room.At((int)(X / Room.CS), (int)(Y / Room.CS));
                    bool wet = cell == Cell.Blood || P.StainAt(X, Y);
                    G.Audio.Play(wet ? Sfx.StepWet : Sfx.Step, wet ? 0.45f : 0.22f, G.Rng.Range(0.85f, 1.15f));
                    P.Noise(X, Y, 1f);
                    if (wet) P.Blood.Footprint(X, Y);
                }
            }
            else
            {
                AnimT = 0;
                StepT = 0.05f;
            }

            if (inp.Pressed(Btn.Confirm))
            {
                inp.Consume(Btn.Confirm);
                P.TryInteract();
            }
            else if (inp.Pressed(Btn.Check))
            {
                inp.Consume(Btn.Check);
                P.StartRealityCheck();
            }
        }

        /// <summary>모서리에 걸렸을 때 수직 방향으로 살짝 비켜 간다.</summary>
        void Slide(float dx, float dy, float dt)
        {
            float step = Speed * 0.8f * dt;
            for (int k = 1; k <= 4; k++)
                for (int s = -1; s <= 1; s += 2)
                {
                    float ox = dx == 0 ? s * k : 0, oy = dy == 0 ? s * k : 0;
                    if (!P.BlockedFor(this, X + ox, Y + oy) && !P.BlockedFor(this, X + ox + dx, Y + oy + dy))
                    {
                        TryMove(dx == 0 ? s * step : 0, dy == 0 ? s * step : 0);
                        return;
                    }
                }
        }

        public bool TryMove(float dx, float dy)
        {
            if (dx == 0 && dy == 0) return true;
            float nx = X + dx, ny = Y + dy;
            if (P.BlockedFor(this, nx, ny)) return false;
            X = nx; Y = ny;
            return true;
        }

        public void Face(float tx, float ty)
        {
            float dx = tx - X, dy = ty - Y;
            if (Math.Abs(dx) > Math.Abs(dy)) Dir = dx < 0 ? 2 : 3;
            else Dir = dy < 0 ? 1 : 0;
            AimX = FaceX; AimY = FaceY;
        }

        public void ResetLight()
        {
            Battery = 1f;
            depleted = false;
            LightOn = false;
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            if (Hidden || Lying) return;
            if (Hurt > 0 && ((int)(Hurt * 20)) % 2 == 0) return;
            int frame = Moving ? ((int)(AnimT * 7)) % 2 + 1 : 0;
            Sprite s = Sprites.Hana(Dir, frame, Counting);
            int bob = Moving && frame == 1 ? -1 : 0;
            int px = ox + (int)Math.Round(X), py = oy + (int)Math.Round(Y) + bob + (int)VisualOffsetY;
            c.Blit(s, px, py, Dir == 2);
            if (HasLight && Dir != 1)
            {
                // 손에 든 별 손전등
                int lx = Dir == 2 ? px - 4 : (Dir == 3 ? px + 4 : px + 3);
                int ly = py - 4;
                c.Set(lx, ly, Beam ? Pal.PaleYellow : Pal.Yellow);
            }
        }
    }
}
