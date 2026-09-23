namespace AnoMech.Scenarios.Dsr;

internal static class DsrConstants
{
    public static class Geometry
    {
        public const float FireCenterRadius = 12;
        public const float IceGraceSeconds = 2.5f;
    }
    public static class Npc
    {
        public const uint Thordan = 0x313C;
        public const uint Zephirin = 0x3130;
        public const uint Adelphel = 0x3139;
        public const uint Janlenoux = 0x3158;
        public const uint Vellguine = 0x3159;
        public const uint Paulecrain = 0x315A;
        public const uint Ignasse = 0x315B;
        public const uint Grinnaux = 0x313A;
        public const uint Charibert = 0x313B;
        public const uint Hermenost = 0x315C;
        public const uint Haumeric = 0x315E;
        public const uint Noudenet = 0x315F;
        public const uint Helper = 0x233C;
        public const uint Sphere = 0x330E;
        public const uint Comet = 0x312F;
    }

    public static uint NameId(uint npc) => npc switch
    {
        Npc.Thordan => 3632,
        Npc.Zephirin => 3633,
        Npc.Adelphel => 3634,
        Npc.Janlenoux => 3635,
        Npc.Vellguine => 3636,
        Npc.Paulecrain => 3637,
        Npc.Ignasse => 3638,
        Npc.Grinnaux => 3639,
        Npc.Hermenost => 3640,
        Npc.Charibert => 3642,
        Npc.Haumeric => 3643,
        Npc.Noudenet => 3644,
        _ => 0,
    };

    public static class Vfx
    {
        public const string Ice = "bg/ffxiv/roc_r1/common/vfx/eff/b2419dmgf1_h1.avfx";
        public const string Fire = "bg/ffxiv/roc_r1/common/vfx/eff/b2420dmgf1_h1.avfx";
    }

    public static class Timeline
    {
        public const ushort KnightEntrance = 0x1E43;
        public const ushort KnightDeparture = 0x1E39;
        public const ushort BattleIdle = 34;
    }

    public static class Action
    {
        public const uint Teleport = 25540;
        public const uint Reappear = 25532;
        public const uint Sanctity = 25569;
        public const uint Gaze = 25552;
        public const uint Sever = 25571;
        public const uint Blade = 25570;
        public const uint Flare = 25295;
        public const uint Ice = 25575;
        public const uint HiemalStorm = 25574;
        public const uint Fire = 28591;
        public const uint Donut = 28592;
        public const uint Tower1 = 29564;
        public const uint Tower2 = 28651;
        public const uint Comet = 25577;
        public const uint Knockback = 25308;
    }
}
