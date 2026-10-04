using System;
using System.Collections;
using Gawi.Engine;
using Gawi.Engine.Audio;

namespace Gawi.Game
{
    /// <summary>사진 맞추기 → TV → 계단 추격 → 보스 → 엔딩.</summary>
    public static class Finale
    {
        static PlayState PS(GameCore g) { return g.Play; }

        // ═════════════════ 사진 ═════════════════

        public static IEnumerator AssemblePhoto(GameCore g)
        {
            var ps = PS(g);
            ps.Player.Frozen = true;
            g.Flags.Set("stalker_off");
            ps.Stalker.Reset();
            yield return g.Say(Speaker.Think, "조각을… 맞춰 보자.");
            g.Audio.StopAllMusic(1.5f);
            yield return g.PlayCutscene(new PhotoCutscene());
            g.Flags.Set("photo_done");
            var frame = ps.Room.Find<Prop>("frame");
            if (frame != null) frame.Spr = Sprites.PhotoFrame;
            yield return g.Wait(0.8f);
            // TV가 저절로 켜진다
            g.Audio.Play(Sfx.TvOn, 0.9f);
            ps.Player.Face(56, 30);
            yield return g.Wait(1.2f);
            yield return g.PlayCutscene(new TvCutscene());
            yield return g.Wait(0.5f);
            g.Audio.Play(Sfx.Thump, 1f, 0.7f);
            g.Fx.Shake(4, 0.6f);
            yield return g.Wait(0.4f);
            g.Audio.Play(Sfx.Lock, 0.8f, 0.8f);
            yield return g.Say(Speaker.Think, "…현관문이 열렸다.");
            yield return g.Say(Speaker.Think, "가야 해. 숨기만 하면… 나도 오려질 거야.");
            ps.Hud.Objective("현관 밖으로");
            ps.SetCheckpoint("d_living", Story_Cx(31), Story_Cy(24), true);
            g.Audio.SetMusic(Ch.Ambience, Music.AmbNightmare, 0.7f, 2f);
            ps.Player.Frozen = false;
        }

        static float Story_Cx(float cx) { return cx * Room.CS + Room.CS * 0.5f; }
        static float Story_Cy(float cy) { return cy * Room.CS + Room.CS - 0.5f; }

        // ═════════════════ 계단 ═════════════════

        public static IEnumerator StairsChase(GameCore g, int n)
        {
            var ps = PS(g);
            g.Flags.Set("chase_scripted");
            bool retry = g.Flags.Has("stairs_seen");
            if (n == 1 && !retry)
            {
                g.Flags.Set("stairs_seen");
                // 다시 시작할 때는 계단 쪽에 더 가까운 곳에서
                var rp = ps.SpawnPoint("d_stairs1", "retry");
                ps.SetCheckpoint("d_stairs1", rp.X, rp.Y, true);
                ps.Player.Frozen = true;
                yield return g.Wait(0.3f);
                yield return g.Say(Speaker.Think, "계단… 4층.");
                yield return g.Say(Speaker.Think, "우리 아파트에는 4층이 없는데. F층인데.");
                ps.Player.Frozen = false;
            }
            // 위쪽 문에서 다가오는 발소리 → 문이 열린다
            g.Audio.Play(Sfx.Thump, 0.5f, 0.6f);
            yield return g.Wait(n == 1 ? (retry ? 1.8f : 1.2f) : 0.8f);
            if (ps.Room == null || !ps.Room.Def.Id.StartsWith("d_stairs") || ps.Dying) yield break;
            // 위쪽 문에서 가위가 내려온다
            g.Audio.Play(Sfx.DoorOpen, 0.9f, 0.6f);
            var sm = ps.Stalker.Appear(Story_Cx(14), Story_Cy(4.5f));
            sm.Speed = 29f + n * 1.5f;
            sm.CanLunge = false;
            g.Fx.StaticFlicker = 0.4f;
            if (n == 3)
            {
                yield return g.Wait(0.5f);
                g.Dialogue.Open(Speaker.Gawi, "어디 가니, 하나야.", 2.0f);
            }
        }

        // ═════════════════ 보스 ═════════════════

        public static IEnumerator BossIntro(GameCore g)
        {
            var ps = PS(g);
            g.Flags.Set("chase_scripted");
            g.Flags.Set("boss");
            ps.Player.Frozen = true;
            var start = ps.SpawnPoint("d_cut", "top");
            ps.SetCheckpoint("d_cut", start.X, start.Y, true);
            g.Audio.StopAllMusic(1f);
            bool retry = g.Flags.Has("boss_seen");
            g.Flags.Set("boss_seen");
            yield return g.Wait(retry ? 0.4f : 1.0f);
            if (!retry)
            {
                yield return g.Say(Speaker.Think, "…여기는.");
                yield return g.Say(Speaker.Think, "오려 낸 사람들이 매달려 있다.");
            }
            var sm = new ScissorMan { X = 80, Y = 40, Dir = 0 };
            sm.State = ScissorMan.St.Scripted;
            sm.CanKill = false;
            ps.Spawn(sm);
            g.Audio.Play(Sfx.Snip, 1f);
            g.Fx.Shake(2, 0.3f);
            yield return g.Wait(retry ? 0.3f : 0.8f);
            if (!retry)
            {
                yield return g.Say(Speaker.Gawi, "…하나.");
                yield return g.Say(Speaker.Gawi, "넌 이제 없는 아이야.");
                yield return g.Say(Speaker.Gawi, "아무도 너를 기억하지 않아. 네 엄마도. 네 아빠처럼.");
                yield return g.Say(Speaker.Hana, "…아니야.");
                yield return g.Say(Speaker.Hana, "이건 내 꿈이야.");
            }
            else yield return g.Say(Speaker.Gawi, "또 왔구나.");
            yield return g.Say(Speaker.Hana, "꿈인 걸 알면… 하나도 안 무서워!");
            ps.BecomeLucid(true);
            ps.Player.Battery = 1f;
            if (!retry) yield return g.Say(Speaker.Sys, "자각한 채로 손전등(X)을 비춰 가위를 쓰러뜨려라. 자각이 흐려지면 다시 손가락을 세자(C).");
            ps.Player.Frozen = false;
            g.Audio.SetMusic(Ch.Music, Music.Boss, 0.8f, 0.5f);
            ps.RunAmbient(BossFight(g, sm));
        }

        static IEnumerator BossFight(GameCore g, ScissorMan sm)
        {
            var ps = PS(g);
            int hits = 0;
            bool hitNow = false;
            sm.State = ScissorMan.St.Stalk;
            sm.CanKill = true;
            sm.Speed = 25f;
            sm.CanLunge = true;
            sm.RepelTime = 1.5f;
            sm.LungeCooldownMin = 3.0f; sm.LungeCooldownMax = 4.2f;
            sm.OnRepelled = () => { hitNow = true; };
            ps.LucidMax = 22f;
            ps.Player.RechargeMul = 1.8f;
            float crawlerT = 6f;
            while (!ps.Dying && hits < 3)
            {
                if (hitNow)
                {
                    hitNow = false;
                    hits++;
                    g.Audio.Play(Sfx.Scream, 1f, 0.8f);
                    g.Audio.Play(Sfx.Gore, 0.8f);
                    g.Fx.Flash(Pal.Red, 3);
                    g.Fx.Shake(4, 0.6f);
                    g.Fx.Blood.Splash(160, 100, 0.25f + hits * 0.15f);
                    ps.Blood.Burst(sm.X, sm.Y, 16, 30, 40, 40);
                    ps.Blood.GrowPool(sm.X, sm.Y, 6, 4);
                    if (hits >= 3) break;
                    sm.SetState(ScissorMan.St.Stunned);
                    yield return g.Wait(0.6f);
                    // 다른 곳으로 순간이동
                    g.Fx.StaticFlicker = 0.8f;
                    float nx, ny;
                    for (int k = 0; k < 20; k++)
                    {
                        ps.Room.RandomFloor(g.Rng, out nx, out ny);
                        if (M.Dist(nx, ny, ps.Player.X, ps.Player.Y) > 75) { sm.X = nx; sm.Y = ny; break; }
                    }
                    sm.Exposure = 0;
                    sm.Speed = 25f + hits * 4f;
                    sm.SetState(ScissorMan.St.Stalk);
                    if (hits == 1) g.Dialogue.Open(Speaker.Gawi, "아파… 아프잖아…", 1.8f);
                    if (hits == 2)
                    {
                        g.Dialogue.Open(Speaker.Gawi, "가만히 있어. 예쁘게 오려 줄게.", 2.2f);
                        ps.Room.Ambient = 0.04f;
                    }
                }
                crawlerT -= GameCore.Dt;
                if (crawlerT <= 0 && hits >= 1)
                {
                    crawlerT = 9f - hits * 2f;
                    int cnt = 2 + hits;
                    for (int i = 0; i < cnt; i++)
                    {
                        float x, y;
                        ps.Room.RandomFloor(g.Rng, out x, out y);
                        if (M.Dist(x, y, ps.Player.X, ps.Player.Y) < 40) continue;
                        ps.Spawn(new Crawler(x, y, 0.5f + i * 0.2f));
                    }
                    g.Audio.Play(Sfx.Skitter, 0.7f);
                }
                yield return null;
            }
            if (ps.Dying) yield break;
            yield return BossDefeat(g, sm);
        }

        static IEnumerator BossDefeat(GameCore g, ScissorMan sm)
        {
            var ps = PS(g);
            ps.NoDeath = true;
            ps.Player.Frozen = true;
            ps.Player.LightOn = false;
            ps.Player.RechargeMul = 1f;
            sm.State = ScissorMan.St.Scripted;
            sm.CanKill = false;
            foreach (var e in ps.Room.Entities) if (e is Crawler) { e.Removed = true; ps.Blood.Burst(e.X, e.Y, 2, 6, 20, 20); }
            g.Audio.StopAllMusic(0.3f);
            g.Flags.Set("boss_done");
            yield return g.Wait(0.6f);
            yield return g.PlayCutscene(new BossDeathCutscene());
            sm.Remove();
            yield return g.PlayCutscene(new CutDreamCutscene());
            yield return BeginEnding(g);
        }

        // ═════════════════ 엔딩 ═════════════════

        static IEnumerator BeginEnding(GameCore g)
        {
            var ps = PS(g);
            g.Flags.Set("ending");
            g.Flags.Set("stalker_off");
            ps.NoDeath = true;
            ps.LucidT = 0;
            ps.Player.HasLight = false;
            g.Fx.Reset();
            g.Fx.Fade = 1f;
            g.Fx.Vignette = 0.3f;
            ps.Player.Lying = true;
            ps.LoadRoom("m_hana", 12, 36, 0);
            var head = new Prop("hana_bed", Sprites.HanaInBed, 12, 21) { Layer = 1 };
            ps.Spawn(head);
            ps.Player.Frozen = true;
            yield return g.Wait(2.0f);
            yield return g.FadeIn(3.0f);
            yield return g.Wait(1.0f);
            yield return g.Say(Speaker.Hana, "…아침이다.");
            yield return g.Wait(0.6f);
            yield return g.Say(Speaker.Think, "끝났어. 진짜로… 깼어.");
            head.Removed = true;
            ps.Player.Lying = false;
            var bed = ps.SpawnPoint("m_hana", "bed");
            ps.Player.X = bed.X; ps.Player.Y = bed.Y;
            yield return g.Wait(0.8f);
            yield return g.Say(Speaker.Mom, "(부엌에서) 밥 먹어라~");
            yield return g.Say(Speaker.Hana, "엄마!");
            ps.Hud.Objective("엄마한테 가자");
            ps.Player.Frozen = false;
        }

        public static IEnumerator EndingKitchen(GameCore g)
        {
            var ps = PS(g);
            var mom = ps.Room.Find<Actor>("mom");
            if (mom == null) yield break;
            g.Flags.Set("end_mom_talked");
            ps.Player.Frozen = true;
            ps.Player.Face(mom.X, mom.Y);
            g.Audio.StopMusic(Ch.Ambience, 3f);
            yield return g.Say(Speaker.Hana, "엄마! 엄마, 나 무서운 꿈 꿨어.");
            yield return g.Say(Speaker.Hana, "근데 이겼어! 손가락 세는 거, 진짜 됐어!");
            yield return g.Wait(0.8f);
            mom.Dir = 0;
            yield return g.Wait(1.2f);
            yield return g.Say(Speaker.Mom, "……");
            yield return g.Wait(0.6f);
            yield return g.Say(Speaker.Mom, "……얘, 너 누구니?");
            yield return g.Wait(0.8f);
            yield return g.Say(Speaker.Hana, "…엄마?");
            yield return g.Say(Speaker.Mom, "우리 집에 어떻게 들어왔어? 문 잠겨 있었는데…");
            yield return g.Say(Speaker.Mom, "여보! 여보, 이리 좀 와 봐요!");
            yield return g.Wait(0.8f);
            g.Audio.Play(Sfx.Step, 0.5f, 0.8f);
            yield return g.Wait(0.4f);
            g.Audio.Play(Sfx.Step, 0.55f, 0.8f);
            yield return g.Say(Speaker.Dad, "(복도에서) 응? 무슨 일이야?");
            yield return g.Say(Speaker.Think, "……아빠?");
            yield return g.Say(Speaker.Think, "아빠 목소리야. 처음 듣는데… 알 것 같아.");
            yield return g.Say(Speaker.Think, "아니야. 이건 꿈이야. 꿈이어야 해.");
            yield return g.Say(Speaker.Think, "손가락… 손가락을 세어 봐.");
            yield return Story.WaitForCheck(g, "C 키: 손가락 세기");
            g.Input.Consume(Btn.Check);
            g.Dialogue.Close();
            yield return g.PlayCutscene(new FingerCutscene(5, FingerCutscene.Mode.Ending));
            yield return g.Say(Speaker.Think, "……다섯.");
            yield return g.Wait(0.8f);
            yield return g.Say(Speaker.Think, "꿈이… 아니야.");
            yield return g.Wait(1.0f);
            yield return g.FadeOut(2.5f);
            yield return g.Wait(1.0f);
            yield return g.PlayCutscene(new EndingPhotoCutscene());
            yield return g.PlayCutscene(new TitleCard("가 위", "1장 · 404호 — 끝", true));
            g.DeleteSave();
            g.Store.Set("cleared", "1");
            g.Store.Flush();
            g.SetState(new CreditsState());
        }
    }
}
