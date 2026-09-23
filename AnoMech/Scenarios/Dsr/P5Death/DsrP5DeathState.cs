using System;
using System.Linq;
using System.Numerics;
namespace AnoMech.Scenarios.Dsr.P5Death;
internal sealed class DsrP5DeathState
{
    public readonly int[] Dooms, Clean;
    public readonly float Rotation;
    public readonly int EyeIndex, BossIndex;
    public readonly uint[] Symbols = new uint[8];
    public readonly Vector3[] FinalDirections = new Vector3[8];
    public readonly Vector3[] SpreadSnapshots = new Vector3[8];
    public readonly Vector3[] CleansePositions = new Vector3[4];
    public readonly Vector3?[] Destinations = new Vector3?[8];
    public readonly bool[] Cleansed = new bool[8];
    public readonly Random Random;
    public float Time;
    public const float CleanseRadius=1f;
    public readonly Vector3[] MeteorPositions=Enumerable.Range(0,8).Select(i=>Polar(i*45,18)).ToArray();
    public readonly bool[] MeteorDestroyed=new bool[8];
    public bool MeteorsActive, LimitBreakUsed, LimitBreakCasting;
    public float LimitBreakElapsed;
    public Vector3 LimitBreakOrigin, LimitBreakTarget;
    public string LimitBreakMessage="";
    public bool Assigned, SpreadsResolved, SymbolsAssigned, Knocked, Failed, Complete;
    public DsrP5DeathState(int seed)
    {
        Random=new(seed);
        Rotation=Random.Next(4)*MathF.PI/2;
        EyeIndex=Random.Next(8);
        BossIndex=(EyeIndex+Random.Next(3,6))%8;
        var roles=Enumerable.Range(0,8).ToArray();Random.Shuffle(roles);
        Dooms=roles.Take(4).Order().ToArray();Clean=roles.Skip(4).Order().ToArray();
    }
    public bool HasDoom(int role)=>Dooms.Contains(role);
    public Vector3 Rotate(Vector3 p)=>new(p.X*MathF.Cos(Rotation)-p.Z*MathF.Sin(Rotation),0,p.X*MathF.Sin(Rotation)+p.Z*MathF.Cos(Rotation));
    public static Vector3 Polar(float degrees,float radius)=>new(radius*MathF.Sin(degrees*MathF.PI/180),0,-radius*MathF.Cos(degrees*MathF.PI/180));
    public Vector3 Hammer=>Rotate(new(0,0,-7));
    public Vector3 Eye=>Polar(EyeIndex*45,40);
    public Vector3 Boss=>Polar(BossIndex*45,23);
    public Vector3 Spread(int role)
    {
        var doom=HasDoom(role);var index=Array.IndexOf(doom?Dooms:Clean,role);
        Vector3[] points=doom?[new(-13,0,0),new(-12,0,-15),new(12,0,-15),new(13,0,0)]:[new(-19.5f,0,0),new(-12,0,15),new(12,0,15),new(19.5f,0,0)];
        return Rotate(points[index]);
    }
    public Vector3 SymbolBait(int role)
    {
        var i=Array.IndexOf(Dooms,role);
        if(i>=0)return Rotate(i switch{0=>new(-11,0,0),1=>new(-2.5f,0,3),2=>new(2.5f,0,3),_=>new(11,0,0)});
        return Rotate(new((Array.IndexOf(Clean,role)-1.5f)*2.8f,0,-4));
    }
    public Vector3 FinalDirection(int role)=>FinalDirections[role];
    public float SafeFacing(Vector3 position)
    {
        var a=Vector3.Normalize(Eye-position);var b=Vector3.Normalize(Boss-position);
        var away=-(a+b);
        if(away.LengthSquared()<.001f)away=new(-a.Z,0,a.X);
        return MathF.Atan2(away.X,away.Z);
    }
}
