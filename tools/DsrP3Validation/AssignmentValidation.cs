using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P3Wyrmhole;

internal static class AssignmentValidation
{
    public static void Run()
    {
        var directions = new HashSet<int>();
        for (var seed = 0; seed < 100; seed++)
        for (var player = 0; player < 8; player++)
        for (var number = 0; number <= 3; number++)
        for (var arrows = 0; arrows <= 4; arrows++)
        {
            var s = new DsrP3WyrmholeState(seed);
            var original = (int[])s.Order.Clone();
            s.SetPlayerAssignment(player, number, arrows);
            if (number > 0 && s.Order[player] != number - 1) throw new Exception("number override");
            if (arrows == 1 && s.Direction[player] != 0 || arrows == 2 && s.Direction[player] == 0 || arrows == 3 && s.Direction[player] != 1 || arrows == 4 && s.Direction[player] != -1) throw new Exception("arrow override");
            if (number == 0 && arrows == 0 && !original.SequenceEqual(s.Order)) throw new Exception("default changed");
            if (s.Arrows.Count(a => a) is not (1 or 2)) throw new Exception("invalid arrow group count");
            if (arrows == 2) directions.Add(s.Direction[player]);
            for (var wave = 0; wave < 3; wave++)
            {
                var roles = Enumerable.Range(0, 8).Where(r => s.Order[r] == wave).ToArray();
                var expected = wave == 1 ? 2 : 3;
                if (roles.Length != expected || roles.Select(r => s.Lane[r]).Distinct().Count() != expected || roles.Select(r => s.NumberLane[r]).Distinct().Count() != expected) throw new Exception("assignment duplicated");
                foreach (var r in roles)
                    if (Vector3.Distance(DsrP3WyrmholeState.Landing(s.JumpPosition(r), MathF.PI / 2, s.Direction[r]), s.Towers[wave][s.LandingLane(r)]) > .001f) throw new Exception("bad landing");
            }
        }
        if (directions.Count != 2) throw new Exception("random arrow direction biased");
        for (var number = 0; number <= 3; number++)
        for (var arrows = 0; arrows <= 4; arrows++)
        for (var seed = 0; seed < 6; seed++)
        {
            var scenario = new DsrP3WyrmholeScenario();
            scenario.UseSeed(seed);
            scenario.UseOptions(number, arrows);
            var world = new SimWorld();
            SimCharacter.Failures.Clear();
            scenario.Run(world, 0);
            for (var frame = 1; frame <= 99 * 60; frame++)
            {
                SimCharacter.Time = frame / 60f;
                world.Events.Tick(1f / 60);
                foreach (var m in world.Party.Slots) m.Advance(1f / 60);
                scenario.Tick(1f / 60, SimCharacter.Time);
            }
            if (SimCharacter.Failures.Count != 0 || !scenario.State.Complete) throw new Exception($"options {number}/{arrows} seed {seed}: {string.Join(";", SimCharacter.Failures)}");
        }
        Console.WriteLine("PASS: 16,000 P3 assignment configurations + 120 full runs with overrides.");
    }
}
namespace AnoMech.Scenarios.Dsr.P3Wyrmhole
{
    public sealed partial class DsrP3WyrmholeScenario
    {
        internal void UseOptions(int number, int arrows) { playerNumber = number; playerArrows = arrows; }
    }
}
