using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    public interface IState
    {
        void Enter(GameCore g);
        void Exit();
        void Update(float dt);
        /// <summary>장면(월드/컷신 배경).</summary>
        void Draw(Canvas c);
        /// <summary>후처리(비네트, 페이드) 뒤에 그리는 UI.</summary>
        void DrawUi(Canvas c);
    }

    /// <summary>
    /// 게임 전체. Unity와 무관한 순수 C#.
    /// 호스트는 매 틱 Input을 넣고 Tick(), 매 프레임 Render() + Present()를 부른다.
    /// </summary>
    public sealed class GameCore
    {
        public const int W = 320, H = 240;
        public const int WorldW = 160, WorldH = 120;
        public const float Dt = 1f / 60f;

        public readonly Canvas Screen = new Canvas(W, H);
        public readonly InputState Input = new InputState();
        public readonly AudioSys Audio;
        public readonly ISaveStore Store;
        public readonly IPlatform Platform;
        public readonly BitmapFont Font, FontSmall, FontTiny;
        public readonly Rng Rng = new Rng(20250404);
        public readonly ScriptRunner Scripts = new ScriptRunner();
        public readonly Settings Settings = new Settings();
        public readonly Flags Flags = new Flags();
        public readonly ScreenFx Fx;
        public readonly Dialogue Dialogue;
        public readonly NoteView Note;

        public float Time;
        public int Frame;
        public float PlayTime;
        public bool QuitRequested;
        /// <summary>플레이어가 화면 아래쪽에 있으면 대화창을 위로.</summary>
        public bool PreferTopBox;

        IState state;
        readonly List<IState> overlays = new List<IState>();
        public Cutscene Cutscene;
        public PlayState Play { get { return state as PlayState; } }
        public IState State { get { return state; } }
        public bool Paused { get { return overlays.Count > 0; } }

        public GameCore(byte[] font11, byte[] font9, byte[] font7, IAudioBackend audio, ISaveStore store, IPlatform platform)
        {
            Font = BitmapFont.Load(font11);
            FontSmall = BitmapFont.Load(font9);
            FontTiny = BitmapFont.Load(font7);
            Font.LineStep = 15;
            FontSmall.LineStep = 12;
            FontTiny.LineStep = 9;
            Store = store ?? new MemorySaveStore();
            Platform = platform ?? new NullPlatform();
            Audio = new AudioSys(audio);
            Fx = new ScreenFx(this);
            Dialogue = new Dialogue(this);
            Note = new NoteView(this);
            Settings.Load(Store, Platform.Fullscreen);
            ApplySettings();
            SetState(new WarningState());
        }

        public void ApplySettings()
        {
            Audio.MasterVolume = Settings.Volume / 10f;
            Audio.MusicVolume = Settings.MusicVolume / 10f;
            Audio.RefreshVolumes();
            if (Platform.Fullscreen != Settings.Fullscreen) Platform.Fullscreen = Settings.Fullscreen;
        }

        // ───────── 상태 ─────────

        public void SetState(IState s)
        {
            if (state != null) state.Exit();
            overlays.Clear();
            Scripts.StopAll();
            Dialogue.Close();
            Note.Active = false;
            Cutscene = null;
            Input.Blocked = false;
            PreferTopBox = false;
            state = s;
            if (state != null) state.Enter(this);
        }

        public void PushOverlay(IState s)
        {
            overlays.Add(s);
            s.Enter(this);
        }

        public void PopOverlay()
        {
            if (overlays.Count == 0) return;
            var top = overlays[overlays.Count - 1];
            overlays.RemoveAt(overlays.Count - 1);
            top.Exit();
            Input.ConsumeAll();
        }

        public IState TopOverlay { get { return overlays.Count > 0 ? overlays[overlays.Count - 1] : null; } }

        /// <summary>창이 포커스를 잃으면 게임 중일 때 일시정지.</summary>
        public void OnFocusLost()
        {
            var ps = state as PlayState;
            if (ps == null || overlays.Count > 0 || Cutscene != null || ps.Dying) return;
            PushOverlay(new PauseState());
        }

        // ───────── 루프 ─────────

        public void Tick(float dt)
        {
            Input.Latch(dt);
            Time += dt;
            Frame++;
            if (overlays.Count > 0)
            {
                overlays[overlays.Count - 1].Update(dt);
                Audio.Update(dt);
                return;
            }
            if (!Audio.WarmDone) Audio.WarmStep();
            if (Dialogue.Active) Dialogue.Update(dt);
            else if (Note.Active) Note.Update(dt);
            if (state != null) state.Update(dt);
            if (Cutscene != null) Cutscene.Tick(dt);
            Scripts.Tick(dt);
            Fx.Update(dt);
            Audio.Update(dt);
            if (state is PlayState) PlayTime += dt;
        }

        public void Render()
        {
            Screen.OffX = 0; Screen.OffY = 0;
            Screen.ResetClip();
            Screen.Clear(Pal.Black);
            if (state != null && (Cutscene == null || !Cutscene.Opaque)) state.Draw(Screen);
            if (Cutscene != null) Cutscene.Draw(Screen);
            Screen.OffX = 0; Screen.OffY = 0;
            Screen.ResetClip();
            Fx.Apply(Screen);
            if (state != null) state.DrawUi(Screen);
            if (Cutscene != null) Cutscene.DrawUi(Screen);
            Note.Draw(Screen);
            Dialogue.Draw(Screen);
            foreach (var o in overlays) { o.Draw(Screen); o.DrawUi(Screen); }
        }

        /// <summary>인덱스 → RGBA (Unity 텍스처 순서: 아래 행부터).</summary>
        public void Present(byte[] rgba)
        {
            var tint = Fx.Tint;
            bool inv = Fx.Invert;
            var px = Screen.Px;
            for (int y = 0; y < H; y++)
            {
                int src = y * W;
                int dst = (H - 1 - y) * W * 4;
                for (int x = 0; x < W; x++)
                {
                    int idx = px[src + x];
                    if (tint != null) idx = tint[idx];
                    if (inv) idx = Pal.Negative[idx];
                    if (idx >= Pal.Count) idx = 0;
                    int p = idx * 3;
                    rgba[dst] = Pal.Rgb[p];
                    rgba[dst + 1] = Pal.Rgb[p + 1];
                    rgba[dst + 2] = Pal.Rgb[p + 2];
                    rgba[dst + 3] = 255;
                    dst += 4;
                }
            }
        }

        // ───────── 스크립트 도우미 ─────────

        public IWait Wait(float seconds) { return new WaitSeconds(seconds); }

        public IEnumerator Say(Speaker s, string text)
        {
            Dialogue.Open(s, text);
            while (Dialogue.Active) yield return null;
        }

        public IEnumerator SayAt(Speaker s, string text, BoxPos pos)
        {
            var old = Dialogue.Pos;
            Dialogue.Pos = pos;
            Dialogue.Open(s, text);
            while (Dialogue.Active) yield return null;
            Dialogue.Pos = old;
        }

        /// <summary>입력 없이 일정 시간 후 닫히는 대사.</summary>
        public IEnumerator SayTimed(Speaker s, string text, float seconds)
        {
            Dialogue.Open(s, text, seconds);
            while (Dialogue.Active) yield return null;
        }

        public IEnumerator Sing(Speaker s, string text, int[] melody)
        {
            Dialogue.Open(s, text, -1f, melody);
            while (Dialogue.Active) yield return null;
        }

        public IEnumerator Ask(string question, string[] options, Action<int> result)
        {
            Dialogue.Ask(question, options, result);
            while (Dialogue.Active) yield return null;
        }

        public IEnumerator ShowNote(string title, string body, bool bloody = false, bool child = false, bool lucid = false)
        {
            Note.Open(title, body, bloody, child, lucid);
            while (Note.Active) yield return null;
        }

        public IEnumerator PlayCutscene(Cutscene cs)
        {
            Cutscene = cs;
            cs.Begin(this);
            var h = Scripts.Start(cs.Run(), "cutscene");
            while (!h.Done) yield return null;
            cs.End();
            if (Cutscene == cs) Cutscene = null;
        }

        public IEnumerator FadeOut(float t) { return Fx.FadeTo(1f, t); }
        public IEnumerator FadeIn(float t) { return Fx.FadeTo(0f, t); }

        // ───────── 저장 ─────────

        public bool HasSave { get { return Store.Get("save_exists", "0") == "1"; } }

        public void SaveCheckpoint(string room, float x, float y)
        {
            Store.Set("save_exists", "1");
            Store.Set("save_room", room);
            Store.Set("save_x", x.ToString("0.0", CultureInfo.InvariantCulture));
            Store.Set("save_y", y.ToString("0.0", CultureInfo.InvariantCulture));
            Store.Set("save_flags", Flags.Serialize());
            Store.Set("save_time", PlayTime.ToString("0", CultureInfo.InvariantCulture));
            Store.Flush();
        }

        public bool LoadCheckpoint(out string room, out float x, out float y)
        {
            room = Store.Get("save_room", "");
            float.TryParse(Store.Get("save_x", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out x);
            float.TryParse(Store.Get("save_y", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out y);
            float pt;
            float.TryParse(Store.Get("save_time", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out pt);
            PlayTime = pt;
            Flags.Deserialize(Store.Get("save_flags", ""));
            return room.Length > 0;
        }

        public void DeleteSave()
        {
            Store.Delete("save_exists");
            Store.Delete("save_room");
            Store.Delete("save_flags");
            Store.Flush();
        }

        public void NewGame()
        {
            Flags.Reset();
            PlayTime = 0;
            Fx.Reset();
            Audio.StopAllMusic(0.3f);
            var ps = new PlayState();
            SetState(ps);
            Scripts.Start(Story.Prologue(this), "story");
        }

        public void ContinueGame()
        {
            string room; float x, y;
            Fx.Reset();
            if (!LoadCheckpoint(out room, out x, out y)) { NewGame(); return; }
            var ps = new PlayState();
            SetState(ps);
            ps.StartAt(room, x, y);
            Scripts.Start(Story.OnContinue(this), "story");
        }
    }
}
