using Gawi.Engine;

namespace Gawi.Game
{
    /// <summary>
    /// 모든 스프라이트 (아스키 아트). 문자 → 색:
    /// . 투명  0 검정 1 먹색 2 밤색 3 진회색 4 회색 5 밝은회색 6 흰색
    /// c 연파랑 b 파랑 B 남색 e 청록 G 진초록 g 초록 o 올리브
    /// N 진갈색 n 갈색 T 황갈색 y 노랑 Y 연노랑 a 주황
    /// t 밝은살 s 살 S 살그늘 k 분홍 p 보라 P 진보라
    /// x 선홍(피) r 빨강 R 검붉음 m 적갈 f 살점 w 뼈/종이
    /// </summary>
    public static class Sprites
    {
        static Sprite S(string block) { return Sprite.Parse(block); }
        static Sprite S(string block, int ox, int oy) { return Sprite.Parse(block, ox, oy); }

        // ───────────────── 하나 ─────────────────

        static readonly Sprite[] hanaDown =
        {
            S(@"
            ..111..
            .11111.
            1111111
            11sss11
            1s0s0s1
            .1sss1.
            ..ccc..
            .ccccc.
            scccccs
            .cbcbc.
            ..c.c..
            ..s.s.."),
            S(@"
            ..111..
            .11111.
            1111111
            11sss11
            1s0s0s1
            .1sss1.
            ..ccc..
            .ccccc.
            scccccs
            .cbcbc.
            ..c.c..
            ..s...."),
            S(@"
            ..111..
            .11111.
            1111111
            11sss11
            1s0s0s1
            .1sss1.
            ..ccc..
            .ccccc.
            scccccs
            .cbcbc.
            ..c.c..
            ....s.."),
        };

        static readonly Sprite[] hanaUp =
        {
            S(@"
            ..111..
            .11111.
            1111111
            1111111
            1111111
            .11111.
            ..ccc..
            .ccccc.
            scccccs
            .ccccc.
            ..c.c..
            ..s.s.."),
            S(@"
            ..111..
            .11111.
            1111111
            1111111
            1111111
            .11111.
            ..ccc..
            .ccccc.
            scccccs
            .ccccc.
            ..c.c..
            ..s...."),
            S(@"
            ..111..
            .11111.
            1111111
            1111111
            1111111
            .11111.
            ..ccc..
            .ccccc.
            scccccs
            .ccccc.
            ..c.c..
            ....s.."),
        };

        static readonly Sprite[] hanaSide =
        {
            S(@"
            ..111..
            .11111.
            .111111
            .111ss.
            .11s0s.
            ..1sss.
            ..ccc..
            ..cccc.
            ..ccsc.
            ..ccc..
            ..c.c..
            ..s.s.."),
            S(@"
            ..111..
            .11111.
            .111111
            .111ss.
            .11s0s.
            ..1sss.
            ..ccc..
            ..cccc.
            ..cscc.
            ..ccc..
            .c...c.
            .s...s."),
            S(@"
            ..111..
            .11111.
            .111111
            .111ss.
            .11s0s.
            ..1sss.
            ..ccc..
            ..cccc.
            ..ccsc.
            ..ccc..
            ...c...
            ...s..."),
        };

        static readonly Sprite hanaCount = S(@"
            ..111..
            .11111.
            1111111
            11sss11
            1s0s0s1
            .1sss1.
            ..ccc..
            .ssccc.
            .ccccc.
            .cbcbc.
            ..c.c..
            ..s.s..");

        public static Sprite Hana(int dir, int frame, bool counting)
        {
            if (counting) return hanaCount;
            switch (dir)
            {
                case 1: return hanaUp[frame];
                case 2:
                case 3: return hanaSide[frame];
                default: return hanaDown[frame];
            }
        }

        /// <summary>침대에 누운 하나 (머리만 보임).</summary>
        public static readonly Sprite HanaInBed = S(@"
            .111.
            11111
            1sss1
            s0s0s
            .sss.");

        // ───────────────── 엄마 ─────────────────

        public static readonly Sprite[] MomDown =
        {
            S(@"
            ..NNN..
            .NNNNN.
            NNNNNNN
            NNsssNN
            Ns0s0sN
            NNsssNN
            N.sss.N
            .TTTTT.
            TTTTTTT
            sTTTTTs
            .TTTTT.
            .TTnTT.
            .TTTTT.
            ..3.3..
            ..3.3..
            ..0.0.."),
            S(@"
            ..NNN..
            .NNNNN.
            NNNNNNN
            NNsssNN
            Ns0s0sN
            NNsssNN
            N.sss.N
            .TTTTT.
            TTTTTTT
            sTTTTTs
            .TTTTT.
            .TTnTT.
            .TTTTT.
            ..3.3..
            ..3....
            ..0...."),
        };

        public static readonly Sprite[] MomUp =
        {
            S(@"
            ..NNN..
            .NNNNN.
            NNNNNNN
            NNNNNNN
            NNNNNNN
            NNNNNNN
            NNNNNNN
            .TNNNT.
            TTTTTTT
            sTTTTTs
            .TTTTT.
            .TTTTT.
            .TTTTT.
            ..3.3..
            ..3.3..
            ..0.0.."),
            S(@"
            ..NNN..
            .NNNNN.
            NNNNNNN
            NNNNNNN
            NNNNNNN
            NNNNNNN
            NNNNNNN
            .TNNNT.
            TTTTTTT
            sTTTTTs
            .TTTTT.
            .TTTTT.
            .TTTTT.
            ..3.3..
            ....3..
            ....0.."),
        };

        public static readonly Sprite[] MomSide =
        {
            S(@"
            ..NNN..
            .NNNNN.
            .NNNNNN
            .NNNss.
            .NNs0s.
            .NNsss.
            .NN.s..
            ..TTTT.
            ..TTTT.
            ..TTsT.
            ..TTTT.
            ..TTTT.
            ..TTTT.
            ...3.3.
            ...3.3.
            ...0.0."),
            S(@"
            ..NNN..
            .NNNNN.
            .NNNNNN
            .NNNss.
            .NNs0s.
            .NNsss.
            .NN.s..
            ..TTTT.
            ..TTTT.
            ..TsTT.
            ..TTTT.
            ..TTTT.
            ..TTTT.
            ..3...3
            ..3...3
            ..0...0"),
        };

        // 아침의 엄마 (앞치마)
        public static readonly Sprite MomApronBack = S(@"
            ..NNN..
            .NNNNN.
            NNNNNNN
            NNNNNNN
            NNNNNNN
            .NNNNN.
            ..kkk..
            .k6k6k.
            sk666ks
            .k666k.
            .k666k.
            .kkkkk.
            .kkkkk.
            ..s.s..
            ..s.s..
            ..0.0..");

        public static readonly Sprite MomApronFront = S(@"
            ..NNN..
            .NNNNN.
            NNNNNNN
            NNsssNN
            Ns0s0sN
            NNsssNN
            N.sss.N
            .k666k.
            k66666k
            s66666s
            .k666k.
            .k666k.
            .kkkkk.
            ..s.s..
            ..s.s..
            ..0.0..");

        // ───────────────── 가위 ─────────────────
        // 몸만 (가위는 따로 그린다). 기준점: 발 가운데.

        public static readonly Sprite[] GawiFront =
        {
            S(@"
            ...www...
            ..ww4ww..
            .wwwwwww.
            .w4www4w.
            .wwwwwww.
            .ww000ww.
            .w0rrr0w.
            ..w000w..
            ...www...
            ....3....
            ..33333..
            .3.333.3.
            .3.333.3.
            .3.313.3.
            3..333..3
            3..333..3
            3..313..3
            3..333..3
            3..333..3
            w..333..w
            w..3.3..w
            w..3.3...
            ...3.3...
            ...3.3...
            ...3.3...
            ...3.3...
            ..33.33.."),
            S(@"
            ...www...
            ..ww4ww..
            .wwwwwww.
            .w4www4w.
            .wwwwwww.
            .ww000ww.
            .w0rrr0w.
            ..w000w..
            ...www...
            ....3....
            ..33333..
            .3.333.3.
            .3.333.3.
            .3.313.3.
            3..333..3
            3..333..3
            3..313..3
            3..333..3
            3..333..3
            w..333..w
            w..3.3..w
            w..3.3...
            ...3..3..
            ...3..3..
            ..3...3..
            ..3...3..
            .33...33."),
        };

        public static readonly Sprite[] GawiSide =
        {
            S(@"
            ....www..
            ...wwwww.
            ...w4wwww
            ...wwww0w
            ...www0rw
            ....ww00.
            ....3ww..
            ...33....
            ..3333...
            ..33333..
            ..333.3..
            ..333.3..
            ..313..3.
            ..333..3.
            ..333..3.
            ..313..3.
            ..333..3.
            ..333..w.
            ..3.3..w.
            ..3.3....
            ..3.3....
            ..3.3....
            ..3.3....
            ..3..3...
            ..3..3...
            .33..33..
            .........", 4, 25),
            S(@"
            ....www..
            ...wwwww.
            ...w4wwww
            ...wwww0w
            ...www0rw
            ....ww00.
            ....3ww..
            ...33....
            ..3333...
            ..33333..
            ..333.3..
            ..333.3..
            ..313.3..
            ..333..3.
            ..333..3.
            ..313..3.
            ..333..3.
            ..333..w.
            ..3.3..w.
            ..3.3....
            .3...3...
            .3...3...
            .3....3..
            3.....3..
            3.....3..
            33....33.
            .........", 4, 25),
        };

        public static readonly Sprite GawiBack = S(@"
            ...www...
            ..wwwww..
            .wwwwwww.
            .wwwwwww.
            .wwwwwww.
            .wwwwwww.
            .wwwwwww.
            ..wwwww..
            ...www...
            ....3....
            ..33333..
            .3.333.3.
            .3.333.3.
            .3.333.3.
            3..333..3
            3..333..3
            3..333..3
            3..333..3
            3..333..3
            w..333..w
            w..3.3..w
            w..3.3...
            ...3.3...
            ...3.3...
            ...3.3...
            ...3.3...
            ..33.33..");

        // ───────────────── 얼굴 없는 아이들 ─────────────────

        public static readonly Sprite[] KidGirl =
        {
            S(@"
            ..111..
            .11111.
            1111111
            1R000R1
            1000001
            1R000R1
            1.R0R.1
            ..666..
            .66666.
            s66666s
            .BBBBB.
            BBBBBBB
            ..s.s..
            ..0.0.."),
            S(@"
            ..111..
            .11111.
            1111111
            1R000R1
            1000001
            1R000R1
            1.RxR.1
            ..666..
            .66666.
            s66666s
            .BBBBB.
            BBBBBBB
            ..s.s..
            ..0.0.."),
        };

        public static readonly Sprite[] KidBoy =
        {
            S(@"
            .11111.
            1111111
            1R000R1
            1000001
            .R000R.
            ..R0R..
            ..444..
            .44444.
            s44444s
            .44444.
            .BB.BB.
            .ss.ss.
            .00.00."),
            S(@"
            .11111.
            1111111
            1R000R1
            1000001
            .R000R.
            ..RxR..
            ..444..
            .44444.
            s44444s
            .44444.
            .BB.BB.
            .ss.ss.
            .00.00."),
        };

        // ───────────────── 엄마인 척하는 것 ─────────────────

        public static readonly Sprite[] MotherBack =
        {
            S(@"
            ...NN...
            ..NNNN..
            ..NNNN..
            .NNNNNN.
            .NNNNNN.
            .NNNNNN.
            ..NNNN..
            ..pppp..
            .pppppp.
            spppppps
            sppppppR
            .pppppp.
            .pppppp.
            .pppppp.
            .pppppp.
            ..s..s..
            ..s..s..
            ..0..0.."),
            S(@"
            ...NN...
            ..NNNN..
            ..NNNN..
            .NNNNNN.
            .NNNNNNs
            .NNNNNNs
            ..NNNN.s
            ..pppppp
            .pppppp.
            sppppp..
            sppppp..
            .pppppp.
            .pppppp.
            .pppppp.
            .pppppp.
            ..s..s..
            ..s..s..
            ..0..0.."),
        };

        /// <summary>돌아본 얼굴: 테이프로 붙인 엄마 사진 + 그 밑의 살.</summary>
        public static readonly Sprite MotherFront = S(@"
            ...NN...
            ..NNNN..
            .N5ww5N.
            .NwttwN.
            .Nt0t0N.
            .NwttwN.
            .Nfwwf..
            .ffRRfp.
            .pfRRpp.
            Rpppppps
            RppppppR
            .pRpppp.
            .pppRpp.
            .pppppp.
            .pppppp.
            ..s..s..
            ..s..s..
            ..0..0..");

        public static readonly Sprite[] MotherCrawl =
        {
            S(@"
            ....NNNN....
            ..sNNNNNNs..
            .s.5wwww5.s.
            s..wt0t0w..s
            s..fRRRRf..s
            .s.pppppp.s.
            ...pppppp...
            ..s.pppp.s..
            .s........s."),
            S(@"
            ....NNNN....
            ...NNNNNN...
            .ss5wwww5ss.
            s..wt0t0w..s
            ...fRRRRf...
            ..spppppps..
            .s.pppppp.s.
            s...pppp...s
            ............"),
        };

        // ───────────────── 소품 ─────────────────

        public static readonly Sprite BedHana = S(@"
            nnnnnnnnnnnn
            n6666666666n
            n6555555556n
            n6666666666n
            nkkkkkkkkkkn
            nkkkkkkkkkkn
            nkk6kkkk6kkn
            nkkkkkkkkkkn
            nkkkkkkkkkkn
            nkk6kkkk6kkn
            nkkkkkkkkkkn
            nkkkkkkkkkkn
            nkk6kkkk6kkn
            nkkkkkkkkkkn
            NNNNNNNNNNNN
            NnnnnnnnnnnN
            N..........N", 6, 16);

        public static readonly Sprite BedMom = S(@"
            nnnnnnnnnnnnnnnnnn
            n6666666666666666n
            n6555556n6555556nn
            n6666666n6666666nn
            nccccccccccccccccn
            ncbccbccbccbccbccn
            nccccccccccccccccn
            ncbccbccbccbccbccn
            nccccccccccccccccn
            ncbccbccbccbccbccn
            nccccccccccccccccn
            ncbccbccbccbccbccn
            nccccccccccccccccn
            ncbccbccbccbccbccn
            nccccccccccccccccn
            NNNNNNNNNNNNNNNNNN
            NnnnnnnnnnnnnnnnnN
            N................N", 9, 17);

        public static readonly Sprite Desk = S(@"
            nnnnnnnnnnnnnn
            nTTTTTTTTTTTTn
            nT66T....yyTTn
            nT66T....yYTTn
            nTTTTTTTTTTTTn
            NNNNNNNNNNNNNN
            NnnnnnNnnnnnnN
            Nnn4nnNnnn4nnN
            NnnnnnNnnnnnnN
            N............N", 7, 9);

        public static readonly Sprite Chair = S(@"
            .nnnn.
            nTTTTn
            nTTTTn
            NNNNNN
            N....N", 3, 4);

        public static readonly Sprite Wardrobe = S(@"
            NNNNNNNNNNNNNNNNNNNN
            NnnnnnnnnnnnnnnnnnnN
            NNNNNNNNNNNNNNNNNNNN
            N1111111111111111111
            N1n1nnnnnnn1nnnnnnn1
            N1n1nkn6nnn1nnn6nkn1
            N1n1nnnnnnn1nnnnnnn1
            N1n1n6nnncn1ncnnn6n1
            N1n1nnnnnnn1nnnnnnn1
            N1n1nnnnnnn1nnnnnnn1
            N1n1nkn6nny1ynn6nkn1
            N1n1nnnnnnn1nnnnnnn1
            N1n1nncnnnn1nnnncnn1
            N1n1nnnnnnn1nnnnnnn1
            N1n1nnn6nnn1nn6nnnn1
            N1n1nnnnnnn1nnnnnnn1
            N1111111111111111111
            N1.................1", 10, 17);

        public static readonly Sprite Tv = S(@"
            ....4....4..
            .....4..4...
            ......44....
            333333333333
            344444444443
            342222222243
            342222222243
            342222222243
            342222222243
            344444444443
            3444444y4a43
            333333333333
            NNNNNNNNNNNN
            NnnnnnnnnnnN
            NnnnnnnnnnnN
            N..........N", 6, 15);

        public static readonly Sprite Sofa = S(@"
            .gggggggggggggggggggggg.
            gGGGGGGGGGGGGGGGGGGGGGGg
            gGgggggggggggggggggggGGg
            gGgggggggGgggggggggggGGg
            gGgggggggGgggggggggggGGg
            gGGGGGGGGGGGGGGGGGGGGGGg
            gggggggggggggggggggggggg
            GGGGGGGGGGGGGGGGGGGGGGGG
            G.G..................G.G", 12, 8);

        public static readonly Sprite LowTable = S(@"
            nnnnnnnnnnnnnn
            nTTTTTTTTTTTTn
            nTTTTTTTTTTTTn
            nTTTTTTTTTTTTn
            NNNNNNNNNNNNNN
            N.N........N.N", 7, 5);

        public static readonly Sprite Fridge = S(@"
            5555555555
            5666666665
            5666666665
            5666666665
            5666666465
            5666666465
            5555555555
            5666666665
            5666666665
            5666666665
            5666666465
            5666666465
            5666666465
            5666666665
            5666666665
            5555555555
            4444444444", 5, 16);

        public static readonly Sprite Stove = S(@"
            5555555555
            5333553335
            5313553135
            5333553335
            5555555555
            4444444444
            4a4a4a4a44
            4444444444
            4444444444
            4........4", 5, 9);

        public static readonly Sprite Sink = S(@"
            5555555555
            5444444445
            5433333345
            5433333345
            5444434445
            5555555555
            4444444444
            4444444444
            4........4", 5, 8);

        public static readonly Sprite CounterBlock = S(@"
            5555555555
            5555555555
            5555555555
            5555555555
            5555555555
            5555555555
            4444444444
            4444n44444
            4444444444
            4........4", 5, 9);

        public static readonly Sprite DiningTable = S(@"
            nnnnnnnnnnnnnnnn
            nTTTTTTTTTTTTTTn
            nTTT66TTTTTTTTTn
            nTTT66TTTTT555Tn
            nTTTTTTTTTT555Tn
            nTTTTTTTTTTTTTTn
            NNNNNNNNNNNNNNNN
            N.N..........N.N
            N.N..........N.N", 8, 8);

        public static readonly Sprite Bathtub = S(@"
            66666666666666
            65555555555556
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbbbbb56
            65bbbbbbb3bb56
            65555555555556
            66666666666666
            55555555555555
            5............5", 7, 19);

        public static readonly Sprite Toilet = S(@"
            .666666.
            65555556
            65555556
            .666666.
            66555566
            65533556
            65533556
            66555566
            .666666.
            ..5555..", 4, 9);

        public static readonly Sprite BathSink = S(@"
            .66666666.
            6655555566
            6553333556
            6553333556
            6655535566
            .66666666.
            ...5555...
            ...5..5...", 5, 7);

        public static readonly Sprite ShoeRack = S(@"
            nnnnnnnnnnnn
            nTTTTTTTTTTn
            NNNNNNNNNNNN
            N0N1NN0N3NNN
            NNNNNNNNNNNN
            N..........N", 6, 5);

        public static readonly Sprite Plant = S(@"
            ..G.g..
            .GgGgG.
            GgG.GgG
            .GGgGG.
            ..GgG..
            ...N...
            .nnnnn.
            .NnnnN.
            ..NNN..", 3, 8);

        public static readonly Sprite DeadPlant = S(@"
            ..N.N..
            .N.N.N.
            N..N..N
            .N.N.N.
            ...N...
            ...N...
            .nnnnn.
            .NnnnN.
            ..NNN..", 3, 8);

        public static readonly Sprite NightLight = S(@"
            .yYy.
            yYYYy
            yYYYy
            .yyy.
            ..5..
            .555.", 2, 5);

        public static readonly Sprite NightLightOff = S(@"
            .454.
            45554
            45554
            .444.
            ..4..
            .444.", 2, 5);

        public static readonly Sprite Note = S(@"
            6665
            6446
            6646
            5666", 2, 3);

        public static readonly Sprite NoteBloody = S(@"
            666r
            64rr
            6646
            r666", 2, 3);

        public static readonly Sprite PhotoPiece = S(@"
            .Y.
            YwY
            .Y.", 1, 2);

        public static readonly Sprite Flashlight = S(@"
            .y.
            yYy
            .3.
            .3.", 1, 3);

        public static readonly Sprite Rabbit = S(@"
            .6.6.
            .6.6.
            66666
            60606
            66k66
            .666.", 2, 5);

        public static readonly Sprite RabbitGutted = S(@"
            .6...
            .6.6.
            66666
            60606
            6xRx6
            fRxRf
            .xfx.", 2, 6);

        public static readonly Sprite ToyBox = S(@"
            nnnnnnnn
            nkyyckbn
            NNNNNNNN
            NnnnnnnN
            NnnnnnnN
            N......N", 4, 5);

        public static readonly Sprite Vanity = S(@"
            .555555.
            54cccc45
            54cccc45
            54cccc45
            .555555.
            nnnnnnnn
            nTkTTyTn
            NNNNNNNN
            NnnNNnnN
            N......N", 4, 9);

        public static readonly Sprite JewelBox = S(@"
            1111
            1k61
            1111", 2, 2);

        public static readonly Sprite CuttingBoard = S(@"
            TTTTTTTT
            TsSxsSTT
            TxsRsxsT
            TTTTRTTT", 4, 3);

        public static readonly Sprite Pot = S(@"
            .444444.
            44555544
            4RxRRxR4
            4RRsRRR4
            .444444.", 4, 4);

        public static readonly Sprite Calendar = S(@"
            .r55r.
            666666
            6R0606
            606066
            660606
            666666", 3, 5);

        public static readonly Sprite PhotoFrame = S(@"
            NNNNNNNNNN
            NTTTTTTTTN
            NT5s55s5TN
            NTNcNNcNTN
            NTccc6ccTN
            NTTTTTTTTN
            NNNNNNNNNN", 5, 6);

        public static readonly Sprite PhotoFrameEmpty = S(@"
            NNNNNNNNNN
            NTTTTTTTTN
            NT000000TN
            NT000000TN
            NT000000TN
            NTTTTTTTTN
            NNNNNNNNNN", 5, 6);

        public static readonly Sprite Clock = S(@"
            .555.
            56665
            56066
            56605
            .555.", 2, 4);

        public static readonly Sprite DoorClosed = S(@"
            NNNNNNNNNN
            NnnnnnnnnN
            NnNNNNNNnN
            NnNnnnnNnN
            NnNnnnnNnN
            NnNNNNNNnN
            NnnnnnnyyN
            NnnnnnnnnN", 5, 7);

        public static readonly Sprite FrontDoor = S(@"
            4444444444444444
            4333333333333334
            4344444444444434
            4343333333333434
            4343333333333434
            4343333333333434
            4343333333336634
            4343333333336634
            4343333333333434
            4344444444444434
            4333333333333334", 8, 10);

        public static readonly Sprite Elevator = S(@"
            44444444444444
            45555555555554
            45444444444454
            45434444443454
            45434444443454
            45434444443454
            45434444443454
            45434444443454
            45434444443454
            45434444443454
            45434444443454
            45444444444454
            45555555555554", 7, 12);

        public static readonly Sprite PaperDoll = S(@"
            ..666..
            .66666.
            .66666.
            ..666..
            6666666
            6.666.6
            6.666.6
            ..666..
            ..6.6..
            ..6.6..
            .66.66.", 3, 10);

        public static readonly Sprite BigScissors = S(@"
            .555.....555.
            5...5...5...5
            5...5...5...5
            .555.5.5.555.
            ......5......
            .....565.....
            ....56.65....
            ...56...65...
            ..56.....65..
            .56.......65.
            56.........65", 6, 10);

        public static readonly Sprite FingerPile = S(@"
            .s.S.ss.s..
            sSsRsxs.Ss.
            .ssSsRsSsSs
            sRsSssxsRs.
            .sSsRsSss..", 5, 4);

        public static readonly Sprite ShoesSmall = S(@"
            kk.kk
            kk.kk", 2, 1);

        public static readonly Sprite ShoesMom = S(@"
            00.00
            00.00
            0..0.", 2, 2);

        public static readonly Sprite Diary = S(@"
            kkkk
            k6yk
            kkkk", 2, 2);

        public static readonly Sprite Blanket = S(@"
            .ccccccccc.
            ccbccbccbcc
            ccccccccccc
            ccbccbccbcc
            .ccccccccc.", 5, 4);

        public static readonly Sprite Lamp = S(@"
            .yyy.
            yYYYy
            .yyy.
            ..3..
            ..3..
            .333.", 2, 5);

        public static readonly Sprite RiceCooker = S(@"
            .5555.
            556655
            5a6665
            555555", 3, 3);

        public static readonly Sprite Bag = S(@"
            .00.
            0..0
            NNNN
            NnnN
            NNNN", 2, 4);

        public static readonly Sprite SofaBack = S(@"
            .gggggggggggggggggggggg.
            gGGGGGGGGGGGGGGGGGGGGGGg
            gGGGGGGGGGGGGGGGGGGGGGGg
            gggggggggggggggggggggggg
            GGGGGGGGGGGGGGGGGGGGGGGG
            GGGGGGGGGGGGGGGGGGGGGGGG
            GGGGGGGGGGGGGGGGGGGGGGGG
            G......................G", 12, 7);

        public static readonly Sprite Mirror = S(@"
            555555
            5cccc5
            5cbcc5
            5cccc5
            5ccbc5
            5cccc5
            555555", 3, 6);

        public static readonly Sprite FacePile = S(@"
            ....w0ww0w....
            ..wwwwRwwww...
            .w0w0wwww0w0w.
            wwwRwwwww0w0ww
            w0w0wwRwwwwwRw
            wwwwwRwwwRwwww
            .RRxRRRxRRRRR.", 7, 6);

        public static readonly Sprite ExitSign = S(@"
            gggggg
            g6gg6g
            gggggg", 3, 2);
    }
}
