using AnoMech.Core.Native;
using AnoMech.Core.SimObjects;
using AnoMech.Pointers;

var actor = new SimCharacter();
var expired = new SimVfx(actor, "headmarker", 1);
VfxDataPointers.Notify((nint)1);
expired.Tick(2);
expired.Despawn();
if (expired.IsActive || VfxFunctions.Removals != 0)
    throw new Exception("A game-freed VFX was destroyed again");

var removed = new SimVfx(actor, "headmarker", 1);
removed.Tick(2);
removed.Despawn();
if (removed.IsActive || VfxFunctions.Removals != 1)
    throw new Exception("Manual teardown did not destroy exactly once");

var old = new SimVfx(actor, "old marker", 1);
VfxDataPointers.Notify((nint)1);
var replacement = new SimVfx(actor, "new marker", 1);
old.Despawn();
if (!replacement.IsActive || VfxFunctions.Removals != 1)
    throw new Exception("Old owner destroyed a reused native address");
replacement.Despawn();
if (replacement.IsActive || VfxFunctions.Removals != 2)
    throw new Exception("Replacement was not released");
Console.WriteLine("VFX lifetime: native expiry, repeated teardown, and address reuse passed.");

namespace FFXIVClientStructs.FFXIV.Client.Game.Character { public struct Character { } }
namespace FFXIVClientStructs.FFXIV.Client.Graphics.Vfx { public struct VfxData { } }
namespace AnoMech.Core.SimObjects
{
    public interface ISimObject { }
    public unsafe class SimCharacter { public void* BattleCharaPtr => null; }
}
namespace AnoMech.Pointers
{
    public static class VfxDataPointers
    {
        public static event Action<nint>? Destroying;
        public static void Notify(nint pointer) => Destroying?.Invoke(pointer);
    }
}
namespace AnoMech.Core.Native
{
    public static unsafe class VfxFunctions
    {
        public static int Removals;
        public static FFXIVClientStructs.FFXIV.Client.Graphics.Vfx.VfxData* SpawnActorVfx(string path,
            FFXIVClientStructs.FFXIV.Client.Game.Character.Character* caster,
            FFXIVClientStructs.FFXIV.Client.Game.Character.Character* target)
            => (FFXIVClientStructs.FFXIV.Client.Graphics.Vfx.VfxData*)1;
        public static void RemoveActorVfx(FFXIVClientStructs.FFXIV.Client.Graphics.Vfx.VfxData* pointer)
        {
            Removals++;
            VfxDataPointers.Notify((nint)pointer);
        }
    }
}
