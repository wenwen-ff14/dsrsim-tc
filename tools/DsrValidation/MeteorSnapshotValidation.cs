using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr;
using AnoMech.Scenarios.Dsr.P2Sanctity;

internal static class MeteorSnapshotValidation
{
    public static void Run()
    {
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var scenario = new DsrP2SanctityScenario();
            var world = new SimWorld();
            scenario.UseSeed(0);
            scenario.Run(world, 0);
            var firstPositions = new Vector3[2];
            var captureTimes = new List<float>();
            for (var frame = 1; frame <= 54 * fps; frame++)
            {
                SimCharacter.Time = frame / (float)fps;
                var state = scenario.CurrentState;
                var before = state.MeteorSnapshots;
                var positions = state.MeteorRoles.Select(r => world.Party.Get(r)!.Position).ToArray();
                world.Events.Tick(1f / fps);
                if (state.MeteorSnapshots != before)
                {
                    captureTimes.Add(SimCharacter.Time);
                    for (var i = 0; i < 2; i++)
                        if (scenario.CapturedMeteor(before, i) != positions[i])
                            throw new Exception("Meteor capture must use the actual position before movement");
                    if (before == 0)
                    {
                        if (state.FirstTowersVisible || state.Stage != SanctityStage.Meteors)
                            throw new Exception("First meteor must lock as towers resolve and runners may leave");
                        positions.CopyTo(firstPositions, 0);
                    }
                }
                foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / fps);
                scenario.Tick(1f / fps, SimCharacter.Time);
                if (SimCharacter.Time is > 44.5f and < 44.6f)
                {
                    var firstComets = world.Enemies.Where(e => e.BNpcBaseId == DsrConstants.Npc.Comet).Take(2).ToArray();
                    for (var i = 0; i < 2; i++)
                        if (firstComets[i].Position != firstPositions[i] ||
                            Vector3.Distance(world.Party.Get(state.MeteorRoles[i])!.Position, firstPositions[i]) < 1)
                            throw new Exception("First meteor must fall at the tower position after its runner has left");
                }
            }
            if (captureTimes.Count != 7 || MathF.Abs(captureTimes[0] - 43.031f) > 1f / fps + .003f ||
                captureTimes.Zip(captureTimes.Skip(1)).Any(p => MathF.Abs(p.Second - p.First - 1.432f) > 1f / fps + .004f))
                throw new Exception("Meteor capture cadence differs from tower release plus seven regular snapshots");
        }
        Console.WriteLine("Meteor snapshots: tower-release first position, delayed falling VFX and seven regular captures passed at 30/60/144 FPS.");
    }
}
