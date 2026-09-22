using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;





namespace AnoMech.Scenarios.Dsr.P2Sanctity;

public sealed partial class DsrP2SanctityScenario : IScenario
{
    public string Name => "聖仗";
    public IPhase Phase => DsrZone.P2;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } = [new DsrP2SanctityAi()];
    private DsrP2SanctityState? state;
    private SimWorld? world;
    private SimEnemy? boss, darkKnight, grinnaux, haumeric;
    private readonly SimEnemy?[] knights = new SimEnemy?[2];
    private readonly SimEnemy?[,] spheres = new SimEnemy?[2, 9];
    private readonly List<SimEnemy> towerCasters = [];
    private readonly List<Vector3> ice = [];
    private readonly List<Vector3> meteors = [];
    private readonly List<SimEnemy> openingKnights = [];
    private readonly Vector3[,] meteorSnapshots = new Vector3[7, 2];
    private readonly SimEnemy?[,] cometActors = new SimEnemy?[7, 2];
    private readonly List<(Vector3 Position, int Role, int Number)> landedMeteors = [];
    private int meteorSnapshotCount;
    private readonly HashSet<int> puddleHits = [];
    private bool showHints = true, showMap = true, fixedSeed;
    private int seed = 1, direction, meteorPreference, meteorAngleSelection;
    private float time, puddleGrace, iceGrace;

    public void Run(SimWorld simWorld, int? selectedAi)
    {
        world = simWorld;
        state = new(fixedSeed ? seed : Random.Shared.Next(), direction, meteorPreference, (int)world.Party.PlayerRole,
            meteorAngleSelection switch { 1 => 120, 2 => 150, 3 => 180, 4 => 210, 5 => 240, _ => 0 });
        for (var role = 0; role < 8; role++)
            if (world.Party.Get(role) is SimPartyNpc npc)
                npc.SetPosition(DsrP2SanctityState.OpeningPosition(role));
        time = puddleGrace = iceGrace = 0;
        ice.Clear(); meteors.Clear(); puddleHits.Clear(); towerCasters.Clear();
        openingKnights.Clear(); landedMeteors.Clear();
        meteorSnapshotCount = 0;
        Array.Clear(meteorSnapshots);
        Array.Clear(cometActors);
        Array.Clear(spheres); Array.Clear(knights);
        boss = Spawn(DsrConstants.Npc.Thordan, Vector3.Zero);
        boss?.SetTargetable(true);
        darkKnight = grinnaux = haumeric = null;
        world.Events.Add(.5f, () =>
        {
            BossAction(DsrConstants.Action.Reappear);
        });
        world.Events.Add(2.5f, SummonOpeningKnights);
        world.Events.Add(3f, () => boss?.Cast(DsrConstants.Action.Sanctity, castSeconds: 3.7f, targetId: boss.GameObjectId, fireDelay: .280f));
        world.Events.Add(7.1f, () =>
        {
            foreach (var knight in openingKnights) knight.PlayDeparture(DsrConstants.Timeline.KnightDeparture);
        });
        world.Events.Add(8.5f, () =>
        {
            foreach (var knight in openingKnights) knight.Despawn();
            openingKnights.Clear();
        });
        world.Events.Add(9.2f, () => BossAction(DsrConstants.Action.Teleport));
        world.Events.Add(10.1f, () =>
        {
            boss?.SetTargetable(false);
            boss?.SetVisible(false);
        });
        world.Events.Add(11.5f, RevealSwords);
        world.Events.Add(14.5f, () => state!.SwordGroupsMoving = true);
        world.Events.Add(15.656f, () => boss?.Cast(DsrConstants.Action.Gaze, castSeconds: 3.7f, targetId: boss.GameObjectId, fireDelay: .280f));
        world.Events.Add(20.574f, () => Sever(0));
        world.Events.Add(20.709f, () => { ResolveGaze(); Charge(0); });
        world.Events.Add(21.2f, () => WarnFlare(0));
        world.Events.Add(21.9f, () => WarnFlare(1));
        world.Events.Add(22.2f, () => Explode(0));
        world.Events.Add(22.275f, () => Charge(1));
        world.Events.Add(22.365f, () => Sever(1));
        world.Events.Add(22.6f, () => WarnFlare(2));
        world.Events.Add(22.9f, () => Explode(1));
        world.Events.Add(23.3f, () => WarnFlare(3));
        world.Events.Add(23.6f, () => Explode(2));
        world.Events.Add(23.844f, () => Charge(2));
        world.Events.Add(24f, () => WarnFlare(4));
        world.Events.Add(24.158f, () => Sever(0));
        world.Events.Add(24.3f, () => Explode(3));
        world.Events.Add(24.7f, () => WarnFlare(5));
        world.Events.Add(25f, () => Explode(4));
        world.Events.Add(25.4f, () => WarnFlare(6));
        world.Events.Add(25.7f, () => Explode(5));
        world.Events.Add(25.946f, () => Sever(1));
        world.Events.Add(26.1f, () => WarnFlare(7));
        world.Events.Add(26.4f, () => Explode(6));
        world.Events.Add(26.797f, () => BossAction(DsrConstants.Action.Teleport));
        world.Events.Add(26.8f, () => WarnFlare(8));
        world.Events.Add(27.1f, () => Explode(7));
        world.Events.Add(27.8f, () => Explode(8));
        world.Events.Add(28f, RevealMeteors);
        world.Events.Add(29.5f, () =>
        {
            foreach (var sphere in spheres) sphere?.Despawn();
            foreach (var knight in knights) knight?.Despawn();
            darkKnight?.Despawn();
        });
        world.Events.Add(31.047f, () => { ShowTowers(false, 11.7f, .284f); MarkMeteors(); });
        world.Events.Add(31.136f, () =>
        {
            state.FireVisible = true;
            for (var q = 0; q < 4; q++)
                Spawn(DsrConstants.Npc.Helper, DsrP2SanctityState.Polar(q * 90 + 45, DsrConstants.Geometry.FireCenterRadius), false)?.Cast(DsrConstants.Action.Fire, castSeconds: 7.2f, fireDelay: .270f);
            Spawn(DsrConstants.Npc.Helper, Vector3.Zero, false)?.Cast(DsrConstants.Action.Donut, castSeconds: 7.2f, fireDelay: .270f);
        });
        world.Events.Add(31.784f, () => haumeric?.Cast(DsrConstants.Action.HiemalStorm, castSeconds: 7,
            targetId: haumeric.GameObjectId));
        world.Events.Add(38.606f, ResolveFire);
        world.Events.Add(38.784f, ResolveIce);
        world.Events.Add(40.4f, () => haumeric?.SetVisible(false));
        world.Events.Add(43.031f, () =>
        {
            ResolveTowers(false);
            SnapshotMeteors();
            grinnaux = Spawn(DsrConstants.Npc.Grinnaux, Vector3.Zero);
            state.Stage = SanctityStage.Meteors;
            state.MeteorElapsed = 0;
        });
        world.Events.Add(44.462f, () => ShowMeteorFall(0));
        world.Events.Add(44.463f, SnapshotMeteors);
        world.Events.Add(45.176f, () => LandMeteors(0));
        world.Events.Add(45.355f, () => ShowTowers(true, 10.7f, .266f));
        world.Events.Add(45.894f, SnapshotMeteors);
        world.Events.Add(45.894f, () => ShowMeteorFall(1));
        world.Events.Add(46.610f, () => LandMeteors(1));
        world.Events.Add(47.325f, SnapshotMeteors);
        world.Events.Add(47.325f, () => ShowMeteorFall(2));
        world.Events.Add(48.041f, () => LandMeteors(2));
        world.Events.Add(48.756f, SnapshotMeteors);
        world.Events.Add(48.756f, () => ShowMeteorFall(3));
        world.Events.Add(49.428f, () =>
        {
            grinnaux?.Cast(DsrConstants.Action.Knockback, castSeconds: 3.7f, fireDelay: .283f);
        });
        world.Events.Add(49.472f, () => LandMeteors(3));
        world.Events.Add(50.189f, SnapshotMeteors);
        world.Events.Add(50.189f, () => ShowMeteorFall(4));
        world.Events.Add(50.905f, () => LandMeteors(4));
        world.Events.Add(51.621f, SnapshotMeteors);
        world.Events.Add(51.621f, () => ShowMeteorFall(5));
        world.Events.Add(52.336f, () => LandMeteors(5));
        world.Events.Add(53.052f, () => ShowMeteorFall(6));
        world.Events.Add(53.411f, ResolveKnockback);
        world.Events.Add(53.767f, () => LandMeteors(6));
        world.Events.Add(56.321f, () => { ResolveTowers(true); state.Stage = SanctityStage.SecondTowers; });
        world.Events.Add(60.888f, () =>
        {
            state.Stage = SanctityStage.Complete;
            boss?.SetPosition(new Vector3(0, 0, -15));
            boss?.HoldFacing(0);
            boss?.SetVisible(true);
            boss?.SetTargetable(true);
            BossAction(DsrConstants.Action.Reappear);
            grinnaux?.SetVisible(false);
        });
    }

    private SimEnemy? Spawn(uint id, Vector3 position, bool visible = true)
    {
        var rotation = position.LengthSquared() > .001f ? MathF.Atan2(-position.X, -position.Z) : 0;
        var nameId = DsrConstants.NameId(id);
        var enemy = world!.SpawnEnemy(new EnemySpawnConfig(id, NameId: nameId, Level: 90,
            EnemyList: visible && nameId != 0 ? EnemyListMode.ScenarioVisible : EnemyListMode.Never,
            IsVisible: visible, Placement: new(position, rotation), DisableLookAt: true,
            WeaponDrawn: id is DsrConstants.Npc.Thordan or DsrConstants.Npc.Zephirin or
                DsrConstants.Npc.Adelphel or DsrConstants.Npc.Janlenoux or DsrConstants.Npc.Grinnaux or
                DsrConstants.Npc.Charibert or DsrConstants.Npc.Hermenost or DsrConstants.Npc.Haumeric or DsrConstants.Npc.Noudenet));
        enemy?.HoldFacing(rotation);
        if (visible && id is DsrConstants.Npc.Zephirin or DsrConstants.Npc.Adelphel or
            DsrConstants.Npc.Janlenoux or DsrConstants.Npc.Grinnaux or DsrConstants.Npc.Charibert or
            DsrConstants.Npc.Hermenost or DsrConstants.Npc.Haumeric or DsrConstants.Npc.Noudenet)
            enemy?.QueueEntrance(DsrConstants.Timeline.KnightEntrance, 32f / 30f);
        return enemy;
    }

    private void RevealSwords()
    {
        var s = state!;
        s.Stage = SanctityStage.Swords;
        boss?.SetPosition(s.BossPosition);
        boss?.HoldFacing(MathF.Atan2(-s.BossPosition.X, -s.BossPosition.Z));
        boss?.SetVisible(true);
        BossAction(DsrConstants.Action.Reappear);
        darkKnight = Spawn(DsrConstants.Npc.Zephirin, s.DarkKnightPosition);
        for (var k = 0; k < 2; k++)
        {
            knights[k] = Spawn(k == 0 ? DsrConstants.Npc.Adelphel : DsrConstants.Npc.Janlenoux, s.ChargePoints[k][0]);
            knights[k]?.HoldFacing((k == 0) == s.Clockwise ? 0 : MathF.PI);
            world!.Party.Get(s.Swords[k])?.AttachLockonVfx((uint)(50 + k), 15.5f);
        }
        world!.Map.AddEffect(0x00020001, (byte)s.EyeIndex, resetFlags: 0x00080004);
    }

    private void SummonOpeningKnights()
    {
        uint[] ids = [DsrConstants.Npc.Adelphel, DsrConstants.Npc.Janlenoux,
            DsrConstants.Npc.Grinnaux, DsrConstants.Npc.Hermenost, DsrConstants.Npc.Zephirin,
            DsrConstants.Npc.Charibert, DsrConstants.Npc.Haumeric, DsrConstants.Npc.Noudenet];
        for (var i = 0; i < ids.Length; i++)
            if (Spawn(ids[i], DsrP2SanctityState.Polar(i * 45, 10)) is { } knight)
                openingKnights.Add(knight);
    }

    private void ResolveGaze()
    {
        var s = state!;
        foreach (var member in world!.Party.ActiveMembers())
        {
            var facing = new Vector3(MathF.Sin(member.Rotation), 0, MathF.Cos(member.Rotation));
            if (Vector3.Dot(facing, Vector3.Normalize(s.EyePosition - member.Position)) > .7071f ||
                Vector3.Dot(facing, Vector3.Normalize(s.BossPosition - member.Position)) > .7071f)
                Fail(member, "龍眼視線：結算時沒有背對龍眼／托爾丹");
        }
        s.Stage = SanctityStage.Charges;
    }

    private void Sever(int marker)
    {
        var target = world!.Party.Get(state!.Swords[marker]);
        if (!target.IsAlive()) { Wipe("分攤劍點名者已倒下"); return; }
        var position = target!.Position;
        var members = world.Party.ActiveMembers().Where(m => Vector3.DistanceSquared(m.Position, position) <= 36).ToArray();
        if (members.Length != 4) Wipe($"第 {marker + 1} 組分攤需要四人，目前 {members.Length} 人");
        else if (darkKnight != null && Vector3.DistanceSquared(darkKnight.Position, position) < 30 * 30)
            foreach (var member in members) Fail(member, "分攤劍跳躍距離不足");
        darkKnight?.HoldFacing(null);
        darkKnight?.Face(position);
        darkKnight?.Cast(DsrConstants.Action.Sever, position, 0, target.GameObjectId);
        darkKnight?.SetPosition(position);
    }

    private void Charge(int index)
    {
        for (var k = 0; k < 2; k++)
        {
            var from = state!.ChargePoints[k][index];
            var to = state.ChargePoints[k][index + 1];
            knights[k]?.HoldFacing(null);
            knights[k]?.SetPosition(from);
            knights[k]?.Face(to);
            knights[k]?.Cast(DsrConstants.Action.Blade, to, 0);
            knights[k]?.MoveTo(to, 50);
            foreach (var member in world!.Party.ActiveMembers())
                if (DistanceToSegment(member.Position, from, to) < 3)
                    Fail(member, "碰到騎士衝鋒");
            var first = index == 0 ? 0 : index * 3;
            for (var i = first; i < first + 3; i++)
                spheres[k, i] = Spawn(DsrConstants.Npc.Sphere, state.SpherePoints[k][i]);
        }
    }

    private void BossAction(uint action)
    {
        if (boss == null) return;
        if (action == DsrConstants.Action.Reappear) boss.SetWeaponsVisible(true);
        boss.Cast(action, castSeconds: 0, targetId: boss.GameObjectId);
    }

    private void WarnFlare(int index)
    {
        for (var k = 0; k < 2; k++)
        {
            spheres[k, index]?.Cast(DsrConstants.Action.Flare, castSeconds: 1);
            world!.SpawnOmen("vfx/omen/eff/general01f.avfx",
                new(state!.SpherePoints[k][index], 0), new(9, 1, 9), 1);
        }
    }

    private void Explode(int index)
    {
        for (var k = 0; k < 2; k++)
        {
            spheres[k, index]?.SetVisible(false);
            var position = state!.SpherePoints[k][index];
            foreach (var member in world!.Party.ActiveMembers())
                if (Vector3.DistanceSquared(position, member.Position) < 81)
                    Fail(member, "碰到白球爆炸");
        }
        state!.Explosions = index + 1;
    }

    private void RevealMeteors()
    {
        state!.Stage = SanctityStage.Pairs;
        boss?.SetVisible(false); darkKnight?.SetVisible(false);
        foreach (var knight in knights) knight?.SetVisible(false);
        haumeric = Spawn(DsrConstants.Npc.Haumeric, new Vector3(0, 0, -23));
        world!.Map.AddEffect(0x00080004, (byte)state.EyeIndex);
    }

    private void MarkMeteors()
    {
        for (var i = 0; i < 7; i++)
            for (var j = 0; j < 2; j++)
            {
                cometActors[i, j] = Spawn(DsrConstants.Npc.Comet, Vector3.Zero);
                cometActors[i, j]?.SetVisible(false);
            }
        foreach (var role in state!.MeteorRoles)
        {
            world!.Party.Get(role)?.AttachLockonVfx(285, 24.9f);
            world.Party.Get(role)?.AddStatus(562, 24.9f);
        }
    }

    private void ShowTowers(bool second, float castSeconds, float fireDelay)
    {
        if (second) state!.SecondTowersVisible = true;
        else state!.FirstTowersVisible = true;
        towerCasters.Clear();
        var positions = second ? Enumerable.Range(0, 8).Select(r => state!.SecondTower(r)) : state!.FirstTowers;
        foreach (var position in positions)
        {
            var helper = Spawn(DsrConstants.Npc.Helper, position, false);
            helper?.Cast(second ? DsrConstants.Action.Tower2 : DsrConstants.Action.Tower1, position, castSeconds, fireDelay: fireDelay);
            if (helper != null) towerCasters.Add(helper);
        }
    }

    private void ResolveFire()
    {
        foreach (var member in world!.Party.ActiveMembers())
        {
            if (member.Position.Length() > 15) Fail(member, "外圈甜甜圈範圍");
            for (var q = 0; q < 4; q++)
                if (Vector3.DistanceSquared(member.Position, DsrP2SanctityState.Polar(q * 90 + 45, DsrConstants.Geometry.FireCenterRadius)) < 49)
                    Fail(member, "碰到四角火圈");
        }
        for (var q = 0; q < 4; q++)
            world.SpawnGroundEffect(DsrConstants.Vfx.Fire,
                new(DsrP2SanctityState.Polar(q * 90 + 45, DsrConstants.Geometry.FireCenterRadius), 0), 14.805f, 1.4f);
        puddleGrace = 1.3f;
    }

    private void ResolveIce()
    {
        var members = world!.Party.ActiveMembers().ToArray();
        var targets = Enumerable.Range(0, 8).Where(state!.IsMeteorRole)
            .Select(world.Party.Get).Where(m => m.IsAlive()).Select(m => m!).ToArray();
        foreach (var target in targets)
            if (members.Count(m => Vector3.DistanceSquared(m.Position, target.Position) < 49) != 2)
                Wipe("冰圈需要兩人一組，且不能與其他組重疊");
        foreach (var member in members)
        {
            if (targets.Any(t => Vector3.DistanceSquared(t.Position, member.Position) < 49))
                member.AddStatus(2903, 3f);
            if (targets.Count(t => Vector3.DistanceSquared(t.Position, member.Position) < 49) != 1)
                Fail(member, "未正確參與兩人冰圈分攤");
        }
        foreach (var target in targets)
        {
            ice.Add(target.Position);
            Spawn(DsrConstants.Npc.Helper, target.Position, false)?.Cast(DsrConstants.Action.Ice, target.Position, 0);
            world.SpawnGroundEffect(DsrConstants.Vfx.Ice, new(target.Position, 0), 14.627f, 1.3f);
        }
        state!.Stage = SanctityStage.FirstTowers;
        // The puddle is snapshotted on the pair; allow the exit animation before checking lingering damage.
        iceGrace = DsrConstants.Geometry.IceGraceSeconds;
    }

    private void ResolveTowers(bool second)
    {
        if (second) state!.SecondTowersVisible = false;
        else state!.FirstTowersVisible = false;
        var positions = second ? Enumerable.Range(0, 8).Select(r => state!.SecondTower(r)) : state!.FirstTowers;
        foreach (var tower in positions)
        {
            var count = world!.Party.ActiveMembers().Count(m => Vector3.DistanceSquared(m.Position, tower) <= 9);
            if (count != 1) { Wipe($"第 {(second ? "二" : "一")} 輪塔需要一人，目前 {count} 人"); break; }
        }
    }

    private void SnapshotMeteors()
    {
        for (var i = 0; i < 2; i++)
        {
            var role = state!.MeteorRoles[i];
            var member = world!.Party.Get(role);
            if (!member.IsAlive()) { Wipe("隕石點名者已倒下"); continue; }
            meteorSnapshots[meteorSnapshotCount, i] = member!.Position;
        }
        meteorSnapshotCount++;
        state!.MeteorSnapshots = meteorSnapshotCount;
    }

    private void ShowMeteorFall(int index)
    {
        for (var i = 0; i < 2; i++)
            if (cometActors[index, i] is { } comet)
            {
                var position = meteorSnapshots[index, i];
                comet.SetPosition(position);
                comet.SetVisible(true);
                comet.Cast(DsrConstants.Action.Comet, position, 0, comet.GameObjectId);
            }
    }

    private void LandMeteors(int index)
    {
        for (var i = 0; i < 2; i++)
        {
            var role = state!.MeteorRoles[i];
            var position = meteorSnapshots[index, i];
            foreach (var previous in landedMeteors)
            {
                var distance = Vector2.Distance(new(position.X, position.Z), new(previous.Position.X, previous.Position.Z));
                if (distance < 5)
                {
                    Wipe($"隕石間距不足五公尺：{DsrP2SanctityState.Roles[role]} 第 {index + 1} 顆與 " +
                        $"{DsrP2SanctityState.Roles[previous.Role]} 第 {previous.Number} 顆相距 {distance:F2} 公尺");
                    break;
                }
            }
            landedMeteors.Add((position, role, index + 1));
            meteors.Add(position);
        }
        state!.MeteorCount++;
    }

    private void ResolveKnockback()
    {
        foreach (var (role, member) in world!.Party.FilledSlots())
        {
            if (member is SimPartyNpc && !state!.StartsInside((int)role)) member.AddStatus(1209, 6);
            if (!member.HasStatus(1209) && !member.HasStatus(160))
                (member as ISimPartyMember)?.Knockback(Vector3.Zero, 16);
        }
        state!.KnockbackResolved = true;
        state.FireVisible = false;
        ice.Clear();
    }

    public void Tick(float delta, float elapsed)
    {
        if (state == null || world == null || state.Stage == SanctityStage.Complete) return;
        time = elapsed;
        if (state.Stage == SanctityStage.Meteors) state.MeteorElapsed += delta;
        if (state.KnockbackResolved) state.KnockbackElapsed += delta;
        DsrP2SanctityAi.Tick(state, world);
        puddleGrace = Math.Max(0, puddleGrace - delta);
        iceGrace = Math.Max(0, iceGrace - delta);
        foreach (var (role, member) in world.Party.FilledSlots())
        {
            if (iceGrace <= 0 && ice.Any(p => Vector3.DistanceSquared(p, member.Position) < 49) && puddleHits.Add((int)role))
                Fail(member, "踩入殘留冰圈");
            if (puddleGrace <= 0 && state.Stage >= SanctityStage.FirstTowers && !state.KnockbackResolved)
                for (var q = 0; q < 4; q++)
                    if (Vector3.DistanceSquared(member.Position, DsrP2SanctityState.Polar(q * 90 + 45, DsrConstants.Geometry.FireCenterRadius)) < 49 && puddleHits.Add((int)role + 8))
                        Fail(member, "踩入殘留火圈");
        }
    }

    private void Fail(SimCharacter member, string cause) { state!.Failed = true; member.Die(cause); }
    private void Wipe(string cause) { state!.Failed = true; world!.Party.WipeAllPlayers(cause); }
    private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        var d = b - a;
        return Vector3.Distance(p, a + Math.Clamp(Vector3.Dot(p - a, d) / d.LengthSquared(), 0, 1) * d);
    }

}
