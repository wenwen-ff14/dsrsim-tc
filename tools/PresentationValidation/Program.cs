using AnoMech.Helpers;
using AnoMech.Core.Game;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Text;
using System.Runtime.InteropServices;

var utf8 = new UTF8Encoding(false, true);
foreach (var name in new[] { "騎神托爾丹", "聖騎士阿代爾斐爾", "Omega", new string('龍', 30), new string('A', 61) + "龍", new string('A', 60) + "🐉" })
{
    var buffer = Enumerable.Repeat((byte)255, 64).ToArray();
    Utf8NameEncoder.Write(name, buffer);
    var end = Array.IndexOf(buffer, (byte)0);
    if (end < 0 || end > 63) throw new Exception("Missing terminator");
    var decoded = utf8.GetString(buffer, 0, end);
    if (!name.StartsWith(decoded, StringComparison.Ordinal) ||
        (utf8.GetByteCount(name) <= 63 && decoded != name)) throw new Exception("Name corruption");
    Utf8NameEncoder.Write("龍", buffer);
    if (buffer.Skip(3).Any(b => b != 0)) throw new Exception("Stale name bytes");
}
Console.WriteLine("UTF-8: Chinese names, ASCII, byte limits, surrogate pairs and shorter rewrites passed.");

unsafe
{
    var system = BGMSystem.Instance();
    system->Scenes[0] = new() { SceneId = 0, BgmId = 77, PlayState = BGMSystem.PlayState.Playing };
    using var music = new Bgm();
    music.Play(312);
    if (system->Scenes[0].BgmId != 312) throw new Exception("Practice track missing from event scene");
    var calls = BGMSystem.SetCalls;
    for (var i = 0; i < 120; i++) music.Play(312);
    if (BGMSystem.SetCalls != calls) throw new Exception("Track restarted each frame");
    music.Restart();
    music.Reset();
    if (system->Scenes[0].BgmId != 77 || music.IsActive) throw new Exception("Previous music not restored after restart/reset");
    music.Play(312);
    system->Scenes[0].BgmId = 88;
    music.Reset();
    if (system->Scenes[0].BgmId != 88) throw new Exception("Overwrote a new external track during cleanup");
    system->Scenes[0].BgmId = 0;
    music.Play(312);
    music.Reset();
    if (system->Scenes[0].BgmId != 0) throw new Exception("Empty scene not restored");
}
Console.WriteLine("BGM: event-scene playback, idempotence, restart, restore and external ownership passed.");

unsafe
{
    using var native = new WeaponFixture();
    native.Verify();
}
Console.WriteLine("TC weapons: full native call arguments and weapon pointers for all three slots passed.");

unsafe { StanceFixture.Verify(); }
Console.WriteLine("TC stance: native arguments, state confirmation and null guard passed.");

var resets = new AnoMech.Core.Map.MapEffectResets();
var effects = new List<(uint State, byte Index)>();
for (byte eye = 0; eye < 8; eye++)
{
    resets.Track(eye, 0x00080004);
    resets.Track(eye, 0x00080004);
    resets.Reset((state, index) => effects.Add((state, index)));
    resets.Reset((state, index) => effects.Add((state, index)));
    if (effects.Count != eye + 1 || effects[eye] != (0x00080004u, eye))
        throw new Exception("Dragon eye cleanup lost its index, duplicated, or survived reset");
}
Console.WriteLine("Map effects: all eight dragon-eye indices cleared, repeated resets idempotent, restart tracking passed.");

unsafe class StanceFixture
{
    private static int calls;
    private static bool valid;
    [UnmanagedCallersOnly]
    private static void Draw(FFXIVClientStructs.FFXIV.Client.Game.Character.TimelineContainer* p, byte drawn, byte animate)
    {
        calls++;
        valid = drawn == 1 && animate == 1;
        p->Flags3 |= 0xC0;
    }
    public static void Verify()
    {
        AnoMech.Plugin.SigScanner.Address = (nint)(delegate* unmanaged<FFXIVClientStructs.FFXIV.Client.Game.Character.TimelineContainer*, byte, byte, void>)&Draw;
        var timeline = new FFXIVClientStructs.FFXIV.Client.Game.Character.TimelineContainer { Flags3 = 0x10 };
        if (!AnoMech.Pointers.TimelinePointers.DrawWeapon(&timeline) || timeline.Flags3 != 0xD0 || !valid || calls != 1)
            throw new Exception("Native stance transition failed");
        if (AnoMech.Pointers.TimelinePointers.DrawWeapon(null) || calls != 1)
            throw new Exception("Null timeline reached native function");
    }
}

namespace AnoMech
{
    public static class Plugin
    {
        public static Scanner SigScanner = new();
        public static Logger Log = new();
        public class Scanner
        {
            public nint Address;
            public bool TryScanText(string signature, out nint address) { address = Address; return address != 0; }
        }
        public class Logger { public void Warning(string message) { } }
    }
}

unsafe class WeaponFixture : IDisposable
{
    private FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer* data =
        (FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer*)NativeMemory.AllocZeroed(0x268);
    private static int calls;
    private static bool valid;

    [UnmanagedCallersOnly]
    private static void Load(FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer* p,
        FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer.WeaponSlot slot,
        FFXIVClientStructs.FFXIV.Client.Game.Character.WeaponModelId model,
        byte redraw, byte a5, byte skip, byte a7, int mode)
    {
        calls++;
        valid = p != null && slot == FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer.WeaponSlot.MainHand &&
            model.Value == 0x4003700C9 && redraw == 1 && a5 == 0 && skip == 0 && a7 == 0 && mode == 0;
    }

    public void Verify()
    {
        FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer.MemberFunctionPointers.LoadWeapon =
            (nint)(delegate* unmanaged<FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer*,
            FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer.WeaponSlot,
            FFXIVClientStructs.FFXIV.Client.Game.Character.WeaponModelId, byte, byte, byte, byte, int, void>)&Load;
        AnoMech.Pointers.DrawDataPointers.LoadWeapon(data,
            FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer.WeaponSlot.MainHand,
            new() { Value = 0x4003700C9 });
        if (calls != 1 || !valid) throw new Exception("Native LoadWeapon arguments incorrect");
        for (var slot = 0; slot < 3; slot++)
        {
            *(nint*)((byte*)data + 0x18 + slot * 0x70) = 0x1000 + slot;
            *(nint*)((byte*)data + 0x28 + slot * 0x70) = 0xBAD;
            if ((nint)AnoMech.Pointers.DrawDataPointers.WeaponDrawObject(data, slot) != 0x1000 + slot)
                throw new Exception("Read obsolete attachment pointer instead of Weapon pointer");
        }
    }

    public void Dispose() { NativeMemory.Free(data); data = null; }
}

namespace FFXIVClientStructs.FFXIV.Client.Game.Character
{
    [StructLayout(LayoutKind.Explicit, Size = 0x350)]
    public struct TimelineContainer { [FieldOffset(0x34E)] public byte Flags3; }
    [StructLayout(LayoutKind.Sequential)]
    public struct WeaponModelId { public ulong Value; }
    public struct DrawDataContainer
    {
        public enum WeaponSlot : uint { MainHand, OffHand }
        public static class MemberFunctionPointers { public static nint LoadWeapon; }
    }
}

namespace FFXIVClientStructs.FFXIV.Client.Graphics.Scene
{
    public struct DrawObject { }
}

namespace FFXIVClientStructs.FFXIV.Client.Game
{
    public unsafe struct BGMSystem
    {
        private static readonly BGMSystem* System = (BGMSystem*)NativeMemory.AllocZeroed((nuint)sizeof(BGMSystem));
        private static readonly Scene* SceneData = (Scene*)NativeMemory.AllocZeroed((nuint)sizeof(Scene));
        public static int SetCalls;
        public Span<Scene> Scenes => new(SceneData, 1);
        public static BGMSystem* Instance() => System;
        public static void SetBGM(ushort id, uint sceneId)
        {
            SetCalls++;
            SceneData[0] = new() { SceneId = sceneId, BgmId = id, PlayState = PlayState.Playing };
        }
        public void ResetBGM(uint sceneId) => SceneData[0] = new() { SceneId = sceneId };
        public struct Scene { public uint SceneId; public ushort BgmId; public PlayState PlayState; }
        public enum PlayState { Paused, Playing }
    }
}
