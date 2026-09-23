using System;
using System.Linq;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P4Eyes;

internal sealed class DsrP4EyesState
{
    public readonly Random Random;
    public readonly bool[] Red = new bool[8];
    public readonly float[] SwapCooldown = new float[8];
    public readonly float[] Piercing = new float[8];
    public readonly Vector3[] Positions = new Vector3[8];
    public readonly Vector3?[] Destinations = new Vector3?[8];
    public readonly bool[] OrbsPopped = new bool[6];
    public readonly float[] OrbFadeRemaining = new float[6];
    public readonly float[] DiveFadeRemaining = new float[8];
    public readonly int[] DiveLane = [-1, -1, -1, -1, 0, 3, 2, 1];
    public readonly int[] SwapTarget = Enumerable.Repeat(-1, 8).ToArray();
    public readonly bool[] OrbExchangeDone = new bool[8];
    public readonly Vector3?[] SwapWaitPosition = new Vector3?[8];
    public int[] FirstDps = [];
    public bool BuffsApplied, ColorsAssigned, YellowReady, BlueReady, MirageStarted, Failed, Complete;
    public int DiveCount;
    public float Time;
    public int Seed { get; }
    public bool YellowDone => OrbsPopped[0] && OrbsPopped[1];
    public bool BlueDone => OrbsPopped.Skip(2).All(x => x);
    public static Vector3 BlueEye => new(-9, 0, 0);
    public static Vector3 RedEye => new(9, 0, 0);
    public static Vector3 BuffPosition => new(0, 0, 5);
    public DsrP4EyesState(int seed)
    {
        Seed = seed;
        Random = new(seed);
        int[] roles = [0, 1, 2, 3, 4, 5, 6, 7];
        Random.Shuffle(roles);
        foreach (var role in roles.Take(4)) Red[role] = true;
    }
    public static int Partner(int role) => role switch { 0 => 4, 1 => 7, 2 => 5, 3 => 6, 4 => 0, 5 => 2, 6 => 3, _ => 1 };
    public static Vector3 Opening(int role) => role switch
    {
        0 => new(-4, 0, -1.7f), 1 => new(4, 0, -1.7f), 2 => new(-4, 0, 1.7f), 3 => new(4, 0, 1.7f),
        4 => new(-4, 0, -5.5f), 5 => new(-4, 0, 5.5f), 6 => new(4, 0, 5.5f), _ => new(4, 0, -5.5f)
    };
    public static Vector3 YellowWait(int role, float offset) => new(role is 0 or 2 ? -16 : 16, 0, role < 2 ? -offset : offset);
    public static Vector3 OrbPosition(int index) => index switch
    {
        0 => new(-16, 0, 0), 1 => new(16, 0, 0), 2 => new(-9, 0, -5.5f),
        3 => new(-9, 0, 5.5f), 4 => new(9, 0, 5.5f), _ => new(9, 0, -5.5f)
    };
    public static Vector3 DivePosition(int lane) => BlueEye + (lane switch
    {
        0 => new Vector3(-5, 0, -5), 1 => new(5, 0, -5), 2 => new(5, 0, 5), _ => new(-5, 0, 5)
    });
}
