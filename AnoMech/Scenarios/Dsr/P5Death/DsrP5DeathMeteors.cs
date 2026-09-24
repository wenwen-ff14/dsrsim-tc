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
        foreach(var chain in chains)chain.Tether.Despawn();chains.Clear();
        foreach(var member in world!.Party.ActiveMembers())foreach(var id in Statuses)member.RemoveStatus(id);
        Array.Clear(doomExpires);
        world.Map.AddEffect(0x00080004,(byte)state!.EyeIndex);
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
        state.LimitBreakMessage=state.LimitBreakUsed?"本輪 LB 已使用。":"LB 已滿，可以自由選點施放；命中隕石會消失，不自動結束。";
    }

    internal bool TryLimitBreak(Vector3 target)
    {
        if(state==null||world==null||state.Complete||state.LimitBreakUsed||state.LimitBreakCasting)return false;
        var role=(int)world.Party.PlayerRole;
        if(role is not (6 or 7)||state.LimitBreakAction==0)return false;
        var caster=world.Party.Get(role);
        if(!caster.IsAlive())return false;
        var range=state.LimitBreakAction==4239?30:25;
        if(Vector3.Distance(caster!.Position,target)>range || state.LimitBreakAction==4239&&Vector3.DistanceSquared(caster.Position,target)<.01f)
        {
            state.LimitBreakMessage=$"請選擇 {range} 公尺內的有效 LB2 目標。";
            return false;
        }
        state.LimitBreakTarget=target;
        state.LimitBreakRole=role;
        state.LimitBreakOrigin=caster.Position;
        state.LimitBreakElapsed=0;
        state.LimitBreakCasting=true;
        state.LimitBreakMessage="LB2 原版讀條中，移動會中斷。";
        return true;
    }

    private void TickLimitBreak(float delta)
    {
        if(state!.LimitBreakUsed)
        {
            state.LimitBreakRefill+=delta;
            if(state.LimitBreakRefill>=1f)
            {
                state.LimitBreakUsed=false;
                state.LimitBreakMessage="LB 已補滿，可繼續練習；命中隕石會消失，不自動結束。";
            }
        }
        if(!state!.LimitBreakCasting)return;
        var caster=world!.Party.Get(state.LimitBreakRole);
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
        state.LimitBreakRefill=0;
        if(caster is not SimPlayer)
            Spawn(DsrConstants.Npc.Helper,3632,caster!.Position,false)?.Cast(state.LimitBreakAction,state.LimitBreakTarget,0);
        var destroyed=0;
        if(state.MeteorsActive)
            for(var i=0;i<meteors.Length;i++)
                if(!state.MeteorDestroyed[i]&&LimitBreakHits(state.MeteorPositions[i]))
                {
                    state.MeteorDestroyed[i]=true;
                    meteors[i]?.Despawn();
                    destroyed++;
                }
        state.LimitBreakMessage=$"LB2 擊破 {destroyed} 顆隕石；1 秒後補滿，可繼續練習。";
        Array.Clear(state.Destinations);
    }

    private bool LimitBreakHits(Vector3 position)
    {
        if(state!.LimitBreakAction!=4239)return Vector3.DistanceSquared(position,state.LimitBreakTarget)<=100;
        var direction=state.LimitBreakTarget-state.LimitBreakOrigin;direction.Y=0;
        direction=Vector3.Normalize(direction);
        var offset=position-state.LimitBreakOrigin;offset.Y=0;
        var forward=Vector3.Dot(offset,direction);
        return forward>=0&&forward<=30&&MathF.Abs(offset.X*direction.Z-offset.Z*direction.X)<=2.5f;
    }

}
