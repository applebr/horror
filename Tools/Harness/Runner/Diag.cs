using System;
using Gawi.Engine;
using Gawi.Game;

namespace Gawi.Harness
{
    public static partial class Program
    {
        static int MomTest()
        {
            var s = NewSim("momtest");
            s.Tick(5);
            Warp(s, "d_loop", "from_west", "dream", "has_light", "first_check", "can_check", "stalker_on", "piece_bath", "piece_kitchen");
            s.P.LucidT = 20;
            s.ExitVia('7');
            s.AdvanceUntil(() => s.G.Flags.Has("mom_seen") && !s.P.Player.Frozen && !s.G.Dialogue.Active, 20, "enter");
            Console.WriteLine("entered at " + s.P.Player.X + "," + s.P.Player.Y);
            for (int i = 0; i < 60 * 20 && !s.P.Dying; i++)
            {
                var pl = s.P.Player;
                string kids = "";
                foreach (var e in s.P.Room.Entities)
                {
                    var k = e as FacelessKid;
                    if (k == null) continue;
                    kids += string.Format(" [{0:0},{1:0} d={2:0} lit={3}]", k.X, k.Y, M.Dist(k.X, k.Y, pl.X, pl.Y), k.Frozen ? 1 : 0);
                }
                if (i % 30 == 0) Console.WriteLine(string.Format("t={0:0.0} pl={1:0},{2:0} aim={3:0.0},{4:0.0} beam={5} bat={6:0.00}{7}", i / 60f, pl.X, pl.Y, pl.AimX, pl.AimY, s.P.Light.BeamActive, pl.Battery, kids));
                if (WalkDefensiveStep(s, 46, 31)) { Console.WriteLine("reached"); break; }
            }
            Console.WriteLine("dying=" + s.P.Dying + " deaths=" + s.G.Flags.Get("deaths"));
            return 0;
        }

        static bool WalkDefensiveStep(Sim s, float tx, float ty)
        {
            var pl = s.P.Player;
            if (Math.Abs(pl.X - tx) < 2.5f && Math.Abs(pl.Y - ty) < 2.5f) { s.ReleaseAll(); return true; }
            Entity threat = null;
            float best = 999;
            foreach (var e in s.P.Room.Entities)
            {
                if (e.Removed || !(e is FacelessKid)) continue;
                float d = M.Dist(e.X, e.Y, pl.X, pl.Y);
                if (d < 34 && d < best) { best = d; threat = e; }
            }
            if (threat != null && pl.Battery > 0.1f)
            {
                Aim(s, threat.X - pl.X, (threat.Y - 6) - pl.ChestY);
                s.Set(Btn.Light, true);
                s.Tick();
                return false;
            }
            s.Set(Btn.Light, false);
            s.WalkTo(tx, ty, 0.1f, 2.5f);
            return false;
        }
    }
}
namespace Gawi.Harness
{
    public static partial class Program
    {
        static int BossTest()
        {
            var s = NewSim("bosstest");
            s.Tick(5);
            Warp(s, "d_stairs3", "bottom", "dream", "has_light", "first_check", "can_check", "photo_done", "chase_scripted");
            s.ExitVia('1');
            s.AdvanceUntil(() => s.G.Flags.Has("boss") && !s.P.Player.Frozen && !s.G.Dialogue.Active, 60, "boss start");
            System.Console.WriteLine("fight start. lucid=" + s.P.LucidT);
            for (int i = 0; i < 60 * 60 && !s.G.Flags.Has("boss_done"); i++)
            {
                Gawi.Game.ScissorMan boss = null;
                foreach (var e in s.P.Room.Entities) if (e is Gawi.Game.ScissorMan && !e.Removed) boss = (Gawi.Game.ScissorMan)e;
                var pl = s.P.Player;
                if (i >= 150 && i <= 260 && i % 4 == 0)
                {
                    string all = "";
                    foreach (var e in s.P.Room.Entities) if (e is Gawi.Game.ScissorMan) { var b = (Gawi.Game.ScissorMan)e; all += string.Format(" [{0:0},{1:0} {2} rem={3} exp={4:0.00} spd={5} mul={6} paused={7}]", b.X, b.Y, b.State, b.Removed, b.Exposure, b.Speed, b.SpeedMul, s.P.WorldPaused); }
                    System.Console.WriteLine(string.Format("   f={0} pl={1:0},{2:0} {3}", i, pl.X, pl.Y, all));
                }
                if (i % 20 == 0 && boss != null)
                    System.Console.WriteLine(string.Format("t={0:0.0} pl={1:0},{2:0} aim={3:0.0},{4:0.0} light={5} beam={6} bat={7:0.00} lucid={8:0.0} boss={9:0},{10:0} st={11} exp={12:0.00} dying={13} frozen={14} busy={15} dlg={16}",
                        i / 60f, pl.X, pl.Y, pl.AimX, pl.AimY, pl.LightOn, s.P.Light.BeamActive, pl.Battery, s.P.LucidT, boss.X, boss.Y, boss.State, boss.Exposure, s.P.Dying, pl.Frozen, s.P.ScriptBusy, s.G.Dialogue.Active));
                if (s.P.Dying) { s.Tick(); continue; }
                if (s.G.Dialogue.Active && !s.G.Dialogue.IsTimed) { s.ReleaseAll(); s.AdvanceOne(); continue; }
                if (boss == null) { s.Tick(); continue; }
                float dist = Gawi.Engine.M.Dist(boss.X, boss.Y, pl.X, pl.Y);
                if (!s.P.Lucid || s.P.LucidT < 1.0f)
                {
                    if (dist < 70 && boss.State != Gawi.Game.ScissorMan.St.Stunned)
                    {
                        s.ReleaseAll();
                        float ax = pl.X - boss.X, ay = pl.Y - boss.Y;
                        s.WalkTo(Gawi.Engine.M.Clamp(pl.X + ax * 2, 10, 150), Gawi.Engine.M.Clamp(pl.Y + ay * 2, 20, 110), 0.15f, 3f);
                        continue;
                    }
                    s.ReleaseAll(); s.Press(Gawi.Engine.Btn.Check); s.Seconds(0.3f);
                    while (s.P.Player.Counting && !s.P.Dying) s.Tick();
                    continue;
                }
                if (pl.Battery < 0.3f && !pl.LightOn && dist < 60)
                {
                    s.ReleaseAll();
                    float ax = pl.X - boss.X, ay = pl.Y - boss.Y;
                    s.WalkTo(Gawi.Engine.M.Clamp(pl.X + ax * 2, 10, 150), Gawi.Engine.M.Clamp(pl.Y + ay * 2, 20, 110), 0.15f, 3f);
                    continue;
                }
                Aim(s, boss.X - pl.X, (boss.Y - 10) - pl.ChestY);
                s.Set(Gawi.Engine.Btn.Light, pl.Battery > 0.02f && (dist < 58 || boss.State == Gawi.Game.ScissorMan.St.Recoil));
                s.Tick();
            }
            System.Console.WriteLine("done=" + s.G.Flags.Has("boss_done") + " deaths=" + s.G.Flags.Get("deaths"));
            return 0;
        }
    }
}
