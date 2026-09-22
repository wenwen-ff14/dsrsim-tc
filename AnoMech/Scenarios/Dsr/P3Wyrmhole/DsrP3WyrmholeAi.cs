using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P3Wyrmhole;

internal sealed class DsrP3WyrmholeAi : IScenarioAi
{
    public string Name => "tuuf／Elemental Easthogg";
    private static readonly float[] OpeningAngles = [0, 180, 270, 90, 225, 135, 315, 45];

    public static Vector3 OpeningPosition(int role)
    {
        var angle = OpeningAngles[role] * MathF.PI / 180;
        return new Vector3(MathF.Sin(angle), 0, -MathF.Cos(angle)) * 10;
    }

    public static Vector3 Destination(DsrP3WyrmholeState s, int role)
    {
        var t = s.Time;
        var order = s.Order[role];
        if (!s.NumbersAssigned) return OpeningPosition(role);
        if (!s.ArrowsAssigned) return s.NumberPosition(role);
        if (t < 10.105f) return s.JumpPosition(role);
        if (t < 17.931f) return order == 0 ? s.JumpPosition(role) : North(7);
        if (t < 24.456f) return WheelPosition(s, role, 0, t < 21.331f);
        if (t < 27.048f) return order == 2 ? Bait(s, 0, role) : order == 1 ? s.JumpPosition(role) : North(7);
        if (t < 27.808f) return order == 2 ? Dodge(s, 0, role) : order == 1 ? s.JumpPosition(role) : North(7);
        if (t < 31.519f) return order == 2 ? Dodge(s, 0, role) : North(7);
        if (t < 31.921f) return North(7);
        if (t < 34.378f) return SoakOrNorth(s, 1, role);
        if (t < 37.014f) return s.SoakLane(1, role) >= 0 ? Bait(s, 1, role) : order == 2 ? s.JumpPosition(role) : North(7);
        if (t < 39.472f) return s.SoakLane(1, role) >= 0 ? Dodge(s, 1, role) : order == 2 ? s.JumpPosition(role) : North(7);
        if (t < 46.001f) return WheelPosition(s, role, 2, t < 42.870f);
        if (t < 48.013f) return s.SoakLane(2, role) >= 0 ? Bait(s, 2, role) : North(7);
        if (t < 52.480f) return s.SoakLane(2, role) >= 0 ? Dodge(s, 2, role) : North(7);
        if (t < 54.583f) return North(7);
        return new Vector3(-MathF.Sin(s.LanceRotation), 0, -MathF.Cos(s.LanceRotation)) * 15;
    }

    private static Vector3 North(float radius) => new(0, 0, -radius);

    private static Vector3 WheelPosition(DsrP3WyrmholeState s, int role, int wave, bool first)
    {
        var outside = first == s.OutFirst[wave == 0 ? 0 : 1];
        var lane = s.SoakLane(wave, role);
        return DsrP3WyrmholeState.AtRadius(lane >= 0 ? s.Towers[wave][lane] : North(7), outside ? 10 : 6);
    }

    private static Vector3 SoakOrNorth(DsrP3WyrmholeState s, int wave, int role)
    {
        var lane = s.SoakLane(wave, role);
        return lane >= 0 ? s.Towers[wave][lane] : North(7);
    }

    private static Vector3 Bait(DsrP3WyrmholeState s, int wave, int role)
        => DsrP3WyrmholeState.AtRadius(s.Towers[wave][s.SoakLane(wave, role)], 14);

    private static Vector3 Dodge(DsrP3WyrmholeState s, int wave, int role)
    {
        if (wave == 1) return North(7);
        var bait = Bait(s, wave, role);
        return Vector3.Transform(bait, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 3));
    }

    public static void Tick(DsrP3WyrmholeState state, SimWorld world)
    {
        if (state.Complete) return;
        for (var role = 0; role < 8; role++)
            if (world.Party.Get(role) is SimPartyNpc npc && npc.IsAlive())
                npc.MoveTo(Destination(state, role), 6, MathF.PI / 2);
    }
}
