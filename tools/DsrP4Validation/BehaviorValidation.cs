using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P4Eyes;
internal static class BehaviorValidation
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        for (var player = 0; player < 8; player++)
        for (var seed = 0; seed < 6; seed++)
        {
            var s = new DsrP4EyesScenario(); s.UseSeed(seed);
            var w = new SimWorld();
            w.Party.Slots[player] = new SimPlayer { Role = player, Position = new(0, 0, 16) };
            SimCharacter.Failures.Clear(); s.Run(w, 0);
            for (var f = 1; f <= 87 * 60; f++)
            {
                SimCharacter.Time = f / 60f;
                w.Party.Slots[player].MoveTo(DsrP4EyesAi.Destination(s.State, player), 6);
                var commands = w.Party.Slots[player].MoveCommands;
                w.Events.Tick(1f / 60);
                foreach (var m in w.Party.Slots) m.Advance(1f / 60);
                s.Tick(1f / 60, SimCharacter.Time);
                Check(w.Party.Slots[player].MoveCommands == commands, "scenario moves real player");
            }
            Check(SimCharacter.Failures.Count == 0, $"player {player} seed {seed}: {string.Join(";", SimCharacter.Failures)}");
            Check(w.Enemies.All(e => !e.Visible), "end leaves visible actors");
            Check(w.Party.Slots.All(m => !m.HasStatus(DsrP4EyesConstants.RedStatus) && !m.HasStatus(DsrP4EyesConstants.BlueStatus)), "end leaves colour status");
        }
        var scenario = new DsrP4EyesScenario(); var world = new SimWorld();
        scenario.Run(world, 0); scenario.PrepareColors();
        for (var r = 0; r < 8; r++) world.Party.Slots[r].Position = new(30 + 10 * r, 0, 30);
        scenario.State.Red[0] = true; scenario.State.Red[1] = false;
        world.Party.Slots[0].Position = world.Party.Slots[1].Position = Vector3.Zero;
        scenario.Tick(.01f, 34);
        Check(!scenario.State.Red[0] && scenario.State.Red[1], "contact did not swap");
        scenario.Tick(1, 35);
        Check(!scenario.State.Red[0] && scenario.State.Red[1], "swap lock not enforced");
        scenario.Tick(2.1f, 37.1f);
        Check(scenario.State.Red[0] && !scenario.State.Red[1], "swap lock did not expire");
        Check(world.Party.Slots[0].HasStatus(DsrP4EyesConstants.RedStatus) && world.Party.Slots[1].HasStatus(DsrP4EyesConstants.BlueStatus), "status mismatch after swaps");
        foreach (var kind in new[] { "early", "solo", "blue", "overlap" })
        {
            var s = new DsrP4EyesScenario(); var w = new SimWorld(); s.Run(w, 0); s.PrepareColors();
            for (var r=0;r<8;r++) w.Party.Slots[r].Position = new(30+10*r,0,30);
            SimCharacter.Failures.Clear();
            s.State.Red[0] = s.State.Red[1] = kind != "blue";
            s.State.YellowReady = kind != "early";
            w.Party.Slots[0].Position = DsrP4EyesState.OrbPosition(0) + Vector3.UnitZ;
            if (kind != "solo") w.Party.Slots[1].Position = DsrP4EyesState.OrbPosition(0) - Vector3.UnitZ;
            if (kind == "overlap")
            {
                for (var r=0;r<8;r++) { s.State.Red[r] = r >= 4; w.Party.Slots[r].Position = DsrP4EyesState.BlueEye; }
                s.DiveTest();
            }
            else s.Tick(0, 40);
            var expected = kind switch { "early" => "撞球過早", "solo" => "兩人", "blue" => "藍線承傷", _ => "範圍波及" };
            Check(SimCharacter.Failures.Any(f => f.Contains(expected)), $"missed {kind} failure");
        }
        var afk = new DsrP4EyesScenario(); var afkWorld = new SimWorld();
        afkWorld.Party.Slots[0] = new SimPlayer { Role=0, Position=new(0,0,16) };
        afk.Run(afkWorld,0); SimCharacter.Failures.Clear();
        for(var f=1;f<=18*60;f++) { afkWorld.Events.Tick(1f/60); foreach(var m in afkWorld.Party.Slots) m.Advance(1f/60); afk.Tick(1f/60,f/60f); }
        Check(SimCharacter.Failures.Any(f=>f.Contains("取得兩種思念")), "missed buffs not detected");
        Console.WriteLine("PASS: all 8 player roles; player ownership, cleanup, contact swaps and cooldown; early/solo/blue soaks, overlapping dives, missing buffs rejected.");
    }
}
namespace AnoMech.Scenarios.Dsr.P4Eyes
{
    public sealed partial class DsrP4EyesScenario
    {
        internal void PrepareColors() => AssignColors();
        internal void DiveTest() => Dive(0);
    }
}
