using System;
using System.Collections;
using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>월드에 놓이는 모든 것. 좌표는 방 기준 픽셀, (X,Y)는 발 위치.</summary>
    public abstract class Entity
    {
        public string Name;
        public float X, Y;
        /// <summary>충돌 상자 (발 기준 상대 좌표).</summary>
        public float BX0 = -3, BY0 = -2, BX1 = 3, BY1 = 0;
        public bool Solid;
        public bool Removed;
        /// <summary>-1 바닥에 붙음, 0 y정렬, 1 위에 덮음.</summary>
        public int Layer;
        public PlayState P;
        public GameCore G { get { return P.G; } }

        /// <summary>조사 스크립트. null이면 조사 불가.</summary>
        public Func<IEnumerator> OnInteract;
        public Func<bool> CanInteract;
        /// <summary>조사 가능 범위 (상자 바깥 여유, px).</summary>
        public float IRange = 5f;
        public bool ShowMarker = true;
        /// <summary>자각 상태에서만 보이는/존재하는 것.</summary>
        public bool LucidOnly;
        /// <summary>자각 상태에서는 사라지는 것.</summary>
        public bool HideWhenLucid;
        /// <summary>얼마나 조명을 받는가 (0..1, 그리기 후 갱신).</summary>
        public float LightHere;

        public virtual void Added() { }
        public virtual void Update(float dt) { }
        public virtual void Draw(Canvas c, int ox, int oy) { }
        /// <summary>320×240 화면 좌표계 추가 그리기 (피 글씨 등).</summary>
        public virtual void DrawScreen(Canvas sc, int ox2, int oy2) { }

        public bool Visible
        {
            get
            {
                if (P == null) return true;
                bool l = P.Lucid;
                if (LucidOnly && !l) return false;
                if (HideWhenLucid && l) return false;
                return true;
            }
        }

        public bool Interactable
        {
            get
            {
                if (OnInteract == null || Removed || !Visible) return false;
                return CanInteract == null || CanInteract();
            }
        }

        public float L { get { return X + BX0; } }
        public float R { get { return X + BX1; } }
        public float T { get { return Y + BY0; } }
        public float B { get { return Y + BY1; } }

        public bool Overlaps(float x0, float y0, float x1, float y1)
        {
            return x0 < R && x1 > L && y0 < B && y1 > T;
        }

        /// <summary>점과 상자 사이 거리.</summary>
        public float DistTo(float px, float py)
        {
            float dx = Math.Max(Math.Max(L - px, 0), px - R);
            float dy = Math.Max(Math.Max(T - py, 0), py - B);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public void SetBox(float x0, float y0, float x1, float y1)
        {
            BX0 = x0; BY0 = y0; BX1 = x1; BY1 = y1;
        }
    }

    /// <summary>가구/소품: 스프라이트 + 충돌 상자 + 조사.</summary>
    public class Prop : Entity
    {
        public Sprite Spr;
        public Sprite[] Frames;
        public float FrameRate = 4f;
        public bool FlipX;
        float animT;
        public byte[] Remap;
        public Action<Prop, Canvas, int, int> CustomDraw;
        public Action<Prop, float> CustomUpdate;

        public Prop(string name, Sprite s, float x, float y)
        {
            Name = name;
            Spr = s;
            X = x; Y = y;
            if (s != null)
            {
                // 기본 충돌: 스프라이트 아래쪽 절반
                SetBox(-s.OriginX, -Math.Max(2, s.H / 2), s.W - s.OriginX, 1);
            }
        }

        public Prop Solidify(bool solid = true) { Solid = solid; return this; }

        public Prop Box(float x0, float y0, float x1, float y1) { SetBox(x0, y0, x1, y1); return this; }

        public Prop Interact(Func<IEnumerator> script) { OnInteract = script; return this; }

        public override void Update(float dt)
        {
            animT += dt;
            if (CustomUpdate != null) CustomUpdate(this, dt);
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            if (CustomDraw != null) { CustomDraw(this, c, ox, oy); return; }
            var s = Spr;
            if (Frames != null && Frames.Length > 0) s = Frames[(int)(animT * FrameRate) % Frames.Length];
            if (s == null) return;
            if (Remap != null) c.BlitRemap(s, ox + (int)Math.Round(X), oy + (int)Math.Round(Y), FlipX, Remap);
            else c.Blit(s, ox + (int)Math.Round(X), oy + (int)Math.Round(Y), FlipX);
        }
    }

    /// <summary>바닥 위 보이지 않는 조사 지점 / 밟으면 실행되는 영역.</summary>
    public sealed class Zone : Entity
    {
        public Func<IEnumerator> OnEnter;
        public bool Once = true;
        /// <summary>false면 플레이어 조작을 막지 않는 배경 스크립트로 실행.</summary>
        public bool Blocking = true;
        bool inside;
        public Func<bool> Condition;

        public Zone(string name, float x0, float y0, float x1, float y1)
        {
            Name = name;
            X = x0; Y = y0;
            SetBox(0, 0, x1 - x0, y1 - y0);
            ShowMarker = false;
        }

        public override void Update(float dt)
        {
            if (OnEnter == null || P.Player == null) return;
            var pl = P.Player;
            bool now = Overlaps(pl.L, pl.T, pl.R, pl.B);
            if (!now) { inside = false; return; }
            if (inside) return;
            // 방 전환/대화 중이면 다음 프레임에 다시 시도한다
            if (P.ScriptBusy || pl.Dead || G.Dialogue.Active) return;
            if (Condition != null && !Condition()) return;
            inside = true;
            var s = OnEnter;
            if (Once) OnEnter = null;
            if (Blocking) P.RunScript(s());
            else P.RunAmbient(s());
        }
    }

    /// <summary>쪽지: 다시 읽으면 글씨가 바뀐다. 자각 상태에서는 진짜 내용.</summary>
    public sealed class NoteProp : Prop
    {
        public string Title;
        public string[] Texts;
        public string LucidText;
        public bool Bloody, Child;
        public int Reads;
        public Action<NoteProp> AfterRead;

        public NoteProp(string name, Sprite s, float x, float y, string title, string[] texts, string lucidText)
            : base(name, s, x, y)
        {
            Title = title;
            Texts = texts;
            LucidText = lucidText;
            SetBox(-2, -2, 2, 1);
            OnInteract = Read;
        }

        IEnumerator Read()
        {
            var g = G;
            string body;
            bool lucidMode = P.Lucid && LucidText != null;
            if (lucidMode) body = LucidText;
            else
            {
                body = Texts[Math.Min(Reads, Texts.Length - 1)];
                Reads++;
                g.Flags.Put("note_" + Name, Reads);
            }
            if (Reads >= 2 && !lucidMode && Texts.Length > 1 && Reads <= Texts.Length)
            {
                g.Audio.Play(Engine.Audio.Sfx.StingSoft, 0.5f);
            }
            yield return g.ShowNote(Title, body, Bloody, Child, lucidMode);
            if (AfterRead != null) AfterRead(this);
        }
    }
}
