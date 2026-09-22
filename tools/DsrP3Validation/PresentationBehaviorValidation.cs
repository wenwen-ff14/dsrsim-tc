using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P3Wyrmhole;

internal static class PresentationBehaviorValidation
{
    public static void Run()
    {
        var scenario = new DsrP3WyrmholeScenario();
        scenario.UseSeed(0);
        var world = new SimWorld();
        scenario.Run(world, 0);
        for (var frame = 1; frame <= 25 * 60; frame++)
        {
            SimCharacter.Time = frame / 60f;
            world.Events.Tick(1f / 60);
            foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
            scenario.Tick(1f / 60, SimCharacter.Time);
        }
        var boss = world.Enemies.Single(e => e.BNpcBaseId == DsrP3WyrmholeConstants.Nidhogg);
        if (boss.Casts.Count(c => c.Action == DsrP3WyrmholeConstants.Stack) != 1 ||
            world.Enemies.Where(e => e != boss).Any(e => e.Casts.Any(c => c.Action == DsrP3WyrmholeConstants.Stack)))
            throw new Exception("Stack VFX must be released by the Nidhogg skeleton");

        var clone = world.Enemies.First(e => e.BNpcBaseId == DsrP3WyrmholeConstants.Drake);
        if (!clone.InEnemyList) throw new Exception("Landed clone must appear in the enemy list for its spear cast");
        void ParkAllAway()
        {
            foreach (var member in world.Party.ActiveMembers()) member.SetPosition(new(0, 0, -40));
        }
        void ExpectFacing(Vector3 offset)
        {
            var direction = new Vector3(MathF.Sin(clone.Rotation), 0, MathF.Cos(clone.Rotation));
            if (Vector3.Dot(direction, Vector3.Normalize(offset)) < .999f)
                throw new Exception("Unlocked spear did not track the nearest player");
        }
        ParkAllAway();
        world.Party.Get(0)!.SetPosition(clone.Position + Vector3.UnitX * 2);
        scenario.Tick(0, 25);
        ExpectFacing(Vector3.UnitX);
        world.Party.Get(0)!.SetPosition(clone.Position + Vector3.UnitZ * 2);
        scenario.Tick(0, 25.1f);
        ExpectFacing(Vector3.UnitZ);
        world.Party.Get(1)!.SetPosition(clone.Position - Vector3.UnitX);
        scenario.Tick(0, 25.2f);
        ExpectFacing(-Vector3.UnitX);
        SimCharacter.Time = 27.05f;
        world.Events.Tick(2.05f);
        var locked = clone.Rotation;
        ParkAllAway();
        world.Party.Get(0)!.SetPosition(clone.Position + Vector3.UnitZ);
        scenario.Tick(0, 28);
        if (clone.Rotation != locked) throw new Exception("Spear direction changed after its cast locked");

        var state = scenario.State;
        state.NumbersAssigned = state.ArrowsAssigned = true;
        state.Time = 30;
        foreach (var lane in new[] { 0, 1 })
        {
            var role = state.Soaker(1, lane);
            if (DsrP3WyrmholeAi.Destination(state, role) != new Vector3(0, 0, -7))
                throw new Exception("Second-wave soaker entered before the towers appeared");
        }
        state.Time = 32;
        foreach (var lane in new[] { 0, 1 })
            if (DsrP3WyrmholeAi.Destination(state, state.Soaker(1, lane)) != state.Towers[1][lane])
                throw new Exception("Second-wave soaker did not enter after the towers appeared");
        Console.WriteLine("Stack caster, nearest-player tracking/lock and second-wave tower entry passed.");
        var walkingWorld = new SimWorld();
        var walkingState = new DsrP3WyrmholeState(0) { NumbersAssigned = true, ArrowsAssigned = true, Time = 53 };
        for (var frame = 0; frame < 300; frame++)
        {
            DsrP3WyrmholeAi.Tick(walkingState, walkingWorld);
            foreach (var member in walkingWorld.Party.ActiveMembers()) member.Advance(1f / 60);
        }
        foreach (var (role, member) in walkingWorld.Party.FilledSlots())
        {
            var expected = DsrP3WyrmholeState.AtRadius(DsrP3WyrmholeState.FinalTowerPosition(DsrP3WyrmholeState.FinalTowerHome(role)), 16);
            if (member.MoveCommands != 1 || Vector3.Distance(member.Position, expected) > .001f)
                throw new Exception("NPC must walk directly to its lance-safe preposition without restarting movement every frame");
        }
        walkingState.Time = 60;
        walkingState.FinalTowersVisible = true;
        DsrP3WyrmholeAi.Tick(walkingState, walkingWorld);
        foreach (var member in walkingWorld.Party.ActiveMembers())
            if (member.MoveCommands != 2) throw new Exception("New tower destination must start movement immediately");
        Console.WriteLine("P3 movement commands persist until arrival; final tower preparation avoids in/out detours.");
    }
}
