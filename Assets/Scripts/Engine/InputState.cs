namespace Gawi.Engine
{
    public enum Btn
    {
        Up = 0,
        Down,
        Left,
        Right,
        Confirm,
        Cancel,
        Light,
        Check,
        Pause,
        Count
    }

    /// <summary>
    /// 고정 프레임(60Hz) 로직용 입력 상태.
    /// 호스트는 매 렌더 프레임 SetRaw()로 현재 키 상태를 넣고,
    /// 게임은 틱마다 Latch()로 확정한 뒤 Down/Pressed/Released를 읽는다.
    /// 틱보다 짧은 탭도 놓치지 않도록 눌림을 보관한다.
    /// </summary>
    public sealed class InputState
    {
        const int N = (int)Btn.Count;
        readonly bool[] raw = new bool[N];
        readonly bool[] lastRaw = new bool[N];
        readonly bool[] pending = new bool[N];
        readonly bool[] cur = new bool[N];
        readonly bool[] prev = new bool[N];
        readonly float[] held = new float[N];
        readonly float[] repeatT = new float[N];
        readonly bool[] repeatFire = new bool[N];
        bool blocked;

        public void SetRaw(Btn b, bool down)
        {
            int i = (int)b;
            if (down && !lastRaw[i]) pending[i] = true;
            raw[i] = down;
            lastRaw[i] = down;
        }

        public void ClearAll()
        {
            for (int i = 0; i < N; i++)
            {
                raw[i] = lastRaw[i] = pending[i] = cur[i] = prev[i] = false;
                held[i] = 0;
            }
        }

        public void Latch(float dt)
        {
            for (int i = 0; i < N; i++)
            {
                prev[i] = cur[i];
                cur[i] = raw[i] || pending[i];
                pending[i] = false;
                repeatFire[i] = false;
                if (cur[i])
                {
                    if (!prev[i]) { held[i] = 0; repeatT[i] = 0.32f; repeatFire[i] = true; }
                    else
                    {
                        held[i] += dt;
                        repeatT[i] -= dt;
                        if (repeatT[i] <= 0) { repeatT[i] += 0.09f; repeatFire[i] = true; }
                    }
                }
                else held[i] = 0;
            }
        }

        /// <summary>대화창 직후 등, 이미 눌려 있던 키의 "눌림"을 한 번 무시한다.</summary>
        public void Consume(Btn b)
        {
            prev[(int)b] = true;
            repeatFire[(int)b] = false;
        }

        public void ConsumeAll()
        {
            for (int i = 0; i < N; i++) { prev[i] = cur[i]; repeatFire[i] = false; }
        }

        /// <summary>스크립트 컷신 동안 플레이어 입력 차단 (메뉴/대화는 Raw 메서드 사용).</summary>
        public bool Blocked { get { return blocked; } set { blocked = value; } }

        public bool Down(Btn b) { return cur[(int)b]; }
        public bool Pressed(Btn b) { return cur[(int)b] && !prev[(int)b]; }
        public bool Released(Btn b) { return !cur[(int)b] && prev[(int)b]; }
        public bool Repeat(Btn b) { return repeatFire[(int)b]; }
        public float HeldTime(Btn b) { return held[(int)b]; }

        public bool AnyPressed()
        {
            for (int i = 0; i < N; i++) if (cur[i] && !prev[i]) return true;
            return false;
        }

        public bool ConfirmPressed()
        {
            return Pressed(Btn.Confirm);
        }
    }
}
