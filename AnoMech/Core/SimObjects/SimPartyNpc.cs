using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace AnoMech.Core.SimObjects;

public sealed unsafe class SimPartyNpc : SimNpc, ISimPartyMember
{
    private nint modelDrawObject;
    public PartyRole Role { get; set; }
    public bool Dead { get; private set; }
    public byte ClassJob { get; }
    public string DisplayName { get; }

    internal SimPartyNpc(int index, Coordinates coordinates, PartyRole role, byte classJob, string name) : base(index, coordinates)
    {
        Role = role;
        ClassJob = classJob;
        DisplayName = name;
    }

    public void Knockback(Vector3 source, float distance, float speed) => Movement.Knockback(source, distance, speed);

    public override void Tick(float deltaSeconds)
    {
        base.Tick(deltaSeconds);
        var chara = BattleCharaPtr;
        if (chara == null || !chara->IsReadyToDraw() || chara->DrawObject == null) return;
        var draw = chara->DrawObject;
        if (modelDrawObject != (nint)draw)
        {
            modelDrawObject = (nint)draw;
            Plugin.Log.Info($"SimPartyNpc: {DisplayName} model ready, visible={draw->IsVisible}, renderFlags={chara->RenderFlags:X}, scale={chara->Scale}, drawScale={draw->Object.Scale}, position={chara->Position}, drawPosition={draw->Object.Position}");
            draw->Object.Scale = new Vector3(chara->Scale);
            SetPosition(Position);
            SetRotation(Rotation);
        }
        // Party actors have no hidden phase; native PC updates can hide an asynchronously created draw object.
        chara->RenderFlags &= ~(int)FFXIVClientStructs.FFXIV.Client.Game.Object.VisibilityFlags.Model;
        if (!draw->IsVisible) draw->IsVisible = true;
    }

    public override void Despawn()
    {
        base.Despawn();
    }

    public void OnKilled()
    {
        Dead = true;
        StopMoving();
        var bc = BattleCharaPtr;
        if (bc == null) return;
        ApplyDeadState(bc);
        this.PlayKoActionTimeline();
    }

    private static void ApplyDeadState(BattleChara* bc)
    {
        bc->Health = 0;
        bc->Mana = 0;
        bc->Mode = CharacterModes.Dead;
    }
}
