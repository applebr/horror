using System;
using System.Collections.Generic;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    public enum Speaker
    {
        Narr,       // 해설 / 지문
        Hana,       // 하나 (소리 내어 말함)
        Think,      // 하나의 속마음
        Mom,
        Gawi,       // 가위
        Mother,     // 엄마인 척하는 것
        Kids,       // 얼굴 없는 아이들
        Tv,
        Dad,
        Sys,        // 조작 안내
        Whisper,    // 정체 모를 속삭임
    }

    public enum BoxPos { Auto, Bottom, Top, Middle }

    /// <summary>FAITH풍 대화창: 검은 상자, 흰 테두리, 타자기처럼 나오는 글자.</summary>
    public sealed class Dialogue
    {
        readonly GameCore g;
        public bool Active;
        Speaker speaker;
        string text = "";
        List<string> lines = new List<string>();
        int total;      // 줄바꿈 없는 전체 글자 수
        float shown;    // 보이는 글자 수 (실수)
        float pause;
        int lastBlip = -1;
        VoiceLine voice;
        float voiceT;
        bool finished;
        float autoClose = -1;
        float openT;
        public BoxPos Pos = BoxPos.Auto;

        // 선택지
        string[] options;
        int sel;
        Action<int> onChoice;

        const int BoxX = 12, BoxW = 296, Pad = 8;

        public Dialogue(GameCore game) { g = game; }

        public string Text { get { return text; } }
        /// <summary>시간이 지나면 저절로 닫히는 대사 (이때는 세계가 멈추지 않는다).</summary>
        public bool IsTimed { get { return autoClose >= 0 && options == null; } }
        public Speaker CurrentSpeaker { get { return speaker; } }

        public static byte Color(Speaker s)
        {
            switch (s)
            {
                case Speaker.Hana: return Pal.White;
                case Speaker.Think: return Pal.PaleBlue;
                case Speaker.Mom: return Pal.PaleYellow;
                case Speaker.Gawi: return Pal.BloodBright;
                case Speaker.Mother: return Pal.Pink;
                case Speaker.Kids: return Pal.LightGray;
                case Speaker.Tv: return Pal.White;
                case Speaker.Dad: return Pal.Tan;
                case Speaker.Sys: return Pal.Yellow;
                case Speaker.Whisper: return Pal.Gray;
                default: return Pal.LightGray;
            }
        }

        static string Name(Speaker s)
        {
            switch (s)
            {
                case Speaker.Hana: return "하나";
                case Speaker.Mom: return "엄마";
                case Speaker.Dad: return "???";
                default: return null;
            }
        }

        static bool Voiced(Speaker s, out VoiceKind k)
        {
            switch (s)
            {
                case Speaker.Gawi: k = VoiceKind.Demon; return true;
                case Speaker.Mother: k = VoiceKind.Mother; return true;
                case Speaker.Kids: k = VoiceKind.Kids; return true;
                case Speaker.Tv: k = VoiceKind.Tv; return true;
                case Speaker.Whisper: k = VoiceKind.Whisper; return true;
            }
            k = VoiceKind.Whisper;
            return false;
        }

        public void Open(Speaker s, string t, float autoCloseAfter = -1f, int[] melody = null)
        {
            Active = true;
            speaker = s;
            text = t ?? "";
            int maxW = BoxW - Pad * 2;
            lines = g.Font.Wrap(text, maxW);
            total = 0;
            foreach (var l in lines) total += l.Length;
            shown = 0;
            pause = 0.05f;
            lastBlip = -1;
            finished = false;
            autoClose = autoCloseAfter;
            openT = 0;
            options = null;
            onChoice = null;
            voice = null;
            voiceT = 0;
            VoiceKind vk;
            if (Voiced(s, out vk) && t.Length > 0)
            {
                string flat = string.Join("", lines.ToArray());
                voice = g.Audio.GetVoice(flat, vk, melody);
                g.Audio.PlayVoice(flat, vk, s == Speaker.Whisper ? 0.8f : 1f, melody);
            }
        }

        public void Ask(string question, string[] opts, Action<int> result)
        {
            Open(Speaker.Narr, question);
            options = opts;
            sel = 0;
            onChoice = result;
        }

        public bool Finished { get { return !Active; } }

        public void Close()
        {
            Active = false;
        }

        float BlipPitch()
        {
            switch (speaker)
            {
                case Speaker.Mom: return 1.25f;
                case Speaker.Hana: return 1.6f;
                case Speaker.Think: return 1.4f;
                case Speaker.Dad: return 0.8f;
                case Speaker.Sys: return 1.1f;
                default: return 1.0f;
            }
        }

        public void Update(float dt)
        {
            if (!Active) return;
            openT += dt;
            var input = g.Input;
            if (!finished)
            {
                if (voice != null)
                {
                    voiceT += dt;
                    int n = 0;
                    var times = voice.CharTimes;
                    while (n < times.Length && times[n] <= voiceT) n++;
                    shown = Math.Min(total, n);
                    if (voiceT > voice.Duration) shown = total;
                }
                else
                {
                    if (pause > 0) pause -= dt;
                    else
                    {
                        shown += dt * g.Settings.CharsPerSecond;
                        int si = (int)shown;
                        if (si > total) si = total;
                        if (si > 0 && si <= total)
                        {
                            char ch = CharAt(si - 1);
                            if (si != lastBlip)
                            {
                                if (ch == '.' || ch == '?' || ch == '!') pause = 0.22f;
                                else if (ch == ',') pause = 0.1f;
                                else if (ch == '…') pause = 0.3f;
                                if (ch != ' ' && si % 2 == 0 && speaker != Speaker.Narr)
                                    g.Audio.Play(Sfx.Blip, 0.35f, BlipPitch() * g.Rng.Range(0.94f, 1.06f));
                                else if (ch != ' ' && si % 3 == 0 && speaker == Speaker.Narr)
                                    g.Audio.Play(Sfx.Blip, 0.18f, 0.7f);
                                lastBlip = si;
                            }
                        }
                    }
                }
                if (shown >= total) { shown = total; finished = true; }
                if (openT > 0.1f && autoClose < 0 && input.Pressed(Btn.Confirm))
                {
                    shown = total;
                    finished = true;
                    input.Consume(Btn.Confirm);
                }
                return;
            }
            if (options != null)
            {
                if (input.Repeat(Btn.Up)) { sel = (sel + options.Length - 1) % options.Length; g.Audio.Play(Sfx.MenuMove, 0.5f); }
                if (input.Repeat(Btn.Down)) { sel = (sel + 1) % options.Length; g.Audio.Play(Sfx.MenuMove, 0.5f); }
                if (input.Pressed(Btn.Confirm))
                {
                    g.Audio.Play(Sfx.MenuSelect, 0.5f);
                    input.Consume(Btn.Confirm);
                    var cb = onChoice;
                    int s = sel;
                    Active = false;
                    if (cb != null) cb(s);
                }
                else if (input.Pressed(Btn.Cancel))
                {
                    input.Consume(Btn.Cancel);
                    var cb = onChoice;
                    int s = options.Length - 1;
                    Active = false;
                    if (cb != null) cb(s);
                }
                return;
            }
            if (autoClose >= 0)
            {
                autoClose -= dt;
                if (autoClose <= 0) { Active = false; return; }
            }
            if (input.Pressed(Btn.Confirm) || (autoClose < 0 && input.Pressed(Btn.Cancel)))
            {
                input.Consume(Btn.Confirm);
                input.Consume(Btn.Cancel);
                Active = false;
            }
        }

        char CharAt(int idx)
        {
            foreach (var l in lines)
            {
                if (idx < l.Length) return l[idx];
                idx -= l.Length;
            }
            return ' ';
        }

        public void Draw(Canvas c)
        {
            if (!Active) return;
            var f = g.Font;
            int nLines = Math.Max(2, lines.Count);
            int optH = options != null ? options.Length * f.LineStep + 4 : 0;
            int h = Pad * 2 + nLines * f.LineStep - 2 + optH;
            int y;
            var pos = Pos;
            if (pos == BoxPos.Auto) pos = g.PreferTopBox && (g.Cutscene == null || !g.Cutscene.Opaque) ? BoxPos.Top : BoxPos.Bottom;
            switch (pos)
            {
                case BoxPos.Top: y = 10; break;
                case BoxPos.Middle: y = (GameCore.H - h) / 2; break;
                default: y = GameCore.H - h - 10; break;
            }
            byte col = Color(speaker);
            c.FillRect(BoxX, y, BoxW, h, Pal.Black);
            byte border = speaker == Speaker.Gawi ? Pal.DarkRed : (speaker == Speaker.Mother ? Pal.Maroon : Pal.LightGray);
            c.Rect(BoxX, y, BoxW, h, border);
            c.Rect(BoxX + 2, y + 2, BoxW - 4, h - 4, Pal.DarkGray);
            string name = Name(speaker);
            if (name != null)
            {
                int nw = g.FontSmall.Measure(name) + 8;
                c.FillRect(BoxX + 8, y - 6, nw, 11, Pal.Black);
                c.Rect(BoxX + 8, y - 6, nw, 11, border);
                g.FontSmall.Draw(c, name, BoxX + 12, y - 4, col);
            }
            int remaining = (int)shown;
            int ty = y + Pad;
            int shakeX = 0, shakeY = 0;
            foreach (var l in lines)
            {
                if (remaining <= 0) break;
                int n = Math.Min(remaining, l.Length);
                if (speaker == Speaker.Gawi || speaker == Speaker.Tv)
                {
                    // 떨리는 글자
                    shakeX = g.Rng.Chance(0.15f) ? g.Rng.Range(-1, 1) : 0;
                    shakeY = g.Rng.Chance(0.1f) ? g.Rng.Range(-1, 1) : 0;
                }
                f.Draw(c, l, BoxX + Pad + shakeX, ty + shakeY, col, n);
                remaining -= n;
                ty += f.LineStep;
            }
            if (finished && options == null && autoClose < 0)
            {
                // 다음 표시 (깜빡이는 삼각형)
                if (((int)(g.Time * 3)) % 2 == 0)
                {
                    int ax = BoxX + BoxW - 14, ay = y + h - 9;
                    c.FillTri(ax, ay, ax + 6, ay, ax + 3, ay + 3, Pal.LightGray);
                }
            }
            if (finished && options != null)
            {
                int oy = y + Pad + nLines * f.LineStep + 2;
                for (int i = 0; i < options.Length; i++)
                {
                    bool s = i == sel;
                    if (s) c.FillTri(BoxX + Pad + 2, oy + 3, BoxX + Pad + 2, oy + 9, BoxX + Pad + 5, oy + 6, Pal.BloodBright);
                    f.Draw(c, options[i], BoxX + Pad + 10, oy, s ? Pal.White : Pal.Gray);
                    oy += f.LineStep;
                }
            }
        }
    }

    /// <summary>쪽지/일기 전체 화면 보기.</summary>
    public sealed class NoteView
    {
        readonly GameCore g;
        public bool Active;
        string title, body;
        bool bloody, childHand, lucidText;
        float t;

        public NoteView(GameCore game) { g = game; }

        public void Open(string noteTitle, string noteBody, bool isBloody, bool isChildHand, bool lucid)
        {
            Active = true;
            title = noteTitle;
            body = noteBody;
            bloody = isBloody;
            childHand = isChildHand;
            lucidText = lucid;
            t = 0;
            g.Audio.Play(Sfx.Rip, 0.25f, 1.6f);
        }

        public void Update(float dt)
        {
            if (!Active) return;
            t += dt;
            if (t > 0.25f && (g.Input.Pressed(Btn.Confirm) || g.Input.Pressed(Btn.Cancel)))
            {
                g.Input.Consume(Btn.Confirm);
                g.Input.Consume(Btn.Cancel);
                Active = false;
            }
        }

        public void Draw(Canvas c)
        {
            if (!Active) return;
            c.DitherDarken(0, 0, c.W, c.H, Pal.DarkNight[3], 12);
            int pw = 220, ph = 190;
            int px = (c.W - pw) / 2, py = (c.H - ph) / 2;
            byte paper = lucidText ? Pal.Flesh : Pal.Bone;
            byte ink = lucidText ? Pal.Maroon : (childHand ? Pal.DarkBlue : Pal.Ink);
            c.FillRect(px, py, pw, ph, paper);
            // 줄 공책
            for (int ly = py + 28; ly < py + ph - 8; ly += g.Font.LineStep + 2)
                c.HLine(px + 8, px + pw - 9, ly + g.Font.LineStep - 1, lucidText ? Pal.Red : Pal.PaleBlue);
            c.VLine(px + 20, py + 2, py + ph - 3, lucidText ? Pal.DarkRed : Pal.Pink);
            // 찢긴 가장자리
            for (int x = px; x < px + pw; x++)
                if (M.Hash(x, 1, 7) > 0.6f) c.Set(x, py + ph - 1, Pal.Black);
            if (bloody)
            {
                for (int k = 0; k < 6; k++)
                {
                    float bx = px + M.Hash(k, 2, 9) * pw, by = py + M.Hash(k, 3, 9) * ph;
                    c.FillCircle(bx, by, 2 + M.Hash(k, 4, 9) * 7, Pal.DarkRed);
                    c.VLine((int)bx, (int)by, (int)(by + M.Hash(k, 5, 9) * 30), Pal.DarkRed);
                }
            }
            if (!string.IsNullOrEmpty(title)) g.Font.Draw(c, title, px + 26, py + 8, lucidText ? Pal.DarkRed : Pal.DarkBrown);
            var lines = g.Font.Wrap(body, pw - 34);
            int ty = py + 28;
            for (int i = 0; i < lines.Count; i++)
            {
                int jx = childHand ? (int)(M.Hash(i, 0, 5) * 3) : 0;
                g.Font.Draw(c, lines[i], px + 26 + jx, ty, ink);
                ty += g.Font.LineStep + 2;
            }
            if (((int)(g.Time * 2)) % 2 == 0) g.FontSmall.Draw(c, "Z", px + pw - 12, py + ph - 14, Pal.Gray);
        }
    }
}
