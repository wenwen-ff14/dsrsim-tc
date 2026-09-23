using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P4Eyes;

public sealed partial class DsrP4EyesScenario : IScenario
{
    public string Name => "雙眼";
    public IPhase Phase => DsrZone.P4;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } = [new DsrP4EyesAi()];
    private SimWorld? world;
    private DsrP4EyesState? state;
    private SimEnemy? redEye, blueEye, haurchefant, ysayle, estinien, alphinaud;
    private readonly SimEnemy?[] orbs = new SimEnemy?[6];
    private readonly SimEnemy?[] divers = new SimEnemy?[8];
    private readonly SimTether?[] tethers = new SimTether?[8];
    private bool showMap = true, showHints = true;
    private int? validationSeed = null;

    public void Run(SimWorld simWorld, int? selectedAi)
    {
        world = simWorld;
        state = new(validationSeed ?? Random.Shared.Next());
        Array.Clear(tethers);
        redEye = Spawn(DsrP4EyesConstants.RedEye, 11317, DsrP4EyesState.RedEye, false);
        blueEye = Spawn(DsrP4EyesConstants.BlueEye, 11318, DsrP4EyesState.BlueEye, false);
        haurchefant = Spawn(DsrP4EyesConstants.Haurchefant, 1455, new(-3, 0, 5), true);
        ysayle = Spawn(DsrP4EyesConstants.Ysayle, 11313, new(3, 0, 5), true);
        estinien = Spawn(DsrP4EyesConstants.Estinien, 11312, Vector3.Zero, true);
        alphinaud = Spawn(DsrP4EyesConstants.Alphinaud, 4130, DsrP4EyesState.BuffPosition, true);
        for (var i = 0; i < 6; i++)
            orbs[i] = Spawn(i < 2 ? DsrP4EyesConstants.YellowOrb : DsrP4EyesConstants.BlueOrb, 0, DsrP4EyesState.OrbPosition(i), false);
        for (var i = 0; i < 8; i++) divers[i] = Spawn(P3Wyrmhole.DsrP3WyrmholeConstants.Drake, 3458, Vector3.Zero, false);
        // Relative cadence follows the Eyes sequence; these are not calibrated FFLogs timestamps.
        world.Events.Add(17.3f, () => { redEye?.SetVisible(true); blueEye?.SetVisible(true); redEye?.SetTargetable(true); blueEye?.SetTargetable(true); });
        world.Events.Add(17.5f, ApplyBuffs);
        world.Events.Add(20f, () => { haurchefant?.SetVisible(false); ysayle?.SetVisible(false); });
        world.Events.Add(24.6f, () => estinien?.Cast(DsrP4EyesConstants.Resentment, castSeconds: 0));
        world.Events.Add(30f, () => CastEyes(DsrP4EyesConstants.Hatebound, 3));
        world.Events.Add(33.8f, AssignColors);
        world.Events.Add(36.8f, () => GrowOrbs(true, 1.4f));
        world.Events.Add(39.9f, () => { GrowOrbs(true, 2); state.YellowReady = true; });
        world.Events.Add(42.9f, () => GrowOrbs(false, 1.4f));
        world.Events.Add(44f, () => CheckOrbs(true));
        world.Events.Add(45.9f, () => { GrowOrbs(false, 2); state.BlueReady = true; });
        world.Events.Add(50f, () => CheckOrbs(false));
        world.Events.Add(56.2f, () => { state.MirageStarted = true; CastEyes(DsrP4EyesConstants.MirageDive, 3); });
        world.Events.Add(60f, () => Dive(0));
        world.Events.Add(65.1f, () => Dive(1));
        world.Events.Add(70.2f, () => Dive(2));
        world.Events.Add(75.3f, () => Dive(3));
        world.Events.Add(79.1f, () =>
        {
            // The solo exercise supplies party DPS after all four dive pairs have resolved.
            redEye?.SetVisible(false);
            redEye?.SetTargetable(false);
            blueEye?.Cast(DsrP4EyesConstants.SteepInRage, castSeconds: 6);
        });
        world.Events.Add(86f, Complete);
        RefreshPositions();
        DsrP4EyesAi.Tick(state, world);
    }

    private SimEnemy? Spawn(uint id, uint name, Vector3 position, bool visible)
        => world!.SpawnEnemy(new EnemySpawnConfig(id, NameId: name, Level: 90,
            EnemyList: id is DsrP4EyesConstants.RedEye or DsrP4EyesConstants.BlueEye ? EnemyListMode.ScenarioVisible : EnemyListMode.Never,
            IsVisible: visible, Placement: new(position, 0), DisableLookAt: true));

    private void ApplyBuffs()
    {
        state!.BuffsApplied = true;
        var target = world!.Party.Get((int)world.Party.PlayerRole);
        haurchefant?.Cast(DsrP4EyesConstants.Friendship, DsrP4EyesState.BuffPosition, 0, target?.GameObjectId);
        ysayle?.Cast(DsrP4EyesConstants.Devotion, DsrP4EyesState.BuffPosition, 0, alphinaud?.GameObjectId);
        foreach (var member in world.Party.ActiveMembers())
        {
            if (Vector3.DistanceSquared(member.Position, DsrP4EyesState.BuffPosition) > 25)
                Hit(member, "未在阿爾菲諾旁集合取得兩種思念增益");
            else
            {
                member.AddStatus(DsrP4EyesConstants.FriendshipStatus, 90);
                member.AddStatus(DsrP4EyesConstants.DevotionStatus, 90);
            }
        }
    }

    private void CastEyes(uint action, float seconds)
    {
        redEye?.Cast(action, castSeconds: seconds);
        blueEye?.Cast(action, castSeconds: seconds);
    }

    private void AssignColors()
    {
        state!.ColorsAssigned = true;
        for (var role = 0; role < 8; role++) UpdateColor(role);
        foreach (var orb in orbs) orb?.SetVisible(true);
    }

    private void UpdateColor(int role)
    {
        var member = world!.Party.Get(role);
        if (member == null) return;
        var red = state!.Red[role];
        member.RemoveStatus(red ? DsrP4EyesConstants.BlueStatus : DsrP4EyesConstants.RedStatus);
        member.AddStatus(red ? DsrP4EyesConstants.RedStatus : DsrP4EyesConstants.BlueStatus, 90);
        tethers[role]?.Despawn();
        tethers[role] = world.Tether(member, red ? redEye : blueEye, red ? DsrP4EyesConstants.RedLink : DsrP4EyesConstants.BlueLink);
    }

    private void SwapColors()
    {
        if (!state!.ColorsAssigned) return;
        for (var a = 0; a < 8; a++)
        for (var b = a + 1; b < 8; b++)
        {
            if (state.Red[a] == state.Red[b] || state.SwapCooldown[a] > 0 || state.SwapCooldown[b] > 0 ||
                !world!.Party.Get(a).IsAlive() || !world.Party.Get(b).IsAlive() ||
                Vector3.DistanceSquared(state.Positions[a], state.Positions[b]) > 1) continue;
            var outgoing = state.Red[a] ? a : b;
            var incoming = state.Red[a] ? b : a;
            (state.Red[a], state.Red[b]) = (state.Red[b], state.Red[a]);
            if (state.YellowDone && !state.BlueDone && !state.MirageStarted)
            {
                state.OrbExchangeDone[a] = state.Red[a] == (a >= 4);
                state.OrbExchangeDone[b] = state.Red[b] == (b >= 4);
            }
            if (state.MirageStarted || state.BlueDone)
            {
                state.DiveLane[incoming] = state.DiveLane[outgoing];
                state.DiveLane[outgoing] = -1;
                state.SwapTarget[incoming] = state.SwapTarget[outgoing] = -1;
                state.SwapWaitPosition[incoming] = state.SwapWaitPosition[outgoing] = null;
            }
            foreach (var role in new[] { a, b })
            {
                state.SwapCooldown[role] = 3;
                world.Party.Get(role)?.RemoveStatus(DsrP4EyesConstants.SwapLock);
                world.Party.Get(role)?.AddStatus(DsrP4EyesConstants.SwapLock, 3);
                UpdateColor(role);
            }
        }
    }

    private void GrowOrbs(bool yellow, float scale)
    {
        for (var orb = yellow ? 0 : 2; orb < (yellow ? 2 : 6); orb++) orbs[orb]?.SetScale(scale);
    }

    private void CheckOrbContacts()
    {
        if (!state!.ColorsAssigned || state.MirageStarted) return;
        for (var orb = 0; orb < 6; orb++)
        {
            if (state.OrbsPopped[orb]) continue;
            var position = DsrP4EyesState.OrbPosition(orb);
            var nearby = world!.Party.FilledSlots().Where(p => p.Item2.IsAlive() && Vector3.DistanceSquared(p.Item2.Position, position) <= 36).ToArray();
            if (!nearby.Any(p => Vector3.DistanceSquared(p.Item2.Position, position) <= 2.25f)) continue;
            state.OrbsPopped[orb] = true;
            var yellow = orb < 2;
            var ready = yellow ? state.YellowReady : state.BlueReady;
            if (!ready) Fail("撞球過早：球必須長大兩次才能碰觸");
            if (nearby.Length != (yellow ? 2 : 1)) Fail(yellow ? "黃球需要兩人共同承傷" : "藍球每顆只由一人承傷");
            foreach (var (role, member) in nearby)
                if (!state.Red[(int)role]) Hit(member, "撞球必須持有紅線，藍線承傷會治療右眼");
            orbs[orb]?.Cast(yellow ? DsrP4EyesConstants.FlareNova : DsrP4EyesConstants.FlareStar, position, 0);
            state.OrbFadeRemaining[orb] = 1;
        }
    }

    private void CheckOrbs(bool yellow)
    {
        if (yellow ? !state!.YellowDone : !state!.BlueDone) Fail(yellow ? "黃球未及時處理" : "藍球未及時處理");
        for (var i = yellow ? 0 : 2; i < (yellow ? 2 : 6); i++) orbs[i]?.SetVisible(false);
    }

    private void Dive(int wave)
    {
        var eligible = Enumerable.Range(0, 8).Where(r => state!.Red[r] && state.Piercing[r] <= 0 && world!.Party.Get(r).IsAlive()).ToArray();
        if (eligible.Length < 2) { Fail("幻象俯衝：沒有兩名可承傷的紅線目標，請正確換線"); return; }
        state!.Random.Shuffle(eligible);
        var targets = eligible.Take(2).OrderBy(r => state.DiveLane[r] switch { 0 => 0, 3 => 1, 2 => 2, _ => 3 }).ToArray();
        foreach (var role in targets)
        {
            var member = world!.Party.Get(role)!;
            if (state.DiveLane[role] < 0) Fail("幻象俯衝：坦補與 DPS 換線尚未完成");
            foreach (var other in world.Party.ActiveMembers())
                if (other != member && Vector3.DistanceSquared(other.Position, member.Position) < 16)
                    Hit(other, "幻象俯衝：被其他紅線目標的範圍波及");
            var cloneIndex = wave * 2 + Array.IndexOf(targets, role);
            var clone = divers[cloneIndex];
            clone?.SetPosition(member.Position);
            clone?.SetVisible(true);
            clone?.Cast(DsrP4EyesConstants.MirageHit, member.Position, 0, member.GameObjectId);
            state.DiveFadeRemaining[cloneIndex] = 2;
            state.Piercing[role] = 15;
            member.AddStatus(DsrP4EyesConstants.Piercing, 15);
        }
        if (wave == 0) state.FirstDps = targets.Order().ToArray();
        int[] incoming = wave switch { 0 => [2, 3], 1 => [0, 1], 2 => state.FirstDps, _ => [] };
        for (var i = 0; i < incoming.Length; i++)
        {
            state.SwapTarget[incoming[i]] = targets[i];
            state.SwapWaitPosition[targets[i]] = world!.Party.Get(targets[i])!.Position;
        }
        state.DiveCount++;
    }

    private void RefreshPositions()
    {
        for (var role = 0; role < 8; role++) state!.Positions[role] = world!.Party.Get(role)?.Position ?? Vector3.Zero;
    }

    private void Complete()
    {
        state!.Complete = true;
        foreach (var tether in tethers) tether?.Despawn();
        foreach (var actor in orbs.Concat(divers).Concat(new[] { redEye, blueEye, estinien, alphinaud, haurchefant, ysayle })) actor?.SetVisible(false);
        foreach (var member in world!.Party.ActiveMembers())
            foreach (var status in new[] { DsrP4EyesConstants.RedStatus, DsrP4EyesConstants.BlueStatus, DsrP4EyesConstants.SwapLock, DsrP4EyesConstants.Piercing, DsrP4EyesConstants.FriendshipStatus, DsrP4EyesConstants.DevotionStatus }) member.RemoveStatus(status);
    }
    private void Hit(SimCharacter member, string reason) { state!.Failed = true; member.Die(reason); }
    private void Fail(string reason) { state!.Failed = true; world!.Party.WipeAllPlayers(reason); }
    public void Tick(float delta, float elapsed)
    {
        if (state == null || world == null || state.Complete) return;
        state.Time = elapsed;
        for (var role = 0; role < 8; role++)
        {
            if (state.SwapCooldown[role] > 0)
            {
                state.SwapCooldown[role] = MathF.Max(0, state.SwapCooldown[role] - delta);
                if (state.SwapCooldown[role] == 0) world.Party.Get(role)?.RemoveStatus(DsrP4EyesConstants.SwapLock);
            }
            state.Piercing[role] = MathF.Max(0, state.Piercing[role] - delta);
        }
        RefreshPositions();
        SwapColors();
        CheckOrbContacts();
        HideResolvedActors(orbs, state.OrbFadeRemaining, delta);
        HideResolvedActors(divers, state.DiveFadeRemaining, delta);
        DsrP4EyesAi.Tick(state, world);
    }

    private static void HideResolvedActors(SimEnemy?[] actors, float[] remaining, float delta)
    {
        for (var i = 0; i < actors.Length; i++)
        {
            if (remaining[i] <= 0) continue;
            remaining[i] -= delta;
            if (remaining[i] <= 0) actors[i]?.SetVisible(false);
        }
    }
}
