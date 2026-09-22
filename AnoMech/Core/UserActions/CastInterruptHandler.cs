using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Network;

namespace AnoMech.Core.UserActions;

// The server interrupts a player's cast on move/jump/cancel by echoing an
// ActorControl(15) back, which the client handles to clear the bar. The sim firewall
// eats that packet, so we synthesize it. No interrupt inside the last
// CastInterruptThreshold seconds (~0.5s = when the server resolves the action): that's
// the slidecast window, where the cast is committed.
internal sealed unsafe class CastInterruptHandler : IUserActionHandler
{
    // ActorControl category the server sends to interrupt a cast (0x0F). Captured from
    // replay data: a real self-cancel is ActorControl(15) on the caster with params
    // (538, 1, <castActionId>, 0) — param1/2 constant, param3 = the action.
    private const uint InterruptCastControl = 15;
    private const uint InterruptCastReason = 538;

    public void OnTick(float deltaSeconds)
    {
        var hooks = Plugin.PlayerInputHooks;
        var cancelRequested = hooks.PollCancelCast(); // drain every frame so a stale latch can't carry over

        var player = (BattleChara*)(Plugin.ObjectTable.LocalPlayer?.Address ?? 0);
        if (player == null || !player->CastInfo.IsCasting) return;

        var remaining = player->CastInfo.TotalCastTime - player->CastInfo.CurrentCastTime;
        if (remaining <= Plugin.Config.CastInterruptThreshold) return;

        if (hooks.MovementInputActive || hooks.IsJumping || cancelRequested)
            InterruptCast(player);
    }

    private static void InterruptCast(BattleChara* player)
    {
        AnoMech.Pointers.PacketDispatcherPointers.HandleActorControlPacket(
            player->EntityId, InterruptCastControl, InterruptCastReason, 1,
            player->CastInfo.ActionId, 0, 0, 0, 0, 0, 0xE0000000, false);
    }
}
