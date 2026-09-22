using AnoMech.Core.Native;
using AnoMech.Pointers;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Vfx;

namespace AnoMech.Core.SimObjects;

// A single actor-attached VFX (head markers, looping arm-unit arrows, etc.)
// bound to a SimCharacter. Spawned and tracked by SimCharacter.AddVfx; ticks
// its own optional auto-expire countdown and frees the native VfxData on
// Despawn.
//
// The game may free any actor VFX before our timer expires. Its destructor hook
// invalidates the owner before freeing memory, including when the address is reused.
//
// duration <= 0 means the VFX lives until RemoveVfx / ClearAttachedVfx /
// Despawn; duration > 0 adds a visible-time auto-expire on its own counter.
public sealed unsafe class SimVfx : ISimObject
{
    private static readonly object lifetimeLock = new();
    private static readonly Dictionary<nint, SimVfx> owners = [];
    private float duration;
    private float elapsed;

    public string Path { get; }
    public VfxData* Handle { get; private set; }
    public bool IsActive => Handle != null;

    static SimVfx() => VfxDataPointers.Destroying += OnNativeDestroy;

    private static void OnNativeDestroy(nint pointer)
    {
        lock (lifetimeLock)
            if (owners.Remove(pointer, out var owner)) owner.Handle = null;
    }

    internal SimVfx(SimCharacter target, string path, float duration)
    {
        Path = path;
        this.duration = duration;
        var chara = (Character*)target.BattleCharaPtr;
        lock (lifetimeLock)
        {
            Handle = VfxFunctions.SpawnActorVfx(path, chara, chara);
            if (Handle != null)
            {
                var pointer = (nint)Handle;
                if (owners.Remove(pointer, out var previous)) previous.Handle = null;
                owners.Add(pointer, this);
            }
        }
    }

    // Restart the auto-expire countdown when the same path is re-added. A
    // re-add with no duration leaves any existing countdown alone (matches the
    // original lazy "re-adding the same path is a no-op" contract).
    public void Refresh(float duration)
    {
        if (duration <= 0f) return;
        this.duration = duration;
        elapsed = 0f;
    }

    public void Tick(float deltaSeconds)
    {
        if (!IsActive || duration <= 0f) return;
        elapsed += deltaSeconds;
        if (elapsed >= duration) Despawn();
    }

    public void Despawn()
    {
        lock (lifetimeLock)
        {
            var pointer = Handle;
            if (pointer == null) return;
            Handle = null;
            owners.Remove((nint)pointer);
            VfxFunctions.RemoveActorVfx(pointer);
        }
    }
}
