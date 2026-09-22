using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P3Wyrmhole;

void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
LogTimingValidation.Run();
foreach (var fps in new[] { 30, 60, 144 })
for (var seed = 0; seed < 300; seed++)
{
    var state = new DsrP3WyrmholeState(seed);
    Check(state.Order.Count(x => x == 0) == 3 && state.Order.Count(x => x == 1) == 2 && state.Order.Count(x => x == 2) == 3, "3/2/3 assignments");
    Check(state.Arrows.Count(x => x) is 1 or 2, "one or two arrow pairs");
    for (var role = 0; role < 8; role++)
    {
        var landing = DsrP3WyrmholeState.Landing(state.JumpPosition(role), MathF.PI / 2, state.Direction[role]);
        Check(Vector3.Distance(landing, state.Towers[state.Order[role]][state.LandingLane(role)]) < .001f, "east-facing arrow landing");
    }
    var scenario = new DsrP3WyrmholeScenario();
    scenario.UseSeed(seed);
    var world = new SimWorld();
    SimCharacter.Failures.Clear();
    var initialPositions = world.Party.ActiveMembers().Select(m => m.Position).ToArray();
    scenario.Run(world, 0);
    Check(world.Party.ActiveMembers().Select(m => m.Position).SequenceEqual(initialPositions), "opening does not teleport NPCs");
    Check(Enumerable.Range(0, 8).Select(DsrP3WyrmholeAi.OpeningPosition).Distinct().Count() == 8, "eight distinct opening destinations");
    Check(!scenario.State.NumbersAssigned && !scenario.State.ArrowsAssigned, "opening does not reveal assignments");
    for (var role = 0; role < 8; role++)
        Check(DsrP3WyrmholeAi.Destination(scenario.State, role) == DsrP3WyrmholeAi.OpeningPosition(role), "hold opening position until numbers");
    for (var frame = 1; frame <= 67 * fps; frame++)
    {
        SimCharacter.Time = frame / (float)fps;
        var beforeMove = world.Party.ActiveMembers().Select(m => m.Position).ToArray();
        world.Events.Tick(1f / fps);
        foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / fps);
        scenario.Tick(1f / fps, SimCharacter.Time);
        for (var role = 0; role < 8; role++)
            Check(Vector3.Distance(beforeMove[role], world.Party.Get(role)!.Position) <= 6f / fps + .001f,
                $"NPC {role} teleported at {SimCharacter.Time:F3}s");
    }
    Check(SimCharacter.Failures.Count == 0, $"Seed {seed}, {fps} FPS: {string.Join("; ", SimCharacter.Failures.Distinct())}");
    Check(world.Events.IsEmpty && scenario.State.Complete, "scenario completes");
    Check(world.Enemies.Count(e => e.BNpcBaseId == DsrP3WyrmholeConstants.Drake) == 12, "eight jump actors and four final tower actors");
    Check(world.Enemies.Where(e => e.BNpcBaseId is DsrP3WyrmholeConstants.Nidhogg or DsrP3WyrmholeConstants.Drake)
        .All(e => e.NameId == 3458), "boss and jump actors resolve the localized Nidhogg name");
}
Console.WriteLine("300 seeds at 30/60/144 FPS: opening positions, assignments, facing offsets, all AI routes and scenario completion passed.");

void ExpectFailure(int seed, float start, float end, Action<SimWorld, DsrP3WyrmholeState> disturb, string expected)
{
    var scenario = new DsrP3WyrmholeScenario();
    scenario.UseSeed(seed);
    var world = new SimWorld();
    scenario.Run(world, 0);
    SimCharacter.Failures.Clear();
    for (var frame = 1; frame <= 67 * 60; frame++)
    {
        var time = frame / 60f;
        SimCharacter.Time = time;
        if (time >= start && time <= end) disturb(world, scenario.State);
        world.Events.Tick(1f / 60);
        foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
        scenario.Tick(1f / 60, time);
    }
    Check(SimCharacter.Failures.Any(message => message.Contains(expected)), $"Missing failure detection: {expected}");
}
var arrowSeed = Enumerable.Range(0, 100).First(seed => new DsrP3WyrmholeState(seed).Arrows[0]);
ExpectFailure(arrowSeed, 17.6f, 17.8f, (world, state) => world.Party.Get(state.RoleAt(0, 0))!.SetRotation(-MathF.PI / 2), "塔落在場外");
ExpectFailure(0, 24.3f, 24.5f, (world, state) => world.Party.Get(state.Soaker(0, 0))!.SetPosition(new(0, 0, -19)), "輪塔需要一人");
ExpectFailure(0, 17.8f, 18f, (world, state) => world.Party.Get(state.RoleAt(1, 0))!.SetPosition(new(0, 0, -19)), "分攤需要五人");
ExpectFailure(0, 21.3f, 21.5f, (world, state) => world.Party.Get(0)!.SetPosition(new(0, 0, state.OutFirst[0] ? 0 : 18)), "未躲開");
Console.WriteLine("Wrong facing, missed towers, missing stack members and in/out failures detected.");

for (var role = 0; role < 8; role++)
{
    var scenario = new DsrP3WyrmholeScenario();
    scenario.UseSeed(role * 17);
    var world = new SimWorld();
    world.Party.Slots[role] = new SimPlayer { Role = role, Position = new(0, 0, 16) };
    scenario.Run(world, 0);
    Check(world.Party.Get(role)!.Position == new Vector3(0, 0, 16), "opening placement does not teleport player");
    SimCharacter.Failures.Clear();
    for (var frame = 1; frame <= 4020; frame++)
    {
        var player = world.Party.Get(role)!;
        player.MoveTo(DsrP3WyrmholeAi.Destination(scenario.State, role), 6, MathF.PI / 2);
        SimCharacter.Time = frame / 60f;
        world.Events.Tick(1f / 60);
        foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
        scenario.Tick(1f / 60, frame / 60f);
    }
    Check(SimCharacter.Failures.Count == 0, $"Scripted player route failed for role {role}");
}
Console.WriteLine("All eight player roles completed with scripted player input and seven AI partners.");

Check(DsrP3WyrmholeScenario.InLine(new(0, 0, 5), Vector3.Zero, Vector3.UnitZ), "line hit");
Check(!DsrP3WyrmholeScenario.InLine(new(5, 0, 5), Vector3.Zero, Vector3.UnitZ), "line side dodge");
Check(!DsrP3WyrmholeScenario.InLine(new(0, 0, -5), Vector3.Zero, Vector3.UnitZ), "behind line origin");
var playerWorld = new SimWorld();
playerWorld.Party.Slots[0] = new SimPlayer { Position = new(3, 0, 4) };
var playerState = new DsrP3WyrmholeState(42) { Time = 20, NumbersAssigned = true, ArrowsAssigned = true };
DsrP3WyrmholeAi.Tick(playerState, playerWorld);
playerWorld.Party.Slots[0].Advance(10);
Check(playerWorld.Party.Slots[0].Position == new Vector3(3, 0, 4), "AI does not move player");
Console.WriteLine("Line geometry and player movement ownership passed. Native rendering is not exercised.");
PresentationBehaviorValidation.Run();
FinalTowerValidation.Run();
BossFacingValidation.Run();

namespace AnoMech.Scenarios.Dsr.P3Wyrmhole
{
    public sealed partial class DsrP3WyrmholeScenario
    {
        internal DsrP3WyrmholeState State => state!;
        internal void UseSeed(int value) { fixedSeed = true; seed = value; }
    }
}
