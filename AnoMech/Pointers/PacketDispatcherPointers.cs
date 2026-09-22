using Dalamud.Utility.Signatures;
using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Pointers;

// API 13's bundled ClientStructs predates the named spawn packet binding.
// Layout follows FFXIVClientStructs Client/Game/Network/SpawnPackets.cs.
[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public struct SpawnObjectPacket
{
    [FieldOffset(0x00)] public byte ObjectIndex;
    [FieldOffset(0x01)] public byte ObjectKind;
    [FieldOffset(0x02)] public byte TargetableStatus;
    [FieldOffset(0x03)] public byte Visibility;
    [FieldOffset(0x04)] public uint BaseId;
    [FieldOffset(0x08)] public uint EntityId;
    [FieldOffset(0x0C)] public uint LayoutId;
    [FieldOffset(0x10)] public EventId EventId;
    [FieldOffset(0x14)] public uint OwnerId;
    [FieldOffset(0x18)] public uint GimmickId;
    [FieldOffset(0x1C)] public float Radius;
    [FieldOffset(0x22)] public ushort Rotation;
    [FieldOffset(0x24)] public ushort FateId;
    [FieldOffset(0x26)] public byte EventState;
    [FieldOffset(0x2C)] public ushort TimelineState;
    [FieldOffset(0x34)] public float PositionX;
    [FieldOffset(0x38)] public float PositionY;
    [FieldOffset(0x3C)] public float PositionZ;
}

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public partial struct ActorCastPacket
{
    [FieldOffset(0x00)] public ushort ActionId;
    [FieldOffset(0x02)] public byte ActionType;
    [FieldOffset(0x03)] public byte OmenDelay; // The value gets divided by 10.0f
    [FieldOffset(0x04)] public uint ActionId_2;
    [FieldOffset(0x08)] public float CastTime;
    [FieldOffset(0x0C)] public uint TargetEntityId;
    [FieldOffset(0x10)] public ushort RotationInt; // Quantized Rotation
    [FieldOffset(0x12)] public bool Interruptible;
    [FieldOffset(0x14)] public uint BallistaEntityId;
    [FieldOffset(0x18)] public ushort PositionX; // Quantized Position
    [FieldOffset(0x1A)] public ushort PositionY; // Quantized Position
    [FieldOffset(0x1C)] public ushort PositionZ; // Quantized Position
}

[StructLayout(LayoutKind.Explicit, Size = 0x1)]
public partial struct DespawnCharacterPacket
{
    [FieldOffset(0x0)] public byte Index;
}

[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public partial struct UpdateClassInfoPacket
{
    [FieldOffset(0x0)] public byte ClassJobId;
    [FieldOffset(0x2)] public ushort CurrentLevel;
    [FieldOffset(0x4)] public ushort ClassJobLevel;
    [FieldOffset(0x6)] public ushort SyncedLevel;
    [FieldOffset(0x8)] public ushort ClassJobExp;
    [FieldOffset(0xC)] public uint BaseRestedExperience;
}

internal unsafe class PacketDispatcherPointers
{
    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", UseFlags = SignatureUseFlags.Pointer, ScanType = ScanType.Text)]
    private static ActorControlDelegate ActorControl { get; set; } = null!;

    // API 13 has six payload arguments, so target/replay must precede the newer arg7/arg8 slots.
    // Verified against BossMod 07e4209 WorldStateGameSync.ProcessPacketActorControlDelegate.
    private delegate void ActorControlDelegate(uint entityId, uint category, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6, ulong targetId, byte isRecorded);

    public static void HandleActorControlPacket(uint entityId, uint category, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6, uint arg7, uint arg8, GameObjectId targetId, bool isRecorded)
    {
        if (arg7 != 0 || arg8 != 0)
            throw new NotSupportedException("API 13 ActorControl supports six payload arguments.");
        ActorControl(entityId, category, arg1, arg2, arg3, arg4, arg5, arg6, targetId, isRecorded ? (byte)1 : (byte)0);
    }

    [Signature("40 53 57 48 83 EC ?? F6 42", UseFlags = SignatureUseFlags.Pointer, ScanType = ScanType.Text)]
    public static HandleSpawnObjectPacketDelegate HandleSpawnObjectPacket { get; private set; } = null!;

    public delegate void HandleSpawnObjectPacketDelegate(uint targetId, SpawnObjectPacket* packet);

    [Signature("40 53 57 48 81 EC ?? ?? ?? ?? 48 8B FA 8B", UseFlags = SignatureUseFlags.Pointer, ScanType = ScanType.Text)]
    public static HandleActorCastPacketDelegate HandleActorCastPacket { get; private set; } = null!;

    [Signature("40 53 48 83 EC 20 48 8B DA 48 8D 0D ?? ?? ?? ?? 0F", UseFlags = SignatureUseFlags.Pointer, ScanType = ScanType.Text)]
    public static HandleDespawnObjectPacketDelegate HandleDespawnObjectPacket { get; private set; } = null!;

    [Signature("48 89 5C 24 ?? 57 48 83 EC 40 0F B6 1A", UseFlags = SignatureUseFlags.Pointer, ScanType = ScanType.Text)]
    public static HandleDespawnCharacterPacketDelegate HandleDespawnCharacterPacket { get; private set; } = null!;

    // Technically the real HandleUpdateClassInfoPacket is a wrapper to this sig... but this is still close to other HandleX methods, so it fits here
    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 48 8D 0D ?? ?? ?? ?? 33", UseFlags = SignatureUseFlags.Pointer, ScanType = ScanType.Text)]
    public static HandleUpdateClassInfoPacketDelegate HandleUpdateClassInfoPacket { get; private set; } = null!;

    public delegate void HandleActorCastPacketDelegate(uint entityId, ActorCastPacket* packet);
    public delegate void HandleDespawnObjectPacketDelegate(uint unused, byte* packet);
    public delegate void HandleDespawnCharacterPacketDelegate(ulong unused, DespawnCharacterPacket* packet);
    public delegate void HandleUpdateClassInfoPacketDelegate(ulong unused, UpdateClassInfoPacket* packet);

    public static void Initialize()
    {
        Plugin.GameInterop.InitializeFromAttributes(new PacketDispatcherPointers());
    }
}
