using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P2Sanctity;

internal enum SanctityStage { Preparation, Swords, Charges, Pairs, FirstTowers, Meteors, SecondTowers, Complete }

internal sealed class DsrP2SanctityState
{
    public static readonly string[] Roles = ["MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4"];
    public static readonly string[] Directions = ["北", "東", "南", "西"];
    public static readonly int[] BaseQuadrants = [0, 2, 3, 1, 3, 1, 0, 2];
    public int Seed { get; }
    public bool Clockwise { get; }
    public int MeteorTowerAngle { get; private set; }
    public int DarkKnightQuadrant { get; }
    public int EyeIndex { get; }
    public int BossIndex { get; }
    public int[] Swords { get; }
    public int[] Groups { get; } = [0, 1, 0, 1, 0, 1, 0, 1];
    public bool MeteorsOnSupports { get; }
    public int[] MeteorRoles { get; }
    public int[] Quadrants { get; } = (int[])BaseQuadrants.Clone();
    public int[] FirstTowerByRole { get; } = new int[8];
    public int[] OuterMasks { get; } = new int[4];
    public List<Vector3> FirstTowers { get; } = [];
    public Vector3[][] ChargePoints { get; } = new Vector3[2][];
    public Vector3[][] SpherePoints { get; } = new Vector3[2][];
    public SanctityStage Stage { get; set; }
    public int Explosions { get; set; }
    public int MeteorCount { get; set; }
    public int MeteorSnapshots { get; set; }
    public bool KnockbackResolved { get; set; }
    public float KnockbackElapsed { get; set; }
    public float MeteorElapsed { get; set; }
    public bool Failed { get; set; }
    public bool FirstTowersVisible { get; set; }
    public bool SecondTowersVisible { get; set; }
    public bool FireVisible { get; set; }
    public bool EarlyMove => ((DarkKnightQuadrant & 1) == 1) == Clockwise;
    public Vector3 DarkKnightPosition => Polar(DarkKnightQuadrant * 90 + 45, 16);
    public Vector3 EyePosition => Polar(EyeIndex * 45, 40);
    public Vector3 BossPosition => Polar(BossIndex * 45, 23);
    public bool IsMeteorRole(int role) => (role < 4) == MeteorsOnSupports;
    public bool HasMeteor(int role) => Array.IndexOf(MeteorRoles, role) >= 0;
    public Vector3 FirstTower(int role) => FirstTowers[FirstTowerByRole[role]];
    public bool StartsInside(int role) => FirstTower(role).LengthSquared() < 100;
    public Vector3 PairPosition(int role) => Polar(Quadrants[role] * 90, 10);
    public Vector3 SecondTower(int role) => Polar(SecondTowerAngle(role), 18);
    public float SecondTowerAngle(int role) => IsMeteorRole(role)
        ? (Quadrants[role] * 90 + (HasMeteor(role) ? 180 : 0)) % 360
        : BaseQuadrants[role] * 90 + 45;

    public DsrP2SanctityState(int seed, int direction = 0, int meteorPreference = 0, int playerRole = 0, int meteorAngle = 0)
    {
        Seed = seed;
        var random = new Random(seed);
        Clockwise = direction == 0 ? random.Next(2) == 0 : direction == 1;
        DarkKnightQuadrant = random.Next(4);
        EyeIndex = random.Next(8);
        BossIndex = (EyeIndex + random.Next(2, 7)) % 8;
        Swords = Enumerable.Range(0, 8).OrderBy(_ => random.Next()).Take(2).ToArray();
        for (var i = 0; i < 2; i++)
            if (Groups[Swords[i]] != i)
            {
                Groups[Swords[i]] ^= 1;
                Groups[Swords[i] ^ 1] ^= 1;
            }
        MeteorsOnSupports = meteorPreference == 0 ? random.Next(2) == 0 :
            meteorPreference == 1 || meteorPreference == 3 && playerRole < 4;
        var candidates = Enumerable.Range(MeteorsOnSupports ? 0 : 4, 4).OrderBy(_ => random.Next()).ToList();
        if (meteorPreference == 3) { candidates.Remove(playerRole); candidates.Insert(0, playerRole); }
        MeteorRoles = candidates.Take(2).ToArray();
        AdjustMeteorPairs();
        if (meteorAngle is not (0 or 120 or 150 or 180)) throw new ArgumentOutOfRangeException(nameof(meteorAngle));
        GenerateTowers(random, meteorAngle);
        GenerateCharges();
    }

    private void AdjustMeteorPairs()
    {
        void Swap(int from, int to)
        {
            var a = Enumerable.Range(0, 8).Single(r => IsMeteorRole(r) && Quadrants[r] == from);
            var b = Enumerable.Range(0, 8).Single(r => IsMeteorRole(r) && Quadrants[r] == to);
            (Quadrants[a], Quadrants[b]) = (Quadrants[b], Quadrants[a]);
        }
        var occupied = MeteorRoles.Select(r => Quadrants[r]).ToArray();
        if (occupied.All(q => q % 2 == 1)) { Swap(3, 0); Swap(1, 2); }
        else if (occupied.Any(q => q % 2 == 1))
            Swap(occupied.Single(q => q % 2 == 1), occupied.Contains(0) ? 2 : 0);
    }

    private void GenerateTowers(Random random, int meteorAngle)
    {
        int[] priority = [1, 0, 2];
        int[] choices;
        var attempts = 0;
        do
        {
            if (++attempts > 4096) throw new InvalidOperationException("無法產生指定角度的隕石塔配置");
            // Preserve legal tower layouts and Tuuf tower priority when forcing an angle.
            var extraCount = random.Next(5) < 2 ? 1 : 2;
            var doubleQuadrants = Enumerable.Range(0, 4).OrderBy(_ => random.Next()).Take(extraCount).ToArray();
            for (var q = 0; q < 4; q++)
            {
                var omittedOrPresent = random.Next(3);
                OuterMasks[q] = doubleQuadrants.Contains(q) ? 7 ^ (1 << omittedOrPresent) : 1 << omittedOrPresent;
            }
            choices = Enumerable.Range(0, 4).Select(q => priority.First(t => (OuterMasks[q] & (1 << t)) != 0)).ToArray();
            var bestPenalty = Math.Abs(choices[0] - choices[2]);
            foreach (var n in priority)
                foreach (var s in priority)
                    if ((OuterMasks[0] & (1 << n)) != 0 && (OuterMasks[2] & (1 << s)) != 0 && Math.Abs(n - s) < bestPenalty)
                    {
                        choices[0] = n;
                        choices[2] = s;
                        bestPenalty = Math.Abs(n - s);
                    }
            MeteorTowerAngle = 180 - bestPenalty * 30;
        }
        while (meteorAngle != 0 && MeteorTowerAngle != meteorAngle);
        var innerRoles = new List<int>();
        for (var q = 0; q < 4; q++)
        {
            var meteor = Enumerable.Range(0, 8).Single(r => Quadrants[r] == q && IsMeteorRole(r));
            var other = Enumerable.Range(0, 8).Single(r => Quadrants[r] == q && !IsMeteorRole(r));
            FirstTowerByRole[meteor] = FirstTowers.Count;
            FirstTowers.Add(Polar(q * 90 + (choices[q] - 1) * 30, 18));
            var remaining = OuterMasks[q] & ~(1 << choices[q]);
            if (remaining != 0)
            {
                FirstTowerByRole[other] = FirstTowers.Count;
                FirstTowers.Add(Polar(q * 90 + (priority.First(t => (remaining & (1 << t)) != 0) - 1) * 30, 18));
            }
            else innerRoles.Add(other);
        }
        var innerQuadrants = Enumerable.Range(0, 4).OrderBy(_ => random.Next()).Take(innerRoles.Count).ToList();
        foreach (var role in innerRoles.ToArray())
            if (innerQuadrants.Remove(Quadrants[role]))
            {
                FirstTowerByRole[role] = FirstTowers.Count;
                FirstTowers.Add(Polar(Quadrants[role] * 90 + 45, 6));
                innerRoles.Remove(role);
            }
        for (var i = 0; i < innerRoles.Count; i++)
        {
            FirstTowerByRole[innerRoles[i]] = FirstTowers.Count;
            FirstTowers.Add(Polar(innerQuadrants[i] * 90 + 45, 6));
        }
    }

    private void GenerateCharges()
    {
        for (var k = 0; k < 2; k++)
        {
            var start = new Vector3(k == 0 ? 5 : -5, 0, 0);
            var firstAngle = (k == 0) == Clockwise ? 180 : 0;
            var step = Clockwise ? 112.5f : -112.5f;
            var p = ChargePoints[k] = [start, Polar(firstAngle, 21), Polar(firstAngle + step, 21), Polar(firstAngle + 2 * step, 21)];
            SpherePoints[k] = [p[0], Vector3.Lerp(p[0], p[1], .5f), p[1],
                Vector3.Lerp(p[1], p[2], 1f / 3), Vector3.Lerp(p[1], p[2], 2f / 3), p[2],
                Vector3.Lerp(p[2], p[3], 1f / 3), Vector3.Lerp(p[2], p[3], 2f / 3), p[3]];
        }
    }

    public Vector3 SwordPosition(int role, bool moved)
    {
        var offset = moved ? 33.3f : EarlyMove ? 15 : 11.7f;
        return Polar(DarkKnightQuadrant * 90 + 45 + (Groups[role] == 0 ? 180 : 0) + (Clockwise ? offset : -offset), 20);
    }

    public Vector3 MeteorStart(int role)
    {
        var angle = Angle(FirstTower(role));
        var cardinal = Quadrants[role] * 90;
        var delta = SignedAngle(cardinal - angle);
        return Polar(angle + Math.Clamp(delta, -6, 6), 19);
    }

    public Vector3 MeteorPosition(int role, float progress)
        => Polar(Angle(MeteorStart(role)) + MeteorArc(role) * Math.Clamp(progress, 0, 1), 19.5f);

    public float MeteorArc(int role)
    {
        var start = Angle(MeteorStart(role));
        var other = MeteorRoles.Single(r => r != role);
        return (Angle(MeteorStart(other)) - start + 360) % 360;
    }

    public static Vector3 Polar(float degrees, float radius) => new(
        MathF.Sin(degrees * MathF.PI / 180) * radius, 0, -MathF.Cos(degrees * MathF.PI / 180) * radius);
    public static float Angle(Vector3 p) => MathF.Atan2(p.X, -p.Z) * 180 / MathF.PI;
    public static float SignedAngle(float a) => (a % 360 + 540) % 360 - 180;
}
