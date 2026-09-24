using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

internal sealed record DsrP6WingsPattern(bool SouthCleave, bool Far, bool DiveWest, bool DiveFromSouth, bool HotWing)
{
    public int SafeZ => SouthCleave ? -1 : 1;
    public uint CastAction => SouthCleave ? (Far ? 27940u : 27939u) : (Far ? 27943u : 27942u);
    public uint CleaveAction => SouthCleave ? 27941u : 27944u;
    public Vector3 DiveOrigin => new(DiveWest ? -11 : 11, 0, DiveFromSouth ? 34 : -34);
    public Vector3 DiveTarget => new(DiveOrigin.X, 0, DiveFromSouth ? -40 : 40);

    public Vector3 Position(int role, bool second, bool hotKnown = true)
    {
        if (second)
        {
            var x = role switch { 0 => -18, 1 => -4, _ => 15 };
            return new(Far ? x : -x, 0, SafeZ * (!hotKnown || HotWing ? 2 : 15));
        }
        var centerX = DiveWest ? 11 : -11;
        var tankX = Far ? -9 : 9;
        return new(centerX + (role < 2 ? tankX : -tankX), 0,
            SafeZ * (role switch { 0 => 18, 1 => 3, _ => 11 }));
    }
}
