using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace AnoMech.Pointers;

internal static unsafe class DrawDataPointers
{
    // TC's LoadWeapon has an eighth argument absent from the API13 binding.
    // Its mode=0 branch stores Weapon* at WeaponData+0x08, not the old +0x18.
    public static void LoadWeapon(DrawDataContainer* data, DrawDataContainer.WeaponSlot slot, WeaponModelId model)
    {
        var load = (delegate* unmanaged<DrawDataContainer*, DrawDataContainer.WeaponSlot, WeaponModelId,
            byte, byte, byte, byte, int, void>)DrawDataContainer.MemberFunctionPointers.LoadWeapon;
        if (load == null) throw new System.InvalidOperationException("LoadWeapon binding is unavailable.");
        load(data, slot, model, 1, 0, 0, 0, 0);
    }

    public static DrawObject* WeaponDrawObject(DrawDataContainer* data, int slot)
        => *(DrawObject**)((byte*)data + 0x10 + slot * 0x70 + 0x08);
}
