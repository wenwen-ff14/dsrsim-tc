using System.Text.Json;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P3Wyrmhole;

internal static class LogTimingValidation
{
    public static void Run()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "p3-fight42.json")));
        var data = fixture.RootElement;
        var origin = data.GetProperty("originFightSeconds").GetSingle();
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var world = new SimWorld();
            var scenario = new DsrP3WyrmholeScenario();
            scenario.UseSeed(0);
            SimCharacter.Time = 0;
            SimCharacter.Failures.Clear();
            scenario.Run(world, 0);
            Array.Fill(scenario.State.OutFirst, false);
            var numbers = 0f;
            var arrows = 0f;
            float[] removed = [0, 0, 0];
            float[] towersResolved = [0, 0, 0];
            for (var frame = 1; frame <= 60 * fps; frame++)
            {
                SimCharacter.Time = frame / (float)fps;
                var beforeNumbers = scenario.State.NumbersAssigned;
                var beforeArrows = scenario.State.ArrowsAssigned;
                var beforeTowers = scenario.State.TowersVisible.ToArray();
                world.Events.Tick(1f / fps);
                if (!beforeNumbers && scenario.State.NumbersAssigned) numbers = SimCharacter.Time;
                if (!beforeArrows && scenario.State.ArrowsAssigned) arrows = SimCharacter.Time;
                for (var wave = 0; wave < 3; wave++)
                {
                    if (beforeTowers[wave] && !scenario.State.TowersVisible[wave]) towersResolved[wave] = SimCharacter.Time;
                    if (scenario.State.NumbersAssigned && removed[wave] == 0 &&
                        !world.Party.Get(scenario.State.RoleAt(wave, 0))!.HasStatus((ushort)(3004 + wave)))
                        removed[wave] = SimCharacter.Time;
                }
                foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / fps);
                scenario.Tick(1f / fps, SimCharacter.Time);
            }
            var tolerance = 1f / fps + .003f;
            void Match(float actual, float expected, string label)
            {
                if (MathF.Abs(actual + origin - expected) > tolerance)
                    throw new Exception($"FFLogs P3 {label} mismatch at {fps} FPS: {actual + origin:F3} vs {expected:F3}");
            }
            Match(numbers, data.GetProperty("numbersApplied").GetSingle(), "numbers");
            Match(arrows, data.GetProperty("arrowsApplied").GetSingle(), "arrows");
            for (var wave = 0; wave < 3; wave++)
                Match(removed[wave], data.GetProperty("statusesRemoved")[wave].GetSingle(), $"wave {wave + 1} status removal");
            var casts = world.Enemies.SelectMany(e => e.Casts).ToArray();
            foreach (var expected in data.GetProperty("casts").EnumerateArray())
            {
                var ids = expected.GetProperty("actions").EnumerateArray().Select(x => x.GetUInt32()).ToArray();
                var begin = expected.GetProperty("begin").GetSingle();
                var release = expected.GetProperty("release").GetSingle();
                var duration = expected.GetProperty("castSeconds").GetSingle();
                var actual = casts.Where(c => ids.Contains(c.Action) && MathF.Abs(c.Time + origin - begin) <= tolerance).ToArray();
                if (actual.Length != expected.GetProperty("count").GetInt32() || actual.Any(c =>
                    c.Duration == null || MathF.Abs(c.Duration.Value - duration) > .001f ||
                    MathF.Abs(c.Time + c.Duration.Value + c.FireDelay + origin - release) > tolerance))
                    throw new Exception($"FFLogs P3 cast mismatch at {fps} FPS: {string.Join('/', ids)}, fight {begin:F3}s");
            }
            var expectedTowers = data.GetProperty("casts").EnumerateArray()
                .Where(x => x.GetProperty("actions")[0].GetUInt32() == 26385).ToArray();
            for (var wave = 0; wave < 3; wave++)
                Match(towersResolved[wave], expectedTowers[wave].GetProperty("release").GetSingle(), $"wave {wave + 1} tower resolution");
            if (SimCharacter.Failures.Count != 0)
                throw new Exception($"P3 AI route failed at {fps} FPS: {string.Join(';', SimCharacter.Failures.Distinct())}");
        }
        Console.WriteLine("FFLogs fight 42: 20 P3 cast groups, number/arrow timing, status removal and tower resolution match at 30/60/144 FPS.");
    }
}
