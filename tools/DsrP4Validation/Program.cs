using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P4Eyes;

SwapWaitingValidation.Run();
BehaviorValidation.Run();
foreach (var fps in new[] { 30, 60, 144 })
for (var seed = 0; seed < 70; seed++)
{
    var scenario = new DsrP4EyesScenario();
    scenario.UseSeed(seed);
    var world = new SimWorld();
    SimCharacter.Failures.Clear();
    scenario.Run(world, 0);
    var masks = Enumerable.Range(0, 256).Where(m => int.PopCount(m) == 4).ToArray();
    for (var r = 0; r < 8; r++) scenario.State.Red[r] = (masks[seed] & (1 << r)) != 0;
    for (var f = 1; f <= 87 * fps; f++)
    {
        SimCharacter.Time = f / (float)fps;
        var before = world.Party.Slots.Select(m => m.Position).ToArray();
        world.Events.Tick(1f / fps);
        foreach (var member in world.Party.Slots) member.Advance(1f / fps);
        scenario.Tick(1f / fps, SimCharacter.Time);
        for (var role = 0; role < 8; role++)
            if (Vector3.Distance(before[role], world.Party.Slots[role].Position) > 6f / fps + .001f)
                throw new Exception($"teleport role {role}, {SimCharacter.Time}");
        if (SimCharacter.Failures.Count > 0)
        {
            Console.WriteLine(string.Join("; ", SimCharacter.Failures));
            Console.WriteLine($"seed {seed} mask {masks[seed]} fps {fps}");
            for (var r=0;r<8;r++) Console.WriteLine($"role {r} red {scenario.State.Red[r]} pos {world.Party.Slots[r].Position} lane {scenario.State.DiveLane[r]} target {scenario.State.SwapTarget[r]}");
            throw new Exception("P4 path failed");
        }
    }
    if (!scenario.State.Complete || scenario.State.DiveCount != 4 || !scenario.State.OrbsPopped.All(x => x)) throw new Exception("incomplete");
}
Console.WriteLine("PASS: all 70 initial colour combinations at 30/60/144 FPS; six orbs, four dives, continuous movement, completion.");

namespace AnoMech.Scenarios.Dsr.P4Eyes
{
    public sealed partial class DsrP4EyesScenario
    {
        internal DsrP4EyesState State => state!;
        public void UseSeed(int value) { validationSeed = value; }
    }
}
