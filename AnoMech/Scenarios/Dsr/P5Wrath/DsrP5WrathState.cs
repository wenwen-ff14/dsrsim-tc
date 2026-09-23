using System;
using System.Linq;
using System.Numerics;
namespace AnoMech.Scenarios.Dsr.P5Wrath;
internal sealed class DsrP5WrathState
{
    public int Seed { get; }
    public readonly float Rotation;
    public readonly int Blue;
    public readonly int[] TetherRoles;
    public readonly int[] EastRoles;
    public readonly bool GrinnauxNorth;
    public readonly Vector3[] Twisters = new Vector3[8];
    public readonly Vector3?[] Destinations = new Vector3?[8];
    public bool Assigned, ChargesResolved, TwistersActive, Failed, Complete;
    public float Time;
    public DsrP5WrathState(int seed)
    {
        Seed = seed;
        var random = new Random(seed);
        Rotation = random.Next(4) * MathF.PI / 2;
        GrinnauxNorth = random.Next(2) == 0;
        var roles = Enumerable.Range(0,8).ToArray();
        random.Shuffle(roles);
        Blue = roles[0];
        TetherRoles = [roles[1],roles[2]];
        EastRoles = roles.Skip(3).Order().ToArray();
    }
    public Vector3 Rotate(Vector3 p) => new(p.X*MathF.Cos(Rotation)-p.Z*MathF.Sin(Rotation),p.Y,p.X*MathF.Sin(Rotation)+p.Z*MathF.Cos(Rotation));
    public Vector3 InitialPosition(int role)
    {
        if(role == Blue) return Rotate(new(-16,0,-11));
        if(role == TetherRoles[0]) return Rotate(new(6,0,18.8f));
        if(role == TetherRoles[1]) return Rotate(new(-6,0,18.8f));
        var angle = (56 + Array.IndexOf(EastRoles,role)*17) * MathF.PI / 180;
        return Rotate(new(19.5f*MathF.Sin(angle),0,-19.5f*MathF.Cos(angle)));
    }
    public Vector3 DodgePosition(int role)
    {
        var p = InitialPosition(role);
        if(Array.IndexOf(EastRoles,role)>=0) return Vector3.Normalize(p)*15;
        var angle = 15*MathF.PI/180;
        return new(p.X*MathF.Cos(angle)-p.Z*MathF.Sin(angle),0,p.X*MathF.Sin(angle)+p.Z*MathF.Cos(angle));
    }
    public Vector3 KnightPosition(int index) => Rotate(new(index==0 ? -10 : 10,0,-21));
    public Vector3 WhiteDragon => Rotate(new(0,0,-24));
    public Vector3 Grinnaux => Rotate(new(0,0,GrinnauxNorth ? -12 : 12));
    public Vector3 Charibert => -Grinnaux;
}
