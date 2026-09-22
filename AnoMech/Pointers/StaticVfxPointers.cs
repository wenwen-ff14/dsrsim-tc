using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace AnoMech.Pointers;

internal unsafe class StaticVfxPointers
{
    [Signature("E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08", UseFlags = SignatureUseFlags.Pointer)]
    public static CreateDelegate Create { get; private set; } = null!;

    [Signature("E8 ?? ?? ?? ?? ?? ?? ?? 8B 4A ?? 85 C9", UseFlags = SignatureUseFlags.Pointer)]
    public static UpdateDelegate Update { get; private set; } = null!;

    public delegate VfxObject* CreateDelegate(byte* path, byte* pool);
    public delegate void UpdateDelegate(VfxObject* vfx, float deltaSeconds, int flags);

    // These members are not named in API 13's Scene.Object/VfxObject bindings.
    public static void CleanupRender(VfxObject* vfx)
        => ((delegate* unmanaged<VfxObject*, void>)(*(void***)vfx)[1])(vfx);

    public static void ClearHiddenFlag(VfxObject* vfx) => *((byte*)vfx + 0x248) &= 0xF7;

    public static void Initialize() => Plugin.GameInterop.InitializeFromAttributes(new StaticVfxPointers());
}
