using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P3Wyrmhole;

public sealed partial class DsrP3WyrmholeScenario : IScenario
{
    public string Name => "尼德霍格";
    public IPhase Phase => DsrZone.P3;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } = [new DsrP3WyrmholeAi()];
    private DsrP3WyrmholeState? state;
    private SimWorld? world;
    private SimEnemy? boss;
    private readonly SimEnemy?[][] drakes = [new SimEnemy?[3], new SimEnemy?[2], new SimEnemy?[3]];
    private readonly SimEnemy?[] finalDrakes = new SimEnemy?[4];
    private readonly List<(int Wave, Vector3 Source, Vector3 Direction)> lines = [];
    private bool fixedSeed, showHints = true, showMap = true;
    private int seed = 1;
    private int playerNumber, playerArrows;

    public void Run(SimWorld simWorld, int? selectedAi)
    {
        world = simWorld;
        state = new(fixedSeed ? seed : Random.Shared.Next());
        state.SetPlayerAssignment((int)world.Party.PlayerRole, playerNumber, playerArrows);
        for (var role = 0; role < 8; role++)
            if (world.Party.Get(role) is SimPartyNpc npc)
            {
                npc.MoveTo(DsrP3WyrmholeAi.OpeningPosition(role), 6, MathF.PI / 2);
            }
        lines.Clear();
        Array.Clear(soulTethers);
        foreach (var wave in drakes) Array.Clear(wave);
        boss = Spawn(DsrP3WyrmholeConstants.Nidhogg, Vector3.Zero, true);
        boss?.SetTargetable(true);
        // Instant hide/show actions need a loaded skeleton before their release packet arrives.
        for (var wave = 0; wave < drakes.Length; wave++)
            for (var lane = 0; lane < drakes[wave].Length; lane++)
                drakes[wave][lane] = Spawn(DsrP3WyrmholeConstants.Drake, state.Towers[wave][lane], false);
        for (var tower = 0; tower < finalDrakes.Length; tower++)
            finalDrakes[tower] = Spawn(DsrP3WyrmholeConstants.Drake, DsrP3WyrmholeState.FinalTowerPosition(tower), false);
        // FFLogs V4F6z9GCthdf2Ppq / fight 42: 03:13.037 is this fragment's time zero.
        world.Events.Add(3f, () => boss?.Cast(DsrP3WyrmholeConstants.DiveFromGrace, castSeconds: 4.7f, fireDelay: .262f));
        world.Events.Add(3f, AssignNumbers);
        world.Events.Add(8.721f, AssignArrows);
        world.Events.Add(10.105f, () => StartWheels(0, .259f));
        world.Events.Add(17.708f, () => ClearJumpStatuses(0));
        world.Events.Add(17.797f, () => Jump(0));
        world.Events.Add(17.930f, () => ResolveStack(0));
        world.Events.Add(21.330f, () => ResolveWheel(0, true));
        world.Events.Add(21.910f, () => ShowTowers(0, .300f));
        world.Events.Add(24.410f, () => ResolveTowers(0));
        world.Events.Add(24.455f, () => ResolveWheel(0, false));
        world.Events.Add(27.047f, () => BaitLines(0, .271f));
        world.Events.Add(27.717f, () => ClearJumpStatuses(1));
        world.Events.Add(27.807f, () => Jump(1));
        world.Events.Add(31.518f, () => ResolveLines(0));
        world.Events.Add(31.606f, () => StartWheels(1, .298f));
        world.Events.Add(31.920f, () => ShowTowers(1, .257f));
        world.Events.Add(34.377f, () => ResolveTowers(1));
        world.Events.Add(37.013f, () => BaitLines(1, .270f));
        world.Events.Add(38.712f, () => ClearJumpStatuses(2));
        world.Events.Add(38.802f, () => Jump(2));
        world.Events.Add(39.471f, () => ResolveStack(2));
        world.Events.Add(41.483f, () => ResolveLines(1));
        world.Events.Add(42.869f, () => ResolveWheel(1, true));
        world.Events.Add(42.914f, () => ShowTowers(2, .259f));
        world.Events.Add(45.373f, () => ResolveTowers(2));
        world.Events.Add(46.000f, () => ResolveWheel(1, false));
        world.Events.Add(48.012f, () => BaitLines(2, .267f));
        world.Events.Add(48.012f, () => state.TrackingMainTank = true);
        world.Events.Add(50.156f, AutoAttack);
        world.Events.Add(52.479f, () => ResolveLines(2));
        world.Events.Add(53.285f, AutoAttack);
        world.Events.Add(53.982f, BeginLanceTurn);
        world.Events.Add(54.582f, StartLance);
        world.Events.Add(54.627f, StartLanceAoe);
        world.Events.Add(58.114f, ResolveLance);
        world.Events.Add(59.590f, ShowFinalTowers);
        world.Events.Add(64.553f, ResolveFinalTowers);
        world.Events.Add(64.553f, StartSoulTethers);
        world.Events.Add(67.189f, BaitFinalLines);
        world.Events.Add(71.617f, ResolveSoulTethers);
        world.Events.Add(71.661f, ResolveFinalLines);
        world.Events.Add(72f, () => state.TrackingMainTank = true);
        world.Events.Add(76.759f, AutoAttack);
        world.Events.Add(79.889f, AutoAttack);
        world.Events.Add(83.020f, AutoAttack);
        world.Events.Add(86.149f, AutoAttack);
        world.Events.Add(89.281f, AutoAttack);
        world.Events.Add(92.170f, BeginFinalLanceTurn);
        world.Events.Add(92.770f, StartLance);
        world.Events.Add(92.815f, StartLanceAoe);
        world.Events.Add(96.302f, ResolveLance);
        world.Events.Add(98f, () =>
        {
            HideFinalDrakes();
            state.Complete = true;
        });
    }

    private SimEnemy? Spawn(uint id, Vector3 position, bool visible)
        => world!.SpawnEnemy(new EnemySpawnConfig(id, Level: 90,
            EnemyList: id == DsrP3WyrmholeConstants.Drake ? EnemyListMode.Manual : visible ? EnemyListMode.ScenarioVisible : EnemyListMode.Never,
            IsVisible: visible, Placement: new(position, MathF.PI), DisableLookAt: true,
            NameId: id is DsrP3WyrmholeConstants.Nidhogg or DsrP3WyrmholeConstants.Drake
                ? DsrP3WyrmholeConstants.NidhoggName : 0));

    private void AssignNumbers()
    {
        state!.NumbersAssigned = true;
        for (var role = 0; role < 8; role++)
        {
            var member = world!.Party.Get(role);
            member?.AddStatus((ushort)(DsrP3WyrmholeConstants.First + state.Order[role]), 9999);
            // Match the cast bar's real-time duration, excluding its post-cast release delay.
            member?.AttachLockonVfx(DsrP3WyrmholeConstants.FirstHeadmarker + (uint)state.Order[role], 4.7f);
        }
    }

    private void AssignArrows()
    {
        state!.ArrowsAssigned = true;
        for (var role = 0; role < 8; role++)
            world!.Party.Get(role)?.AddStatus(DirectionStatus(role), state.Order[role] switch { 0 => 9, 1 => 19, _ => 30 });
    }

    private void ClearJumpStatuses(int wave)
    {
        for (var role = 0; role < 8; role++)
            if (state!.Order[role] == wave)
            {
                var member = world!.Party.Get(role);
                member?.RemoveStatus((ushort)(DsrP3WyrmholeConstants.First + wave));
                member?.RemoveStatus(DirectionStatus(role));
            }
    }

    private ushort DirectionStatus(int role) => state!.Direction[role] switch
    {
        1 => DsrP3WyrmholeConstants.Forward,
        -1 => DsrP3WyrmholeConstants.Backward,
        _ => DsrP3WyrmholeConstants.Center
    };

    private void StartWheels(int round, float fireDelay)
        => boss?.Cast(state!.OutFirst[round] ? DsrP3WyrmholeConstants.GnashAndLash : DsrP3WyrmholeConstants.LashAndGnash,
            castSeconds: 7.3f, fireDelay: fireDelay);

    private void Jump(int wave)
    {
        for (var role = 0; role < 8; role++)
        {
            if (state!.Order[role] != wave) continue;
            var member = world!.Party.Get(role);
            if (!member.IsAlive()) { Fail("尼德霍格跳躍點名者已倒下"); continue; }
            var landing = DsrP3WyrmholeState.Landing(member!.Position, member.Rotation, state.Direction[role]);
            var lane = state.LandingLane(role);
            state.Towers[wave][lane] = landing;
            if (landing.Length() > 20) Fail("尼德霍格：塔落在場外，請檢查站位與面向");
            foreach (var other in world.Party.ActiveMembers())
                if (other != member && Vector3.DistanceSquared(other.Position, member.Position) < 25)
                    Hit(other, "尼德霍格：被其他人的跳躍範圍命中");
            var clone = drakes[wave][lane];
            clone?.SetPosition(landing);
            clone?.SetVisible(true);
            var action = state.Direction[role] switch
            {
                1 => DsrP3WyrmholeConstants.ForwardJump,
                -1 => DsrP3WyrmholeConstants.BackwardJump,
                _ => DsrP3WyrmholeConstants.HighJump
            };
            clone?.Cast(action, landing, 0, clone.GameObjectId);
        }
    }

    private void ResolveStack(int jumpWave)
    {
        var targets = world!.Party.ActiveMembers().Where(m => m.Position.Z < 0).ToArray();
        if (targets.Length == 0) { Fail("尼德霍格：北側沒有分攤目標"); return; }
        var target = targets.OrderBy(m => Vector3.DistanceSquared(m.Position, new Vector3(0, 0, -7))).First();
        var stack = world.Party.ActiveMembers().Where(m => Vector3.DistanceSquared(m.Position, target.Position) <= 36).ToArray();
        if (stack.Length != 5) Fail($"尼德霍格：北側分攤需要五人，目前 {stack.Length} 人");
        for (var role = 0; role < 8; role++)
            if (state!.Order[role] == jumpWave && stack.Contains(world.Party.Get(role)))
                Fail("尼德霍格：本輪跳躍點名者不能參與分攤");
        // The release timeline resolves mon_sp/[SKL_ID]/mon_sp004 on Nidhogg's skeleton.
        boss?.Cast(DsrP3WyrmholeConstants.Stack, target.Position, 0, target.GameObjectId);
    }

    private void ResolveWheel(int round, bool first)
    {
        var gnash = first == state!.OutFirst[round];
        boss?.Cast(gnash ? DsrP3WyrmholeConstants.Gnash : DsrP3WyrmholeConstants.Lash, castSeconds: 0);
        foreach (var member in world!.Party.ActiveMembers())
            if (gnash ? member.Position.LengthSquared() < 64 : member.Position.LengthSquared() > 64)
                Hit(member, gnash ? "尼德霍格：未躲開鋼鐵（離開八公尺內圈）" : "尼德霍格：未躲開月環（進入八公尺內圈）");
    }

    private void ShowTowers(int wave, float fireDelay)
    {
        state!.TowersVisible[wave] = true;
        for (var lane = 0; lane < drakes[wave].Length; lane++)
            drakes[wave][lane]?.Cast(DsrP3WyrmholeConstants.Tower, castSeconds: 2.2f, fireDelay: fireDelay);
    }

    private void ResolveTowers(int wave)
    {
        state!.TowersVisible[wave] = false;
        foreach (var clone in drakes[wave]) clone?.SetVisibleInEnemyList(true);
        state.LineTracking[wave] = true;
        TrackLines();
        foreach (var tower in state.Towers[wave])
        {
            var inside = world!.Party.FilledSlots().Where(pair => Vector3.DistanceSquared(pair.Item2.Position, tower) <= 25).ToArray();
            if (inside.Length != 1) { Fail($"尼德霍格第 {wave + 1} 輪塔需要一人，目前 {inside.Length} 人"); continue; }
            var role = (int)inside[0].Item1;
            var eligible = wave switch { 0 => state.Order[role] == 2, 1 => state.Order[role] == 0, _ => state.Order[role] != 2 };
            if (!eligible) Hit(inside[0].Item2, "尼德霍格：本輪不應由你的數字組踩塔");
        }
    }

    private void BaitLines(int wave, float fireDelay)
    {
        state!.LineTracking[wave] = false;
        foreach (var clone in drakes[wave])
        {
            if (clone == null) continue;
            var target = world!.Party.ActiveMembers().Where(m => m.IsAlive())
                .OrderBy(m => Vector3.DistanceSquared(m.Position, clone.Position)).FirstOrDefault();
            if (target == null) { Fail("尼德霍格：直線沒有引導目標"); continue; }
            var direction = DsrP3WyrmholeState.AtRadius(target.Position - clone.Position, 1);
            var rotation = MathF.Atan2(direction.X, direction.Z);
            clone.HoldFacing(rotation);
            clone.Cast(DsrP3WyrmholeConstants.Geirskogul, castSeconds: 4.2f, omenDelay: 3.2f, fireDelay: fireDelay);
            lines.Add((wave, clone.Position, direction));
        }
    }

    private void TrackLines()
    {
        for (var wave = 0; wave < drakes.Length; wave++)
        {
            if (!state!.LineTracking[wave]) continue;
            foreach (var clone in drakes[wave])
            {
                if (clone == null) continue;
                var target = world!.Party.ActiveMembers().Where(m => m.IsAlive())
                    .OrderBy(m => Vector3.DistanceSquared(m.Position, clone.Position)).FirstOrDefault();
                if (target == null) continue;
                var offset = target.Position - clone.Position;
                if (offset.LengthSquared() > .001f)
                    clone.HoldFacing(MathF.Atan2(offset.X, offset.Z));
            }
        }
    }

    internal static bool InLine(Vector3 position, Vector3 source, Vector3 direction)
    {
        var relative = position - source;
        var along = Vector3.Dot(relative, direction);
        return along >= 0 && along <= 62 && (relative - direction * along).LengthSquared() < 16;
    }

    private void ResolveLines(int wave)
    {
        foreach (var line in lines.Where(line => line.Wave == wave))
            foreach (var member in world!.Party.ActiveMembers())
                if (InLine(member.Position, line.Source, line.Direction)) Hit(member, "尼德霍格：未躲開分身直線");
        lines.RemoveAll(line => line.Wave == wave);
        foreach (var clone in drakes[wave]) { clone?.SetVisible(false); clone?.SetVisibleInEnemyList(false); }
    }

    private void BeginLanceTurn()
    {
        state!.TrackingMainTank = false;
        var target = world!.Party.Get(state!.LanceTarget);
        if (target == null || boss == null) return;
        var offset = target.Position - boss.Position;
        state.LanceRotation = offset.LengthSquared() > .001f ? MathF.Atan2(offset.X, offset.Z) : boss.Rotation;
        state.LanceTurnStartRotation = boss.Rotation;
        state.LanceTurnStartTime = 53.982f;
        state.TurningForLance = true;
    }

    private void StartLance()
    {
        state!.TurningForLance = false;
        boss?.HoldFacing(state.LanceRotation);
        boss?.Cast(DsrP3WyrmholeConstants.Drachenlance, castSeconds: 2.6f, fireDelay: .263f);
    }

    private void AutoAttack()
    {
        var target = world!.Party.Get(0);
        if (target == null || !target.IsAlive()) return;
        // Supplying a ground position would snap the facing in SimCast.FaceTarget.
        boss?.Cast(DsrP3WyrmholeConstants.AutoAttack, castSeconds: 0, targetId: target.GameObjectId);
    }

    internal static float TurnTowards(float current, float target, float maxStep)
        => current + Math.Clamp(MathF.IEEERemainder(target - current, MathF.Tau), -MathF.Max(0, maxStep), MathF.Max(0, maxStep));

    private void UpdateBossFacing(float delta, float elapsed)
    {
        if (boss == null) return;
        if (state!.TurningForLance)
        {
            var progress = Math.Clamp((elapsed - state.LanceTurnStartTime) / .6f, 0, 1);
            var eased = progress * progress * (3 - 2 * progress);
            var angle = MathF.IEEERemainder(state.LanceRotation - state.LanceTurnStartRotation, MathF.Tau);
            boss.HoldFacing(state.LanceTurnStartRotation + angle * eased);
        }
        else if (state.TrackingMainTank && world!.Party.Get(0) is { } tank && tank.IsAlive())
        {
            var offset = tank.Position - boss.Position;
            if (offset.LengthSquared() > .001f)
                boss.HoldFacing(TurnTowards(boss.Rotation, MathF.Atan2(offset.X, offset.Z), MathF.Tau * delta));
        }
    }

    private void ShowFinalTowers()
    {
        state!.FinalTowersVisible = true;
        for (var tower = 0; tower < finalDrakes.Length; tower++)
        {
            var count = state.FinalTowerCounts[tower];
            finalDrakes[tower]?.Cast(DsrP3WyrmholeConstants.FinalTowerOne + (uint)count - 1, castSeconds: 4.7f, fireDelay: .263f);
        }
    }

    private void ResolveFinalTowers()
    {
        state!.FinalTowersVisible = false;
        state.FinalTowersResolved = true;
        for (var tower = 0; tower < finalDrakes.Length; tower++)
        {
            finalDrakes[tower]?.SetVisible(true);
            finalDrakes[tower]?.SetVisibleInEnemyList(true);
            var position = DsrP3WyrmholeState.FinalTowerPosition(tower);
            var count = world!.Party.ActiveMembers().Count(m => m.IsAlive() && Vector3.DistanceSquared(m.Position, position) <= 25);
            if (count != state.FinalTowerCounts[tower])
                Fail($"最後四塔：第 {tower + 1} 座需要 {state.FinalTowerCounts[tower]} 人，目前 {count} 人");
        }
    }

    private void StartLanceAoe()
    {
        var helper = Spawn(DsrConstants.Npc.Helper, Vector3.Zero, false);
        helper?.HoldFacing(state!.LanceRotation);
        helper?.Cast(DsrP3WyrmholeConstants.DrachenlanceAoe, castSeconds: 3.2f, fireDelay: .287f);
    }

    private void ResolveLance()
    {
        var forward = new Vector3(MathF.Sin(state!.LanceRotation), 0, MathF.Cos(state.LanceRotation));
        foreach (var member in world!.Party.ActiveMembers())
            if (member.Position.LengthSquared() < 169 && (member.Position.LengthSquared() < .001f ||
                Vector3.Dot(Vector3.Normalize(member.Position), forward) > .7071068f))
                Hit(member, "尼德霍格：被龍槍扇形命中");
    }

    private void Hit(SimCharacter member, string reason) { state!.Failed = true; member.Die(reason); }
    private void Fail(string reason) { state!.Failed = true; world!.Party.WipeAllPlayers(reason); }

    public void Tick(float delta, float elapsed)
    {
        if (state == null || world == null) return;
        state.Time = elapsed;
        UpdateBossFacing(delta, elapsed);
        UpdateSoulTethers();
        TrackFinalLines();
        TrackLines();
        DsrP3WyrmholeAi.Tick(state, world);
    }
}
