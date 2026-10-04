using System;
using System.Collections.Generic;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>
    /// 모든 방의 지도. 한 칸 = 4px. 기호:
    /// ' ' 바깥  '#' 벽 윗면  'F' 벽 앞면  'W' 창문  '.' 장판  ',' 타일  '_' 깔개
    /// '~' 핏물  's' 계단  'o' 구멍  '0'~'9' 출구 (RoomDef.Exits)
    /// r_ = 현실(프롤로그), d_ = 꿈, m_ = 아침(엔딩)
    /// </summary>
    public static class Rooms
    {
        static Dictionary<string, RoomDef> defs;

        public static RoomDef Get(string id)
        {
            if (defs == null) Build();
            RoomDef d;
            if (!defs.TryGetValue(id, out d)) throw new KeyNotFoundException("room " + id);
            return d;
        }

        public static IEnumerable<string> Ids
        {
            get { if (defs == null) Build(); return defs.Keys; }
        }

        static void Add(RoomDef d)
        {
            // 줄 길이 맞추기
            int w = 0;
            foreach (var r in d.Map) w = Math.Max(w, r.Length);
            for (int i = 0; i < d.Map.Length; i++) if (d.Map[i].Length < w) d.Map[i] = d.Map[i].PadRight(w, ' ');
            defs[d.Id] = d;
        }

        // ───────── 지도 ─────────

        static readonly string[] HanaMap =
        {
            "##########################",
            "#FFFFFFFFWWWWWWFFFFFFFFFF#",
            "#FFFFFFFFWWWWWWFFFFFFFFFF#",
            "#FFFFFFFFFFFFFFFFFFFFFFFF#",
            "#........................#",
            "#........................#",
            "#........................#",
            "#........................#",
            "#........................#",
            "#.........______.........#",
            "#.........______.........#",
            "#.........______.........#",
            "#.........______.........#",
            "#........................#",
            "#........................#",
            "#........................#",
            "#........................#",
            "#........................#",
            "#........................#",
            "###########111############",
        };

        static readonly string[] HallMap =
        {
            "########################################",
            "#FFF111FFFFFFFFF222FFFFFFFFFFFFF333FFFF#",
            "#FFF111FFFFFFFFF222FFFFFFFFFFFFF333FFFF#",
            "#FFF111FFFFFFFFF222FFFFFFFFFFFFF333FFFF#",
            "#......................................#",
            "#......................................#",
            "4......................................5",
            "4......................................5",
            "4......................................5",
            "#......................................#",
            "#......................................#",
            "########################################",
        };

        static readonly string[] BathMap =
        {
            "####################",
            "#FFFFFFFFFFFFFFFFFF#",
            "#FFFFFFFFFFFFFFFFFF#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,#",
            "########111#########",
        };

        static readonly string[] MomMap =
        {
            "##############################",
            "#FFFFFFFFFFFFFFFFFFFWWWWWFFFF#",
            "#FFFFFFFFFFFFFFFFFFFWWWWWFFFF#",
            "#FFFFFFFFFFFFFFFFFFFFFFFFFFFF#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#........__________..........#",
            "#........__________..........#",
            "#........__________..........#",
            "#........__________..........#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#............................#",
            "###########111################",
        };

        static readonly string[] LivingMap =
        {
            "########################################",
            "#FFFFFFFFFFWWWWWWWWWWWWWWWWWWFFFFFFFFFF#",
            "#FFFFFFFFFFWWWWWWWWWWWWWWWWWWFFFFFFFFFF#",
            "#FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF#",
            "#......................................#",
            "#......................................#",
            "#......................................#",
            "#......................................#",
            "#......................................#",
            "#.......____________...................#",
            "#.......____________...................1",
            "#.......____________...................1",
            "#.......____________...................1",
            "#.......____________...................#",
            "#......................................#",
            "#......................................#",
            "2......................................#",
            "2......................................#",
            "2......................................#",
            "#......................................#",
            "#......................................#",
            "#...........................#,,,,,,,,,,#",
            "#...........................#,,,,,,,,,,#",
            "#...........................#,,,,,,,,,,#",
            "#...........................,,,,,,,,,,,#",
            "#...........................,,,,,,,,,,,#",
            "#...........................,,,,,,,,,,,#",
            "##############################3333######",
        };

        static readonly string[] KitchenMap =
        {
            "################################",
            "#FFFFFFFFFFFFFFFFFFFFFFFFFFFFFF#",
            "#FFFFFFFFFFFFFFFFFFFFFFFFFFFFFF#",
            "#FFFFFFFFFFFFFFFFFFFFFFFFFFFFFF#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,1",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,1",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,1",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "#,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,#",
            "################################",
        };

        static readonly string[] LoopMap =
        {
            "########################################",
            "#FFFFFFFFFFFFFFFFFFFFFFFF777FFFFFFFFFFF#",
            "#FFFFFFFFFFFFFFFFFFFFFFFF777FFFFFFFFFFF#",
            "#FFFFFFFFFFFFFFFFFFFFFFFF777FFFFFFFFFFF#",
            "#......................................#",
            "#......................................#",
            "4......................................5",
            "4......................................5",
            "4......................................5",
            "#......................................#",
            "#......................................#",
            "########################################",
        };

        static readonly string[] StairsMap =
        {
            "##############################",
            "#FFFFFFFFFFFF000FFFFFFFFFFFFF#",
            "#FFFFFFFFFFFF000FFFFFFFFFFFFF#",
            "#FFFFFFFFFFFF000FFFFFFFFFFFFF#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#............................#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "#........#ssssssssss#........#",
            "##########1111111111##########",
        };

        static readonly string[] CutMap =
        {
            "########################################",
            "#FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF#",
            "#FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF#",
            "#FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF#",
            "#......................................#",
            "#......................................#",
            "#....~~~...............................#",
            "#...~~~~~.......................~~.....#",
            "#....~~~.......................~~~~....#",
            "#...............................~~.....#",
            "#......................................#",
            "#..........oo..................oo......#",
            "#..........oo..................oo......#",
            "#......................................#",
            "#......................................#",
            "#......................................#",
            "#......................................#",
            "#......................................#",
            "#..........oo..................oo......#",
            "#..........oo..................oo......#",
            "#......................................#",
            "#.....~~...............................#",
            "#....~~~~.........................~~~..#",
            "#.....~~.........................~~~~~.#",
            "#..................................~~..#",
            "#......................................#",
            "#......................................#",
            "#......................................#",
            "#......................................#",
            "#################111####################",
        };

        static string[] Copy(string[] m) { return (string[])m.Clone(); }

        static string[] NoEast(string[] m)
        {
            var c = Copy(m);
            for (int i = 0; i < c.Length; i++)
                if (c[i].EndsWith("5")) c[i] = c[i].Substring(0, c[i].Length - 1) + "#";
            return c;
        }

        // ───────── 정의 ─────────

        static void Build()
        {
            defs = new Dictionary<string, RoomDef>();

            // ── 현실 ──
            foreach (var pre in new[] { "r", "d", "m" })
            {
                var theme = pre == "r" ? Theme.Reality : (pre == "d" ? Theme.Dream : Theme.Morning);
                bool dream = pre == "d";
                float amb = pre == "r" ? 1f : (pre == "d" ? 0.5f : 1f);
                Music music = pre == "r" ? Music.AmbReality : (pre == "d" ? Music.AmbDream : Music.Morning);

                var hana = new RoomDef { Id = pre + "_hana", Name = "하나 방", Theme = theme, Map = Copy(HanaMap), Ambient = amb, Ambience = music, Dream = dream };
                hana.Exit('1', pre + "_hall", "from_hana");
                hana.Spawn("bed", 4, 9, 0).Spawn("door", 12, 17, 1).Spawn("center", 12, 14, 0);
                if (dream) { hana.Safe = true; hana.Ambient = 0.5f; }
                Add(hana);

                var hall = new RoomDef { Id = pre + "_hall", Name = "복도", Theme = theme, Map = dream ? Copy(HallMap) : NoEast(HallMap), Ambient = amb, Ambience = music, Dream = dream };
                hall.Exit('1', pre + "_mom", "door").Exit('2', pre + "_bath", "door").Exit('3', pre + "_hana", "door");
                hall.Exit('4', pre + "_living", "from_hall", ExitStyle.Open);
                if (dream) hall.Exit('5', "d_loop", "from_west", ExitStyle.Open);
                hall.Spawn("from_mom", 5, 5, 0).Spawn("from_bath", 17, 5, 0).Spawn("from_hana", 33, 5, 0)
                    .Spawn("from_living", 2, 7, 3).Spawn("from_loop", 37, 7, 2);
                if (dream) hall.Ambient = 0.5f;
                Add(hall);

                var living = new RoomDef { Id = pre + "_living", Name = "거실", Theme = theme, Map = Copy(LivingMap), Ambient = amb, Ambience = music, Dream = dream };
                living.Exit('1', pre + "_hall", "from_living", ExitStyle.Open);
                living.Exit('2', pre + "_kitchen", "from_living", ExitStyle.Open);
                living.Exit('3', dream ? "d_stairs1" : pre + "_living", "top");
                living.Spawn("from_hall", 37, 11, 2).Spawn("from_kitchen", 2, 17, 3).Spawn("start", 25, 23, 3)
                    .Spawn("mom", 31, 25, 2).Spawn("door_in", 31, 25, 1).Spawn("center", 20, 17, 0);
                if (dream) living.Ambient = 0.5f;
                Add(living);

                var kitchen = new RoomDef { Id = pre + "_kitchen", Name = "부엌", Theme = theme, Map = Copy(KitchenMap), Ambient = amb, Ambience = music, Dream = dream };
                kitchen.Exit('1', pre + "_living", "from_kitchen", ExitStyle.Open);
                kitchen.Spawn("from_living", 29, 13, 2).Spawn("center", 16, 12, 0);
                if (dream) { kitchen.Ambient = 0.5f; kitchen.StalkerAllowed = false; }
                Add(kitchen);

                var bath = new RoomDef { Id = pre + "_bath", Name = "욕실", Theme = theme, Map = Copy(BathMap), Ambient = amb, Ambience = music, Dream = dream };
                bath.Exit('1', pre + "_hall", "from_bath");
                bath.Spawn("door", 9, 15, 1);
                if (dream) { bath.Ambient = 0.5f; bath.StalkerAllowed = false; }
                Add(bath);

                var mom = new RoomDef { Id = pre + "_mom", Name = "안방", Theme = theme, Map = Copy(MomMap), Ambient = amb, Ambience = music, Dream = dream };
                mom.Exit('1', pre + "_hall", "from_mom");
                mom.Spawn("door", 12, 19, 1).Spawn("from_seam", 3, 13, 3);
                if (dream) { mom.Ambient = 0.25f; mom.StalkerAllowed = false; }
                Add(mom);
            }

            // 꿈 전용 출구 조건
            var dh = Get("d_hana");
            dh.Lock('1', g => g.Flags.Has("first_check"), "…뭔가 이상해. 손가락을 세어 보자. (C 키)");
            var dhall = Get("d_hall");
            dhall.Lock('1', g => g.Flags.Has("piece_mom"), "안 열린다. …안에서 누가 숨 쉬는 소리가 난다.");
            var dl = Get("d_living");
            dl.Lock('3', g => g.Flags.Has("photo_done"), "열리지 않는다. 문 너머에서… 싹둑, 싹둑.");
            var rl = Get("r_living");
            rl.Lock('3', g => false, "엄마가 아무한테도 문 열어 주지 말랬어.");
            var ml = Get("m_living");
            ml.Lock('3', g => false, "현관문. …밖에 나가고 싶지 않다.");
            Get("m_hall").Lock('1', g => false, "안방 문은 닫혀 있다.");
            Get("m_hall").Lock('2', g => false, "욕실은… 지금은 들어가고 싶지 않다.");
            Get("m_mom").Lock('1', g => true, null);

            // ── 끝없는 복도 ──
            var loop = new RoomDef { Id = "d_loop", Name = "복도?", Theme = Theme.Dream, Map = Copy(LoopMap), Ambient = 0.25f, Ambience = Music.AmbNightmare, AmbienceVol = 0.5f };
            loop.Exit('4', "d_hall", "from_loop", ExitStyle.Open);
            loop.Exit('5', "d_loop", "from_west", ExitStyle.Open);
            loop.Exit('7', "d_mom", "from_seam", ExitStyle.Seam);
            loop.Exits['5'].Before = g => { g.Flags.Inc("loops"); return null; };
            loop.Exits['5'].Silent = true;
            loop.Spawn("from_west", 2, 7, 3).Spawn("from_east", 37, 7, 2);
            Add(loop);

            // ── 계단 ──
            for (int i = 1; i <= 3; i++)
            {
                var st = new RoomDef { Id = "d_stairs" + i, Name = "계단", Theme = Theme.Stairs, Map = Copy(StairsMap), Ambient = 0.5f, Ambience = Music.Chase, AmbienceVol = 0.75f, StalkerAllowed = false };
                st.Exit('0', i == 1 ? "d_living" : "d_stairs" + (i - 1), i == 1 ? "door_in" : "bottom");
                st.Exits['0'].Enabled = g => false;
                st.Exits['0'].LockedText = "뒤에서… 가위 소리가 난다. 돌아갈 수 없어.";
                st.Exit('1', i == 3 ? "d_cut" : "d_stairs" + (i + 1), "top", ExitStyle.Open);
                st.Spawn("top", 14, 6, 0).Spawn("retry", 14, 9, 0).Spawn("bottom", 14, 21, 1);
                Add(st);
            }

            // ── 가위의 방 ──
            var cut = new RoomDef { Id = "d_cut", Name = "가위의 방", Theme = Theme.CutRoom, Map = Copy(CutMap), Ambient = 0.25f, Ambience = Music.AmbNightmare, AmbienceVol = 0.8f, StalkerAllowed = false };
            cut.Exit('1', "d_stairs3", "bottom");
            cut.Exits['1'].Enabled = g => false;
            cut.Exits['1'].LockedText = "돌아갈 곳은 없다.";
            cut.Spawn("top", 18, 27, 1).Spawn("center", 20, 15, 0);
            Add(cut);

            Story.AttachSetups();
        }
    }
}
