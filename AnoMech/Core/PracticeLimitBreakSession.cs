using System;
using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P5Death;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace AnoMech.Core;

internal sealed unsafe class PracticeLimitBreakSession : IDisposable
{
    private DsrP5DeathScenario? scenario;
    private SimWorld? world;
    private SimPlayer? caster;
    private bool captured, casting;
    private byte savedBars;
    private ushort savedUnits, savedBarUnits;
    private nint shownAddon;
    private bool savedVisible;

    public PracticeLimitBreakSession()
    {
        Plugin.PlayerInputHooks.CanPracticeLimitBreak=CanUse;
        Plugin.PlayerInputHooks.UsePracticeLimitBreak=Use;
        Plugin.PlayerInputHooks.CastCancelled+=Cancel;
    }

    private bool CanUse()=>scenario?.PracticeReady==true&&caster.IsAlive()&&!Plugin.GameInstance.Paused;

    private bool Use(Vector3? target)
    {
        if(!CanUse())return false;
        if(target.HasValue)return scenario!.TryLimitBreak(world!.Coordinates.ToLocal(target.Value));
        scenario!.RequestLimitBreak();
        return scenario.PracticeCasting;
    }

    private void Cancel()
    {
        if(!casting)return;
        scenario?.CancelPracticeCast();
        casting=false;
        caster?.EndPracticeLimitBreak(false);
        Plugin.PlayerInputHooks.PracticeLimitBreakCasting=false;
    }

    public void Update(DsrP5DeathScenario? current,SimWorld simWorld)
    {
        if(current?.PracticeActive!=true||!simWorld.Map.IsInInstance){Reset();return;}
        if(scenario!=null&&scenario!=current)Reset();
        scenario=current;world=simWorld;caster=world.Party.Get(7) as SimPlayer;
        var gauge=LimitBreakController.Instance();
        if(gauge==null)return;
        if(!captured)
        {
            savedBars=gauge->BarCount;savedUnits=gauge->CurrentUnits;savedBarUnits=gauge->BarUnits;
            captured=true;
        }
        gauge->BarCount=2;gauge->BarUnits=10000;
        gauge->CurrentUnits=(ushort)(current.PracticeUsed?0:20000);
        var manager=RaptureAtkUnitManager.Instance();
        var addon=manager==null?null:manager->GetAddonByName("_LimitBreak");
        if(addon!=null)
        {
            if(shownAddon!=(nint)addon){shownAddon=(nint)addon;savedVisible=addon->IsVisible;}
            if(!addon->IsVisible)addon->Show(true,0);
        }
        Plugin.PlayerInputHooks.PracticeLimitBreakEnabled=caster!=null;
        if(caster==null)return;
        if(current.PracticeCasting&&!casting)
        {
            caster.BeginPracticeLimitBreak(current.PracticeTarget);
            casting=true;
        }
        if(casting&&!current.PracticeCasting)
        {
            casting=false;
            caster.EndPracticeLimitBreak(current.PracticeUsed,current.PracticeTarget);
        }
        if(casting)
        {
            if(!caster.IsAlive()||Plugin.PlayerInputHooks.IsJumping||Plugin.PlayerInputHooks.MovementInputActive)Cancel();
            else caster.UpdatePracticeLimitBreak(current.PracticeCastElapsed);
        }
        Plugin.PlayerInputHooks.PracticeLimitBreakCasting=casting;
    }

    public void Reset()
    {
        Cancel();
        var actions=FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
        if(captured&&actions!=null&&actions->AreaTargetingActionId==204)
        {
            actions->AreaTargetingActionId=0;
            actions->AreaTargetingSpellId=0;
            actions->AreaTargetingExecuteAtCursor=false;
        }
        Plugin.PlayerInputHooks.PracticeLimitBreakEnabled=false;
        Plugin.PlayerInputHooks.PracticeLimitBreakCasting=false;
        if(captured)
        {
            var gauge=LimitBreakController.Instance();
            if(gauge!=null){gauge->BarCount=savedBars;gauge->CurrentUnits=savedUnits;gauge->BarUnits=savedBarUnits;}
            var manager=RaptureAtkUnitManager.Instance();
            var addon=manager==null?null:manager->GetAddonByName("_LimitBreak");
            if(addon!=null&&(nint)addon==shownAddon&&!savedVisible)addon->Hide(true,false,0);
        }
        captured=false;shownAddon=0;scenario=null;world=null;caster=null;
    }

    public void Dispose()
    {
        Reset();
        Plugin.PlayerInputHooks.CanPracticeLimitBreak=null;
        Plugin.PlayerInputHooks.UsePracticeLimitBreak=null;
        Plugin.PlayerInputHooks.CastCancelled-=Cancel;
    }
}
