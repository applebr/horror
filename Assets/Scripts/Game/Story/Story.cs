using System;
using System.Collections;
using System.Collections.Generic;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>
    /// 방 배치(소품, 조사 대사)와 스토리 이벤트.
    /// 좌표는 방 픽셀 기준 (셀 4px). 발 위치(Y)로 정렬된다.
    /// </summary>
    public static partial class Story
    {
        public static void AttachSetups()
        {
            Bind("r_hana", SetupHanaReal);
            Bind("d_hana", SetupHanaDream);
            Bind("m_hana", SetupHanaMorning);
            Bind("r_hall", SetupHallReal);
            Bind("d_hall", SetupHallDream);
            Bind("m_hall", SetupHallMorning);
            Bind("r_living", SetupLivingReal);
            Bind("d_living", SetupLivingDream);
            Bind("m_living", SetupLivingMorning);
            Bind("r_kitchen", SetupKitchenReal);
            Bind("d_kitchen", SetupKitchenDream);
            Bind("m_kitchen", SetupKitchenMorning);
            Bind("r_bath", SetupBathReal);
            Bind("d_bath", SetupBathDream);
            Bind("m_bath", SetupBathReal);
            Bind("r_mom", SetupMomReal);
            Bind("d_mom", SetupMomDream);
            Bind("m_mom", SetupMomReal);
            Bind("d_loop", SetupLoop);
            Bind("d_stairs1", SetupStairs);
            Bind("d_stairs2", SetupStairs);
            Bind("d_stairs3", SetupStairs);
            Bind("d_cut", SetupCut);
        }

        static void Bind(string id, Action<Room, GameCore> setup)
        {
            Rooms.Get(id).Setup = (r, g) => { CurrentGame = g; setup(r, g); };
        }

        // ═════════════════ 하나 방 ═════════════════

        static void HanaCommon(Room r, GameCore g, bool dream, bool morning)
        {
            var bed = new Prop("bed", Sprites.BedHana, 12, 33).Box(-6, -16, 6, 1);
            bed.Solid = true;
            r.Add(bed);
            var desk = new Prop("desk", Sprites.Desk, 86, 26).Box(-7, -6, 7, 1);
            desk.Solid = true;
            r.Add(desk);
            r.Add(new Prop("chair", Sprites.Chair, 86, 33).Box(-3, -2, 3, 0));
            Look(r, "toybox", Sprites.ToyBox, 95, 54, true,
                dream ? "장난감 상자. …안에서 무언가 긁는 소리가 난다." :
                (morning ? "장난감 상자. 어제랑 똑같다." : "장난감 상자. 인형이랑 블록이 들어 있다."));
            r.Add(new Prop("clock", Sprites.Clock, 56, 13) { OnInteract = () => ClockLine(g, dream, morning) });
            SpotLines(r, "window", 36, 4, 60, 12,
                dream ? new[] { "창밖이 빨갛다.", "…하늘이 숨을 쉬는 것처럼 꿈틀거린다." } :
                (morning ? new[] { "햇빛이 눈부시다." } : new[] { "창밖은 깜깜하다. 맞은편 아파트에 불 켜진 집이 하나도 없다." }));
        }

        static IEnumerator ClockLine(GameCore g, bool dream, bool morning)
        {
            if (dream) yield return g.Say(Speaker.Think, "4시 44분. …초침이 움직이지 않는다.");
            else if (morning) yield return g.Say(Speaker.Think, "7시 30분.");
            else yield return g.Say(Speaker.Think, g.Flags.Has("mom_left") ? "11시 50분. 잘 시간이다." : "11시 44분.");
        }

        static void SetupHanaReal(Room r, GameCore g)
        {
            HanaCommon(r, g, false, false);
            var ps = PS(g);
            r.Add(new Prop("rabbit", Sprites.Rabbit, 9, 22) { Layer = 1, OnInteract = () => SayLines(g, Speaker.Think, new[] { "토순이. 엄마가 생일 선물로 사 줬다." }) });
            r.Add(new Prop("diary", Sprites.Diary, 81, 21) { Layer = 1, IRange = 9, OnInteract = () => Diary(g) });
            r.Add(new Prop("flashlight", Sprites.Flashlight, 90, 21) { Layer = 1, IRange = 9, OnInteract = () => SayLines(g, Speaker.Think, new[] { "별 모양 손전등. 엄마가 무서우면 켜라고 사 줬다." }) });
            var lamp = new Light(91, 20, 30, 0.35f);
            r.Lights.Add(lamp);
            // 침대: 자러 가기
            r.Find<Prop>("bed").OnInteract = () => BedReal(g);
            if (g.Flags.Has("lights_off")) { r.Ambient = 0.25f; lamp.On = false; }
        }

        static IEnumerator Diary(GameCore g)
        {
            yield return g.ShowNote("그림일기 — 4월 3일",
                "오늘은 엄마랑 짜장면을 먹었다. 맛있었다.\n\n그림: 우리 가족.\n\n(크레파스로 그린 사람이 세 명 있다. 엄마, 나, 그리고 키가 큰 사람.)",
                false, true, false);
            yield return g.Say(Speaker.Think, "…세 명? 이 키 큰 사람은 누구지?");
            yield return g.Say(Speaker.Think, "내가 그렸는데… 기억이 안 난다.");
            g.Flags.Set("saw_diary");
        }

        static void SetupHanaDream(Room r, GameCore g)
        {
            HanaCommon(r, g, true, false);
            var ps = PS(g);
            r.Add(new NightLightProp(24, 25));
            r.Lights.Add(new Light(48, 8, 26, 0.18f) { Flicker = 0.4f }); // 붉은 창
            var rabbit = new Prop("rabbit", Sprites.Rabbit, 9, 22) { Layer = 1, HideWhenLucid = true };
            rabbit.OnInteract = () => SayLines(g, Speaker.Think, new[] { "토순이. …눈이 축축하다." });
            r.Add(rabbit);
            var gutted = new Prop("rabbit_gutted", Sprites.RabbitGutted, 9, 22) { Layer = 1, LucidOnly = true };
            gutted.OnInteract = () => SayLines(g, Speaker.Think, new[] { "토순이의 배가 갈라져 있다.", "솜 대신… 따뜻하고 축축한 것이 들어 있다." });
            r.Add(gutted);
            r.Add(new WallText("꼭꼭 숨어라", 72, 5, Pal.BloodBright, true, true));
            var note = new NoteProp("hana_note", Sprites.Note, 81, 21, "책상 위의 쪽지",
                new[]
                {
                    "하나야, 엄마 금방 올게.\n무서우면 손가락을 세어 봐.",
                    "하나야, 엄마는 안 와.",
                    "하나야 하나야 하나야 하나야 하나야 하나야 하나야 하나야 하나야 하나야 하나야",
                },
                "꿈에서는 글씨가 바뀐대.\n두 번 읽으면 알 수 있어.\n\n…그 사람은 생각을 들어. 자각하면, 그게 들려.")
            { Layer = 1, IRange = 9 };
            r.Add(note);
            if (!g.Flags.Has("has_light"))
            {
                var fl = new Prop("flashlight", Sprites.Flashlight, 90, 21) { Layer = 1, IRange = 9 };
                fl.OnInteract = () => TakeFlashlight(g, fl);
                r.Add(fl);
            }
            r.Find<Prop>("bed").OnInteract = () => SayLines(g, Speaker.Think, new[] { "다시 자면… 깰 수 있을까?", "…아니. 이건 꿈이야. 이미 자고 있어." });
            OnStep(r, "door_hint", Cx(9), Cy(15), Cx(16), Cy(18), () => DoorHint(g), () => !g.Flags.Has("first_check"));
        }

        static IEnumerator TakeFlashlight(GameCore g, Prop fl)
        {
            var ps = PS(g);
            fl.Removed = true;
            g.Flags.Set("has_light");
            ps.Player.HasLight = true;
            g.Audio.Play(Sfx.Click, 0.6f);
            yield return g.Say(Speaker.Think, "별 손전등. …다행이다.");
            yield return g.Say(Speaker.Sys, "X 키를 누르고 있으면 손전등을 켠다. 배터리는 끄면 다시 찬다.");
        }

        static IEnumerator DoorHint(GameCore g)
        {
            yield return g.Say(Speaker.Think, "…뭔가 이상해. 집이 너무 조용해.");
            yield return g.Say(Speaker.Think, "엄마가 그랬어. 손가락을 세어 보라고.");
            yield return g.Say(Speaker.Sys, "C 키: 손가락 세기");
        }

        static void SetupHanaMorning(Room r, GameCore g)
        {
            HanaCommon(r, g, false, true);
            r.Add(new Prop("rabbit", Sprites.Rabbit, 9, 22) { Layer = 1, OnInteract = () => SayLines(g, Speaker.Think, new[] { "토순이. …멀쩡하다." }) });
            r.Add(new Prop("nl_off", Sprites.NightLightOff, 24, 25) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "수면등이 꺼져 있다." }) });
            r.Find<Prop>("bed").OnInteract = () => SayLines(g, Speaker.Think, new[] { "이제 안 자도 돼." });
            r.Lights.Add(new Light(48, 10, 50, 0.4f));
        }

        // ═════════════════ 복도 ═════════════════

        static void SetupHallReal(Room r, GameCore g)
        {
            r.Add(new Prop("frame1", Sprites.PhotoFrame, 40, 12) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "엄마 졸업 사진." }) });
            r.Add(new Prop("frame2", Sprites.PhotoFrame, 96, 12) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "내 돌 사진. 한복이 크다." }) });
            r.Add(new Prop("frame3", Sprites.PhotoFrame, 116, 12) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "바다에서 찍은 사진. 엄마랑 나. …사진기는 누가 들고 있었지?" }) });
            r.Add(new Prop("plant", Sprites.Plant, 150, 24) { Solid = true, OnInteract = () => SayLines(g, Speaker.Think, new[] { "엄마가 키우는 화분." }) });
        }

        static void SetupHallDream(Room r, GameCore g)
        {
            r.Add(new Prop("frame1", Sprites.PhotoFrameEmpty, 40, 12) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "사진 속 얼굴이… 오려져 있다." }) });
            r.Add(new Prop("frame2", Sprites.PhotoFrameEmpty, 96, 12) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "이것도. 얼굴만 동그랗게." }) });
            r.Add(new Prop("frame3", Sprites.PhotoFrameEmpty, 116, 12) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "누가… 가위로 오려 갔어." }) });
            r.Add(new Prop("plant", Sprites.DeadPlant, 150, 24) { Solid = true, OnInteract = () => SayLines(g, Speaker.Think, new[] { "화분이 말라 죽었다. 흙에서 머리카락이 자란다." }) });
            r.Lights.Add(new Light(70, 30, 16, 0.12f) { Flicker = 0.8f });
            if (!g.Flags.Has("hall_kid_seen"))
            {
                var kid = new FacelessKid(Cx(36), Cy(8), false) { Hunter = false };
                kid.Name = "hall_kid";
                r.Add(kid);
                OnStep(r, "hall_scare", Cx(14), Cy(4), Cx(24), Cy(10), () => HallScare(g));
            }
        }

        static IEnumerator HallScare(GameCore g)
        {
            var ps = PS(g);
            var kid = ps.Room.Find<FacelessKid>("hall_kid");
            if (kid == null) yield break;
            ps.Player.Frozen = true;
            ps.Player.Face(kid.X, kid.Y);
            yield return g.Wait(0.4f);
            g.Audio.PlayVoice("꼭꼭 숨어라.", VoiceKind.Kids, 0.6f);
            yield return g.Say(Speaker.Think, "…누구야?");
            yield return g.Wait(0.6f);
            g.Fx.StaticFlicker = 0.6f;
            g.Audio.Play(Sfx.Static, 0.6f);
            kid.Removed = true;
            g.Flags.Set("hall_kid_seen");
            yield return g.Wait(0.5f);
            yield return g.Say(Speaker.Think, "…없어졌다. 얼굴이… 없었어.");
            ps.Player.Frozen = false;
        }

        static void SetupHallMorning(Room r, GameCore g)
        {
            SetupHallReal(r, g);
            r.Lights.Add(new Light(80, 30, 60, 0.2f));
        }

        // ═════════════════ 거실 ═════════════════

        static void LivingCommon(Room r, GameCore g, bool dream, bool morning)
        {
            var tv = new Prop("tv", Sprites.Tv, 56, 30).Box(-6, -5, 6, 1);
            tv.Solid = true;
            r.Add(tv);
            var table = new Prop("table", Sprites.LowTable, 56, 49).Box(-7, -3, 7, 1);
            table.Solid = true;
            r.Add(table);
            var sofa = new Prop("sofa", Sprites.SofaBack, 56, 68).Box(-12, -4, 12, 1);
            sofa.Solid = true;
            r.Add(sofa);
            r.Add(new Prop("clock", Sprites.Clock, 136, 12) { OnInteract = () => ClockLine(g, dream, morning) });
            r.Add(new Prop("calendar", Sprites.Calendar, 150, 13)
            {
                OnInteract = () => SayLines(g, Speaker.Think, dream
                    ? new[] { "달력. 4월 4일에 빨간 동그라미. 그 아래에 '데리러 감'." , "…누구를?" }
                    : new[] { "은행 달력. 1998년 4월. 오늘은 4월 4일." })
            });
            r.Add(new Prop("plant", dream ? Sprites.DeadPlant : Sprites.Plant, 8, 28) { Solid = true });
            var rack = new Prop("rack", Sprites.ShoeRack, 150, 93).Box(-6, -3, 6, 1);
            rack.Solid = true;
            rack.OnInteract = () => SayLines(g, Speaker.Think, dream ? new[] { "신발장. 작은 신발이… 하나도 없다." } : new[] { "신발장." });
            r.Add(rack);
            r.Add(new Prop("shoes", Sprites.ShoesSmall, 128, 104) { Layer = -1 });
        }

        static void SetupLivingReal(Room r, GameCore g)
        {
            LivingCommon(r, g, false, false);
            r.Add(new Prop("frame", Sprites.PhotoFrame, 24, 13) { OnInteract = () => FamilyPhotoReal(g) });
            r.Find<Prop>("tv").OnInteract = () => SayLines(g, Speaker.Think, new[] { "애국가가 끝나고 지지직 소리만 난다.", "방송이 다 끝났나 봐." });
            r.Find<Prop>("tv").Frames = new[] { TvStatic(0), TvStatic(1), TvStatic(2) };
            r.Find<Prop>("tv").FrameRate = 12;
            r.Lights.Add(new Light(56, 28, 22, 0.3f) { Flicker = 0.5f });
            SpotLines(r, "window", 44, 4, 116, 12, "베란다 창문. 밖에 바람이 분다.");
            r.Find<Prop>("sofa").OnInteract = () => SayLines(g, Speaker.Think, new[] { "소파. 엄마는 여기서 자주 잠든다." });
            r.Find<Prop>("table").OnInteract = () => SayLines(g, Speaker.Think, new[] { "엄마가 깎아 놓은 사과. 갈색으로 변했다." });
            if (!g.Flags.Has("mom_left"))
            {
                var mom = new Actor("mom", Sprites.MomDown, Sprites.MomUp, Sprites.MomSide, Cx(31), Cy(25)) { Dir = 2 };
                r.Add(mom);
                r.Add(new Prop("momshoes", Sprites.ShoesMom, 136, 104) { Layer = -1 });
                r.Add(new Prop("bag", Sprites.Bag, 140, 100));
            }
            if (g.Flags.Has("lights_off")) r.Ambient = 0.25f;
        }

        static IEnumerator FamilyPhotoReal(GameCore g)
        {
            int n = g.Flags.Inc("photo_looks");
            yield return g.Say(Speaker.Think, "우리 가족사진. 엄마랑 나.");
            if (n >= 2)
            {
                yield return g.Say(Speaker.Think, "…사진 오른쪽이 이상하게 비어 있다.");
                yield return g.Say(Speaker.Think, "누가 서 있어야 할 것처럼.");
            }
        }

        static Sprite[] tvStatic;
        static Sprite TvStatic(int k)
        {
            if (tvStatic == null)
            {
                tvStatic = new Sprite[3];
                for (int i = 0; i < 3; i++)
                {
                    var s = Sprite.Parse(new[]
                    {
                        "....4....4..", ".....4..4...", "......44....",
                        "333333333333", "344444444443", "342222222243", "342222222243", "342222222243",
                        "342222222243", "344444444443", "3444444y4a43", "333333333333",
                        "NNNNNNNNNNNN", "NnnnnnnnnnnN", "NnnnnnnnnnnN", "N..........N"
                    }, 6, 15);
                    for (int y = 5; y <= 8; y++)
                        for (int x = 2; x <= 9; x++)
                        {
                            float h = M.Hash(x, y, i * 31 + 7);
                            s.Set(x, y, h < 0.33f ? Pal.LightGray : (h < 0.6f ? Pal.Gray : Pal.DarkGray));
                        }
                    tvStatic[i] = s;
                }
            }
            return tvStatic[k];
        }

        static void SetupLivingDream(Room r, GameCore g)
        {
            LivingCommon(r, g, true, false);
            var ps = PS(g);
            r.Add(new NightLightProp(20, 60));
            var frame = new Prop("frame", g.Flags.Has("photo_done") ? Sprites.PhotoFrame : Sprites.PhotoFrameEmpty, 24, 13);
            frame.OnInteract = () => EmptyFrame(g);
            frame.IRange = 7;
            r.Add(frame);
            var tv = r.Find<Prop>("tv");
            tv.Frames = new[] { TvStatic(0), TvStatic(1), TvStatic(2) };
            tv.FrameRate = 14;
            tv.OnInteract = () => SayLines(g, Speaker.Think, new[] { "지지직…", "화면 속 노이즈 사이로, 누군가 이쪽을 보고 있다." });
            r.Lights.Add(new Light(56, 28, 26, 0.35f) { Flicker = 0.7f });
            r.Lights.Add(new Light(80, 8, 30, 0.15f) { Flicker = 0.3f });
            SpotLines(r, "window", 44, 4, 116, 12, "창밖에 빨간 달이 떠 있다.", "…달이 아니다.", "눈이다. 깜빡였다.");
            r.Find<Prop>("sofa").OnInteract = () => SayLines(g, Speaker.Think, new[] { "소파 쿠션 사이에 손톱이 잔뜩 끼어 있다." });
            r.Find<Prop>("table").OnInteract = () => SayLines(g, Speaker.Think, new[] { "사과 대신… 귀가 접시에 놓여 있다.", "깎다 만 것처럼." });
            if (!g.Flags.Has("saw_frame"))
                OnStep(r, "frame_hint", Cx(8), Cy(4), Cx(30), Cy(14), () => FrameHint(g));
            if (g.Flags.Has("photo_done")) r.Add(new WallText("찾았다", 56, 74, Pal.BloodBright, false, true));
        }

        static IEnumerator FrameHint(GameCore g)
        {
            yield return g.Say(Speaker.Think, "…가족사진이 없어졌다.");
            g.Flags.Set("saw_frame");
            yield return null;
        }

        static IEnumerator EmptyFrame(GameCore g)
        {
            var ps = PS(g);
            if (g.Flags.Has("photo_done"))
            {
                yield return g.Say(Speaker.Think, "엄마, 나… 그리고 아빠 모양의 구멍.");
                yield break;
            }
            int n = Pieces(g);
            if (!g.Flags.Has("saw_frame2"))
            {
                g.Flags.Set("saw_frame");
                g.Flags.Set("saw_frame2");
                yield return g.Say(Speaker.Think, "액자가 비어 있다.");
                yield return g.Say(Speaker.Think, "누가 사진을… 오려 갔어. 조각조각.");
                yield return g.Say(Speaker.Think, "찾아야 해. 우리 가족사진.");
                ps.Hud.Objective("가족사진 조각을 찾자 (" + n + "/3)");
                if (n < 3) yield break;
            }
            if (n < 3)
            {
                yield return g.Say(Speaker.Think, "사진 조각이 " + (3 - n) + "개 더 필요하다.");
                yield break;
            }
            yield return Finale.AssemblePhoto(g);
        }

        static void SetupLivingMorning(Room r, GameCore g)
        {
            LivingCommon(r, g, false, true);
            r.Add(new Prop("frame", Sprites.PhotoFrame, 24, 13) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "가족사진…", "…잘 안 보인다. 햇빛 때문에." }) });
            r.Find<Prop>("tv").OnInteract = () => SayLines(g, Speaker.Think, new[] { "아침 뉴스. 오늘은 맑음." });
            r.Lights.Add(new Light(80, 10, 70, 0.3f));
        }

        // ═════════════════ 부엌 ═════════════════

        static void KitchenCommon(Room r, GameCore g, bool dream)
        {
            var fridge = new Prop("fridge", Sprites.Fridge, 10, 33).Box(-5, -8, 5, 1);
            fridge.Solid = true;
            r.Add(fridge);
            int[] xs = { 26, 36, 46, 56, 66, 76, 86, 96, 106 };
            foreach (int x in xs)
            {
                Sprite s = x == 46 ? Sprites.Sink : (x == 86 ? Sprites.Stove : Sprites.CounterBlock);
                var c = new Prop("counter", s, x, 26).Box(-5, -9, 5, 1);
                c.Solid = true;
                r.Add(c);
            }
            r.Add(new Prop("rice", Sprites.RiceCooker, 106, 20) { Layer = 1 });
            var table = new Prop("dtable", Sprites.DiningTable, 64, 64).Box(-8, -5, 8, 1);
            table.Solid = true;
            r.Add(table);
            r.Add(new Prop("chair1", Sprites.Chair, 52, 70));
            r.Add(new Prop("chair2", Sprites.Chair, 76, 70));
        }

        static void SetupKitchenReal(Room r, GameCore g)
        {
            KitchenCommon(r, g, false);
            r.Find<Prop>("fridge").OnInteract = () => FridgeNote(g);
            r.Find<Prop>("dtable").OnInteract = () => SayLines(g, Speaker.Think, new[] { "식탁. 의자가 세 개다." });
            foreach (var e in r.Entities)
                if (e.Name == "counter" && ((Prop)e).Spr == Sprites.Sink) e.OnInteract = () => SayLines(g, Speaker.Think, new[] { "설거지가 다 되어 있다." });
            if (g.Flags.Has("lights_off")) r.Ambient = 0.25f;
        }

        static IEnumerator FridgeNote(GameCore g)
        {
            yield return g.ShowNote("냉장고에 붙은 쪽지", "하나야,\n냉장고에 우유 있어.\n이 닦고 일찍 자기!\n\n— 엄마가", false, false, false);
        }

        static void SetupKitchenDream(Room r, GameCore g)
        {
            KitchenCommon(r, g, true);
            var ps = PS(g);
            r.Add(new Prop("board", Sprites.CuttingBoard, 66, 21) { Layer = 1 });
            r.Add(new Prop("pot", Sprites.Pot, 86, 20) { Layer = 1 });
            r.Lights.Add(new Light(66, 18, 24, 0.32f) { Flicker = 0.25f }); // 부엌 형광등
            // 핏자국
            ps.Blood.SetRoom(r);
            for (int i = 0; i < 6; i++) ps.Blood.InstantPool(60 + i * 3, 29 + (i % 2), 1.6f, Pal.DarkRed);
            r.Find<Prop>("dtable").OnInteract = () => SayLines(g, Speaker.Think, new[] { "식탁 위에 밥그릇이 세 개.", "하나는 비어 있고, 하나는 손가락이 담겨 있고, 하나는…", "내 이름이 적혀 있다." });
            if (!g.Flags.Has("piece_kitchen"))
            {
                var mother = new MotherThing(66, 35);
                mother.KitchenMinX = 6; mother.KitchenMaxX = 122; mother.KitchenMinY = 30; mother.KitchenMaxY = 74;
                mother.OnCaught = () => ps.RunAmbient(MotherCaught(g));
                r.Add(mother);
                var piece = new PieceProp("piece_kitchen", 11, 23) { Layer = 1, IRange = 14 };
                piece.OnInteract = () => TakeKitchenPiece(g, piece);
                r.Add(piece);
                OnStep(r, "kitchen_enter", Cx(24), Cy(4), Cx(31), Cy(18), () => KitchenEnter(g), () => !g.Flags.Has("kitchen_seen"));
            }
            else
            {
                r.Add(new WallText("밥 먹어야지", 64, 6, Pal.BloodBright, true, true));
            }
            var fridgePiece = r.Find<PieceProp>("piece_piece_kitchen");
            r.Find<Prop>("fridge").OnInteract = () =>
                g.Flags.Has("piece_kitchen") || fridgePiece == null
                    ? SayLines(g, Speaker.Think, new[] { "냉장고 문틈으로 차가운 숨이 새어 나온다." })
                    : TakeKitchenPiece(g, fridgePiece);
        }

        static IEnumerator KitchenEnter(GameCore g)
        {
            var ps = PS(g);
            g.Flags.Set("kitchen_seen");
            ps.Player.Frozen = true;
            yield return g.Wait(0.5f);
            yield return g.Say(Speaker.Think, "…엄마?");
            yield return g.Say(Speaker.Think, "엄마는 일하러 갔는데.");
            yield return g.Say(Speaker.Think, "냉장고에… 사진 조각이 있다.");
            yield return g.Say(Speaker.Think, "칼질하는 소리가 날 때만 움직이자. 소리가 멈추면… 가만히.");
            ps.Player.Frozen = false;
        }

        static IEnumerator MotherCaught(GameCore g)
        {
            var ps = PS(g);
            ps.Player.Frozen = true;
            yield return g.Wait(0.5f);
            ps.Kill(DeathKind.Mother);
        }

        static IEnumerator TakeKitchenPiece(GameCore g, PieceProp piece)
        {
            var ps = PS(g);
            var mother = ps.Room.Find<MotherThing>("mother");
            if (mother != null && mother.Listening)
            {
                // 듣고 있을 때 집으면 들킨다
                mother.SetState(MotherThing.St.Turn);
                g.Audio.Play(Sfx.Sting, 0.9f);
                yield return MotherCaught(g);
                yield break;
            }
            piece.Removed = true;
            yield return GetPiece(g, "piece_kitchen");
            if (mother == null) yield break;
            // 칼질이 멈춘다
            mother.SetState(MotherThing.St.Idle);
            g.Audio.Play(Sfx.StingSoft, 0.7f);
            yield return g.Wait(1.0f);
            yield return g.Sing(Speaker.Mother, "하나야… 밥 먹어야지?", new[] { 76, 76, 74, 72, 74, 76, 69 });
            yield return g.Wait(0.3f);
            ps.Player.Frozen = false;
            mother.SetState(MotherThing.St.Hunt);
            g.Audio.Play(Sfx.Scream, 0.7f, 0.8f);
            g.Fx.Shake(2, 0.5f);
            g.Audio.SetMusic(Ch.Layer, Music.Chase, 0.6f, 0.3f);
            ps.RunAmbient(StopChaseOnLeave(g, "d_kitchen"));
        }

        static IEnumerator StopChaseOnLeave(GameCore g, string roomId)
        {
            var ps = PS(g);
            while (ps.Room != null && ps.Room.Def.Id == roomId && !ps.Dying) yield return null;
            g.Audio.StopMusic(Ch.Layer, 2f);
        }

        static void SetupKitchenMorning(Room r, GameCore g)
        {
            KitchenCommon(r, g, false);
            r.Lights.Add(new Light(64, 10, 60, 0.3f));
            if (!g.Flags.Has("end_mom_talked"))
            {
                var mom = new Actor("mom", new[] { Sprites.MomApronFront }, new[] { Sprites.MomApronBack }, Sprites.MomSide, 66, 35) { Dir = 1 };
                r.Add(mom);
                r.Add(new Prop("pot", Sprites.Pot, 86, 20) { Layer = 1 });
                OnStep(r, "end_mom", Cx(10), Cy(4), Cx(26), Cy(18), () => Finale.EndingKitchen(g));
            }
        }

        // ═════════════════ 욕실 ═════════════════

        static void SetupBathReal(Room r, GameCore g)
        {
            var tub = new Prop("tub", Sprites.Bathtub, 12, 32).Box(-7, -19, 7, 1);
            tub.Solid = true;
            tub.OnInteract = () => SayLines(g, Speaker.Think, new[] { "욕조. 물이 다 빠져 있다." });
            r.Add(tub);
            var toilet = new Prop("toilet", Sprites.Toilet, 68, 24).Box(-4, -5, 4, 1);
            toilet.Solid = true;
            r.Add(toilet);
            var sink = new Prop("sink", Sprites.BathSink, 44, 20).Box(-5, -4, 5, 1);
            sink.Solid = true;
            r.Add(sink);
            r.Add(new Prop("mirror", Sprites.Mirror, 44, 10) { IRange = 9 });
            bool morning = r.Def.Theme == Theme.Morning;
            if (morning)
            {
                r.Find<Prop>("mirror").OnInteract = () => SayLines(g, Speaker.Think, new[] { "거울 속의 나. …괜찮아 보인다." });
                return;
            }
            sink.OnInteract = () => BrushTeeth(g);
            r.Find<Prop>("mirror").OnInteract = () => MirrorReal(g);
            toilet.OnInteract = () => SayLines(g, Speaker.Think, new[] { "변기." });
            if (g.Flags.Has("lights_off")) r.Ambient = 0.25f;
        }

        static IEnumerator BrushTeeth(GameCore g)
        {
            if (g.Flags.Has("brushed"))
            {
                yield return g.Say(Speaker.Think, "이는 아까 닦았다.");
                yield break;
            }
            var ps = PS(g);
            ps.Player.Frozen = true;
            yield return g.FadeOut(0.4f);
            for (int i = 0; i < 6; i++) { g.Audio.Play(Sfx.Scrape, 0.12f, 2.5f); yield return g.Wait(0.25f); }
            g.Audio.Play(Sfx.Drip, 0.5f);
            yield return g.FadeIn(0.4f);
            g.Flags.Set("brushed");
            yield return g.Say(Speaker.Think, "이를 닦았다. 치약이 맵다.");
            ps.Hud.Objective("잘 준비를 하자: 침대로");
            ps.Player.Frozen = false;
        }

        static IEnumerator MirrorReal(GameCore g)
        {
            int n = g.Flags.Inc("mirror_real");
            yield return g.Say(Speaker.Think, "거울 속의 나.");
            if (n >= 2)
            {
                yield return g.Say(Speaker.Think, "……");
                yield return g.Say(Speaker.Think, "방금, 거울 속의 내가 조금 늦게 움직인 것 같다.");
            }
        }

        static void SetupBathDream(Room r, GameCore g)
        {
            var ps = PS(g);
            var tub = new BathtubProp(12, 32);
            r.Add(tub);
            var toilet = new Prop("toilet", Sprites.Toilet, 68, 24).Box(-4, -5, 4, 1);
            toilet.Solid = true;
            toilet.OnInteract = () => SayLines(g, Speaker.Think, new[] { "…뚜껑 안에서 똑똑, 두드리는 소리가 난다.", "열지 않는 게 좋겠다." });
            r.Add(toilet);
            var sink = new Prop("sink", Sprites.BathSink, 44, 20).Box(-5, -4, 5, 1);
            sink.Solid = true;
            sink.OnInteract = () => SayLines(g, Speaker.Think, new[] { "수도꼭지에서 피가 한 방울씩 떨어진다.", "똑. 똑. 똑." });
            sink.CustomUpdate = (p, dt) => { if (g.Rng.Chance(dt * 1.5f)) { ps.Blood.Drip(46, 18, 4); } };
            r.Add(sink);
            var mirror = new Prop("mirror", Sprites.Mirror, 44, 10) { IRange = 9 };
            mirror.OnInteract = () => MirrorDream(g);
            r.Add(mirror);
            r.Lights.Add(new Light(40, 14, 20, 0.2f) { Flicker = 0.6f });
            if (!g.Flags.Has("bath_seen"))
                OnStep(r, "bath_enter", Cx(4), Cy(9), Cx(16), Cy(16), () => BathEnter(g));
        }

        static IEnumerator BathEnter(GameCore g)
        {
            g.Flags.Set("bath_seen");
            yield return g.Say(Speaker.Think, "비린내…");
            yield return g.Say(Speaker.Think, "욕조에 뭐가 가득 차 있다.");
        }

        static IEnumerator MirrorDream(GameCore g)
        {
            var ps = PS(g);
            if (ps.Lucid)
            {
                yield return g.Say(Speaker.Think, "거울 속의 내가 웃고 있다.");
                yield return g.Say(Speaker.Think, "나는 웃지 않았는데.");
                g.Audio.Play(Sfx.StingSoft, 0.6f);
                g.Fx.Subliminal(cv => Art.DrawMirrorFace(cv, g.Time), 2);
            }
            else
            {
                yield return g.Say(Speaker.Think, "거울 속의 나는… 얼굴이 없다.");
            }
        }

        // ═════════════════ 안방 ═════════════════

        static void MomCommon(Room r, GameCore g, bool dream)
        {
            var bed = new Prop("bed", Sprites.BedMom, 96, 35).Box(-9, -17, 9, 1);
            bed.Solid = true;
            r.Add(bed);
            var vanity = new Prop("vanity", Sprites.Vanity, 46, 25).Box(-4, -4, 4, 1);
            vanity.Solid = true;
            r.Add(vanity);
            r.Add(new Prop("jewel", Sprites.JewelBox, 46, 20) { Layer = 1 });
        }

        static void SetupMomReal(Room r, GameCore g)
        {
            MomCommon(r, g, false);
            var w = new Prop("wardrobe", Sprites.Wardrobe, 16, 33).Box(-10, -8, 10, 1);
            w.Solid = true;
            w.OnInteract = () => SayLines(g, Speaker.Think, new[] { "자개장롱. 반짝이는 학 무늬.", "어릴 땐 여기 숨어서 숨바꼭질을 했다." });
            r.Add(w);
            r.Find<Prop>("bed").OnInteract = () => SayLines(g, Speaker.Think, new[] { "엄마 침대. 베개가 두 개다." });
            r.Find<Prop>("vanity").OnInteract = () => SayLines(g, Speaker.Think, new[] { "엄마 화장대. 립스틱, 빗, 보석함." });
            SpotLines(r, "window", 80, 4, 100, 12, r.Def.Theme == Theme.Morning ? "햇빛." : "창문에 내 얼굴이 비친다.");
            if (g.Flags.Has("lights_off")) r.Ambient = 0.25f;
        }

        static void SetupMomDream(Room r, GameCore g)
        {
            MomCommon(r, g, true);
            var ps = PS(g);
            var w = new HideSpotProp("wardrobe", Sprites.Wardrobe, 16, 33);
            w.SetBox(-10, -8, 10, 1);
            w.Question = "자개장롱 안에 숨을까?";
            r.Add(w);
            r.Lights.Add(new Light(90, 8, 26, 0.12f) { Flicker = 0.5f });
            var bed = r.Find<Prop>("bed");
            bed.OnInteract = () => MomBedLump(g);
            bed.CustomDraw = (p, c, ox, oy) =>
            {
                c.Blit(Sprites.BedMom, ox + (int)p.X, oy + (int)p.Y);
                if (!g.Flags.Has("blanket_pulled"))
                {
                    // 이불 밑에서 무언가 숨 쉰다
                    float br = M.Sin(ps.Room.Time * 1.6f);
                    int hgt = br > 0.3f ? 1 : 0;
                    c.FillEllipse(ox + p.X, oy + p.Y - 8 - hgt, 6, 4 + hgt, Pal.PaleBlue);
                    c.HLine(ox + (int)p.X - 4, ox + (int)p.X + 4, oy + (int)p.Y - 6, Pal.Blue);
                }
                else
                {
                    c.Blit(Sprites.PaperDoll, ox + (int)p.X, oy + (int)p.Y - 3);
                    c.FillRect(ox + (int)p.X - 3, oy + (int)p.Y - 10, 6, 2, Pal.Red);
                }
            };
            r.Find<Prop>("vanity").OnInteract = () => SayLines(g, Speaker.Think, new[] { "화장대 거울에 립스틱으로 쓴 글씨: '머리카락 보일라'" });
            if (!g.Flags.Has("piece_mom"))
            {
                var jewel = r.Find<Prop>("jewel");
                jewel.IRange = 10;
                jewel.OnInteract = () => TakeMomPiece(g);
                if (!g.Flags.Has("mom_kids_done"))
                {
                    r.Add(new FacelessKid(104, 72, true) { Speed = 12f });
                    r.Add(new FacelessKid(96, 50, false) { Speed = 11f });
                    r.Add(new FacelessKid(62, 80, false) { Speed = 13f });
                }
                if (!g.Flags.Has("mom_seen"))
                    OnStep(r, "mom_enter", 0, 0, 120, 88, () => MomEnter(g));
            }
        }

        static IEnumerator MomEnter(GameCore g)
        {
            var ps = PS(g);
            g.Flags.Set("mom_seen");
            ps.Player.Frozen = true;
            g.Audio.Play(Sfx.Rip, 0.6f, 0.7f);
            yield return g.Wait(0.4f);
            yield return g.Say(Speaker.Think, "벽을… 통과했다.");
            yield return g.Say(Speaker.Think, "엄마 방이다.");
            g.Audio.PlayVoice("꼭꼭 숨어라, 머리카락 보일라.", VoiceKind.Kids, 0.8f);
            yield return g.Say(Speaker.Kids, "꼭꼭 숨어라, 머리카락 보일라.");
            yield return g.Say(Speaker.Think, "…빛을 비추면 멈춘다. 그런 느낌이 든다.");
            ps.Player.Frozen = false;
        }

        static IEnumerator MomBedLump(GameCore g)
        {
            var ps = PS(g);
            if (g.Flags.Has("blanket_pulled"))
            {
                yield return g.Say(Speaker.Think, "종이로 오린 사람. 피로 붙여 놓았다.");
                yield break;
            }
            yield return g.Say(Speaker.Think, "이불 밑에서… 무언가 숨을 쉰다.");
            if (!ps.Lucid)
            {
                yield return g.Say(Speaker.Think, "들춰 볼 용기가 나지 않는다.");
                yield break;
            }
            int choice = 1;
            yield return g.Ask("이불을 들춰 볼까?", new[] { "들춘다", "그만둔다" }, i => choice = i);
            if (choice != 0) yield break;
            g.Flags.Set("blanket_pulled");
            g.Audio.Play(Sfx.Rip, 0.8f);
            g.Audio.Play(Sfx.Gore, 0.6f);
            g.Fx.Shake(2, 0.3f);
            ps.Blood.Burst(96, 26, 6, 16, 30, 30);
            yield return g.Wait(0.6f);
            yield return g.Say(Speaker.Think, "…종이로 오린 사람이다.");
            yield return g.Say(Speaker.Think, "키가 큰 남자. 오린 자국마다 피가 배어 나온다.");
            yield return g.Say(Speaker.Think, "…아빠 냄새가 난다.");
            yield return g.Say(Speaker.Think, "아빠? 나한테… 아빠가 있었나?");
            g.Flags.Set("knows_dad");
        }

        static IEnumerator TakeMomPiece(GameCore g)
        {
            var ps = PS(g);
            yield return g.Say(Speaker.Think, "보석함 안에… 사진 조각.");
            yield return GetPiece(g, "piece_mom");
            g.Flags.Set("mom_kids_done");
            // 아이들이 비명을 지르며 사라진다
            foreach (var e in ps.Room.Entities)
                if (e is FacelessKid) { e.Removed = true; ps.Blood.Burst(e.X, e.Y, 6, 10, 20, 26); }
            g.Audio.Play(Sfx.Scream, 0.6f, 1.6f);
            g.Fx.StaticFlicker = 0.8f;
            yield return g.Wait(0.8f);
            yield return g.Say(Speaker.Kids, "찾았다.");
            yield return g.Wait(0.3f);
            // 문이 열리고 가위가 들어온다
            g.Audio.Play(Sfx.DoorOpen, 0.8f, 0.6f);
            yield return g.Wait(0.5f);
            ps.Stalker.Appear(Cx(12), Cy(18));
            g.Audio.Play(Sfx.Sting, 0.9f);
            g.Fx.Shake(3, 0.5f);
            yield return g.Say(Speaker.Think, "…!");
            yield return g.Say(Speaker.Sys, "도망치거나, 숨거나, 자각한 채 빛을 비추자.");
        }

        // ═════════════════ 끝없는 복도 ═════════════════

        static void SetupLoop(Room r, GameCore g)
        {
            var ps = PS(g);
            int loops = g.Flags.Get("loops");
            r.Add(new Prop("frame1", Sprites.PhotoFrameEmpty, 30, 12) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "얼굴이 오려진 사진." }) });
            r.Add(new Prop("frame2", Sprites.PhotoFrameEmpty, 70, 12) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "또. 또. 또." }) });
            r.Add(new Prop("frame3", Sprites.PhotoFrameEmpty, 140, 12) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "사진 속 아이들 얼굴이 전부 없다." }) });
            r.Lights.Add(new Light(80, 30, 18, 0.12f) { Flicker = 0.9f });
            int kids = Math.Min(loops, 6);
            float[] kx = { 120, 44, 88, 140, 24, 64 };
            float[] ky = { 22, 42, 22, 42, 22, 42 };
            for (int i = 0; i < kids; i++)
            {
                var k = new FacelessKid(kx[i], ky[i], i % 2 == 1) { Hunter = loops >= 4, Speed = 9f + loops };
                r.Add(k);
            }
            if (loops >= 2)
            {
                var note = new NoteProp("loop_note", Sprites.NoteBloody, 60, 36, "바닥의 쪽지",
                    new[] { "엄마 방은 벽 너머에 있어.\n잘라 낸 자국을 찾아.\n\n손가락을 세어 봐." },
                    "절취선을 따라 지나가.\n자각하고 있을 때만 열려.")
                { Layer = -1, Bloody = true, Child = true };
                r.Add(note);
            }
            if (loops == 1 && ps.PrevRoomId == "d_loop")
                OnStep(r, "loop1", 0, 0, 40, 48, () => SayLines(g, Speaker.Think, new[] { "…또 복도다.", "아까랑 똑같아." }));
            if (loops == 3 && ps.PrevRoomId == "d_loop")
                OnStep(r, "loop3", 0, 0, 40, 48, () => SayLines(g, Speaker.Think, new[] { "끝이 없어. 계속 같은 복도야.", "…애들이 늘어나고 있어." }));
            if (g.Flags.Has("piece_mom"))
            {
                r.Add(new WallText("나가", 106, 18, Pal.BloodBright, true, true));
            }
        }

        // ═════════════════ 계단 ═════════════════

        static void SetupStairs(Room r, GameCore g)
        {
            var ps = PS(g);
            int n = r.Def.Id[r.Def.Id.Length - 1] - '0';
            string floor = n == 3 ? "F층" : "4층";
            r.Add(new WallText(floor, 30, 6, n == 3 ? Pal.BloodBright : Pal.LightGray, false, n == 3));
            r.Add(new WallText("비상구", 96, 6, Pal.Green, false, false));
            r.Lights.Add(new Light(96, 10, 22, 0.35f) { Flicker = 0.4f });
            r.Lights.Add(new Light(60, 50, 30, 0.18f) { Flicker = 0.85f });
            OnStep(r, "stairs_chase", 0, 0, 120, 96, () => Finale.StairsChase(g, n)).Blocking = false;
        }

        // ═════════════════ 가위의 방 ═════════════════

        static void SetupCut(Room r, GameCore g)
        {
            var ps = PS(g);
            float[] dx = { 24, 48, 112, 136, 30, 130, 70, 90 };
            float[] dy = { 30, 26, 28, 32, 92, 90, 34, 36 };
            for (int i = 0; i < dx.Length; i++)
            {
                var doll = new Prop("doll", Sprites.PaperDoll, dx[i], dy[i]);
                float phase = i * 1.3f;
                doll.CustomDraw = (p, c, ox, oy) =>
                {
                    float sway = M.Sin(ps.Room.Time * 0.9f + phase) * 1.5f;
                    int x = ox + (int)(p.X + sway), y = oy + (int)p.Y;
                    c.VLine(ox + (int)p.X, oy + (int)p.Y - 30, y - 11, Pal.DarkGray);
                    c.Blit(Sprites.PaperDoll, x, y);
                    c.Set(x, y - 9, Pal.Red);
                    c.Set(x, y - 8, Pal.DarkRed);
                };
                doll.OnInteract = () => SayLines(g, Speaker.Think, new[] { "오려 낸 사람. 이름이 적혀 있다.", "…모르는 이름이다. 아니, 기억나지 않는 이름이다." });
                r.Add(doll);
            }
            r.Add(new Prop("faces", Sprites.FacePile, 80, 64) { OnInteract = () => SayLines(g, Speaker.Think, new[] { "오려 낸 얼굴들이 산처럼 쌓여 있다." }) });
            r.Lights.Add(new Light(80, 60, 34, 0.35f) { Flicker = 0.35f });
            r.Lights.Add(new Light(30, 40, 18, 0.2f) { Flicker = 0.6f });
            r.Lights.Add(new Light(130, 80, 18, 0.2f) { Flicker = 0.6f });
            if (!g.Flags.Has("boss_done"))
                OnStep(r, "boss_start", 0, 0, 160, 110, () => Finale.BossIntro(g));
        }
    }
}
