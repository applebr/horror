using System;
using System.IO;
using System.IO.Compression;
using Gawi.Engine;

namespace Gawi.Harness
{
    /// <summary>의존성 없는 PNG / WAV 저장.</summary>
    public static class ImageIO
    {
        static readonly uint[] crcTable = MakeCrc();

        static uint[] MakeCrc()
        {
            var t = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                t[n] = c;
            }
            return t;
        }

        static uint Crc(byte[] buf, int off, int len, uint c = 0xFFFFFFFFu)
        {
            for (int i = 0; i < len; i++) c = crcTable[(c ^ buf[off + i]) & 0xFF] ^ (c >> 8);
            return c;
        }

        static void Chunk(Stream s, string type, byte[] data)
        {
            var len = BitConverter.GetBytes(data.Length); Array.Reverse(len);
            s.Write(len, 0, 4);
            var t = System.Text.Encoding.ASCII.GetBytes(type);
            s.Write(t, 0, 4);
            s.Write(data, 0, data.Length);
            uint c = Crc(t, 0, 4);
            c = Crc(data, 0, data.Length, c) ^ 0xFFFFFFFFu;
            var cb = BitConverter.GetBytes(c); Array.Reverse(cb);
            s.Write(cb, 0, 4);
        }

        public static void SavePng(string path, byte[] rgb, int w, int h)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using (var fs = File.Create(path))
            {
                fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
                var ihdr = new byte[13];
                var wb = BitConverter.GetBytes(w); Array.Reverse(wb);
                var hb = BitConverter.GetBytes(h); Array.Reverse(hb);
                Array.Copy(wb, 0, ihdr, 0, 4); Array.Copy(hb, 0, ihdr, 4, 4);
                ihdr[8] = 8; ihdr[9] = 2; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
                Chunk(fs, "IHDR", ihdr);
                var raw = new byte[(w * 3 + 1) * h];
                for (int y = 0; y < h; y++)
                {
                    raw[y * (w * 3 + 1)] = 0;
                    Array.Copy(rgb, y * w * 3, raw, y * (w * 3 + 1) + 1, w * 3);
                }
                using (var ms = new MemoryStream())
                {
                    using (var z = new ZLibStream(ms, CompressionLevel.Optimal, true)) z.Write(raw, 0, raw.Length);
                    Chunk(fs, "IDAT", ms.ToArray());
                }
                Chunk(fs, "IEND", new byte[0]);
            }
        }

        /// <summary>인덱스 캔버스를 배율 확대해서 PNG로.</summary>
        public static void SaveCanvas(string path, Canvas c, int scale, byte[] remap = null)
        {
            int w = c.W * scale, h = c.H * scale;
            var rgb = new byte[w * h * 3];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int idx = c.Px[(y / scale) * c.W + (x / scale)];
                    if (remap != null) idx = remap[idx];
                    if (idx >= Pal.Count) idx = 0;
                    int o = (y * w + x) * 3;
                    rgb[o] = Pal.Rgb[idx * 3]; rgb[o + 1] = Pal.Rgb[idx * 3 + 1]; rgb[o + 2] = Pal.Rgb[idx * 3 + 2];
                }
            SavePng(path, rgb, w, h);
        }

        /// <summary>RGBA (Unity 형식: 아래쪽 행부터) 버퍼를 PNG로.</summary>
        public static void SaveRgbaFlipped(string path, byte[] rgba, int w, int h, int scale)
        {
            int W = w * scale, H = h * scale;
            var rgb = new byte[W * H * 3];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int sy = h - 1 - y / scale, sx = x / scale;
                    int si = (sy * w + sx) * 4;
                    int o = (y * W + x) * 3;
                    rgb[o] = rgba[si]; rgb[o + 1] = rgba[si + 1]; rgb[o + 2] = rgba[si + 2];
                }
            SavePng(path, rgb, W, H);
        }

        public static void SaveWav(string path, float[] samples, int rate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using (var fs = File.Create(path))
            using (var bw = new BinaryWriter(fs))
            {
                int dataLen = samples.Length * 2;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataLen);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16); bw.Write((short)1); bw.Write((short)1);
                bw.Write(rate); bw.Write(rate * 2); bw.Write((short)2); bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataLen);
                foreach (var s in samples)
                {
                    float v = Math.Max(-1f, Math.Min(1f, s));
                    bw.Write((short)(v * 32767));
                }
            }
        }
    }
}
