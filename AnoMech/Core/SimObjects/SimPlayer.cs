using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Core.SimObjects;

public sealed unsafe class SimPlayer(Coordinates coordinates) : SimCharacter(coordinates), ISimPartyMember
{
    private SimCast? practiceLimitBreak;
    private bool practiceLimitBreakActive;
    private uint practiceLimitBreakAction;
    private GameObjectId? practiceLimitBreakTarget;

    internal void BeginPracticeLimitBreak(Vector3 target,uint action,GameObjectId? targetId=null)
    {
        if(BattleCharaPtr==null)return;
        practiceLimitBreak??=new SimCast(this,Coordinates);
        practiceLimitBreakActive=true;
        practiceLimitBreakAction=action;
        practiceLimitBreakTarget=targetId;
        if(action==4239)SetRotation(MathF.Atan2(target.X-Position.X,target.Z-Position.Z));
        practiceLimitBreak.NativeCast(action,FFXIVClientStructs.FFXIV.Client.Game.ActionType.Action,0,3,false,position:target,targetId:targetId);
        var am=FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
        if(am!=null&&BattleCharaPtr!=null)
        {
            am->CastActionType=FFXIVClientStructs.FFXIV.Client.Game.ActionType.Action;
            am->CastActionId=am->CastSpellId=action;
            am->CastTimeElapsed=0;am->CastTimeTotal=3;
            am->CastTargetId=targetId??new GameObjectId{ObjectId=0xE0000000};
            am->CastTargetPosition=Coordinates.ToGlobal(target);
            am->CastRotation=Rotation;
            am->OpenCastBar(BattleCharaPtr,FFXIVClientStructs.FFXIV.Client.Game.ActionType.Action,action,action,0,0,3);
        }
    }

    internal void UpdatePracticeLimitBreak(float elapsed)
    {
        if(!practiceLimitBreakActive||BattleCharaPtr==null)return;
        BattleCharaPtr->CastInfo.CurrentCastTime=elapsed;
        var am=FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
        if(am!=null&&am->CastActionId==practiceLimitBreakAction)
        {
            am->CastTimeElapsed=Math.Clamp(elapsed,0,3);
            am->CastTimeTotal=3;
        }
    }

    internal void EndPracticeLimitBreak(bool completed,Vector3 target=default)
    {
        if(!practiceLimitBreakActive)return;
        practiceLimitBreakActive=false;
        if(!completed&&BattleCharaPtr!=null)
            AnoMech.Pointers.PacketDispatcherPointers.HandleActorControlPacket(
                BattleCharaPtr->EntityId,15,538,1,practiceLimitBreakAction,0,0,0,0,0,0xE0000000,false);
        practiceLimitBreak?.Despawn();
        var am=FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
        if(am!=null&&am->CastActionId==practiceLimitBreakAction)
        {
            am->CastActionId=am->CastSpellId=0;
            am->CastTimeElapsed=am->CastTimeTotal=0;
            am->CastTargetId=new GameObjectId{ObjectId=0xE0000000};
            am->CastTargetPosition=default;
        }
        if(completed)
        {
            if(practiceLimitBreakAction==4239)SetRotation(MathF.Atan2(target.X-Position.X,target.Z-Position.Z));
            practiceLimitBreak?.NativeActionEffect(practiceLimitBreakAction,0,(ushort)practiceLimitBreakAction,0,FFXIVClientStructs.FFXIV.Client.Game.ActionType.Action,0,position:target,animationTargetId:practiceLimitBreakTarget,actionTargetId:practiceLimitBreakTarget);
        }
        practiceLimitBreakTarget=null;
    }
    private const ushort StunStatusId = 896;  // "Down for the Count" (896) — IsPermanent + LockControl variant.
    private const ushort DeepFreezeStatusId = 3480;

    // The player's HP bar (real bc->Health) is touched only on a scenario KO — dropped to a 1-HP
    // sliver here (from OnKilled on a real death, and from Game.Kill for the godmode preview),
    // restored in RestoreHpBar (revive / godmode heal-back).
    public void DropHpBar()
    {
        var bc = BattleCharaPtr;
        if (bc != null) bc->Health = 1;
    }

    public void RestoreHpBar()
    {
        var bc = BattleCharaPtr;
        if (bc != null && bc->Health < bc->MaxHealth) bc->Health = bc->MaxHealth;
    }

    public PartyRole Role { get; set; }
    public bool Dead { get; private set; }

    // Player activity for stillness/movement mechanics (e.g. Pyretic, Acceleration Bomb).
    // IsMoving = locomotion input (the engine's own RMIWalk movement sample, the same signal
    // bossmod keys off) OR jumping OR using any action — all three "break" a don't-move mechanic
    // in real FFXIV, so all three count here. IsActing = IsMoving OR auto-attacking, i.e. the
    // strictly-broader "is the player doing something" trigger. Both are re-sampled each tick and
    // forced false while KO'd or frozen. Scenarios read these on Party.Player at the mechanic's resolve time.
    public bool IsMoving { get; private set; }
    public bool IsActing { get; private set; }

    internal override BattleChara* BattleCharaPtr => (BattleChara*)(Plugin.ObjectTable.LocalPlayer?.Address ?? 0);

    private PlayerMovement? movement;
    private protected override PlayerMovement Movement => movement ??= new PlayerMovement(this);

    public void Knockback(Vector3 source, float distance, float speed) => Movement.Knockback(source, distance, speed);

    // The player's input lock is a pure function of its own state, re-derived
    // every tick: KO and Deep Freeze block movement and actions; knockback
    // blocks movement input only. base.Tick advances Movement
    // first, so a slide that arrives this frame has already cleared IsMoving.
    public override void Tick(float deltaSeconds)
    {
        base.Tick(deltaSeconds);
        SampleActivity();
        SyncInputLock();
    }

    private void SampleActivity()
    {
        var hooks = Plugin.PlayerInputHooks;
        // Drain the action latch every frame — even while dead — so a stale press can't carry over.
        var actedThisFrame = hooks.PollActionUsed();
        if (Dead || HasStatus(DeepFreezeStatusId))
        {
            IsMoving = false;
            IsActing = false;
            return;
        }
        IsMoving = hooks.MovementInputActive || actedThisFrame || hooks.IsJumping;
        IsActing = IsMoving || hooks.IsAutoAttacking;
    }

    public void OnKilled()
    {
        Dead = true;
        StopMoving();
        DropHpBar(); // real-death bar drop (bots do the same in their own OnKilled); godmode skips this path
        AddStatus(StunStatusId);
        this.PlayKoActionTimeline();
        SyncInputLock(); // engage the lock now, not one frame later
    }

    public override void Despawn()
    {
        EndPracticeLimitBreak(false);
        base.Despawn();
        StopMoving();
        // Undo any KO bar drop (no-op if already full). Unconditional so it also covers a godmode
        // preview drop, where Dead is never set and a pending heal on Game.Events may be cleared by reset.
        RestoreHpBar();
        if (Dead)
        {
            ResetActionTimeline();
            PlayActionTimeline(77); // revive
            Dead = false;
        }
        // No SimPlayer ticks between a reset and the next scenario, so the lock
        // must be cleared here — otherwise a die-then-reset leaves the player
        // input-locked in the inn.
        SyncInputLock();
    }

    private void SyncInputLock()
    {
        var hooks = Plugin.PlayerInputHooks;
        var incapacitated = Dead || HasStatus(DeepFreezeStatusId);
        hooks.ZeroMovement = incapacitated || Movement.IsMoving;
        hooks.DisableAllActions = incapacitated;
    }
}
