using System;
using System.Linq;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P3Wyrmhole;

internal sealed class DsrP3WyrmholeState
{
    public readonly int Seed;
    public readonly int[] Order = new int[8];
    public readonly int[] Lane = new int[8];
    public readonly int[] NumberLane = new int[8];
    public readonly int[] Direction = new int[8];
    public readonly bool[] Arrows = new bool[3];
    public readonly bool[] OutFirst = new bool[2];
    public readonly Vector3[][] Towers = [new Vector3[3], new Vector3[2], new Vector3[3]];
    public readonly bool[] TowersVisible = new bool[3];
    public readonly bool[] LineTracking = new bool[3];
    public float Time;
    public bool NumbersAssigned;
    public bool ArrowsAssigned;
    public bool Failed;
    public bool Complete;
    public float LanceRotation;
    public float LanceTurnStartRotation;
    public bool TrackingMainTank;
    public bool TurningForLance;
    public readonly int LanceTarget;
    public readonly int[] FinalTowerCounts = new int[4];
    public bool FinalTowersVisible;
    public bool FinalTowersResolved;
    public bool TethersActive;
    public bool TethersResolved;
    public readonly int TetherClone;
    public readonly int[] TetherOwners = new int[2];
    public readonly Vector3[] TetherPickup = new Vector3[2];
    public float LanceTurnStartTime;
    public readonly Vector3?[] MovementTargets = new Vector3?[8];
    public readonly float?[] MovementFacings = new float?[8];

    public DsrP3WyrmholeState(int seed)
    {
        Seed = seed;
        var random = new Random(seed);
        var roles = Enumerable.Range(0, 8).ToArray();
        random.Shuffle(roles);
        var arrowOrders = new[] { 0, 1, 2 };
        random.Shuffle(arrowOrders);
        var arrowCount = random.Next(1, 3);
        for (var i = 0; i < arrowCount; i++) Arrows[arrowOrders[i]] = true;
        var index = 0;
        for (var order = 0; order < 3; order++)
        {
            for (var lane = 0; lane < Towers[order].Length; lane++)
            {
                var role = roles[index++];
                Order[role] = order;
                Lane[role] = lane;
                Direction[role] = !Arrows[order] ? 0 : lane == 0 ? 1 : lane == Towers[order].Length - 1 ? -1 : 0;
                Towers[order][lane] = LanePosition(order, lane);
            }
        }
        OutFirst[0] = random.Next(2) == 0;
        OutFirst[1] = random.Next(2) == 0;
        for (var order = 0; order < 3; order++)
        {
            var lanes = Enumerable.Range(0, Towers[order].Length).ToArray();
            if (Arrows[order]) random.Shuffle(lanes);
            for (var lane = 0; lane < lanes.Length; lane++)
                NumberLane[RoleAt(order, lane)] = lanes[lane];
        }
        LanceTarget = random.Next(8);
        do
        {
            for (var tower = 0; tower < 4; tower++) FinalTowerCounts[tower] = random.Next(1, 5);
        } while (FinalTowerCounts.Sum() != 8);
        TetherClone = random.Next(4);
        var tetherTargets = Enumerable.Range(2, 6).ToArray();
        random.Shuffle(tetherTargets);
        Array.Copy(tetherTargets, TetherOwners, 2);
    }

    public static int FinalTowerHome(int role) => role switch { 0 or 6 => 0, 1 or 7 => 1, 3 or 5 => 2, _ => 3 };

    public static Vector3 FinalTowerPosition(int tower) => tower switch
    {
        0 => new(-10, 0, -10), 1 => new(10, 0, -10),
        2 => new(10, 0, 10), _ => new(-10, 0, 10)
    };

    public int FinalTowerAssignment(int role)
    {
        var home = FinalTowerHome(role);
        if (role is 2 or 3 or 6 or 7 || FinalTowerCounts[home] >= 2) return home;
        foreach (var offset in new[] { 1, 3, 2 })
            if (FinalTowerCounts[(home + offset) % 4] > 2) return (home + offset) % 4;
        return home;
    }

    public static Vector3 LanePosition(int order, int lane) => order == 1
        ? new(lane == 0 ? -7.5f : 7.5f, 0, -7.5f)
        : lane switch { 0 => new(-7.5f, 0, 0), 1 => new(0, 0, 7.5f), _ => new(7.5f, 0, 0) };

    public Vector3 JumpPosition(int role) => LanePosition(Order[role], Lane[role]);
    public Vector3 NumberPosition(int role)
    {
        var position = LanePosition(Order[role], NumberLane[role]);
        return Order[role] == 2 ? AtRadius(position, 10) : position;
    }
    public int LandingLane(int role) => Direction[role] == 0 ? Lane[role] : Towers[Order[role]].Length - 1 - Lane[role];
    public static Vector3 Landing(Vector3 position, float facing, int direction)
        => position + new Vector3(MathF.Sin(facing), 0, MathF.Cos(facing)) * (direction * DsrP3WyrmholeConstants.JumpOffset);

    public int Soaker(int wave, int lane) => wave switch
    {
        0 => RoleAt(2, lane),
        1 => RoleAt(0, lane == 0 ? 0 : 2),
        _ => lane == 1 ? RoleAt(0, 1) : RoleAt(1, lane == 0 ? 0 : 1)
    };

    public int RoleAt(int order, int lane)
        => Enumerable.Range(0, 8).Single(role => Order[role] == order && Lane[role] == lane);

    public int SoakLane(int wave, int role)
    {
        for (var lane = 0; lane < Towers[wave].Length; lane++)
            if (Soaker(wave, lane) == role) return lane;
        return -1;
    }

    public static Vector3 AtRadius(Vector3 point, float radius)
        => point.LengthSquared() > .001f ? Vector3.Normalize(point) * radius : new(0, 0, -radius);
}
