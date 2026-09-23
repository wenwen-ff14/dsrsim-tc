using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Core.Native;

// Synthesizes an ActionEffectN through the game's own handler — the same path the
// client runs on a server ActionEffect packet — so effects the sim firewall blocks
// are replayed rather than poked into internals.
internal static unsafe class ActionEffects
{
    // Synthetic monotonically-increasing GlobalSequence (server's per-action id). The
    // local player's own effect correlates by SourceSequence, so this is only for
    // faithfulness; it just needs to be non-zero and unique per fire.
    private static uint globalSequence = 1;

    // Replays the combo signal (Type-0x1B: Value = used action, Param0 = 0 starter /
    // 1 continuation) delivered to the caster's target, so the client advances
    // ActionManager.Combo itself and lights up the next action. Packet shape from replay
    // data — see reference-combo-mechanism. SourceSequence must be the action's own
    // sequence so the client treats it as that action resolving. Delivers to `target`
    // (falling back to self if it's absent or not a registered BattleChara).
    public static void FireCombo(Character* caster, uint actionId, ushort sourceSequence, byte comboFlag,
        GameObjectId target = default)
        => Fire(caster,actionId,sourceSequence,target,comboFlag);

    public static void FireVisual(Character* caster,uint actionId,ushort sourceSequence,GameObjectId target)
        => Fire(caster,actionId,sourceSequence,target,null);

    private static void Fire(Character* caster,uint actionId,ushort sourceSequence,GameObjectId target,byte? comboFlag)
    {
        if (caster == null) return;
        var deliverTo = caster->GetGameObjectId();
        if (target.ObjectId != 0 && target.ObjectId != 0xE0000000)
        {
            var cm = CharacterManager.Instance();
            if (cm != null && cm->LookupBattleCharaByEntityId(target.ObjectId) != null)
                deliverTo = target;
        }

        var header = new ActionEffectHandler.Header
        {
            AnimationTargetId = deliverTo,
            ActionId = actionId,
            GlobalSequence = globalSequence++,
            AnimationLock = comboFlag.HasValue ? 0f : 0.6f,
            SourceSequence = sourceSequence,
            RotationInt = MathUtil.QuantizeRotation(caster->Rotation),
            SpellId = (ushort)actionId,
            AnimationVariation = 0,
            ActionType = ActionType.Action,
            Flags = 0,
            NumTargets = 1,
        };
        var effects = new ActionEffectHandler.TargetEffects();
        if(comboFlag is {} flag)effects.Effects[0] = new ActionEffectHandler.Effect
        {
            Type = 0x1B,
            Param0 = flag,
            Param4 = 0x80,
            Value = (ushort)actionId,
        };
        var p = caster->Position;
        var pos = new System.Numerics.Vector3(p.X, p.Y, p.Z); // Receive wants System.Numerics
        ActionEffectHandler.Receive(caster->EntityId, caster, &pos, &header, &effects, &deliverTo);
    }
}
