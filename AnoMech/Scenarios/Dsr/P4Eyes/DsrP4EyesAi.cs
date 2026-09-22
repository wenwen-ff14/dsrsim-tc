using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P4Eyes;

internal sealed class DsrP4EyesAi : IScenarioAi
{
    public string Name => "tuuf／坦補紅線、DPS 藍線";
    public static Vector3 Destination(DsrP4EyesState s, int role)
    {
        if (!s.BuffsApplied) return DsrP4EyesState.BuffPosition;
        if (!s.ColorsAssigned) return DsrP4EyesState.Opening(role);
        if (s.BlueDone || s.MirageStarted)
        {
            if (s.SwapTarget[role] >= 0) return s.Positions[s.SwapTarget[role]];
            return s.Red[role] && s.DiveLane[role] >= 0 ? DsrP4EyesState.DivePosition(s.DiveLane[role]) : DsrP4EyesState.BlueEye;
        }
        if (!s.YellowDone)
        {
            if (s.Red[role] != (role < 4)) return Vector3.Zero;
            return role < 4 ? DsrP4EyesState.YellowWait(role, s.YellowReady ? 1 : 3) : DsrP4EyesState.Opening(role);
        }
        if (role < 4) return DsrP4EyesState.YellowWait(role, 1);
        if (!s.Red[role])
        {
            var partner = DsrP4EyesState.Partner(role);
            var target = DsrP4EyesState.YellowWait(partner, 1);
            if (MathF.Abs(s.Positions[role].Z - target.Z) > .1f)
                return new(s.Positions[role].X, 0, target.Z);
            return target;
        }
        var orb = DsrP4EyesState.OrbPosition(role - 2);
        return s.BlueReady ? orb : orb + new Vector3(0, 0, MathF.Sign(orb.Z) * 3);
    }
    public static void Tick(DsrP4EyesState state, SimWorld world)
    {
        if (state.Complete) return;
        for (var role = 0; role < 8; role++)
            if (world.Party.Get(role) is SimPartyNpc npc && npc.IsAlive())
            {
                var destination = Destination(state, role);
                if (state.Destinations[role] == destination) continue;
                state.Destinations[role] = destination;
                npc.MoveTo(destination, 6);
            }
    }
}
