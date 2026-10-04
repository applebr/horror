using System;

namespace Gawi.Engine
{
    /// <summary>
    /// 32색 고정 팔레트. 프레임버퍼는 이 인덱스(byte)로만 그린다.
    /// 어둠/색조 변환은 인덱스→인덱스 리맵 테이블로 처리한다.
    /// </summary>
    public static class Pal
    {
        public const byte Black = 0;
        public const byte Ink = 1;
        public const byte Night = 2;
        public const byte DarkGray = 3;
        public const byte Gray = 4;
        public const byte LightGray = 5;
        public const byte White = 6;
        public const byte PaleBlue = 7;
        public const byte Blue = 8;
        public const byte DarkBlue = 9;
        public const byte Teal = 10;
        public const byte DarkGreen = 11;
        public const byte Green = 12;
        public const byte Olive = 13;
        public const byte DarkBrown = 14;
        public const byte Brown = 15;
        public const byte Tan = 16;
        public const byte Yellow = 17;
        public const byte PaleYellow = 18;
        public const byte Orange = 19;
        public const byte SkinLight = 20;
        public const byte Skin = 21;
        public const byte SkinShadow = 22;
        public const byte Pink = 23;
        public const byte Purple = 24;
        public const byte DarkPurple = 25;
        public const byte BloodBright = 26;
        public const byte Red = 27;
        public const byte DarkRed = 28;
        public const byte Maroon = 29;
        public const byte Flesh = 30;
        public const byte Bone = 31;

        public const int Count = 32;
        public const byte Transparent = 255;

        /// <summary>RGB 테이블 (r,g,b 순서로 3바이트씩).</summary>
        public static readonly byte[] Rgb =
        {
            0, 0, 0,          // Black
            14, 12, 22,       // Ink
            26, 28, 48,       // Night
            52, 52, 62,       // DarkGray
            98, 98, 108,      // Gray
            156, 156, 164,    // LightGray
            232, 232, 226,    // White
            170, 190, 230,    // PaleBlue
            60, 80, 160,      // Blue
            30, 36, 92,       // DarkBlue
            24, 72, 80,       // Teal
            22, 52, 30,       // DarkGreen
            52, 104, 56,      // Green
            110, 110, 50,     // Olive
            52, 30, 20,       // DarkBrown
            104, 62, 36,      // Brown
            168, 124, 76,     // Tan
            232, 196, 92,     // Yellow
            252, 238, 170,    // PaleYellow
            214, 110, 36,     // Orange
            244, 206, 176,    // SkinLight
            220, 164, 128,    // Skin
            164, 104, 84,     // SkinShadow
            222, 128, 152,    // Pink
            104, 56, 124,     // Purple
            46, 26, 56,       // DarkPurple
            232, 24, 24,      // BloodBright
            176, 16, 20,      // Red
            104, 8, 12,       // DarkRed
            58, 4, 8,         // Maroon
            196, 96, 92,      // Flesh
            226, 214, 184,    // Bone
        };

        /// <summary>어둠 단계 수 (0 = 원래 밝기, Levels-1 = 완전한 검정).</summary>
        public const int Levels = 5;

        /// <summary>[level][index] 일반 어둠 (푸른 밤 쪽으로).</summary>
        public static readonly byte[][] DarkNight;
        /// <summary>[level][index] 자각/악몽 어둠 (붉은 쪽으로).</summary>
        public static readonly byte[][] DarkBlood;
        /// <summary>[level][index] 따뜻한 전등빛 어둠 (현실).</summary>
        public static readonly byte[][] DarkWarm;

        public static readonly byte[] Identity = new byte[256];
        public static readonly byte[] RedTint = new byte[256];
        public static readonly byte[] Negative = new byte[256];
        public static readonly byte[] Sepia = new byte[256];
        public static readonly byte[] Gray4 = new byte[256];
        public static readonly byte[] BlueTint = new byte[256];

        static Pal()
        {
            for (int i = 0; i < 256; i++) Identity[i] = (byte)i;

            float[] factors = { 1f, 0.62f, 0.38f, 0.17f, 0f };
            DarkNight = BuildRamps(factors, 10, 12, 34);
            DarkBlood = BuildRamps(factors, 40, 2, 8);
            DarkWarm = BuildRamps(factors, 26, 16, 10);

            byte[] redRamp = { Black, Maroon, DarkRed, Red, BloodBright, Pink, Bone };
            byte[] sepiaRamp = { Black, DarkBrown, Brown, Tan, Bone, PaleYellow };
            byte[] grayRamp = { Black, DarkGray, Gray, LightGray, White };
            byte[] blueRamp = { Black, Ink, Night, DarkBlue, Blue, PaleBlue, White };
            for (int i = 0; i < 256; i++)
            {
                if (i >= Count)
                {
                    RedTint[i] = Sepia[i] = Negative[i] = Gray4[i] = BlueTint[i] = (byte)i;
                    continue;
                }
                float l = Luma(i);
                RedTint[i] = redRamp[Math.Min(redRamp.Length - 1, (int)(l * redRamp.Length))];
                Sepia[i] = sepiaRamp[Math.Min(sepiaRamp.Length - 1, (int)(l * sepiaRamp.Length))];
                Gray4[i] = grayRamp[Math.Min(grayRamp.Length - 1, (int)(l * grayRamp.Length))];
                BlueTint[i] = blueRamp[Math.Min(blueRamp.Length - 1, (int)(l * blueRamp.Length))];
                int r = 255 - Rgb[i * 3], g = 255 - Rgb[i * 3 + 1], b = 255 - Rgb[i * 3 + 2];
                Negative[i] = Nearest(r, g, b);
            }
        }

        static byte[][] BuildRamps(float[] factors, int sr, int sg, int sb)
        {
            var ramps = new byte[factors.Length][];
            for (int lv = 0; lv < factors.Length; lv++)
            {
                var t = new byte[256];
                float f = factors[lv];
                for (int i = 0; i < 256; i++)
                {
                    if (i >= Count) { t[i] = (byte)i; continue; }
                    if (lv == 0) { t[i] = (byte)i; continue; }
                    if (f <= 0f) { t[i] = Black; continue; }
                    // 어두운 쪽으로 갈수록 그림자 색(sr,sg,sb)에 섞인다.
                    float mix = (1f - f) * 0.55f;
                    float r = Rgb[i * 3] * f * (1 - mix) + sr * mix;
                    float g = Rgb[i * 3 + 1] * f * (1 - mix) + sg * mix;
                    float b = Rgb[i * 3 + 2] * f * (1 - mix) + sb * mix;
                    byte n = Nearest((int)r, (int)g, (int)b);
                    // 어둠은 원래 색보다 밝아지면 안 된다.
                    if (Luma(n) > Luma(i)) n = (byte)i;
                    t[i] = n;
                }
                ramps[lv] = t;
            }
            // 단계가 내려갈수록 단조롭게 어두워지도록 보정
            for (int i = 0; i < Count; i++)
            {
                for (int lv = 1; lv < factors.Length; lv++)
                {
                    if (Luma(ramps[lv][i]) > Luma(ramps[lv - 1][i])) ramps[lv][i] = ramps[lv - 1][i];
                }
            }
            return ramps;
        }

        public static float Luma(int i)
        {
            if (i < 0 || i >= Count) return 0f;
            return (Rgb[i * 3] * 0.299f + Rgb[i * 3 + 1] * 0.587f + Rgb[i * 3 + 2] * 0.114f) / 255f;
        }

        public static byte Nearest(int r, int g, int b)
        {
            int best = 0;
            long bestD = long.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                int dr = Rgb[i * 3] - r, dg = Rgb[i * 3 + 1] - g, db = Rgb[i * 3 + 2] - b;
                long d = 3L * dr * dr + 4L * dg * dg + 2L * db * db;
                if (d < bestD) { bestD = d; best = i; }
            }
            return (byte)best;
        }

        /// <summary>두 리맵 테이블을 합성 (먼저 a, 다음 b).</summary>
        public static void Compose(byte[] a, byte[] b, byte[] dst)
        {
            for (int i = 0; i < 256; i++) dst[i] = b[a[i]];
        }
    }
}
