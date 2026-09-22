using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace AnoMech.Pointers;

internal static unsafe class TimelinePointers
{
    // TC RVA 0x879A60: sets the weapon-state flags and dispatches the native
    // draw/sheath transition. PlayActionTimeline alone does not set this state.
    internal const string WeaponStanceSignature = "48 89 6C 24 18 57 48 83 EC 30 44 0F B6 89 4E 03 00 00 0F B6 EA 41 80 C9 80";
    private static nint setWeaponStance;
    private static bool resolved;

    public static bool DrawWeapon(TimelineContainer* timeline)
    {
        if (!resolved)
        {
            resolved = true;
            if (!Plugin.SigScanner.TryScanText(WeaponStanceSignature, out setWeaponStance))
                Plugin.Log.Warning("Native weapon stance binding unavailable; battle pose initialization skipped.");
        }
        if (timeline == null || setWeaponStance == 0) return false;
        ((delegate* unmanaged<TimelineContainer*, byte, byte, void>)setWeaponStance)(timeline, 1, 1);
        return (timeline->Flags3 & 0x40) != 0;
    }
}
