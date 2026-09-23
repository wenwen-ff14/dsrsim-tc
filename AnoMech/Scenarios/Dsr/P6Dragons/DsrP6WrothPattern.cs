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
    public Vector3 Start => new(StartSide*19,0,StartZ*13);
    public Vector3 NextStack(int hit) => hit switch
    {
        0 => new(StartSide*11.5f,0,StartZ*13),
        1 => new(StartSide*4,0,StartZ*13),
        2 => StartSide<0?new(1,0,StartZ*7):new(4,0,StartZ*5),
        _ => new(StartSide<0?1:4,0,-StartZ*3),
    };
    public Vector3[] Fireballs(int wave)
    {
        if(wave==0)return [new(-3,0,-3),new(3,0,0),new(-2,0,3)];
        var x=wave==1?SecondX:-SecondX;
        var z=wave==1?SecondZ:-SecondZ;
        return [new(x*10,0,z*16),new(x*16,0,z*13),new(x*11,0,z*10)];
    }
}
