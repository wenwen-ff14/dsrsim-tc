using System.Text.Json;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P2Sanctity;

internal static class LogTimingValidation
{
    public static void Run()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "p2-fight42.json")));
        var data = fixture.RootElement;
        var origin = data.GetProperty("originP2Seconds").GetSingle();
        foreach (var frameRate in new[] { 30, 60, 144 })
        {
            var scenario = new DsrP2SanctityScenario();
            scenario.UseSeed(0);
            var world = new SimWorld();
            SimCharacter.Time = 0;
            scenario.Run(world, 0);
            var impacts = new List<float>();
            var gaze = 0f;
            for (var frame = 0; frame < 62 * frameRate; frame++)
            {
                SimCharacter.Time = (frame + 1f) / frameRate;
                var before = scenario.CurrentState.Stage;
                var meteorCount = scenario.CurrentState.MeteorCount;
                world.Events.Tick(1f / frameRate);
                if (scenario.CurrentState.Stage == SanctityStage.Charges && before != SanctityStage.Charges)
                    gaze = SimCharacter.Time;
                if (scenario.CurrentState.MeteorCount != meteorCount) impacts.Add(SimCharacter.Time);
                foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / frameRate);
                scenario.Tick(1f / frameRate, SimCharacter.Time);
            }
            var tolerance = 1f / frameRate + .003f;
            var casts = world.Enemies.SelectMany(e => e.Casts).ToArray();
            foreach (var expected in data.GetProperty("casts").EnumerateArray())
            {
                var id = expected.GetProperty("action").GetUInt32();
                var begin = expected.GetProperty("begin").GetSingle() - origin;
                var release = expected.GetProperty("release").GetSingle() - origin;
                var duration = expected.GetProperty("castSeconds").GetSingle();
                var actual = casts.Where(c => c.Action == id && MathF.Abs(c.Time - begin) <= tolerance).ToArray();
                if (actual.Length != expected.GetProperty("count").GetInt32() || actual.Any(c =>
                    c.Duration == null || MathF.Abs(c.Duration.Value - duration) > .001f ||
                    MathF.Abs(c.Time + c.Duration.Value + c.FireDelay - release) > tolerance))
                    throw new Exception($"FFLogs cast mismatch at {frameRate} FPS: action {id}, P2 {begin + origin:F3}s");
            }
            var firstCharge = casts.Where(c => c.Action == 25570).Min(c => c.Time);
            if (MathF.Abs(gaze - firstCharge) > .0001f)
                throw new Exception("Gaze and first charge must resolve in the same frame");
            var expectedImpacts = data.GetProperty("meteorImpacts").EnumerateArray().Select(x => x.GetSingle()).ToArray();
            if (impacts.Count != expectedImpacts.Length || impacts.Where((time, i) => MathF.Abs(time + origin - expectedImpacts[i]) > tolerance).Any())
                throw new Exception("Meteor impact timing differs from FFLogs damage events");
        }
        Console.WriteLine("FFLogs fight 42: 24 cast groups and seven meteor impacts match at 30/60/144 FPS; gaze snapshot shares the first charge frame.");
    }
}
