using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;

namespace AnoMech.Scenarios.Ucob;

public class UcobConstants
{
    public const byte Level = 70;
    public const ushort ItemLevel = 345;

    public static class Geometry
    {
        public const float ArenaRadius = 21f;
    }

    // "Aether Markers": A/B/C on the rim, D and 1-4 inside. Scenario-local == world here,
    // since the arena centre is the zone origin.
    public static IReadOnlyList<Waymark> AetherWaymarks =>
    [
        new(WaymarkSlot.A,     new Vector3( 20.986f, 0f, -11.020f)),
        new(WaymarkSlot.B,     new Vector3(  9.315f, 0f,  21.971f)),
        new(WaymarkSlot.C,     new Vector3(-19.999f, 0f,  11.772f)),
        new(WaymarkSlot.D,     new Vector3(      0f, 0f,   9.000f)),
        new(WaymarkSlot.One,   new Vector3(      0f, 0f,  -8.000f)),
        new(WaymarkSlot.Two,   new Vector3( -8.000f, 0f,   5.000f)),
        new(WaymarkSlot.Three, new Vector3(  8.000f, 0f,   5.000f)),
        new(WaymarkSlot.Four,  new Vector3(      0f, 0f,   0.000f)),
    ];

    public class BNpcBaseId
    {
        public const uint BahamutPrime = 0x1FE8;
        // The fight's generic invisible caster (x42 in the instance): AOE source for
        // everything the boss doesn't cast from its own body.
        public const uint Helper = 0x18D6;
    }

    public class ModelCharaId
    {
        // Model 117 variant 4 is what BNpcBase 8168 ships with (P3 Bahamut Prime); variant 3
        // is the gold skin the boss wears from the Phoenix rebirth on. No BNpcBase pairs with
        // it, so retail swaps the variant on the same actor and so do we.
        public const uint GoldenBahamut = 2112;
    }

    public class BNpcNameId
    {
        public const uint BahamutPrime = 3210;
    }

    public class VfxPath
    {
        // The eruption flame. The Exaflare actions carry no VFX row and no AnimationStart, so
        // the action effect alone renders nothing; retail plays it from the gimmick timeline
        // they declare as AnimationEnd (mon_sp/gimmick/f1bz_boss_gimmick02), whose VFX this is
        // (g02 = gimmick02). Spawned on the helper directly.
        public const string ExaflareEruption = "vfx/monster/gimmick2/eff/f1bz_b0_g02c0i.avfx";
    }

    public class ActionId
    {
        public const uint Exaflare = 9967;       // 0x26EF, boss-body cast, no AOE of its own
        public const uint ExaflareFirst = 9968;  // 0x26F0, helper, 4.0s cast, arrow omen, 6y circle
        public const uint ExaflareRest = 9969;   // 0x26F1, helper, instant, 6y circle
    }
}
