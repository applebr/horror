using UnityEngine;
using Gawi.Engine;
using Gawi.Game;

namespace Gawi.UnityLayer
{
    /// <summary>
    /// Unity와 게임(순수 C#)을 잇는 유일한 MonoBehaviour.
    /// 어떤 씬에서 재생하든 자동으로 생성된다 (RuntimeInitializeOnLoadMethod).
    /// - 60Hz 고정 틱으로 GameCore를 돌리고
    /// - 320×240 프레임버퍼를 텍스처로 올려 화면 가운데에 정수 배율로 그린다
    /// - 키보드/게임패드 입력과 소리를 연결한다
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GawiHost : MonoBehaviour
    {
        public static GawiHost Instance;

        GameCore game;
        Texture2D screenTex;
        Texture2D scanTex;
        int scanScale = -1;
        byte[] rgba;
        float acc;
        UnityAudioBackend audioBackend;
        readonly bool[] guiKeys = new bool[(int)Btn.Count];
#if ENABLE_LEGACY_INPUT_MANAGER
        bool axisOk = true;
#else
        bool f11Pressed;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("GAWI");
            go.AddComponent<GawiHost>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;

            // 화면을 검게 지우기만 하는 카메라 (그림은 OnGUI에서)
            var cam = gameObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;
            cam.orthographic = true;
            cam.depth = -100;
            var main = Camera.main;
            if (main == null || main.GetComponent<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();

            screenTex = new Texture2D(GameCore.W, GameCore.H, TextureFormat.RGBA32, false);
            screenTex.filterMode = FilterMode.Point;
            screenTex.wrapMode = TextureWrapMode.Clamp;
            screenTex.name = "GAWI_Screen";
            rgba = new byte[GameCore.W * GameCore.H * 4];

            audioBackend = gameObject.AddComponent<UnityAudioBackend>();

            var f11 = Resources.Load<TextAsset>("Fonts/galmuri11");
            var f9 = Resources.Load<TextAsset>("Fonts/galmuri9");
            var f7 = Resources.Load<TextAsset>("Fonts/galmuri7");
            if (f11 == null || f9 == null || f7 == null)
            {
                Debug.LogError("[GAWI] Assets/Resources/Fonts/galmuri*.bytes 를 찾을 수 없습니다.");
                enabled = false;
                return;
            }
            game = new GameCore(f11.bytes, f9.bytes, f7.bytes, audioBackend, new PlayerPrefsStore(), new UnityPlatform());
        }

        void Update()
        {
            if (game == null) return;
            PollInput();
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            acc += dt;
            int steps = 0;
            while (acc >= GameCore.Dt && steps < 6)
            {
                game.Tick(GameCore.Dt);
                acc -= GameCore.Dt;
                steps++;
            }
            if (steps >= 6) acc = 0f;
            if (game.QuitRequested)
            {
                game.QuitRequested = false;
                UnityPlatform.DoQuit();
            }
            game.Render();
            game.Present(rgba);
            screenTex.LoadRawTextureData(rgba);
            screenTex.Apply(false, false);
            if (Input_GetKeyDownF11())
            {
                game.Settings.Fullscreen = !Screen.fullScreen;
                game.Settings.Save(game.Store);
                game.ApplySettings();
            }
            Cursor.visible = !Screen.fullScreen;
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && game != null)
            {
                for (int i = 0; i < guiKeys.Length; i++) guiKeys[i] = false;
                game.Input.ClearAll();
                game.OnFocusLost();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (screenTex != null) Destroy(screenTex);
            if (scanTex != null) Destroy(scanTex);
        }

        // ───────── 입력 ─────────

        void PollInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            var inp = game.Input;
            float ax = 0, ay = 0;
            if (axisOk)
            {
                try
                {
                    ax = Input.GetAxisRaw("Horizontal");
                    ay = Input.GetAxisRaw("Vertical");
                }
                catch (System.Exception)
                {
                    axisOk = false;
                }
            }
            inp.SetRaw(Btn.Up, Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || ay > 0.5f);
            inp.SetRaw(Btn.Down, Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || ay < -0.5f);
            inp.SetRaw(Btn.Left, Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || ax < -0.5f);
            inp.SetRaw(Btn.Right, Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || ax > 0.5f);
            inp.SetRaw(Btn.Confirm, Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.JoystickButton0));
            inp.SetRaw(Btn.Cancel, Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Backspace) || Input.GetKey(KeyCode.JoystickButton1));
            inp.SetRaw(Btn.Light, Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.JoystickButton2));
            inp.SetRaw(Btn.Check, Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.JoystickButton3));
            inp.SetRaw(Btn.Pause, Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.P) || Input.GetKey(KeyCode.JoystickButton7));
#else
            // 구 입력 관리자가 꺼져 있으면 IMGUI 키 이벤트로 받는다 (OnGUI에서 갱신)
            for (int i = 0; i < guiKeys.Length; i++) game.Input.SetRaw((Btn)i, guiKeys[i]);
#endif
        }

        bool Input_GetKeyDownF11()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F11);
#else
            bool p = f11Pressed;
            f11Pressed = false;
            return p;
#endif
        }

#if !ENABLE_LEGACY_INPUT_MANAGER
        static int MapKey(KeyCode k)
        {
            switch (k)
            {
                case KeyCode.UpArrow: case KeyCode.W: return (int)Btn.Up;
                case KeyCode.DownArrow: case KeyCode.S: return (int)Btn.Down;
                case KeyCode.LeftArrow: case KeyCode.A: return (int)Btn.Left;
                case KeyCode.RightArrow: case KeyCode.D: return (int)Btn.Right;
                case KeyCode.Z: case KeyCode.Return: case KeyCode.KeypadEnter: case KeyCode.Space: return (int)Btn.Confirm;
                case KeyCode.Backspace: return (int)Btn.Cancel;
                case KeyCode.X: case KeyCode.LeftShift: return (int)Btn.Light;
                case KeyCode.C: return (int)Btn.Check;
                case KeyCode.P: return (int)Btn.Pause;
            }
            return -1;
        }

        void HandleGuiKey(Event e)
        {
            if (e.type != EventType.KeyDown && e.type != EventType.KeyUp) return;
            bool down = e.type == EventType.KeyDown;
            if (e.keyCode == KeyCode.Escape)
            {
                guiKeys[(int)Btn.Cancel] = down;
                guiKeys[(int)Btn.Pause] = down;
                return;
            }
            if (e.keyCode == KeyCode.F11 && down) { f11Pressed = true; return; }
            int b = MapKey(e.keyCode);
            if (b >= 0) guiKeys[b] = down;
        }
#endif

        // ───────── 그리기 ─────────

        void OnGUI()
        {
            var e = Event.current;
            if (e == null) return;
#if !ENABLE_LEGACY_INPUT_MANAGER
            HandleGuiKey(e);
#endif
            if (e.type != EventType.Repaint || screenTex == null || game == null) return;
            float sw = Screen.width, sh = Screen.height;
            float s = Mathf.Min(sw / GameCore.W, sh / GameCore.H);
            bool integer = s >= 1f;
            if (integer) s = Mathf.Floor(s);
            float w = Mathf.Round(GameCore.W * s), h = Mathf.Round(GameCore.H * s);
            var rect = new Rect(Mathf.Round((sw - w) * 0.5f), Mathf.Round((sh - h) * 0.5f), w, h);
            GUI.DrawTexture(rect, screenTex, ScaleMode.StretchToFill, false);
            if (game.Settings.Crt && integer && s >= 3f)
            {
                EnsureScanlines((int)s);
                GUI.DrawTextureWithTexCoords(rect, scanTex, new Rect(0, 0, 1, GameCore.H), true);
            }
        }

        void EnsureScanlines(int scale)
        {
            if (scanTex != null && scanScale == scale) return;
            if (scanTex != null) Destroy(scanTex);
            scanScale = scale;
            scanTex = new Texture2D(1, scale, TextureFormat.RGBA32, false);
            scanTex.filterMode = FilterMode.Point;
            scanTex.wrapMode = TextureWrapMode.Repeat;
            scanTex.name = "GAWI_Scanlines";
            var px = new Color32[scale];
            for (int i = 0; i < scale; i++)
            {
                // 텍스처 0행이 아래쪽: 각 픽셀 줄의 맨 아래 한 줄을 어둡게
                byte a = (byte)(i == 0 ? 90 : (i == 1 && scale >= 5 ? 30 : 0));
                px[i] = new Color32(0, 0, 0, a);
            }
            scanTex.SetPixels32(px);
            scanTex.Apply(false, false);
        }
    }

    /// <summary>PlayerPrefs 저장소.</summary>
    public sealed class PlayerPrefsStore : ISaveStore
    {
        const string Prefix = "gawi_";
        public string Get(string key, string fallback) { return PlayerPrefs.GetString(Prefix + key, fallback); }
        public void Set(string key, string value) { PlayerPrefs.SetString(Prefix + key, value); }
        public void Delete(string key) { PlayerPrefs.DeleteKey(Prefix + key); }
        public void Flush() { PlayerPrefs.Save(); }
    }

    public sealed class UnityPlatform : IPlatform
    {
        public bool Fullscreen
        {
            get { return Screen.fullScreen; }
            set
            {
                if (value == Screen.fullScreen) return;
                if (value)
                {
                    var r = Screen.currentResolution;
                    Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
                }
                else
                {
                    int s = Mathf.Max(2, Mathf.Min((Screen.currentResolution.width - 80) / GameCore.W, (Screen.currentResolution.height - 120) / GameCore.H));
                    Screen.SetResolution(GameCore.W * s, GameCore.H * s, FullScreenMode.Windowed);
                }
            }
        }

        public bool CanQuit { get { return Application.platform != RuntimePlatform.WebGLPlayer; } }

        public void Quit() { DoQuit(); }

        public static void DoQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
