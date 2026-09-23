using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P2Sanctity;

internal sealed class DsrP2SanctityAi : IScenarioAi
{
    public string Name => "tuuf／Elemental（同職能調整）";

    public static Vector3 Destination(DsrP2SanctityState state, int role)
    {
        return state.Stage switch
        {
            SanctityStage.Preparation => DsrP2SanctityState.OpeningPosition(role),
            SanctityStage.Swords when !state.SwordGroupsMoving => DsrP2SanctityState.OpeningPosition(role),
            SanctityStage.Swords => state.SwordPosition(role, false),
            SanctityStage.Charges => state.SwordPosition(role, state.Explosions >= (state.EarlyMove ? 3 : 5)),
            SanctityStage.Pairs => state.PreviewTowerAssignment ? state.TowerPreviewPosition(role) : state.PairPosition(role),
            SanctityStage.FirstTowers => state.HasMeteor(role) ? state.MeteorStart(role) :
                Vector3.Normalize(state.FirstTower(role)) * (state.StartsInside(role) ? 3.5f : 20.4f),
            SanctityStage.Meteors when state.HasMeteor(role) && state.MeteorElapsed < 10.02f =>
                state.MeteorPosition(role, Math.Clamp(state.MeteorElapsed / 10.02f, 0, 1)),
            SanctityStage.Meteors when state.StartsInside(role) && !state.KnockbackResolved =>
                DsrP2SanctityState.Polar(state.SecondTowerAngle(role), 2),
            SanctityStage.Meteors when !state.KnockbackResolved =>
                DsrP2SanctityState.Polar(state.SecondTowerAngle(role), 20),
            SanctityStage.Meteors or SanctityStage.SecondTowers => state.SecondTower(role),
            _ => Vector3.Zero
        };
    }

    public static void Tick(DsrP2SanctityState state, SimWorld world)
    {
        if (state.Stage == SanctityStage.Complete) return;
        for (var role = 0; role < 8; role++)
        {
            if (world.Party.Get(role) is not SimPartyNpc npc || !npc.IsAlive()) continue;
            if (state.KnockbackResolved && state.KnockbackElapsed < .7f && state.StartsInside(role)) continue;
            var destination = Destination(state, role);
            if (state.Stage == SanctityStage.FirstTowers && !state.StartsInside(role))
            {
                var current = DsrP2SanctityState.Angle(npc.Position);
                var change = DsrP2SanctityState.SignedAngle(DsrP2SanctityState.Angle(destination) - current);
                if (MathF.Abs(change) > 4)
                    destination = DsrP2SanctityState.Polar(current + (npc.Position.Length() < 19.5f ? 0 : Math.Clamp(change, -4, 4)), 20.4f);
            }
            if (state.Stage == SanctityStage.FirstTowers && state.StartsInside(role) && npc.Position.Length() > 3 &&
                MathF.Abs(DsrP2SanctityState.SignedAngle(DsrP2SanctityState.Angle(npc.Position) - DsrP2SanctityState.Angle(destination))) > 50)
                destination = Vector3.Normalize(npc.Position) * 2;
            if (state.Stage == SanctityStage.Meteors && !state.HasMeteor(role) && !state.StartsInside(role))
            {
                var current = DsrP2SanctityState.Angle(npc.Position);
                var change = DsrP2SanctityState.SignedAngle(DsrP2SanctityState.Angle(destination) - current);
                if (MathF.Abs(change) > 4)
                    destination = DsrP2SanctityState.Polar(current + Math.Clamp(change, -4, 4), 20);
            }
            if (state.Stage == SanctityStage.Meteors && state.HasMeteor(role) && state.MeteorElapsed < 10.02f)
            {
                var current = DsrP2SanctityState.Angle(npc.Position);
                var change = DsrP2SanctityState.SignedAngle(DsrP2SanctityState.Angle(destination) - current);
                destination = DsrP2SanctityState.Polar(current + Math.Clamp(change, -4, 4), 19.5f);
            }
            var away = -Vector3.Normalize(state.EyePosition - destination) - Vector3.Normalize(state.BossPosition - destination);
            var safeFacing = MathF.Atan2(away.X, away.Z);
            npc.MoveTo(destination, state.Stage == SanctityStage.Meteors && state.HasMeteor(role) ? 7.8f : 6f,
                state.Stage is SanctityStage.Swords or SanctityStage.Charges ? safeFacing : null);
        }
    }
}
