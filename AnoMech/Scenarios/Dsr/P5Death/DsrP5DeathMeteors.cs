using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P5Death;

public sealed partial class DsrP5DeathScenario
{
    private readonly SimEnemy?[] meteors=new SimEnemy?[8];

    private void SpawnMeteors()
    {
        cleansesActive=false;
        foreach(var actor in actors)actor.Despawn();
        foreach(var obj in objects)obj.Despawn();
        state!.MeteorsActive=true;
        Array.Clear(state.Destinations);
        for(var i=0;i<8;i++)
        {
            meteors[i]=Spawn(0x3660,10495,state.MeteorPositions[i],false);
            meteors[i]?.SetVisible(true);
            meteors[i]?.SetTargetable(true);
        }
        state.LimitBreakMessage="D4：瞄準正北隕石，以法系 LB2 擊破北側三顆；其他五顆由隊友處理。";
    }

    internal bool TryLimitBreak(Vector3 target)
    {
        if(state==null||world==null||!state.MeteorsActive||state.Complete||state.LimitBreakUsed||state.LimitBreakCasting)return false;
        var caster=world.Party.Get(7);
        if(!caster.IsAlive())return false;
        if(Vector3.Distance(caster!.Position,target)>25)
        {
            state.LimitBreakMessage="目標超出 LB2 的 25 公尺施放距離。";
            return false;
        }
        state.LimitBreakTarget=target;
        state.LimitBreakOrigin=caster.Position;
        state.LimitBreakElapsed=0;
        state.LimitBreakCasting=true;
        state.LimitBreakMessage="小型隕石讀條中，移動會中斷。";
        return true;
    }

    private void AutoLimitBreak()
    {
        if(world!.Party.Get(7) is SimPartyNpc)TryLimitBreak(state!.MeteorPositions[0]);
    }

    private void TickLimitBreak(float delta)
    {
        if(!state!.LimitBreakCasting)return;
        var caster=world!.Party.Get(7);
        if(!caster.IsAlive()||Vector3.DistanceSquared(caster!.Position,state.LimitBreakOrigin)>.04f)
        {
            state.LimitBreakCasting=false;
            state.LimitBreakMessage="LB2 讀條中斷，可以重新施放。";
            Array.Clear(state.Destinations);
            return;
        }
        state.LimitBreakElapsed+=delta;
        if(state.LimitBreakElapsed<3f)return;
        state.LimitBreakCasting=false;
        state.LimitBreakUsed=true;
        Spawn(DsrConstants.Npc.Helper,3632,state.LimitBreakTarget,false)?.Cast(204,state.LimitBreakTarget,0);
        var count=0;
        for(var i=0;i<8;i++)
            if(!state.MeteorDestroyed[i]&&Vector3.DistanceSquared(state.MeteorPositions[i],state.LimitBreakTarget)<=100)
            {DestroyMeteor(i);count++;}
        state.LimitBreakMessage=$"LB2 擊破 {count} 顆隕石。";
        Array.Clear(state.Destinations);
    }

    private void DestroyMeteor(int index)
    {
        if(state!.MeteorDestroyed[index])return;
        state.MeteorDestroyed[index]=true;
        meteors[index]?.Despawn();
    }

    private void ResolveMeteors()
    {
        state!.LimitBreakCasting=false;
        if(state.MeteorDestroyed.Any(destroyed=>!destroyed))
        {
            Spawn(DsrConstants.Npc.Helper,3632,Vector3.Zero,false)?.Cast(27544,Vector3.Zero,0);
            foreach(var member in world!.Party.ActiveMembers())Hit(member,"隕石未擊破：D4 需以 LB2 覆蓋北側三顆隕石");
        }
        state.MeteorsActive=false;
    }
}
