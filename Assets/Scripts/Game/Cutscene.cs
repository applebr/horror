using System.Collections;
using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>
    /// 절차적 컷신의 기반. Run()은 타임라인 스크립트(코루틴), Draw()는 현재 상태를 그린다.
    /// </summary>
    public abstract class Cutscene
    {
        protected GameCore g;
        public bool Opaque = true;
        public float T;

        public virtual void Begin(GameCore game)
        {
            g = game;
            T = 0;
        }

        public virtual void End() { }

        public virtual void Tick(float dt) { T += dt; }

        public abstract IEnumerator Run();

        public abstract void Draw(Canvas c);

        public virtual void DrawUi(Canvas c) { }
    }

    /// <summary>간단한 세로 메뉴.</summary>
    public sealed class Menu
    {
        public string[] Items;
        public int Sel;
        public bool[] Disabled;

        public Menu(params string[] items)
        {
            Items = items;
            Disabled = new bool[items.Length];
        }

        /// <summary>선택 확정 시 인덱스, 취소 시 -2, 그 외 -1.</summary>
        public int Update(GameCore g)
        {
            var inp = g.Input;
            if (inp.Repeat(Btn.Up)) Move(g, -1);
            if (inp.Repeat(Btn.Down)) Move(g, 1);
            if (inp.Pressed(Btn.Confirm))
            {
                if (Disabled[Sel]) return -1;
                g.Audio.Play(Engine.Audio.Sfx.MenuSelect, 0.6f);
                inp.Consume(Btn.Confirm);
                return Sel;
            }
            if (inp.Pressed(Btn.Cancel) || inp.Pressed(Btn.Pause))
            {
                inp.Consume(Btn.Cancel);
                inp.Consume(Btn.Pause);
                return -2;
            }
            return -1;
        }

        void Move(GameCore g, int d)
        {
            for (int k = 0; k < Items.Length; k++)
            {
                Sel = (Sel + d + Items.Length) % Items.Length;
                if (!Disabled[Sel]) break;
            }
            g.Audio.Play(Engine.Audio.Sfx.MenuMove, 0.5f);
        }

        public void Draw(GameCore g, Canvas c, int x, int y, bool centered)
        {
            var f = g.Font;
            for (int i = 0; i < Items.Length; i++)
            {
                string s = Items[i];
                int w = f.Measure(s);
                int tx = centered ? x - w / 2 : x;
                int ty = y + i * (f.LineStep + 3);
                byte col = Disabled[i] ? Pal.DarkGray : (i == Sel ? Pal.White : Pal.Gray);
                if (i == Sel && !Disabled[i])
                {
                    int ax = tx - 10;
                    float wob = M.Sin(g.Time * 6) * 1.2f;
                    c.FillTri(ax + wob, ty + 2, ax + wob, ty + 10, ax + 5 + wob, ty + 6, Pal.BloodBright);
                }
                f.Draw(c, s, tx, ty, col);
            }
        }
    }
}
