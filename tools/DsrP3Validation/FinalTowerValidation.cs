using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P3Wyrmhole;

internal static class FinalTowerValidation
{
    public static void Run()
    {
        // Tuuf's starting-position diagram: NW MT/D3, NE ST/D4, SE D2/H2, SW D1/H1.
        int[] homes = [0, 1, 3, 2, 3, 2, 0, 1];
        for (var role = 0; role < 8; role++)
            if (DsrP3WyrmholeState.FinalTowerHome(role) != homes[role])
                throw new Exception($"Tuuf starting tower mismatch for role {role}");
        var examples = new (int[] Counts, int Role, int Expected)[]
        {
            ([1, 3, 1, 3], 0, 1),
            ([1, 3, 1, 3], 5, 3),
            ([1, 4, 1, 2], 5, 1),
            ([3, 2, 1, 2], 5, 0)
        };
        foreach (var example in examples)
        {
            var state = new DsrP3WyrmholeState(0);
            example.Counts.CopyTo(state.FinalTowerCounts, 0);
            if (state.FinalTowerAssignment(example.Role) != example.Expected)
                throw new Exception("Tuuf clockwise/counterclockwise/across priority mismatch");
            foreach (var fixedRole in new[] { 2, 3, 6, 7 })
                if (state.FinalTowerAssignment(fixedRole) != homes[fixedRole])
                    throw new Exception("Healers and ranged must stay in their original towers");
        }
        var patterns = 0;
        for (var a = 1; a <= 4; a++)
        for (var b = 1; b <= 4; b++)
        for (var c = 1; c <= 4; c++)
        {
            var d = 8 - a - b - c;
            if (d is < 1 or > 4) continue;
            patterns++;
            foreach (var fps in new[] { 30, 60, 144 })
            {
                var scenario = new DsrP3WyrmholeScenario();
                scenario.UseSeed(patterns);
                var world = new SimWorld();
                SimCharacter.Time = 0;
                SimCharacter.Failures.Clear();
                scenario.Run(world, 0);
                var actors = world.Enemies.Where(e => e.BNpcBaseId == DsrP3WyrmholeConstants.Drake).ToArray();
                if (actors.Length != 12 || actors.Any(e => e.Visible || e.Casts.Count != 0))
                    throw new Exception("Dive actors must preload hidden, without firing actions at spawn");
                int[] counts = [a, b, c, d];
                counts.CopyTo(scenario.State.FinalTowerCounts, 0);
                for (var tower = 0; tower < 4; tower++)
                    if (Enumerable.Range(0, 8).Count(r => scenario.State.FinalTowerAssignment(r) == tower) != counts[tower])
                        throw new Exception($"Four-tower assignment failed: {string.Join(',', counts)}");
                for (var frame = 1; frame <= 99 * fps; frame++)
                {
                    SimCharacter.Time = frame / (float)fps;
                    world.Events.Tick(1f / fps);
                    foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / fps);
                    scenario.Tick(1f / fps, SimCharacter.Time);
                    if (scenario.State.Time < 59.590f && scenario.State.FinalTowersVisible)
                        throw new Exception("Final tower pattern revealed early");
                }
                if (!scenario.State.FinalTowersResolved || !scenario.State.Complete || scenario.State.FinalTowersVisible ||
                    SimCharacter.Failures.Count != 0 || actors.Any(e => e.Visible))
                    throw new Exception($"Final towers {string.Join(',', counts)} failed at {fps} FPS: {string.Join(';', SimCharacter.Failures)}");
                for (var tower = 0; tower < 4; tower++)
                    if (actors[tower + 8].Casts.First().Action != 26390 + counts[tower])
                        throw new Exception("Tower action does not match its required player count");
            }
        }
        foreach (var extra in new[] { false, true })
        {
            var scenario = new DsrP3WyrmholeScenario();
            scenario.UseSeed(0);
            var world = new SimWorld();
            scenario.Run(world, 0);
            Array.Fill(scenario.State.FinalTowerCounts, 2);
            SimCharacter.Failures.Clear();
            for (var frame = 1; frame <= 99 * 60; frame++)
            {
                SimCharacter.Time = frame / 60f;
                if (SimCharacter.Time is > 64.4f and < 64.6f)
                    world.Party.Get(0)!.SetPosition(extra ? DsrP3WyrmholeState.FinalTowerPosition(1) : Vector3.Zero);
                world.Events.Tick(1f / 60);
                foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
                scenario.Tick(1f / 60, SimCharacter.Time);
            }
            if (!SimCharacter.Failures.Any(m => m.Contains("最後四塔")))
                throw new Exception("Final towers must reject missing/extra soakers");
        }
        if (Enumerable.Range(0, 300).Select(seed => new DsrP3WyrmholeState(seed).LanceTarget).Distinct().Count() != 8)
            throw new Exception("Lance target does not cover all party roles");
        Console.WriteLine($"All {patterns} four-tower patterns passed at 30/60/144 FPS; preload, tower action counts, cleanup, under/over-soaking and random lance targets passed.");
    }
}
