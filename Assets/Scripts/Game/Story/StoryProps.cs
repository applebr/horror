using System;
using System.Collections;
using System.Collections.Generic;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>벽에 쓰인 글씨 (월드 해상도의 작은 글씨, 피가 흘러내린다).</summary>
    public sealed class WallText : Entity
    {
        public string Text;
        public byte Color;
        public bool Drip;
        public bool Big;

        public WallText(string text, float x, float y, byte color, bool lucidOnly, bool drip)
        {
            Text = text;
            X = x; Y = y;
            Color = color;
            LucidOnly = lucidOnly;
            Drip = drip;
            Layer = -1;
            SetBox(0, 0, 0, 0);
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            var f = G.FontTiny;
            int w = f.Measure(Text);
            int x = ox + (int)X - w / 2, y = oy + (int)Y;
            f.Draw(c, Text, x, y, Color);
            if (Drip)
            {
                for (int i = 0; i < w; i += 2)
                {
                    float h = M.Hash(i, (int)X, 21);
                    if (h > 0.45f) continue;
                    int len = 1 + (int)(h * 10) + (int)(P.Room.Time * 0.5f + h * 7) % 3;
                    int baseY = y + f.VisualAscent;
                    for (int k = 0; k < len; k++) c.Set(x + i, baseY + k, Color == Pal.BloodBright ? Pal.Red : Color);
                }
            }
        }
    }

    /// <summary>수면등: 체크포인트. 닿으면 저장.</summary>
    public sealed class NightLightProp : Prop
    {
        Light light;
        bool touched;

        public NightLightProp(float x, float y) : base("nightlight", Sprites.NightLight, x, y)
        {
            SetBox(-2, -2, 2, 0);
            Solid = false;
            ShowMarker = false;
        }

        public override void Added()
        {
            light = new Light(X, Y - 3, 22, 0.55f) { Flicker = 0.1f };
            P.Room.Lights.Add(light);
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            var pl = P.Player;
            bool near = M.Dist(pl.X, pl.Y, X, Y) < 16f;
            if (near && !touched && !P.Dying && !pl.Frozen && !pl.Lying && !P.ScriptBusy)
            {
                touched = true;
                if (P.CheckRoom != P.Room.Def.Id || M.Dist(P.CheckX, P.CheckY, X, Y + 6) > 4)
                {
                    P.SetCheckpoint(P.Room.Def.Id, X, Y + 7, true);
                    P.Hud.Notice("수면등의 불빛이 따뜻하다. (저장됨)", 1.8f);
                    G.Audio.Play(Sfx.Pickup, 0.3f, 0.5f);
                }
                if (pl.Fear > 0) pl.Fear = Math.Max(0, pl.Fear - 0.5f);
            }
            if (!near) touched = false;
        }
    }

    /// <summary>숨을 수 있는 곳 (장롱 등).</summary>
    public sealed class HideSpotProp : Prop
    {
        public string Question = "장롱 안에 숨을까?";

        public HideSpotProp(string name, Sprite s, float x, float y) : base(name, s, x, y)
        {
            Solid = true;
            OnInteract = HideRoutine;
        }

        IEnumerator HideRoutine()
        {
            int choice = 1;
            yield return G.Ask(Question, new[] { "숨는다", "그만둔다" }, i => choice = i);
            if (choice == 0) P.Hide(this);
        }
    }

    /// <summary>가족사진 조각.</summary>
    public sealed class PieceProp : Prop
    {
        public string Flag;
        float t;

        public PieceProp(string flag, float x, float y) : base("piece_" + flag, Sprites.PhotoPiece, x, y)
        {
            Flag = flag;
            SetBox(-2, -2, 2, 1);
            Layer = 0;
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            t += dt;
        }

        public override void Draw(Canvas c, int ox, int oy)
        {
            int px = ox + (int)X, py = oy + (int)Y;
            c.Blit(Sprites.PhotoPiece, px, py);
            // 반짝임
            if (((int)(t * 3)) % 4 == 0) c.Set(px + 2, py - 3, Pal.White);
        }
    }

    public static partial class Story
    {
        // ───────── 공용 도우미 ─────────

        static PlayState PS(GameCore g) { return g.Play; }

        /// <summary>조사하면 속마음 대사를 보여 주는 소품.</summary>
        static Prop Look(Room r, string name, Sprite s, float x, float y, bool solid, params string[] lines)
        {
            var p = new Prop(name, s, x, y) { Solid = solid };
            var g = CurrentGame;
            p.OnInteract = () => SayLines(g, Speaker.Think, lines);
            r.Add(p);
            return p;
        }

        /// <summary>보이지 않는 조사 지점 (창문, 벽 등).</summary>
        static Prop Spot(Room r, string name, float x0, float y0, float x1, float y1, Func<IEnumerator> script)
        {
            var p = new Prop(name, null, x0, y0);
            p.SetBox(0, 0, x1 - x0, y1 - y0);
            p.Solid = false;
            p.OnInteract = script;
            p.ShowMarker = false;
            r.Add(p);
            return p;
        }

        static Prop SpotLines(Room r, string name, float x0, float y0, float x1, float y1, params string[] lines)
        {
            var g = CurrentGame;
            return Spot(r, name, x0, y0, x1, y1, () => SayLines(g, Speaker.Think, lines));
        }

        static IEnumerator SayLines(GameCore g, Speaker s, string[] lines)
        {
            foreach (var l in lines) yield return g.Say(s, l);
        }

        static Zone OnStep(Room r, string name, float x0, float y0, float x1, float y1, Func<IEnumerator> script, Func<bool> cond = null)
        {
            var z = new Zone(name, x0, y0, x1, y1) { OnEnter = script, Condition = cond };
            r.Add(z);
            return z;
        }

        /// <summary>셀 좌표 → 방 픽셀 (셀 가운데, 발 기준).</summary>
        static float Cx(float cx) { return cx * Room.CS + Room.CS * 0.5f; }
        static float Cy(float cy) { return cy * Room.CS + Room.CS - 0.5f; }

        public static int Pieces(GameCore g)
        {
            int n = 0;
            if (g.Flags.Has("piece_bath")) n++;
            if (g.Flags.Has("piece_kitchen")) n++;
            if (g.Flags.Has("piece_mom")) n++;
            return n;
        }

        static IEnumerator GetPiece(GameCore g, string flag)
        {
            if (g.Flags.Has(flag)) yield break;
            g.Flags.Set(flag);
            g.Audio.Play(Sfx.Pickup, 0.7f);
            int n = Pieces(g);
            yield return g.PlayCutscene(new PieceCutscene(flag));
            PS(g).Hud.Objective("가족사진 조각 (" + n + "/3)");
            if (!g.Flags.Has("stalker_on"))
            {
                g.Flags.Set("stalker_on");
            }
            if (n == 3) yield return g.Say(Speaker.Think, "다 모았다. …거실의 빈 액자로 가자.");
        }

        static GameCore CurrentGame;

        public static string Objective(GameCore g)
        {
            var f = g.Flags;
            if (f.Has("ending")) return "엄마한테 가자";
            if (f.Has("boss")) return "가위를 쓰러뜨려라 (자각 + 손전등)";
            if (f.Has("photo_done")) return "현관 밖으로";
            if (f.Has("dream"))
            {
                if (!f.Has("first_check")) return "손가락을 세어 보자 (C)";
                int n = Pieces(g);
                if (n == 3) return "거실의 빈 액자에 사진을 맞추자";
                if (f.Has("saw_frame")) return "가족사진 조각을 찾자 (" + n + "/3)";
                return "집 안을 살펴보자";
            }
            if (f.Has("mom_left"))
            {
                if (!f.Has("brushed")) return "잘 준비를 하자: 이 닦기";
                return "잘 준비를 하자: 침대로";
            }
            return null;
        }
    }
}
