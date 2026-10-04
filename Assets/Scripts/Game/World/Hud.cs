using System;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>최소한의 HUD: 자각(눈), 손전등 배터리, 안내 문구, 목표.</summary>
    public sealed class Hud
    {
        readonly PlayState p;
        string notice;
        float noticeT, noticeDur;
        string objective;
        float objT;
        float batteryShow;
        float eyeOpen;

        public Hud(PlayState ps) { p = ps; }

        public void Notice(string s, float seconds = 2.2f)
        {
            notice = s;
            noticeT = seconds;
            noticeDur = seconds;
        }

        public void Objective(string s)
        {
            objective = s;
            objT = 4.5f;
            p.G.Audio.Play(Sfx.Pickup, 0.25f, 0.6f);
        }

        public void Update(float dt)
        {
            if (noticeT > 0) noticeT -= dt;
            if (objT > 0) objT -= dt;
            var pl = p.Player;
            if (pl.LightOn || pl.Battery < 0.999f) batteryShow = 2f;
            else batteryShow = Math.Max(0, batteryShow - dt);
            eyeOpen = M.Approach(eyeOpen, p.Lucid ? 1f : 0f, dt * 4f);
        }

        public void Draw(Canvas c)
        {
            var g = p.G;
            var pl = p.Player;
            if (g.Flags.Has("first_check") && p.Room.Def.Dream && !g.Flags.Has("ending"))
                DrawEye(c, 10, 10);
            if (pl.HasLight && batteryShow > 0 && !pl.Hidden)
                DrawBattery(c, GameCore.W - 28, 10, pl.Battery);
            if (noticeT > 0 && notice != null)
            {
                int w = g.Font.Measure(notice);
                int x = (GameCore.W - w) / 2, y = 24;
                float a = Math.Min(1f, noticeT / 0.4f) * Math.Min(1f, (noticeDur - noticeT) / 0.2f + 0.2f);
                byte col = a > 0.6f ? Pal.White : Pal.Gray;
                g.Font.DrawOutlined(c, notice, x, y, col, Pal.Black);
            }
            if (objT > 0 && objective != null)
            {
                string s = "· " + objective;
                int w = g.FontSmall.Measure(s);
                int x = (GameCore.W - w) / 2, y = 44;
                c.FillRect(x - 6, y - 3, w + 12, 15, Pal.Black);
                c.Rect(x - 6, y - 3, w + 12, 15, Pal.DarkGray);
                g.FontSmall.Draw(c, s, x, y, Pal.PaleYellow);
            }
        }

        void DrawEye(Canvas c, int x, int y)
        {
            // 아몬드 모양 눈. 자각하면 뜨고, 붉은 눈동자.
            float open = eyeOpen;
            int w = 18, h = (int)Math.Round(1 + open * 6);
            int cx = x + w / 2, cy = y + 4;
            for (int i = 0; i < w; i++)
            {
                float k = (i - w / 2f) / (w / 2f);
                float hh = (1 - k * k) * h * 0.5f;
                int top = (int)Math.Round(cy - hh), bot = (int)Math.Round(cy + hh);
                if (open > 0.1f)
                    for (int yy = top + 1; yy < bot; yy++) c.Set(x + i, yy, Pal.Bone);
                c.Set(x + i, top, Pal.LightGray);
                c.Set(x + i, bot, Pal.LightGray);
            }
            if (open > 0.5f)
            {
                float jitter = p.Stalker.Proximity > 0.3f ? p.G.Rng.Range(-1, 1) : 0;
                c.FillCircle(cx + jitter, cy + 0.5f, 2.6f * open, Pal.Red);
                c.FillCircle(cx + jitter, cy + 0.5f, 1.2f, Pal.Black);
            }
            else
            {
                // 감은 눈의 속눈썹
                for (int i = 2; i < w; i += 4) c.Set(x + i, cy + 2, Pal.Gray);
            }
            if (p.Lucid)
            {
                float frac = p.LucidT / p.LucidMax;
                int bw = (int)(w * frac);
                c.FillRect(x, y + 11, w, 2, Pal.Maroon);
                c.FillRect(x, y + 11, bw, 2, frac < 0.25f && ((int)(p.G.Time * 6)) % 2 == 0 ? Pal.White : Pal.BloodBright);
            }
        }

        void DrawBattery(Canvas c, int x, int y, float level)
        {
            c.Rect(x, y, 16, 8, Pal.LightGray);
            c.FillRect(x + 16, y + 2, 2, 4, Pal.LightGray);
            int fill = (int)Math.Round(12 * level);
            byte col = level < 0.25f ? Pal.Red : (level < 0.5f ? Pal.Yellow : Pal.PaleYellow);
            c.FillRect(x + 2, y + 2, fill, 4, col);
        }
    }

    /// <summary>
    /// 손가락 세기 (월드 진행 중, 화면 왼쪽 아래 상자). 세는 동안 하나는 무방비.
    /// </summary>
    public sealed class HandOverlay
    {
        readonly PlayState p;
        public bool Active;
        float t;
        int fingers;
        bool dream;
        int lastCounted;
        bool cracked;
        public static readonly string[] Numbers = { "하나", "둘", "셋", "넷", "다섯", "여섯", "일곱", "여덟" };
        // 첫 몇 번은 천천히, 익숙해지면 빨라진다
        float Step = 0.3f;
        float Start = 0.25f;
        float ExtraStep = 0.75f;

        public HandOverlay(PlayState ps) { p = ps; }

        public void Begin(int count, bool isDream)
        {
            Active = true;
            t = 0;
            fingers = count;
            dream = isDream;
            lastCounted = -1;
            cracked = false;
            int n = p.G.Flags.Get("checks");
            bool fast = n >= 3;
            Step = fast ? 0.18f : 0.3f;
            Start = fast ? 0.12f : 0.25f;
            ExtraStep = fast ? 0.45f : 0.75f;
        }

        public void Cancel() { Active = false; }

        float Duration { get { return Start + Step * 5 + (fingers > 5 ? (fingers - 5) * ExtraStep + 0.3f : 0) + 0.45f; } }

        public void Update(float dt)
        {
            if (!Active) return;
            t += dt;
            int counted = (int)((t - Start) / Step);
            if (counted > 4) counted = 4;
            if (t >= Start && counted != lastCounted && counted >= 0)
            {
                lastCounted = counted;
                p.G.Audio.Play(Sfx.Blip, 0.3f, 1.4f + counted * 0.06f);
            }
            float extraStart = Start + Step * 5 + 0.3f;
            if (fingers > 5 && t > extraStart + ExtraStep * 0.33f && !cracked)
            {
                cracked = true;
                p.G.Audio.Play(Sfx.Crack, 0.8f);
                p.G.Audio.Play(Sfx.Gore, 0.35f, 1.3f);
            }
            if (t > Duration) Active = false;
        }

        public void Draw(Canvas c)
        {
            if (!Active) return;
            int bx = 8, by = GameCore.H - 112, bw = 104, bh = 104;
            float appear = Math.Min(1f, t / 0.15f) * Math.Min(1f, (Duration - t) / 0.15f);
            if (appear <= 0) return;
            if (appear < 1f) { by += (int)((1 - appear) * 20); }
            c.FillRect(bx, by, bw, bh, Pal.Black);
            c.Rect(bx, by, bw, bh, dream && fingers > 5 && cracked ? Pal.DarkRed : Pal.Gray);
            c.SetClip(bx + 1, by + 1, bw - 2, bh - 2);
            var pose = new HandPose();
            int counted = t < Start ? -1 : Math.Min(4, (int)((t - Start) / Step));
            float local = t < Start ? 0 : ((t - Start) - counted * Step) / Step;
            pose.Highlight = counted;
            pose.Flex = counted >= 0 && counted <= 4 && t < Start + Step * 5 ? M.Sin(M.Clamp01(local) * M.PI) * 0.35f : 0;
            float extraStart = Start + Step * 5 + 0.3f;
            if (fingers > 5)
            {
                pose.Extra = fingers - 5;
                pose.Growth = M.Clamp01((t - extraStart) / ExtraStep);
                pose.Blood = M.Clamp01((t - extraStart - ExtraStep * 0.33f) / 1.0f);
            }
            Art.DrawHand(c, bx + bw / 2 + 4, by + bh / 2 + 18, 1.15f, pose, p.G.Time);
            c.ResetClip();
            // 숫자
            string num = null;
            byte col = Pal.White;
            if (counted >= 0 && t < extraStart) num = Numbers[counted];
            else if (fingers > 5 && t >= extraStart) { num = Numbers[Math.Min(Numbers.Length - 1, fingers - 1)] + "…?"; col = Pal.BloodBright; }
            if (num != null) p.G.Font.DrawOutlined(c, num, bx + 6, by + 4, col, Pal.Black);
        }
    }
}
