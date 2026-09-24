using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

internal sealed class DsrP6WrothPattern(int variant)
{
    public int Variant { get; } = variant;
    public int SecondX => (Variant/6)%2==0?1:-1;
    public int SecondZ => Variant/12==0?-1:1;
    public int StartSide => Variant%3==0?1:-1;
    public int StartZ => -SecondZ;
    public Vector3 DiveOrigin => new((Variant%3-1)*11,0,Variant%6<3?-34:34);
    public Vector3 DiveTarget => new(DiveOrigin.X,0,-DiveOrigin.Z);
    public Vector3 Start => new(StartSide*20.5f,0,StartZ*13);
    public Vector3 AfterFirstFireballs => new(StartSide*4,0,StartZ*13);
    public Vector3 NextStack(int hit) => hit switch
    {
        0 => new(StartSide*13.5f,0,StartZ*13),
        1 => new(StartSide*10.5f,0,StartZ*13),
        2 => StartSide<0?new(1,0,StartZ*7):new(4,0,StartZ*5),
        _ => new(StartSide<0?1:4,0,-StartZ*3),
    };
    public Vector3[] Fireballs(int wave)
    {
        // Match the 6-unit native width so parallel arms meet without overlapping.
        if(wave==0)return [new(-6,0,-6),Vector3.Zero,new(6,0,6)];
        var x=wave==1?SecondX:-SecondX;
        var z=wave==1?SecondZ:-SecondZ;
        return [new(x*8.5f,0,z*20.5f),new(x*14.5f,0,z*14.5f),new(x*20.5f,0,z*8.5f)];
    }
}
