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
    public string Name => "數字龍（Easthogg）";
    public IPhase Phase => DsrZone.P3;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } = [new DsrP3WyrmholeAi()];
    private DsrP3WyrmholeState? state;
    private SimWorld? world;
    private SimEnemy? boss;
    private readonly SimEnemy?[][] drakes = [new SimEnemy?[3], new SimEnemy?[2], new SimEnemy?[3]];
    private readonly List<(int Wave, Vector3 Source, Vector3 Direction)> lines = [];
    private bool fixedSeed, showHints = true, showMap = true;
    private int seed = 1;

    public void Run(SimWorld simWorld, int? selectedAi)
    {
        world = simWorld;
        state = new(fixedSeed ? seed : Random.Shared.Next());
        for (var role = 0; role < 8; role++)
            if (world.Party.Get(role) is SimPartyNpc npc)
            {
                npc.SetPosition(DsrP3WyrmholeAi.OpeningPosition(role));
                npc.SetRotation(MathF.PI / 2);
            }
        lines.Clear();
        foreach (var wave in drakes) Array.Clear(wave);
        boss = Spawn(DsrP3WyrmholeConstants.Nidhogg, Vector3.Zero, true);
        boss?.SetTargetable(true);
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
        world.Events.Add(52.479f, () => ResolveLines(2));
        world.Events.Add(54.582f, StartLance);
        world.Events.Add(54.627f, StartLanceAoe);
        world.Events.Add(58.114f, ResolveLance);
        world.Events.Add(59f, () => state.Complete = true);
    }

    private SimEnemy? Spawn(uint id, Vector3 position, bool visible)
        => world!.SpawnEnemy(new EnemySpawnConfig(id, Level: 90,
            EnemyList: visible ? EnemyListMode.ScenarioVisible : EnemyListMode.Never,
            IsVisible: visible, Placement: new(position, MathF.PI), DisableLookAt: true,
            NameId: id is DsrP3WyrmholeConstants.Nidhogg or DsrP3WyrmholeConstants.Drake
                ? DsrP3WyrmholeConstants.NidhoggName : 0));

    private void AssignNumbers()
    {
        state!.NumbersAssigned = true;
        for (var role = 0; role < 8; role++)
            world!.Party.Get(role)?.AddStatus((ushort)(DsrP3WyrmholeConstants.First + state.Order[role]), 9999);
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
            if (!member.IsAlive()) { Fail("數字龍跳躍點名者已倒下"); continue; }
            var landing = DsrP3WyrmholeState.Landing(member!.Position, member.Rotation, state.Direction[role]);
            var lane = state.LandingLane(role);
            state.Towers[wave][lane] = landing;
            if (landing.Length() > 20) Fail("數字龍：塔落在場外，請檢查站位與面向");
            foreach (var other in world.Party.ActiveMembers())
                if (other != member && Vector3.DistanceSquared(other.Position, member.Position) < 25)
                    Hit(other, "數字龍：被其他人的跳躍範圍命中");
            var clone = drakes[wave][lane] = Spawn(DsrP3WyrmholeConstants.Drake, landing, true);
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
        if (targets.Length == 0) { Fail("數字龍：北側沒有分攤目標"); return; }
        var target = targets.OrderBy(m => Vector3.DistanceSquared(m.Position, new Vector3(0, 0, -7))).First();
        var stack = world.Party.ActiveMembers().Where(m => Vector3.DistanceSquared(m.Position, target.Position) <= 36).ToArray();
        if (stack.Length != 5) Fail($"數字龍：北側分攤需要五人，目前 {stack.Length} 人");
        for (var role = 0; role < 8; role++)
            if (state!.Order[role] == jumpWave && stack.Contains(world.Party.Get(role)))
                Fail("數字龍：本輪跳躍點名者不能參與分攤");
        Spawn(DsrConstants.Npc.Helper, target.Position, false)?.Cast(DsrP3WyrmholeConstants.Stack, target.Position, 0, target.GameObjectId);
    }

    private void ResolveWheel(int round, bool first)
    {
        var gnash = first == state!.OutFirst[round];
        boss?.Cast(gnash ? DsrP3WyrmholeConstants.Gnash : DsrP3WyrmholeConstants.Lash, castSeconds: 0);
        foreach (var member in world!.Party.ActiveMembers())
            if (gnash ? member.Position.LengthSquared() < 64 : member.Position.LengthSquared() > 64)
                Hit(member, gnash ? "數字龍：未躲開鋼鐵（離開八公尺內圈）" : "數字龍：未躲開月環（進入八公尺內圈）");
    }

    private void ShowTowers(int wave, float fireDelay)
    {
        state!.TowersVisible[wave] = true;
        for (var lane = 0; lane < drakes[wave].Length; lane++)
        {
            drakes[wave][lane]?.Cast(DsrP3WyrmholeConstants.Tower, castSeconds: 2.2f, fireDelay: fireDelay);
            world!.SpawnOmen("vfx/omen/eff/general01f.avfx", new(state.Towers[wave][lane], 0), new(5, 1, 5), 2.2f + fireDelay);
        }
    }

    private void ResolveTowers(int wave)
    {
        state!.TowersVisible[wave] = false;
        foreach (var tower in state.Towers[wave])
        {
            var inside = world!.Party.FilledSlots().Where(pair => Vector3.DistanceSquared(pair.Item2.Position, tower) <= 25).ToArray();
            if (inside.Length != 1) { Fail($"數字龍第 {wave + 1} 輪塔需要一人，目前 {inside.Length} 人"); continue; }
            var role = (int)inside[0].Item1;
            var eligible = wave switch { 0 => state.Order[role] == 2, 1 => state.Order[role] == 0, _ => state.Order[role] != 2 };
            if (!eligible) Hit(inside[0].Item2, "數字龍：本輪不應由你的數字組踩塔");
        }
    }

    private void BaitLines(int wave, float fireDelay)
    {
        foreach (var clone in drakes[wave])
        {
            if (clone == null) continue;
            var target = world!.Party.ActiveMembers().OrderBy(m => Vector3.DistanceSquared(m.Position, clone.Position)).FirstOrDefault();
            if (target == null) { Fail("數字龍：直線沒有引導目標"); continue; }
            var direction = DsrP3WyrmholeState.AtRadius(target.Position - clone.Position, 1);
            var rotation = MathF.Atan2(direction.X, direction.Z);
            clone.HoldFacing(rotation);
            clone.Cast(DsrP3WyrmholeConstants.Geirskogul, castSeconds: 4.2f, fireDelay: fireDelay);
            lines.Add((wave, clone.Position, direction));
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
                if (InLine(member.Position, line.Source, line.Direction)) Hit(member, "數字龍：未躲開分身直線");
        lines.RemoveAll(line => line.Wave == wave);
        foreach (var clone in drakes[wave]) clone?.SetVisible(false);
    }

    private void StartLance()
    {
        var target = world!.Party.Get(state!.RoleAt(0, 0));
        if (target == null) return;
        state.LanceRotation = MathF.Atan2(target.Position.X, target.Position.Z);
        boss?.HoldFacing(state.LanceRotation);
        boss?.Cast(DsrP3WyrmholeConstants.Drachenlance, castSeconds: 2.6f, fireDelay: .263f);
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
                Hit(member, "數字龍：被龍槍扇形命中");
    }

    private void Hit(SimCharacter member, string reason) { state!.Failed = true; member.Die(reason); }
    private void Fail(string reason) { state!.Failed = true; world!.Party.WipeAllPlayers(reason); }

    public void Tick(float delta, float elapsed)
    {
        if (state == null || world == null) return;
        state.Time = elapsed;
        DsrP3WyrmholeAi.Tick(state, world);
    }
}
