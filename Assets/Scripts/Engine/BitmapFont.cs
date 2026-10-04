using System;
using System.Collections.Generic;
using System.Text;

namespace Gawi.Engine
{
    /// <summary>
    /// Tools/FontBake/bake_font.py 로 구운 비트맵 폰트(.bytes)를 읽고 그린다.
    /// </summary>
    public sealed class BitmapFont
    {
        struct Glyph
        {
            public byte Adv, W, H;
            public sbyte XOff, YOff;
            public int Offset;
            public bool Valid;
        }

        readonly byte[] data;
        readonly Glyph[] ascii = new Glyph[128];
        readonly Glyph[] hangul = new Glyph[11172];
        readonly Dictionary<int, Glyph> other = new Dictionary<int, Glyph>();
        Glyph fallback;

        public int Ascent { get; private set; }
        public int Descent { get; private set; }
        /// <summary>한글 글자 윗부분 높이 (베이스라인 위).</summary>
        public int VisualAscent { get; private set; }
        /// <summary>줄 간격 (픽셀).</summary>
        public int LineStep;

        BitmapFont(byte[] bytes)
        {
            data = bytes;
        }

        public static BitmapFont Load(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 12 || bytes[0] != 'G' || bytes[1] != 'F' || bytes[2] != 'N' || bytes[3] != 'T')
                throw new ArgumentException("Invalid font data");
            var f = new BitmapFont(bytes);
            f.Ascent = bytes[5];
            f.Descent = bytes[6];
            int count = (int)ReadU32(bytes, 8);
            int o = 12;
            for (int i = 0; i < count; i++)
            {
                if (o + 9 > bytes.Length) break;
                int cp = (int)ReadU32(bytes, o);
                var g = new Glyph
                {
                    Adv = bytes[o + 4],
                    W = bytes[o + 5],
                    H = bytes[o + 6],
                    XOff = unchecked((sbyte)bytes[o + 7]),
                    YOff = unchecked((sbyte)bytes[o + 8]),
                    Offset = o + 9,
                    Valid = true
                };
                o += 9 + g.H * ((g.W + 7) / 8);
                if (cp < 128) f.ascii[cp] = g;
                else if (cp >= 0xAC00 && cp <= 0xD7A3) f.hangul[cp - 0xAC00] = g;
                else f.other[cp] = g;
            }
            f.fallback = f.ascii['?'];
            Glyph ga;
            if (f.TryGet(0xAC00, out ga)) f.VisualAscent = ga.YOff + ga.H;
            else f.VisualAscent = f.Ascent;
            f.LineStep = f.VisualAscent + Math.Max(3, f.Descent + 2);
            return f;
        }

        static uint ReadU32(byte[] b, int o)
        {
            return (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));
        }

        bool TryGet(int cp, out Glyph g)
        {
            if (cp >= 0 && cp < 128) { g = ascii[cp]; return g.Valid; }
            if (cp >= 0xAC00 && cp <= 0xD7A3) { g = hangul[cp - 0xAC00]; return g.Valid; }
            return other.TryGetValue(cp, out g);
        }

        Glyph Get(char c)
        {
            Glyph g;
            if (TryGet(c, out g)) return g;
            return fallback;
        }

        public int Advance(char c)
        {
            if (c == '\n') return 0;
            return Get(c).Adv;
        }

        public int Measure(string s)
        {
            int w = 0, best = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\n') { best = Math.Max(best, w); w = 0; continue; }
                w += Advance(s[i]);
            }
            return Math.Max(best, w);
        }

        /// <summary>y는 글줄의 윗부분. maxChars로 타자기 효과.</summary>
        public int Draw(Canvas cv, string s, int x, int y, byte color, int maxChars = int.MaxValue)
        {
            int baseline = y + VisualAscent;
            int cx = x;
            int n = Math.Min(s.Length, maxChars);
            for (int i = 0; i < n; i++)
            {
                char ch = s[i];
                if (ch == '\n') { cx = x; baseline += LineStep; continue; }
                var g = Get(ch);
                DrawGlyph(cv, g, cx, baseline, color);
                cx += g.Adv;
            }
            return cx;
        }

        void DrawGlyph(Canvas cv, Glyph g, int x, int baseline, byte color)
        {
            if (!g.Valid) return;
            int bpr = (g.W + 7) / 8;
            int top = baseline - g.YOff - g.H;
            for (int r = 0; r < g.H; r++)
            {
                int rowOff = g.Offset + r * bpr;
                for (int c = 0; c < g.W; c++)
                {
                    if ((data[rowOff + (c >> 3)] & (0x80 >> (c & 7))) != 0)
                        cv.Set(x + g.XOff + c, top + r, color);
                }
            }
        }

        public void DrawOutlined(Canvas cv, string s, int x, int y, byte color, byte outline, int maxChars = int.MaxValue)
        {
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                    if (ox != 0 || oy != 0) Draw(cv, s, x + ox, y + oy, outline, maxChars);
            Draw(cv, s, x, y, color, maxChars);
        }

        public void DrawShadow(Canvas cv, string s, int x, int y, byte color, byte shadow, int maxChars = int.MaxValue)
        {
            Draw(cv, s, x + 1, y + 1, shadow, maxChars);
            Draw(cv, s, x, y, color, maxChars);
        }

        public void DrawCentered(Canvas cv, string s, int cx, int y, byte color)
        {
            Draw(cv, s, cx - Measure(s) / 2, y, color);
        }

        /// <summary>
        /// 띄어쓰기 기준 줄바꿈. 한 단어가 너무 길면 글자 단위로 자른다.
        /// </summary>
        public List<string> Wrap(string text, int maxWidth)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) { result.Add(""); return result; }
            var paragraphs = text.Replace("\r", "").Split('\n');
            var sb = new StringBuilder();
            foreach (var para in paragraphs)
            {
                var words = para.Split(' ');
                sb.Length = 0;
                int lineW = 0;
                int spaceW = Advance(' ');
                foreach (var word in words)
                {
                    int ww = Measure(word);
                    if (sb.Length > 0 && lineW + spaceW + ww <= maxWidth)
                    {
                        sb.Append(' ').Append(word);
                        lineW += spaceW + ww;
                        continue;
                    }
                    if (sb.Length > 0)
                    {
                        result.Add(sb.ToString());
                        sb.Length = 0;
                        lineW = 0;
                    }
                    if (ww <= maxWidth)
                    {
                        sb.Append(word);
                        lineW = ww;
                    }
                    else
                    {
                        // 글자 단위로 자르기
                        foreach (char ch in word)
                        {
                            int cw = Advance(ch);
                            if (lineW + cw > maxWidth && sb.Length > 0)
                            {
                                result.Add(sb.ToString());
                                sb.Length = 0;
                                lineW = 0;
                            }
                            sb.Append(ch);
                            lineW += cw;
                        }
                    }
                }
                result.Add(sb.ToString());
            }
            return result;
        }
    }
}
