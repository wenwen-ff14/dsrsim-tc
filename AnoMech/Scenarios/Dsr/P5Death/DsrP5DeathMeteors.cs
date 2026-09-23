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
        state.LimitBreakMessage=state.LimitBreakUsed?"本輪 LB 已使用。":"LB 已滿，可以自由選點施放；命中數依實際範圍判定。";
    }

    internal bool TryLimitBreak(Vector3 target)
    {
        if(state==null||world==null||state.Complete||state.LimitBreakUsed||state.LimitBreakCasting)return false;
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
        if(caster is not SimPlayer)
            Spawn(DsrConstants.Npc.Helper,3632,state.LimitBreakTarget,false)?.Cast(204,state.LimitBreakTarget,0);
        var count=0;
        for(var i=0;i<8;i++)
            if(state.MeteorsActive&&!state.MeteorDestroyed[i]&&Vector3.DistanceSquared(state.MeteorPositions[i],state.LimitBreakTarget)<=100)
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
            foreach(var member in world!.Party.ActiveMembers())Hit(member,"隕石未擊破：限時結束時仍有隕石存活");
        }
        state.MeteorsActive=false;
    }
}
