using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr;

public sealed class DsrZone : IZone
{
    public static readonly DsrZone Instance = new();
    // d00 contains the Allagan platform r1fz_d0_grd01; b00/c00 are other arenas.
    public static readonly Phase P2 = new(Instance, "P2", 45, 312);
    // Weather 3 enables the e00 Nidhogg arena through obset_r1fz_bg_05.
    public static readonly Phase P3 = new(Instance, "P3", 3, 922);
    public static readonly Phase P4 = new(Instance, "P4", 66, 923);
    public static readonly Phase P5 = new(Instance, "P5", 46, 925);
    public static readonly Phase P6 = new(Instance, "P6-雙龍", P4.Weather, 926,
        world=>world.EnforceSquareArenaBoundary(21,"超出雙龍方形場地邊界"));
    public string Name => "絕龍詩戰爭";
    public uint TerritoryId => 968;
    public Vector3 Origin => new(100, 0, 100);
    public byte Level => 90;
    public ushort ItemLevel => 605;
    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new("Tuuf P2 內圈標點", [
            new(WaymarkSlot.A, new(0, 0, -13)),
            new(WaymarkSlot.B, new(13, 0, 0)),
            new(WaymarkSlot.C, new(0, 0, 13)),
            new(WaymarkSlot.D, new(-13, 0, 0)),
            new(WaymarkSlot.One, new(9.192f, 0, -9.193f)),
            new(WaymarkSlot.Two, new(9.192f, 0, 9.192f)),
            new(WaymarkSlot.Three, new(-9.193f, 0, 9.192f)),
            new(WaymarkSlot.Four, new(-9.193f, 0, -9.193f)),
        ])];
    public void Run(SimWorld world) => world.EnforceArenaBoundary(21, "超出絕龍詩場地邊界");
}
