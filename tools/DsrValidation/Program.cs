using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P2Sanctity;
using System.Numerics;

var failures = new List<string>();
LogTimingValidation.Run();
MeteorSnapshotValidation.Run();
var peak = 0;
for (var seed = 0; seed < 1000; seed++)
{
    var s = new DsrP2SanctityState(seed);
    void Check(bool valid, string message) { if (!valid) throw new Exception($"Seed {seed}: {message}"); }
    Check(s.Groups[s.Swords[0]] == 0 && s.Groups[s.Swords[1]] == 1, "sword assignments");
    Check(s.Groups.Count(g => g == 0) == 4, "four players per stack");
    Check(s.MeteorRoles.Select(r => s.Quadrants[r]).Order().SequenceEqual(new[] { 0, 2 }), "north/south meteors");
    Check(s.FirstTowers.Count == 8 && s.FirstTowerByRole.Distinct().Count() == 8, "unique first towers");
    Check(s.FirstTowers.Count(t => t.Length() > 10) is 5 or 6, "five or six outer towers");
    Check(Enumerable.Range(0, 8).Select(s.SecondTower).Distinct().Count() == 8, "unique second towers");
    for (var player = 0; player < 8; player++)
    {
        var forced = new DsrP2SanctityState(seed, seed % 3, 3, player);
        Check(forced.HasMeteor(player), "forced player meteor");
        Check(forced.MeteorRoles.All(r => (r < 4) == (player < 4)), "same-role meteor pair");
    }
    for (var r = 0; r < 8; r++)
        if (s.HasMeteor(r)) Check(Vector3.Distance(s.MeteorStart(r), s.FirstTower(r)) < 3, "meteor start inside tower");

    var scenario = new DsrP2SanctityScenario();
    scenario.UseSeed(seed);
    var world = new SimWorld();
    SimCharacter.Failures.Clear();
    scenario.Run(world, 0);
    Check(world.Enemies.Single(e => e.BNpcBaseId == 0x313C).Position == Vector3.Zero, "Thordan starts Sanctity at arena centre");
    var thordan = world.Enemies.Single(e => e.BNpcBaseId == 0x313C);
    Check(thordan.Targetable, "Thordan targetable before departure");
    for (var frame = 0; frame < 61 * 60 + 5; frame++)
    {
        var time = (frame + 1) / 60f;
        SimCharacter.Time = time;
        world.Events.Tick(1f / 60);
        foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
        scenario.Tick(1f / 60, time);
        if (frame == 7 * 60 + 30)
        {
            var opening = world.Enemies.Where(e => e.NameId is >= 3633 and <= 3644).ToArray();
            Check(opening.Length == 8 && opening.All(e => e.Active && e.Departures.Count == 1 &&
                e.Departures[0].Timeline == 0x1E39 && MathF.Abs(e.Departures[0].Time - 7.1f) < .05f),
                "all opening knights take off after Sanctity cast and remain alive for animation");
        }
        if (frame == 9 * 60)
            Check(world.Enemies.Where(e => e.NameId is >= 3633 and <= 3644).All(e => !e.Active), "opening knights retired after takeoff");
        if (frame is 720 or 840)
        {
            var positions = world.Party.ActiveMembers().Select(m => m.Position).ToArray();
            Check(positions.Distinct().Count() == 8, "sword markers have eight distinct positions");
            for (var r = 0; r < 8; r++)
                Check(Vector3.Distance(world.Party.Get(r)!.Position, DsrP2SanctityState.OpeningPosition(r)) < .05f,
                    "NPCs hold opening spread while sword markers are read");
        }
        if (frame == 9 * 60 + 30) Check(thordan.WeaponsVisible, "Thordan takeoff retains native weapon animation control");
        if (frame == 10 * 60 + 30) Check(!thordan.Visible, "Thordan hidden after departure");
        if (frame == 12 * 60) Check(thordan.Visible && thordan.WeaponsVisible, "Thordan body and weapon restored on reappearance");
        if (frame == 720) Check(!thordan.Targetable, "Thordan untargetable during Sanctity mechanics");
        if (frame == 1800)
            Check(world.Party.ActiveMembers().All(m => !m.LockonVfx.Contains(285)), "no meteor headmarkers before first towers");
        if (frame == 1890)
            Check(world.Party.ActiveMembers().Where(m => m.LockonVfx.Contains(285)).Select(m => m.Role).Order()
                .SequenceEqual(s.MeteorRoles.Order()), "meteor headmarkers appear with first towers");
        if (frame == 720)
        {
            foreach (var enemy in world.Enemies.Where(e => e.Active && e.BNpcBaseId is 0x3139 or 0x3158))
            {
                var south = (enemy.Position.X > 0) == s.Clockwise;
                Check(enemy.Entrances.Count == 1 && enemy.Entrances[0].Timeline == 0x1E43 &&
                    enemy.Entrances[0].Duration < 1.1f, "knight has one bounded entrance");
                Check(MathF.Abs(MathF.Cos(enemy.Rotation) - (south ? 1 : -1)) < .001f, "knight facing follows rotation direction");
            }
            foreach (var enemy in world.Enemies.Where(e => e.Active && e.BNpcBaseId is 0x313C or 0x3130))
            {
                var forward = new Vector3(MathF.Sin(enemy.Rotation), 0, MathF.Cos(enemy.Rotation));
                Check(Vector3.Dot(forward, -Vector3.Normalize(enemy.Position)) > .999f, "outer enemies face arena centre");
            }
        }
    }
    peak = Math.Max(peak, world.PeakEnemies);
    Check(world.Events.IsEmpty, "scenario timeline completes");
    Check(thordan.Targetable, "Thordan targetable again after mechanics");
    foreach (var enemy in world.Enemies.Where(e => e.NameId is >= 3633 and <= 3644))
        Check(enemy.Entrances.Count == 1, "each named knight receives one entrance");
    if (SimCharacter.Failures.Count > 0)
        failures.Add($"Seed {seed}: {string.Join("; ", SimCharacter.Failures.Distinct().Take(4))}");
}
Console.WriteLine($"1000 seeds: {failures.Count} failed routes; peak {peak} enemy objects.");
Console.WriteLine("Pre-charge facings: inner knights and outer enemies passed across all seeds.");
foreach (var failure in failures.Take(15)) Console.WriteLine(failure);
if (failures.Count != 0) Environment.ExitCode = 1;

void ExpectFailure(string expected, Action<SimWorld, DsrP2SanctityState, float> disturb)
{
    var scenario = new DsrP2SanctityScenario();
    scenario.UseSeed(0);
    var world = new SimWorld();
    scenario.Run(world, 0);
    SimCharacter.Failures.Clear();
    for (var frame = 0; frame < 61 * 60 + 5; frame++)
    {
        var time = (frame + 1) / 60f;
        SimCharacter.Time = time;
        disturb(world, scenario.CurrentState, time);
        world.Events.Tick(1f / 60);
        foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
        scenario.Tick(1f / 60, time);
    }
    if (!SimCharacter.Failures.Any(f => f.Contains(expected)))
        throw new Exception($"Missing failure detection: {expected}");
}
ExpectFailure("龍眼視線", (w, s, t) => { if (t > 20.7f && t < 20.85f) w.Party.Get(0)!.Face(s.EyePosition); });
ExpectFailure("輪塔需要一人", (w, s, t) => { if (t > 43 && t < 43.2f) w.Party.Get(0)!.SetPosition(Vector3.Zero); });
ExpectFailure("隕石間距不足", (w, s, t) =>
{
    if (t > 45.9f && t < 47.6f) w.Party.Get(s.MeteorRoles[0])!.SetPosition(new(0, 0, 19));
});
var playerWorld = new SimWorld();
var localPlayer = new SimPlayer { Position = new(1, 0, 2) };
playerWorld.Party.Slots[0] = localPlayer;
new DsrP2SanctityScenario().Run(playerWorld, 0);
if (localPlayer.Position != new Vector3(1, 0, 2)) throw new Exception("Opening spread moved the player");
for (var role = 1; role < 8; role++)
    if (Vector3.Distance(playerWorld.Party.Get(role)!.Position, DsrP2SanctityState.OpeningPosition(role)) > .01f)
        throw new Exception("NPC did not start at its opening spread position");
DsrP2SanctityAi.Tick(new(0) { Stage = SanctityStage.Swords }, playerWorld);
localPlayer.Advance(1);
if (localPlayer.Position != new Vector3(1, 0, 2)) throw new Exception("AI moved the player");
Console.WriteLine("Failure detection: gaze / missing tower / meteor collision passed; AI does not move player.");

foreach (var leaveBeforeDamage in new[] { true, false })
{
    var scenario = new DsrP2SanctityScenario();
    scenario.UseSeed(0);
    var world = new SimWorld();
    scenario.Run(world, 0);
    SimCharacter.Failures.Clear();
    float? firstIceHit = null;
    for (var frame = 0; frame < 42 * 60; frame++)
    {
        var t = (frame + 1) / 60f;
        SimCharacter.Time = t;
        if (t >= 38.7f && world.Party.Slots[0] is not SimPlayer)
            world.Party.Slots[0] = new SimPlayer { Role = 0, Position = scenario.CurrentState.PairPosition(0) };
        if (leaveBeforeDamage && t >= 40.8f)
            world.Party.Slots[0].SetPosition(Vector3.Normalize(scenario.CurrentState.PairPosition(0)) * 20);
        world.Events.Tick(1f / 60);
        foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
        scenario.Tick(1f / 60, t);
        if (firstIceHit == null && SimCharacter.Failures.Any(f => f.Contains("role 0:") && f.Contains("踩入殘留冰圈")))
            firstIceHit = t;
    }
    if (leaveBeforeDamage && firstIceHit != null)
        throw new Exception("Ice damaged a player who left during grace");
    if (!leaveBeforeDamage && (firstIceHit == null || firstIceHit < 41.25f || firstIceHit > 41.4f))
        throw new Exception($"Ice damage did not start after grace: {firstIceHit}");
}
Console.WriteLine("Ice grace: leaving before 2.5s is safe; remaining inside triggers damage after grace.");

var humanMeteorRuns = 0;
for (var seed = 0; seed < 100; seed++)
{
    var s = new DsrP2SanctityState(seed);
    if (!s.HasMeteor(0)) continue;
    var other = s.MeteorRoles.Single(r => r != 0);
    var arc = (DsrP2SanctityState.Angle(s.MeteorStart(other)) - DsrP2SanctityState.Angle(s.MeteorStart(0)) + 360) % 360;
    if (MathF.Abs(arc - 180) > 1) continue;
    humanMeteorRuns++;
    var scenario = new DsrP2SanctityScenario();
    scenario.UseSeed(seed);
    var world = new SimWorld();
    scenario.Run(world, 0);
    SimCharacter.Failures.Clear();
    for (var frame = 0; frame < 57 * 60; frame++)
    {
        var t = (frame + 1) / 60f;
        SimCharacter.Time = t;
        if (t > 43.1f)
        {
            if (world.Party.Slots[0] is not SimPlayer) world.Party.Slots[0] = new SimPlayer { Role = 0 };
            world.Party.Slots[0].SetPosition(t <= 55.02f
                ? DsrP2SanctityState.Polar(DsrP2SanctityState.Angle(s.MeteorStart(0)) + (t - 43.1f) * 6 / 19.5f * 180 / MathF.PI, 19.5f)
                : s.SecondTower(0));
        }
        world.Events.Tick(1f / 60);
        foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
        scenario.Tick(1f / 60, t);
    }
    if (SimCharacter.Failures.Any(f => f.Contains("隕石間距不足")))
        throw new Exception($"Normal player run collided with AI meteors, seed {seed}: " + SimCharacter.Failures.First(f => f.Contains("隕石間距不足")));
}
if (humanMeteorRuns == 0) throw new Exception("No normal-speed player meteor cases exercised");
Console.WriteLine($"Normal-speed player meteor runs: {humanMeteorRuns} passed with an AI partner.");

foreach (var angle in new[] { 120, 150, 180, 210, 240 })
    for (var seed = 0; seed < 100; seed++)
    {
        var scenario = new DsrP2SanctityScenario();
        scenario.UseSeed(seed, angle);
        var world = new SimWorld();
        scenario.Run(world, 0);
        var state = scenario.CurrentState;
        if (!state.HasMeteor(0) || MathF.Abs(state.MeteorArc(0) - angle) > .01f || state.FirstTowerByRole.Distinct().Count() != 8)
            throw new Exception($"Forced {angle} degree tower geometry invalid, seed {seed}");
        SimCharacter.Failures.Clear();
        for (var frame = 0; frame < 61 * 60 + 5; frame++)
        {
            var t = (frame + 1) / 60f;
            SimCharacter.Time = t;
            world.Events.Tick(1f / 60);
            foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
            scenario.Tick(1f / 60, t);
        }
        if (SimCharacter.Failures.Count != 0)
            throw new Exception($"Forced {angle} degree route failed, seed {seed}: {SimCharacter.Failures[0]}");
    }
Console.WriteLine("Player 120/150/180/210/240 degree routes: 500 complete routes passed.");

foreach (var angle in new[] { 120, 150, 180, 210, 240 })
    for (var role = 0; role < 8; role++)
        for (var seed = 0; seed < 100; seed++)
        {
            var state = new DsrP2SanctityState(seed, meteorPreference: role < 4 ? 2 : 1, playerRole: role, meteorAngle: angle);
            if (!state.HasMeteor(role) || MathF.Abs(state.MeteorArc(role) - angle) > .01f ||
                MathF.Abs(state.MeteorRoles.Sum(state.MeteorArc) - 360) > .01f ||
                state.MeteorRoles.Any(r => (r < 4) != (role < 4)))
                throw new Exception($"Player route selection failed: role {role}, angle {angle}, seed {seed}");
        }
Console.WriteLine("All eight player roles: 4000 angle selections honor player route and override conflicting target preference.");

{
    var scenario = new DsrP2SanctityScenario();
    scenario.UseSeed(1);
    var world = new SimWorld();
    scenario.Run(world, 0);
    SimEnemy? waitingKnight = null;
    for (var frame = 0; frame < 62 * 60; frame++)
    {
        var t = (frame + 1) / 60f;
        SimCharacter.Time = t;
        world.Events.Tick(1f / 60);
        foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
        scenario.Tick(1f / 60, t);
        if (frame == 23 * 60)
        {
            var balls = world.Enemies.Where(e => e.BNpcBaseId == AnoMech.Scenarios.Dsr.DsrConstants.Npc.Sphere).ToArray();
            if (balls.Count(e => !e.Visible) != 4 || !balls.Any(e => e.Visible))
                throw new Exception("Exploded spheres must hide individually without hiding upcoming spheres");
            if (balls.Any(e => !e.Active))
                throw new Exception("Sphere actors must survive the explosion effect tail");
        }
        if (frame == 28 * 60 && world.Enemies.Any(e =>
            e.BNpcBaseId == AnoMech.Scenarios.Dsr.DsrConstants.Npc.Sphere && e.Visible))
            throw new Exception("Exploded spheres remain visible after last explosion");
        if (frame == 32 * 60)
        {
            var iceKnight = world.Enemies.Last(e => e.BNpcBaseId == AnoMech.Scenarios.Dsr.DsrConstants.Npc.Haumeric);
            if (!iceKnight.Visible || iceKnight.Position.Length() <= 21 || !iceKnight.Casts.Any(c => c.Action == 25574 && c.Duration == 7))
                throw new Exception("Ice knight missing or not casting Hiemal Storm");
        }
        if (frame == 38 * 60 && world.Party.ActiveMembers().Any(m => m.HasStatus(2903)))
            throw new Exception("Ice resistance debuff applied before ice resolved");
        if (frame == 39 * 60 && world.Party.ActiveMembers().Any(m => !m.HasStatus(2903)))
            throw new Exception("Ice resistance debuff missing from an ice-pair participant");
        if (frame == 42 * 60 && world.Party.ActiveMembers().Any(m => m.HasStatus(2903)))
            throw new Exception("Ice resistance debuff lasted longer than three seconds");
        if (frame == 44 * 60)
        {
            waitingKnight = world.Enemies.Last(e => e.BNpcBaseId == AnoMech.Scenarios.Dsr.DsrConstants.Npc.Grinnaux);
            if (!waitingKnight.Active || !waitingKnight.Visible || waitingKnight.Position != Vector3.Zero || waitingKnight.Casts.Count != 0)
                throw new Exception("Grinnaux must wait at center after first towers, before casting");
        }
    }
    if (waitingKnight == null || !waitingKnight.Casts.Any(c => c.Action == 25308 && MathF.Abs(c.Time - 49.428f) < .05f))
        throw new Exception("Knockback cast timing changed");
    foreach (var enemy in world.Enemies)
    {
        var expectedName = AnoMech.Scenarios.Dsr.DsrConstants.NameId(enemy.BNpcBaseId);
        if (enemy.NameId != expectedName || (expectedName == 0 && enemy.ListMode != EnemyListMode.Never))
            throw new Exception("Invalid NPC name or mechanic objects consuming enemy-list slots");
    }
}
Console.WriteLine("Ice knight cast, early knockback knight arrival, localized names and enemy-list filtering passed.");
Console.WriteLine("Sphere explosions hide each resolved pair while preserving pending spheres and effect-tail actors.");

var asymmetricHumanRuns = 0;
foreach (var angle in new[] { 120, 150 })
    for (var seed = 0; seed < 200; seed++)
    {
        var scenario = new DsrP2SanctityScenario();
        scenario.UseSeed(seed, angle);
        var world = new SimWorld();
        scenario.Run(world, 0);
        var state = scenario.CurrentState;
        if (!state.HasMeteor(0)) continue;
        asymmetricHumanRuns++;
        var start = DsrP2SanctityState.Angle(state.MeteorStart(0));
        var speed = Math.Min(6f, state.MeteorArc(0) * MathF.PI / 180 * 19.5f / 10.02f);
        SimCharacter.Failures.Clear();
        for (var frame = 0; frame < 57 * 60; frame++)
        {
            var t = (frame + 1) / 60f;
            SimCharacter.Time = t;
            if (t > 43.1f)
            {
                if (world.Party.Slots[0] is not SimPlayer) world.Party.Slots[0] = new SimPlayer { Role = 0 };
                world.Party.Slots[0].SetPosition(t <= 53.2f
                    ? DsrP2SanctityState.Polar(start + (t - 43.1f) * speed / 19.5f * 180 / MathF.PI, 19.5f)
                    : state.SecondTower(0));
            }
            world.Events.Tick(1f / 60);
            foreach (var member in world.Party.ActiveMembers()) member.Advance(1f / 60);
            scenario.Tick(1f / 60, t);
        }
        if (SimCharacter.Failures.Any(f => f.Contains("隕石間距不足")))
            throw new Exception($"Human/AI {angle} degree meteors collided, seed {seed}: " +
                SimCharacter.Failures.First(f => f.Contains("隕石間距不足")));
        if (world.Enemies[0].Dialogue.Count != 0) throw new Exception("Unrequested subtitles displayed");
    }
if (asymmetricHumanRuns == 0) throw new Exception("No asymmetric human meteor cases exercised");
Console.WriteLine($"Human/AI 120/150 degree routes: {asymmetricHumanRuns} passed without meteor spacing failures.");

namespace AnoMech.Scenarios.Dsr.P2Sanctity
{
    public sealed partial class DsrP2SanctityScenario
    {
        internal void UseSeed(int value, int angle = 0)
        {
            fixedSeed = true; seed = value; direction = 0; meteorPreference = 0;
            meteorAngleSelection = angle switch { 120 => 1, 150 => 2, 180 => 3, 210 => 4, 240 => 5, _ => 0 };
        }
        internal DsrP2SanctityState CurrentState => state!;
        internal Vector3 CapturedMeteor(int index, int target) => meteorSnapshots[index, target];
    }
}
