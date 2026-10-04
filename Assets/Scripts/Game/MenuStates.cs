using System;
using System.Collections;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>시작 경고 화면. 이 동안 소리를 미리 합성한다.</summary>
    public sealed class WarningState : IState
    {
        GameCore g;
        float t;
        bool leaving;

        public void Enter(GameCore game) { g = game; t = 0; }
        public void Exit() { }

        public void Update(float dt)
        {
            t += dt;
            // 프레임마다 소리를 하나 더 만든다 (한 번에 만들면 멈춘다)
            if (!g.Audio.WarmDone) g.Audio.WarmStep();
            if (!leaving && t > 1.0f && g.Input.Pressed(Btn.Confirm))
            {
                leaving = true;
                g.Input.Consume(Btn.Confirm);
                g.Audio.Play(Sfx.MenuSelect, 0.5f);
                g.Scripts.Start(Leave(), "warning");
            }
        }

        IEnumerator Leave()
        {
            yield return g.FadeOut(0.6f);
            // 남은 소리를 마저 만든다
            while (!g.Audio.WarmDone) { g.Audio.WarmStep(); yield return null; }
            g.SetState(new TitleState());
        }

        public void Draw(Canvas c)
        {
            c.Clear(Pal.Black);
            var f = g.Font;
            int y = 48;
            f.DrawCentered(c, "경고", GameCore.W / 2, y, Pal.BloodBright);
            y += 28;
            string[] lines =
            {
                "이 게임에는 잔인하고 끔찍한 묘사,",
                "갑작스러운 큰 소리와",
                "번쩍이는 화면 효과가 포함되어 있습니다.",
                "",
                "광과민성 증상이 있다면 설정에서",
                "'번쩍임 줄이기'를 켜 주세요.",
                "",
                "헤드폰을 쓰고, 불을 끄고 플레이하세요.",
            };
            foreach (var l in lines)
            {
                f.DrawCentered(c, l, GameCore.W / 2, y, Pal.LightGray);
                y += f.LineStep;
            }
            if (t > 1.0f && ((int)(t * 2)) % 2 == 0)
                g.FontSmall.DrawCentered(c, "Z 키를 누르세요", GameCore.W / 2, 212, Pal.Gray);
            // 소리 준비 진행 막대
            if (!g.Audio.WarmDone)
            {
                int w = (int)(60 * g.Audio.WarmProgress);
                c.FillRect(GameCore.W / 2 - 30, 228, 60, 2, Pal.Night);
                c.FillRect(GameCore.W / 2 - 30, 228, w, 2, Pal.DarkRed);
            }
        }

        public void DrawUi(Canvas c) { }
    }

    public sealed class TitleState : IState
    {
        GameCore g;
        Menu menu;
        float t;
        bool busy;
        float nextFlicker = 6f;
        bool confirmNew;

        public void Enter(GameCore game)
        {
            g = game;
            t = 0;
            busy = false;
            menu = new Menu("처음부터", "이어하기", "설정", "끝내기");
            menu.Disabled[1] = !g.HasSave;
            menu.Sel = g.HasSave ? 1 : 0;
            if (!g.Platform.CanQuit) menu.Disabled[3] = true;
            g.Fx.Reset();
            g.Fx.Fade = 1f;
            g.Fx.Vignette = 0.8f;
            g.Audio.SetMusic(Ch.Ambience, Music.AmbDream, 0.7f, 2f);
            g.Audio.SetMusic(Ch.Music, Music.MusicBoxWarped, 0.35f, 3f);
            g.Scripts.Start(g.FadeIn(2.0f), "title");
        }

        public void Exit() { }

        public void Update(float dt)
        {
            t += dt;
            if (t > nextFlicker)
            {
                // 아주 잠깐 비치는 형상
                nextFlicker = t + g.Rng.Range(5f, 11f);
                g.Fx.Subliminal(cv => Art.DrawGawiFace(cv, g.Time, 1f), 1);
                g.Audio.Play(Sfx.Static, 0.25f, 0.8f);
            }
            if (busy) return;
            int r = menu.Update(g);
            if (r == 0)
            {
                if (g.HasSave && !confirmNew)
                {
                    confirmNew = true;
                    g.Scripts.Start(ConfirmNew(), "title");
                    return;
                }
                StartNew();
            }
            else if (r == 1)
            {
                busy = true;
                g.Scripts.Start(Go(false), "title");
            }
            else if (r == 2)
            {
                g.PushOverlay(new SettingsState());
            }
            else if (r == 3)
            {
                g.Settings.Save(g.Store);
                g.QuitRequested = true;
                g.Platform.Quit();
            }
        }

        IEnumerator ConfirmNew()
        {
            busy = true;
            int choice = 1;
            yield return g.Ask("저장된 기록이 사라집니다. 처음부터 할까요?", new[] { "처음부터 한다", "그만둔다" }, i => choice = i);
            confirmNew = false;
            busy = false;
            if (choice == 0) StartNew();
        }

        void StartNew()
        {
            busy = true;
            g.Scripts.Start(Go(true), "title");
        }

        IEnumerator Go(bool fresh)
        {
            g.Audio.Play(Sfx.Snip, 0.8f);
            g.Fx.Shake(2, 0.3f);
            g.Audio.StopAllMusic(1.2f);
            yield return g.FadeOut(1.3f);
            yield return g.Wait(0.5f);
            if (fresh) { g.DeleteSave(); g.NewGame(); }
            else g.ContinueGame();
        }

        public void Draw(Canvas c)
        {
            Art.DrawTitle(c, g, t);
        }

        public void DrawUi(Canvas c)
        {
            if (g.TopOverlay != null) return;
            c.DitherDarken(0, 158, GameCore.W, 82, Pal.DarkNight[4], 13);
            menu.Draw(g, c, GameCore.W / 2, 166, true);
            g.FontTiny.Draw(c, "v1.0  ·  1장 데모", 6, GameCore.H - 11, Pal.DarkGray);
            g.FontTiny.Draw(c, "방향키 이동 · Z 결정", GameCore.W - 6 - g.FontTiny.Measure("방향키 이동 · Z 결정"), GameCore.H - 11, Pal.DarkGray);
        }
    }

    public sealed class SettingsState : IState
    {
        GameCore g;
        int sel;
        const int N = 7;

        public void Enter(GameCore game) { g = game; sel = 0; }
        public void Exit() { g.Settings.Save(g.Store); }

        public void Update(float dt)
        {
            var inp = g.Input;
            if (inp.Repeat(Btn.Up)) { sel = (sel + N - 1) % N; g.Audio.Play(Sfx.MenuMove, 0.5f); }
            if (inp.Repeat(Btn.Down)) { sel = (sel + 1) % N; g.Audio.Play(Sfx.MenuMove, 0.5f); }
            int d = 0;
            if (inp.Repeat(Btn.Left)) d = -1;
            if (inp.Repeat(Btn.Right)) d = 1;
            bool ok = inp.Pressed(Btn.Confirm);
            var s = g.Settings;
            if (d != 0 || ok)
            {
                if (ok && d == 0) d = 1;
                switch (sel)
                {
                    case 0: s.Volume = M.Clamp(s.Volume + d, 0, 10); break;
                    case 1: s.MusicVolume = M.Clamp(s.MusicVolume + d, 0, 10); break;
                    case 2: s.Fullscreen = !s.Fullscreen; break;
                    case 3: s.Crt = !s.Crt; break;
                    case 4: s.ReduceFlash = !s.ReduceFlash; break;
                    case 5: s.TextSpeed = (s.TextSpeed + d + 3) % 3; break;
                    case 6: if (ok) { g.Audio.Play(Sfx.MenuSelect, 0.5f); g.PopOverlay(); return; } break;
                }
                if (sel != 6)
                {
                    g.ApplySettings();
                    g.Audio.Play(Sfx.MenuMove, 0.6f, 1.2f);
                }
                inp.Consume(Btn.Confirm);
            }
            if (inp.Pressed(Btn.Cancel) || inp.Pressed(Btn.Pause))
            {
                g.Audio.Play(Sfx.MenuSelect, 0.5f);
                g.PopOverlay();
            }
        }

        static string Bar(int v)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 10; i++) sb.Append(i < v ? '■' : '□');
            return sb.ToString();
        }

        public void Draw(Canvas c)
        {
            c.DitherDarken(0, 0, c.W, c.H, Pal.DarkNight[4], 13);
        }

        public void DrawUi(Canvas c)
        {
            var f = g.Font;
            var s = g.Settings;
            int x = 56, y = 52;
            f.DrawCentered(c, "설정", GameCore.W / 2, 24, Pal.White);
            string[] names = { "전체 음량", "음악 음량", "화면", "CRT 효과", "번쩍임 줄이기", "글자 속도", "돌아가기" };
            string[] vals =
            {
                Bar(s.Volume), Bar(s.MusicVolume), s.Fullscreen ? "전체화면" : "창 모드",
                s.Crt ? "켜기" : "끄기", s.ReduceFlash ? "켜기" : "끄기",
                s.TextSpeed == 0 ? "느리게" : (s.TextSpeed == 1 ? "보통" : "빠르게"), ""
            };
            for (int i = 0; i < N; i++)
            {
                int ty = y + i * 22;
                bool on = i == sel;
                if (on) c.FillTri(x - 12, ty + 2, x - 12, ty + 10, x - 7, ty + 6, Pal.BloodBright);
                f.Draw(c, names[i], x, ty, on ? Pal.White : Pal.Gray);
                if (vals[i].Length > 0) f.Draw(c, vals[i], x + 110, ty, on ? Pal.PaleYellow : Pal.Gray);
            }
            g.FontSmall.DrawCentered(c, "← → 바꾸기   Esc 돌아가기", GameCore.W / 2, 214, Pal.DarkGray);
        }
    }

    public sealed class PauseState : IState
    {
        GameCore g;
        Menu menu;

        public void Enter(GameCore game)
        {
            g = game;
            menu = new Menu("계속하기", "설정", "타이틀로");
            g.Audio.Play(Sfx.MenuSelect, 0.4f, 0.8f);
        }

        public void Exit() { }

        public void Update(float dt)
        {
            int r = menu.Update(g);
            if (r == 0 || r == -2) { g.PopOverlay(); return; }
            if (r == 1) { g.PushOverlay(new SettingsState()); return; }
            if (r == 2)
            {
                g.PopOverlay();
                g.Audio.StopAllMusic(0.5f);
                g.Audio.StopOneShots();
                g.SetState(new TitleState());
            }
        }

        public void Draw(Canvas c)
        {
            c.DitherDarken(0, 0, c.W, c.H, Pal.DarkNight[4], 12);
        }

        public void DrawUi(Canvas c)
        {
            if (g.TopOverlay != this) return;
            var f = g.Font;
            f.DrawCentered(c, "일시정지", GameCore.W / 2, 50, Pal.White);
            string obj = Story.Objective(g);
            if (!string.IsNullOrEmpty(obj))
            {
                g.FontSmall.DrawCentered(c, "지금 할 일", GameCore.W / 2, 78, Pal.DarkGray);
                f.DrawCentered(c, obj, GameCore.W / 2, 92, Pal.PaleYellow);
            }
            menu.Draw(g, c, GameCore.W / 2, 130, true);
            int pt = (int)g.PlayTime;
            string time = string.Format("{0}:{1:00}", pt / 60, pt % 60);
            g.FontSmall.DrawCentered(c, "플레이 시간 " + time + "   ·   잠든 횟수 " + g.Flags.Get("deaths"), GameCore.W / 2, 206, Pal.DarkGray);
            g.FontTiny.DrawCentered(c, "방향키 이동  Z 조사  X 손전등(누르기)  C 손가락 세기  Esc 일시정지", GameCore.W / 2, 224, Pal.DarkGray);
        }
    }
}
