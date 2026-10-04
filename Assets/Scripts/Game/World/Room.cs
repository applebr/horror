using System;
using System.Collections;
using System.Collections.Generic;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    public enum Cell : byte
    {
        Void = 0,   // ' '  바깥 (검정, 막힘)
        Floor,      // '.'  장판
        Floor2,     // ','  타일
        Rug,        // '_'  깔개
        Wall,       // '#'  벽 윗면
        Face,       // 'F'  벽 앞면
        Window,     // 'W'  창문 (벽 앞면)
        Exit,       // 0-9  출구
        Blood,      // '~'  핏물 바닥
        Stairs,     // 's'  계단
        Pit,        // 'o'  구멍 (막힘, 검정)
    }

    public enum Theme { Reality, Dream, Morning, Stairs, CutRoom }

    public enum ExitStyle { Door, Open, Seam, Hidden }

    public sealed class ExitDef
    {
        public char Key;
        public string To;
        public string Spawn;
        public ExitStyle Style = ExitStyle.Door;
        public Func<GameCore, bool> Enabled;
        public string LockedText;
        public Speaker LockedSpeaker = Speaker.Think;
        public bool Silent;
        /// <summary>지나가기 직전에 실행 (null 반환 시 바로 이동).</summary>
        public Func<GameCore, IEnumerator> Before;

        public bool IsOpen(GameCore g, bool lucid)
        {
            if (Style == ExitStyle.Seam && !lucid) return false;
            return Enabled == null || Enabled(g);
        }
    }

    public struct Pt
    {
        public float X, Y;
        public int Dir;
        public Pt(float x, float y, int dir) { X = x; Y = y; Dir = dir; }
    }

    public sealed class RoomDef
    {
        public string Id;
        public string Name;
        public Theme Theme = Theme.Dream;
        public string[] Map;
        public readonly Dictionary<char, ExitDef> Exits = new Dictionary<char, ExitDef>();
        public readonly Dictionary<string, Pt> Spawns = new Dictionary<string, Pt>();
        public Action<Room, GameCore> Setup;
        public float Ambient = 0.15f;
        public Music Ambience = Music.AmbDream;
        public float AmbienceVol = 0.7f;
        public bool Safe;
        public bool Dream = true;
        public bool StalkerAllowed = true;

        public RoomDef Exit(char key, string to, string spawn, ExitStyle style = ExitStyle.Door)
        {
            Exits[key] = new ExitDef { Key = key, To = to, Spawn = spawn, Style = style };
            return this;
        }

        public RoomDef Lock(char key, Func<GameCore, bool> enabled, string text)
        {
            var e = Exits[key];
            e.Enabled = enabled;
            e.LockedText = text;
            return this;
        }

        /// <summary>셀 좌표로 스폰 지점 등록 (셀 가운데 아래쪽).</summary>
        public RoomDef Spawn(string name, float cx, float cy, int dir)
        {
            Spawns[name] = new Pt(cx * Room.CS + Room.CS * 0.5f, cy * Room.CS + Room.CS - 0.5f, dir);
            return this;
        }
    }

    public sealed class Light
    {
        public float X, Y, R, I;
        public float Flicker;
        public bool On = true;
        public Light(float x, float y, float r, float i) { X = x; Y = y; R = r; I = i; }
    }

    /// <summary>실행 중인 방.</summary>
    public sealed class Room
    {
        public const int CS = 4; // 셀 크기(px)
        public readonly RoomDef Def;
        public readonly int CW, CH;
        public readonly Cell[] Cells;
        public readonly char[] Keys;
        public int OX, OY;
        public readonly List<Entity> Entities = new List<Entity>();
        public readonly List<Light> Lights = new List<Light>();
        public readonly byte[] Stains;
        public int PxW { get { return CW * CS; } }
        public int PxH { get { return CH * CS; } }
        public float Ambient;
        public float Time;

        public Room(RoomDef def)
        {
            Def = def;
            CH = def.Map.Length;
            int w = 0;
            foreach (var r in def.Map) w = Math.Max(w, r.Length);
            CW = w;
            Cells = new Cell[CW * CH];
            Keys = new char[CW * CH];
            for (int y = 0; y < CH; y++)
            {
                string row = def.Map[y];
                for (int x = 0; x < CW; x++)
                {
                    char ch = x < row.Length ? row[x] : ' ';
                    Cell c;
                    switch (ch)
                    {
                        case '.': c = Cell.Floor; break;
                        case ',': c = Cell.Floor2; break;
                        case '_': c = Cell.Rug; break;
                        case '#': c = Cell.Wall; break;
                        case 'F': c = Cell.Face; break;
                        case 'W': c = Cell.Window; break;
                        case '~': c = Cell.Blood; break;
                        case 's': c = Cell.Stairs; break;
                        case 'o': c = Cell.Pit; break;
                        default:
                            if (ch >= '0' && ch <= '9') { c = Cell.Exit; Keys[y * CW + x] = ch; }
                            else c = Cell.Void;
                            break;
                    }
                    Cells[y * CW + x] = c;
                }
            }
            OX = (GameCore.WorldW - CW * CS) / 2;
            OY = (GameCore.WorldH - CH * CS) / 2;
            Stains = new byte[Math.Max(1, CW * CS * CH * CS)];
            for (int i = 0; i < Stains.Length; i++) Stains[i] = Pal.Transparent;
            Ambient = def.Ambient;
        }

        public Cell At(int cx, int cy)
        {
            if (cx < 0 || cy < 0 || cx >= CW || cy >= CH) return Cell.Void;
            return Cells[cy * CW + cx];
        }

        public char KeyAt(int cx, int cy)
        {
            if (cx < 0 || cy < 0 || cx >= CW || cy >= CH) return '\0';
            return Keys[cy * CW + cx];
        }

        public ExitDef ExitAt(int cx, int cy)
        {
            char k = KeyAt(cx, cy);
            if (k == '\0') return null;
            ExitDef e;
            return Def.Exits.TryGetValue(k, out e) ? e : null;
        }

        public bool IsSolid(int cx, int cy, GameCore g, bool lucid)
        {
            var c = At(cx, cy);
            switch (c)
            {
                case Cell.Void:
                case Cell.Wall:
                case Cell.Face:
                case Cell.Window:
                case Cell.Pit:
                    return true;
                case Cell.Exit:
                    {
                        var e = ExitAt(cx, cy);
                        if (e == null) return false;
                        return !e.IsOpen(g, lucid);
                    }
                default:
                    return false;
            }
        }

        /// <summary>몬스터용: 출구 칸은 언제나 막힘으로 취급하지 않고, 잠긴 문/벽은 막힘.</summary>
        public bool IsSolidForMonster(int cx, int cy)
        {
            var c = At(cx, cy);
            if (c == Cell.Exit) return false;
            return c == Cell.Void || c == Cell.Wall || c == Cell.Face || c == Cell.Window || c == Cell.Pit;
        }

        public bool BoxHitsCells(float x0, float y0, float x1, float y1, GameCore g, bool lucid)
        {
            int cx0 = (int)Math.Floor(x0 / CS), cy0 = (int)Math.Floor(y0 / CS);
            int cx1 = (int)Math.Floor((x1 - 0.001f) / CS), cy1 = (int)Math.Floor((y1 - 0.001f) / CS);
            for (int cy = cy0; cy <= cy1; cy++)
                for (int cx = cx0; cx <= cx1; cx++)
                    if (IsSolid(cx, cy, g, lucid)) return true;
            return false;
        }

        public bool BoxHitsCellsMonster(float x0, float y0, float x1, float y1)
        {
            int cx0 = (int)Math.Floor(x0 / CS), cy0 = (int)Math.Floor(y0 / CS);
            int cx1 = (int)Math.Floor((x1 - 0.001f) / CS), cy1 = (int)Math.Floor((y1 - 0.001f) / CS);
            for (int cy = cy0; cy <= cy1; cy++)
                for (int cx = cx0; cx <= cx1; cx++)
                    if (IsSolidForMonster(cx, cy)) return true;
            return false;
        }

        /// <summary>빛/시야 가림 (벽, 바깥).</summary>
        public bool Opaque(int cx, int cy)
        {
            var c = At(cx, cy);
            return c == Cell.Void || c == Cell.Wall || c == Cell.Face || c == Cell.Window;
        }

        public bool LineOfSight(float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0, dy = y1 - y0;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            int steps = (int)(len / 2f) + 1;
            for (int i = 1; i < steps; i++)
            {
                float t = (float)i / steps;
                int cx = (int)Math.Floor((x0 + dx * t) / CS), cy = (int)Math.Floor((y0 + dy * t) / CS);
                if (Opaque(cx, cy)) return false;
            }
            return true;
        }

        public void AddStain(int x, int y, byte c)
        {
            if (x < 0 || y < 0 || x >= PxW || y >= PxH) return;
            var cell = At(x / CS, y / CS);
            if (cell == Cell.Void || cell == Cell.Wall || cell == Cell.Pit) return;
            Stains[y * PxW + x] = c;
        }

        public T Add<T>(T e) where T : Entity
        {
            Entities.Add(e);
            return e;
        }

        public T Find<T>(string name) where T : Entity
        {
            foreach (var e in Entities)
                if (!e.Removed && e.Name == name && e is T) return (T)e;
            return null;
        }

        public Entity Find(string name)
        {
            foreach (var e in Entities)
                if (!e.Removed && e.Name == name) return e;
            return null;
        }

        public bool IsFloorish(int cx, int cy)
        {
            var c = At(cx, cy);
            return c == Cell.Floor || c == Cell.Floor2 || c == Cell.Rug || c == Cell.Blood || c == Cell.Stairs;
        }

        /// <summary>임의의 걸을 수 있는 칸 (몬스터 배치용).</summary>
        public bool RandomFloor(Rng rng, out float x, out float y)
        {
            for (int k = 0; k < 200; k++)
            {
                int cx = rng.Next(CW), cy = rng.Next(CH);
                if (IsFloorish(cx, cy) && IsFloorish(cx + 1, cy) && IsFloorish(cx, cy - 1))
                {
                    x = cx * CS + CS * 0.5f; y = cy * CS + CS - 0.5f;
                    return true;
                }
            }
            x = PxW * 0.5f; y = PxH * 0.5f;
            return false;
        }
    }
}
