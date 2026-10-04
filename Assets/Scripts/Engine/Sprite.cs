using System;
using System.Collections.Generic;

namespace Gawi.Engine
{
    /// <summary>
    /// 인덱스 컬러 스프라이트. 아스키 아트 문자열에서 만든다.
    /// 문자 → 팔레트 매핑은 <see cref="SpriteChars"/> 참고.
    /// </summary>
    public sealed class Sprite
    {
        public readonly int W, H;
        public readonly byte[] Px;
        public int OriginX, OriginY;

        public Sprite(int w, int h)
        {
            W = w; H = h;
            Px = new byte[w * h];
            for (int i = 0; i < Px.Length; i++) Px[i] = Pal.Transparent;
        }

        public byte Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return Pal.Transparent;
            return Px[y * W + x];
        }

        public void Set(int x, int y, byte c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Px[y * W + x] = c;
        }

        /// <summary>
        /// 아스키 아트를 스프라이트로. 줄 길이가 달라도 된다(짧은 줄은 투명으로 채움).
        /// originX/Y가 음수면 아래 가운데(발 위치)를 기준점으로 잡는다.
        /// </summary>
        public static Sprite Parse(string[] rows, int originX = -1, int originY = -1)
        {
            int h = rows.Length;
            int w = 0;
            for (int i = 0; i < h; i++) w = Math.Max(w, rows[i].Length);
            var s = new Sprite(Math.Max(1, w), Math.Max(1, h));
            for (int y = 0; y < h; y++)
            {
                string r = rows[y];
                for (int x = 0; x < r.Length; x++) s.Px[y * s.W + x] = SpriteChars.Map(r[x]);
            }
            s.OriginX = originX >= 0 ? originX : s.W / 2;
            s.OriginY = originY >= 0 ? originY : s.H - 1;
            return s;
        }

        /// <summary>여러 줄 문자열 버전. 앞뒤 빈 줄은 무시한다.</summary>
        public static Sprite Parse(string block, int originX = -1, int originY = -1)
        {
            var lines = new List<string>(block.Replace("\r", "").Split('\n'));
            while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
            while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);
            // 공통 들여쓰기 제거
            int indent = int.MaxValue;
            foreach (var l in lines)
            {
                if (l.Trim().Length == 0) continue;
                int k = 0;
                while (k < l.Length && l[k] == ' ') k++;
                indent = Math.Min(indent, k);
            }
            if (indent == int.MaxValue) indent = 0;
            for (int i = 0; i < lines.Count; i++)
                lines[i] = lines[i].Length >= indent ? lines[i].Substring(indent) : "";
            return Parse(lines.ToArray(), originX, originY);
        }

        public Sprite FlippedX()
        {
            var s = new Sprite(W, H);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    s.Px[y * W + x] = Px[y * W + (W - 1 - x)];
            s.OriginX = W - 1 - OriginX;
            s.OriginY = OriginY;
            return s;
        }

        public Sprite Recolored(byte from, byte to)
        {
            var s = new Sprite(W, H);
            for (int i = 0; i < Px.Length; i++) s.Px[i] = Px[i] == from ? to : Px[i];
            s.OriginX = OriginX; s.OriginY = OriginY;
            return s;
        }

        public Sprite Remapped(byte[] map)
        {
            var s = new Sprite(W, H);
            for (int i = 0; i < Px.Length; i++) s.Px[i] = Px[i] == Pal.Transparent ? Pal.Transparent : map[Px[i]];
            s.OriginX = OriginX; s.OriginY = OriginY;
            return s;
        }
    }

    public static class SpriteChars
    {
        static readonly byte[] table = Build();

        static byte[] Build()
        {
            var t = new byte[128];
            for (int i = 0; i < t.Length; i++) t[i] = Pal.Transparent;
            t['0'] = Pal.Black; t['1'] = Pal.Ink; t['2'] = Pal.Night; t['3'] = Pal.DarkGray;
            t['4'] = Pal.Gray; t['5'] = Pal.LightGray; t['6'] = Pal.White;
            t['c'] = Pal.PaleBlue; t['b'] = Pal.Blue; t['B'] = Pal.DarkBlue; t['e'] = Pal.Teal;
            t['G'] = Pal.DarkGreen; t['g'] = Pal.Green; t['o'] = Pal.Olive;
            t['N'] = Pal.DarkBrown; t['n'] = Pal.Brown; t['T'] = Pal.Tan;
            t['y'] = Pal.Yellow; t['Y'] = Pal.PaleYellow; t['a'] = Pal.Orange;
            t['t'] = Pal.SkinLight; t['s'] = Pal.Skin; t['S'] = Pal.SkinShadow;
            t['k'] = Pal.Pink; t['p'] = Pal.Purple; t['P'] = Pal.DarkPurple;
            t['x'] = Pal.BloodBright; t['r'] = Pal.Red; t['R'] = Pal.DarkRed; t['m'] = Pal.Maroon;
            t['f'] = Pal.Flesh; t['w'] = Pal.Bone;
            return t;
        }

        public static byte Map(char c)
        {
            if (c >= 128) return Pal.Transparent;
            return table[c];
        }
    }
}
