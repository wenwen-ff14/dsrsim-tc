using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P3Wyrmhole;

internal static class BossFacingValidation
{
    private static float Difference(float a, float b) => MathF.Abs(MathF.IEEERemainder(a - b, MathF.Tau));

    public static void Run()
    {
        var wrapped = DsrP3WyrmholeScenario.TurnTowards(MathF.PI - .05f, -MathF.PI + .05f, .025f);
        if (Difference(wrapped, MathF.PI - .025f) > .0001f)
            throw new Exception("Rotation must cross the +/- pi boundary along the shortest arc");
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var scenario = new DsrP3WyrmholeScenario();
            var world = new SimWorld();
            scenario.UseSeed(9);
            scenario.Run(world, 0);
            var boss = world.Enemies.Single(e => e.BNpcBaseId == DsrP3WyrmholeConstants.Nidhogg);
            var turnFrames = 0;
            var locked = 0f;
            var sawTurn = false;
            var sawCast = false;
            for (var frame = 1; frame <= 55 * fps; frame++)
            {
                var time = SimCharacter.Time = frame / (float)fps;
                var previous = boss.Rotation;
                world.Events.Tick(1f / fps);
                foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / fps);
                if (time is > 49 and < 50)
                {
                    world.Party.Get(0)!.SetPosition(new(10, 0, 0));
                    world.Party.Get(0)!.SetRotation(-MathF.PI / 2);
                }
                scenario.Tick(1f / fps, time);
                if (time is > 49.6f and < 50 && Difference(boss.Rotation, MathF.PI / 2) > .001f)
                    throw new Exception("Boss must follow MT position, regardless of the tank's own facing");
                if (time is > 48.05f and < 54.6f && Difference(boss.Rotation, previous) > MathF.Tau * 1.3f / fps + .001f)
                    throw new Exception($"Boss snapped during auto/lance transition at {time:F3}, {fps} FPS");
                if (scenario.State.TurningForLance)
                {
                    if (!sawTurn) { locked = scenario.State.LanceRotation; sawTurn = true; }
                    turnFrames++;
                    if (scenario.State.TrackingMainTank || boss.Casts.Any(c => c.Action == DsrP3WyrmholeConstants.Drachenlance))
                        throw new Exception("Lance must turn before casting and stop tracking MT");
                    world.Party.Get(scenario.State.LanceTarget)!.SetPosition(new(-19, 0, 0));
                    if (Difference(locked, scenario.State.LanceRotation) > .001f)
                        throw new Exception("Lance turn target must remain fixed after selection");
                }
                if (boss.Casts.Any(c => c.Action == DsrP3WyrmholeConstants.Drachenlance))
                {
                    sawCast = true;
                    if (!sawTurn || Difference(boss.Rotation, locked) > .001f)
                        throw new Exception("Lance cast started without finishing its turn");
                }
            }
            if (!sawCast || turnFrames < fps / 2)
                throw new Exception("Missing smooth pre-cast turn");
        }
        Console.WriteLine("MT tracking, shortest-arc smooth turn, pre-cast timing and locked lance direction passed at 30/60/144 FPS.");
    }
}
