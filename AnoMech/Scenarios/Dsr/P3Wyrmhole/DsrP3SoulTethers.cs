using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P3Wyrmhole;

public sealed partial class DsrP3WyrmholeScenario
{
    private readonly SimTether?[] soulTethers = new SimTether?[2];

    private SimEnemy? SoulSource(int index) => index == 0 ? boss : finalDrakes[state!.TetherClone];

    private void StartSoulTethers()
    {
        state!.TethersActive = true;
        for (var i = 0; i < 2; i++)
            soulTethers[i] = world!.Tether(SoulSource(i), world.Party.Get(state.TetherOwners[i]), DsrP3WyrmholeConstants.SoulTetherLink);
        UpdateSoulTethers();
    }

    internal static bool InterceptsSoulTether(Vector3 position, Vector3 source, Vector3 holder)
    {
        var beam = holder - source;
        if (beam.LengthSquared() < .01f) return false;
        var progress = Vector3.Dot(position - source, beam) / beam.LengthSquared();
        return progress >= 0 && progress < 1 && Vector3.DistanceSquared(position, source + beam * progress) <= 1;
    }

    private void UpdateSoulTethers()
    {
        if (!state!.TethersActive) return;
        for (var i = 0; i < 2; i++)
        {
            var source = SoulSource(i);
            var holder = world!.Party.Get(state.TetherOwners[i]);
            var tank = world.Party.Get(i);
            if (source == null || holder == null || tank == null) continue;
            state.TetherPickup[i] = Vector3.Lerp(source.Position, holder.Position, .5f);
            if (state.TetherOwners[i] == i || !tank.IsAlive() ||
                !InterceptsSoulTether(tank.Position, source.Position, holder.Position)) continue;
            soulTethers[i]?.Despawn();
            state.TetherOwners[i] = i;
            soulTethers[i] = world.Tether(source, tank, DsrP3WyrmholeConstants.SoulTetherLink);
        }
    }

    private void ResolveSoulTethers()
    {
        UpdateSoulTethers();
        state!.TethersActive = false;
        state.TethersResolved = true;
        for (var i = 0; i < 2; i++)
        {
            var target = world!.Party.Get(state.TetherOwners[i]);
            if (state.TetherOwners[i] != i || !target.IsAlive()) Fail(i == 0 ? "MT 未接到本體線" : "ST 未接到分身線");
            if (target != null)
            {
                SoulSource(i)?.Cast(DsrP3WyrmholeConstants.SoulTether, target.Position, 0, target.GameObjectId);
                foreach (var (role, member) in world.Party.FilledSlots())
                    if ((int)role >= 2 && Vector3.DistanceSquared(member.Position, target.Position) < 25)
                        Hit(member, "靈魂羈絆：被坦克接線範圍波及");
            }
            soulTethers[i]?.Despawn();
        }
    }

    private void TrackFinalLines()
    {
        if (!state!.FinalTowersResolved || state.Time >= 67.189f) return;
        for (var tower = 0; tower < 4; tower++)
        {
            if (tower == state.TetherClone || finalDrakes[tower] is not { } clone) continue;
            var target = world!.Party.ActiveMembers().Where(m => m.IsAlive())
                .MinBy(m => Vector3.DistanceSquared(m.Position, clone.Position));
            if (target != null)
            {
                var offset = target.Position - clone.Position;
                if (offset.LengthSquared() > .001f) clone.HoldFacing(MathF.Atan2(offset.X, offset.Z));
            }
        }
    }

    private void BaitFinalLines()
    {
        for (var tower = 0; tower < 4; tower++)
        {
            if (tower == state!.TetherClone || finalDrakes[tower] is not { } clone) continue;
            var direction = new Vector3(MathF.Sin(clone.Rotation), 0, MathF.Cos(clone.Rotation));
            clone.Cast(DsrP3WyrmholeConstants.Geirskogul, castSeconds: 4.2f, omenDelay: 3.2f, fireDelay: .272f);
            lines.Add((3, clone.Position, direction));
        }
    }

    private void ResolveFinalLines()
    {
        foreach (var line in lines.Where(l => l.Wave == 3))
            foreach (var member in world!.Party.ActiveMembers())
                if (InLine(member.Position, line.Source, line.Direction)) Hit(member, "未躲開四塔後的引導槍");
        lines.RemoveAll(l => l.Wave == 3);
        HideFinalDrakes();
    }

    private void HideFinalDrakes()
    {
        foreach (var clone in finalDrakes) { clone?.SetVisible(false); clone?.SetVisibleInEnemyList(false); }
    }

    private void BeginFinalLanceTurn()
    {
        state!.TrackingMainTank = false;
        state.LanceRotation = 0;
        state.LanceTurnStartRotation = boss?.Rotation ?? 0;
        state.LanceTurnStartTime = 92.170f;
        state.TurningForLance = true;
    }
}
