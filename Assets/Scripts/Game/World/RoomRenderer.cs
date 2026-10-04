using System;
using Gawi.Engine;

namespace Gawi.Game
{
    public sealed class ThemeColors
    {
        public byte Top, TopEdge, Face, FaceLine, FaceDot, Base, Floor, FloorAlt, Tile, TileLine, Rug, RugAlt, Glass, GlassHi, Frame, Door, DoorDark, Stair, StairEdge;

        static readonly ThemeColors reality = new ThemeColors
        {
            Top = Pal.Black, TopEdge = Pal.DarkGray, Face = Pal.Tan, FaceLine = Pal.Bone, FaceDot = Pal.Brown, Base = Pal.DarkBrown,
            Floor = Pal.Brown, FloorAlt = Pal.Tan, Tile = Pal.LightGray, TileLine = Pal.Gray, Rug = Pal.DarkRed, RugAlt = Pal.Orange,
            Glass = Pal.DarkBlue, GlassHi = Pal.Blue, Frame = Pal.LightGray, Door = Pal.Brown, DoorDark = Pal.DarkBrown,
            Stair = Pal.Gray, StairEdge = Pal.DarkGray
        };
        static readonly ThemeColors dream = new ThemeColors
        {
            Top = Pal.Black, TopEdge = Pal.DarkBlue, Face = Pal.Blue, FaceLine = Pal.PaleBlue, FaceDot = Pal.DarkBlue, Base = Pal.Night,
            Floor = Pal.Brown, FloorAlt = Pal.Tan, Tile = Pal.Gray, TileLine = Pal.DarkGray, Rug = Pal.Red, RugAlt = Pal.DarkRed,
            Glass = Pal.Red, GlassHi = Pal.BloodBright, Frame = Pal.LightGray, Door = Pal.Brown, DoorDark = Pal.DarkBrown,
            Stair = Pal.Gray, StairEdge = Pal.DarkGray
        };
        static readonly ThemeColors morning = new ThemeColors
        {
            Top = Pal.DarkBrown, TopEdge = Pal.Brown, Face = Pal.Bone, FaceLine = Pal.White, FaceDot = Pal.PaleYellow, Base = Pal.Tan,
            Floor = Pal.Tan, FloorAlt = Pal.Yellow, Tile = Pal.White, TileLine = Pal.LightGray, Rug = Pal.Pink, RugAlt = Pal.Red,
            Glass = Pal.PaleBlue, GlassHi = Pal.White, Frame = Pal.White, Door = Pal.Tan, DoorDark = Pal.Brown,
            Stair = Pal.LightGray, StairEdge = Pal.Gray
        };
        static readonly ThemeColors stairs = new ThemeColors
        {
            Top = Pal.Black, TopEdge = Pal.DarkGray, Face = Pal.Gray, FaceLine = Pal.LightGray, FaceDot = Pal.DarkGray, Base = Pal.DarkGray,
            Floor = Pal.DarkGray, FloorAlt = Pal.Gray, Tile = Pal.Gray, TileLine = Pal.DarkGray, Rug = Pal.DarkRed, RugAlt = Pal.Red,
            Glass = Pal.Night, GlassHi = Pal.DarkBlue, Frame = Pal.Gray, Door = Pal.Gray, DoorDark = Pal.DarkGray,
            Stair = Pal.Gray, StairEdge = Pal.Ink
        };
        static readonly ThemeColors cut = new ThemeColors
        {
            Top = Pal.Black, TopEdge = Pal.Maroon, Face = Pal.Flesh, FaceLine = Pal.Pink, FaceDot = Pal.DarkRed, Base = Pal.Maroon,
            Floor = Pal.Maroon, FloorAlt = Pal.DarkRed, Tile = Pal.DarkRed, TileLine = Pal.Maroon, Rug = Pal.DarkRed, RugAlt = Pal.Red,
            Glass = Pal.Black, GlassHi = Pal.Maroon, Frame = Pal.DarkRed, Door = Pal.DarkRed, DoorDark = Pal.Maroon,
            Stair = Pal.DarkRed, StairEdge = Pal.Maroon
        };

        public static ThemeColors For(Theme t)
        {
            switch (t)
            {
                case Theme.Reality: return reality;
                case Theme.Morning: return morning;
                case Theme.Stairs: return stairs;
                case Theme.CutRoom: return cut;
                default: return dream;
            }
        }
    }

    /// <summary>방의 바닥/벽/문/창문 그리기.</summary>
    public static class RoomRenderer
    {
        const int CS = Room.CS;

        static bool IsWallish(Cell c) { return c == Cell.Wall || c == Cell.Void; }
        static bool IsFaceish(Cell c) { return c == Cell.Face || c == Cell.Window; }

        public static void DrawCells(PlayState ps, Canvas w)
        {
            var room = ps.Room;
            var th = ThemeColors.For(room.Def.Theme);
            bool lucid = ps.Lucid;
            var g = ps.G;
            int ox = room.OX, oy = room.OY;
            for (int cy = 0; cy < room.CH; cy++)
            {
                for (int cx = 0; cx < room.CW; cx++)
                {
                    var c = room.At(cx, cy);
                    int px = ox + cx * CS, py = oy + cy * CS;
                    switch (c)
                    {
                        case Cell.Void:
                            break;
                        case Cell.Floor:
                            DrawFloor(w, px, py, th, lucid, cx, cy);
                            break;
                        case Cell.Floor2:
                            w.FillRect(px, py, CS, CS, th.Tile);
                            if (cx % 2 == 0) w.VLine(px, py, py + CS - 1, th.TileLine);
                            if (cy % 2 == 0) w.HLine(px, px + CS - 1, py, th.TileLine);
                            break;
                        case Cell.Rug:
                            w.FillRect(px, py, CS, CS, th.Rug);
                            if (!room.IsFloorish(cx, cy - 1) || room.At(cx, cy - 1) != Cell.Rug) w.HLine(px, px + CS - 1, py, th.RugAlt);
                            if (room.At(cx, cy + 1) != Cell.Rug) w.HLine(px, px + CS - 1, py + CS - 1, th.RugAlt);
                            if (room.At(cx - 1, cy) != Cell.Rug) w.VLine(px, py, py + CS - 1, th.RugAlt);
                            if (room.At(cx + 1, cy) != Cell.Rug) w.VLine(px + CS - 1, py, py + CS - 1, th.RugAlt);
                            if ((cx + cy) % 3 == 0) w.Set(px + 2, py + 2, th.RugAlt);
                            break;
                        case Cell.Blood:
                            w.FillRect(px, py, CS, CS, Pal.DarkRed);
                            for (int k = 0; k < 3; k++)
                            {
                                float h = M.Hash(cx * 4 + k, cy, 77);
                                if (h < 0.4f) w.Set(px + (int)(h * 10) % CS, py + k, Pal.Red);
                            }
                            break;
                        case Cell.Stairs:
                            w.FillRect(px, py, CS, CS, th.Stair);
                            w.HLine(px, px + CS - 1, py + CS - 1, th.StairEdge);
                            if (cy % 2 == 0) w.HLine(px, px + CS - 1, py + 1, th.FaceLine);
                            break;
                        case Cell.Pit:
                            w.FillRect(px, py, CS, CS, Pal.Black);
                            if (room.IsFloorish(cx, cy - 1)) w.HLine(px, px + CS - 1, py, th.TopEdge);
                            break;
                        case Cell.Wall:
                            DrawWallTop(w, room, cx, cy, px, py, th);
                            break;
                        case Cell.Face:
                            DrawFace(w, room, cx, cy, px, py, th, lucid, ps);
                            break;
                        case Cell.Window:
                            DrawWindow(w, room, cx, cy, px, py, th);
                            break;
                        case Cell.Exit:
                            DrawExit(w, room, cx, cy, px, py, th, g, lucid, ps);
                            break;
                    }
                }
            }
        }

        static void DrawFloor(Canvas w, int px, int py, ThemeColors th, bool lucid, int cx, int cy)
        {
            w.FillRect(px, py, CS, CS, th.Floor);
            // 장판 결: 드문드문한 점과 이음매
            byte alt = lucid && th.Floor == Pal.Brown && th.Face == Pal.Blue ? Pal.Red : th.FloorAlt;
            float h = M.Hash(cx, cy, 13);
            if (h < 0.09f) w.Set(px + (int)(h * 40) % CS, py + (int)(h * 97) % CS, alt);
            if (cy % 6 == 0 && M.Hash(cx >> 2, cy, 5) < 0.5f) w.Set(px + 1, py, alt);
        }

        static void DrawWallTop(Canvas w, Room room, int cx, int cy, int px, int py, ThemeColors th)
        {
            w.FillRect(px, py, CS, CS, th.Top);
            // 바닥/벽면과 맞닿은 쪽에 가장자리 선
            if (!IsWallish(room.At(cx, cy + 1))) w.HLine(px, px + CS - 1, py + CS - 1, th.TopEdge);
            if (!IsWallish(room.At(cx, cy - 1))) w.HLine(px, px + CS - 1, py, th.TopEdge);
            if (!IsWallish(room.At(cx - 1, cy))) w.VLine(px, py, py + CS - 1, th.TopEdge);
            if (!IsWallish(room.At(cx + 1, cy))) w.VLine(px + CS - 1, py, py + CS - 1, th.TopEdge);
        }

        static void DrawFace(Canvas w, Room room, int cx, int cy, int px, int py, ThemeColors th, bool lucid, PlayState ps)
        {
            w.FillRect(px, py, CS, CS, th.Face);
            var above = room.At(cx, cy - 1);
            var below = room.At(cx, cy + 1);
            bool top = !IsFaceish(above) && above != Cell.Exit;
            bool bottom = !IsFaceish(below) && below != Cell.Exit;
            if (top) w.HLine(px, px + CS - 1, py, th.FaceLine);
            if (bottom)
            {
                w.HLine(px, px + CS - 1, py + CS - 1, th.Base);
                w.HLine(px, px + CS - 1, py + CS - 2, th.Base);
            }
            // 벽지 무늬
            if ((cx + cy * 2) % 3 == 0) w.Set(px + 2, py + 2, th.FaceDot);
            if (lucid && (room.Def.Theme == Theme.Dream || room.Def.Theme == Theme.Stairs))
            {
                // 자각 상태: 벽에서 흘러내리는 피
                float h = M.Hash(cx, 0, 99);
                if (h < 0.4f)
                {
                    int col = px + (int)(h * 10) % CS;
                    int ty = cy;
                    while (IsFaceish(room.At(cx, ty - 1))) ty--;
                    int runTop = py - (cy - ty) * CS;
                    int len = 2 + (int)(M.Hash(cx, 1, 99) * 9) + (int)(ps.Room.Time * 0.7f + h * 5) % 3;
                    for (int yy = 0; yy < CS; yy++)
                    {
                        int depth = py + yy - runTop;
                        if (depth >= 1 && depth < len) w.Set(col, py + yy, Pal.Maroon);
                    }
                }
            }
            if (room.Def.Theme == Theme.CutRoom && (cx % 3 == 1))
            {
                // 꿰맨 자국
                w.Set(px + 1, py + 1, Pal.Maroon);
                w.Set(px + 2, py + 2, Pal.Maroon);
                w.Set(px + 1, py + 3, Pal.Maroon);
            }
        }

        static void DrawWindow(Canvas w, Room room, int cx, int cy, int px, int py, ThemeColors th)
        {
            w.FillRect(px, py, CS, CS, th.Glass);
            bool l = room.At(cx - 1, cy) != Cell.Window, r = room.At(cx + 1, cy) != Cell.Window;
            bool t = room.At(cx, cy - 1) != Cell.Window, b = room.At(cx, cy + 1) != Cell.Window;
            if (l) w.VLine(px, py, py + CS - 1, th.Frame);
            if (r) w.VLine(px + CS - 1, py, py + CS - 1, th.Frame);
            if (t) w.HLine(px, px + CS - 1, py, th.Frame);
            if (b) { w.HLine(px, px + CS - 1, py + CS - 1, th.Frame); w.HLine(px, px + CS - 1, py + CS - 2, th.Base); }
            if (cx % 3 == 0 && !l) w.VLine(px, py, py + CS - 1, th.Frame);
            if (M.Hash(cx, cy, 5) < 0.3f) w.Set(px + 1, py + 1, th.GlassHi);
        }

        static void DrawExit(Canvas w, Room room, int cx, int cy, int px, int py, ThemeColors th, GameCore g, bool lucid, PlayState ps)
        {
            var e = room.ExitAt(cx, cy);
            var style = e != null ? e.Style : ExitStyle.Open;
            char key = room.KeyAt(cx, cy);
            if (style == ExitStyle.Seam)
            {
                // 평소엔 벽면, 자각하면 갈라진 틈 (테두리는 DrawPostLight에서)
                if (lucid) w.FillRect(px, py, CS, CS, Pal.Black);
                else DrawFace(w, room, cx, cy, px, py, th, false, ps);
                return;
            }
            if (style != ExitStyle.Door)
            {
                var below = room.At(cx, cy + 1);
                var above = room.At(cx, cy - 1);
                if (below == Cell.Floor2 || above == Cell.Floor2) w.FillRect(px, py, CS, CS, th.Tile);
                else DrawFloor(w, px, py, th, lucid, cx, cy);
                return;
            }
            bool open = e == null || e.IsOpen(g, lucid);
            bool sameL = room.KeyAt(cx - 1, cy) == key, sameR = room.KeyAt(cx + 1, cy) == key;
            bool sameU = room.KeyAt(cx, cy - 1) == key, sameD = room.KeyAt(cx, cy + 1) == key;
            if (open)
            {
                w.FillRect(px, py, CS, CS, Pal.Black);
                if (!sameL) w.VLine(px, py, py + CS - 1, th.Door);
                if (!sameR) w.VLine(px + CS - 1, py, py + CS - 1, th.Door);
                if (!sameU) w.HLine(px, px + CS - 1, py, th.Door);
                if (!sameD) w.HLine(px, px + CS - 1, py + CS - 1, th.DoorDark);
            }
            else
            {
                w.FillRect(px, py, CS, CS, th.Door);
                if (!sameL) w.VLine(px, py, py + CS - 1, th.DoorDark);
                if (!sameR) w.VLine(px + CS - 1, py, py + CS - 1, th.DoorDark);
                if (!sameU) w.HLine(px, px + CS - 1, py, th.DoorDark);
                if (!sameD) w.HLine(px, px + CS - 1, py + CS - 1, th.DoorDark);
                // 손잡이
                if (!sameR && sameU && !sameD) w.Set(px + CS - 2, py, Pal.Yellow);
                if (!sameR && !sameU && sameD) w.Set(px + CS - 2, py + CS - 1, Pal.Yellow);
            }
        }

        /// <summary>조명 뒤에 그리는 것: 절취선, 조사 표시.</summary>
        public static void DrawPostLight(PlayState ps, Canvas w)
        {
            var room = ps.Room;
            int ox = room.OX, oy = room.OY;
            if (ps.Lucid)
            {
                int phase = (int)(room.Time * 6);
                for (int cy = 0; cy < room.CH; cy++)
                    for (int cx = 0; cx < room.CW; cx++)
                    {
                        if (room.At(cx, cy) != Cell.Exit) continue;
                        var e = room.ExitAt(cx, cy);
                        if (e == null || e.Style != ExitStyle.Seam) continue;
                        int px = ox + cx * CS, py = oy + cy * CS;
                        byte col = (phase % 4 < 2) ? Pal.BloodBright : Pal.White;
                        if (room.At(cx - 1, cy) != Cell.Exit) w.DashedLine(px, py, px, py + CS - 1, col, 2, 1, phase);
                        if (room.At(cx + 1, cy) != Cell.Exit) w.DashedLine(px + CS - 1, py, px + CS - 1, py + CS - 1, col, 2, 1, phase);
                        if (room.At(cx, cy - 1) != Cell.Exit) w.DashedLine(px, py, px + CS - 1, py, col, 2, 1, phase);
                        if (room.At(cx, cy + 1) != Cell.Exit) w.DashedLine(px, py + CS - 1, px + CS - 1, py + CS - 1, col, 2, 1, phase);
                    }
                // 가위 표시 (절취선 옆)
                foreach (var kv in room.Def.Exits)
                {
                    if (kv.Value.Style != ExitStyle.Seam) continue;
                    for (int cy = 0; cy < room.CH; cy++)
                        for (int cx = 0; cx < room.CW; cx++)
                            if (room.KeyAt(cx, cy) == kv.Key && room.KeyAt(cx - 1, cy) != kv.Key && room.KeyAt(cx, cy - 1) != kv.Key)
                                DrawTinyScissors(w, ox + cx * CS - 4, oy + cy * CS - 1);
                }
            }
            var t = ps.InteractTarget;
            if (t != null && t.ShowMarker && !ps.G.Dialogue.Active)
            {
                int mx = ox + (int)((t.L + t.R) * 0.5f);
                int my = oy + (int)t.T - 4 - (int)(Math.Abs(M.Sin(room.Time * 4f)) * 2);
                if (t is Prop && ((Prop)t).Spr != null)
                {
                    var s = ((Prop)t).Spr;
                    my = Math.Min(my, oy + (int)t.Y - s.OriginY - 3 - (int)(Math.Abs(M.Sin(room.Time * 4f)) * 2));
                }
                w.Set(mx - 1, my, Pal.White);
                w.Set(mx, my, Pal.White);
                w.Set(mx + 1, my, Pal.White);
                w.Set(mx, my + 1, Pal.White);
            }
        }

        static void DrawTinyScissors(Canvas w, int x, int y)
        {
            w.Set(x, y, Pal.White); w.Set(x + 2, y, Pal.White);
            w.Set(x + 1, y + 1, Pal.LightGray);
            w.Set(x, y + 2, Pal.BloodBright); w.Set(x + 2, y + 2, Pal.BloodBright);
        }
    }
}
