using System;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public sealed partial class DsrP6DragonsScenario
{
    private readonly SimEnemy?[,] fireballs=new SimEnemy?[3,3];

    private void DepartWrothHraesvelgr()
    {
        hraesvelgr?.SetTargetable(false);
        hraesvelgr?.PlayDeparture(DsrConstants.Timeline.KnightDeparture);
    }
    private void ShowWrothDive()
    {
        hraesvelgr?.SetPosition(state!.Wroth.DiveOrigin);
        hraesvelgr?.HoldFacing(state!.Wroth.DiveOrigin.Z<0?0:MathF.PI);
        hraesvelgr?.QueueEntrance(DsrConstants.Timeline.KnightEntrance,.65f);
        hraesvelgr?.SetVisible(true);
        for(var r=0;r<8;r++)state!.Destinations[r]=new(state.Wroth.StartSide*19,0,0);
    }
    private void ReturnWrothHraesvelgr()
    {
        hraesvelgr?.SetPosition(DsrP6DragonsState.Hraesvelgr);
        hraesvelgr?.HoldFacing(-MathF.PI/2);
        hraesvelgr?.QueueEntrance(DsrConstants.Timeline.KnightEntrance,.65f);
        hraesvelgr?.SetVisible(true);
        hraesvelgr?.SetTargetable(true);
    }

    private void ApplyFlames()
    {
        for(var i=0;i<6;i++)world!.Party.Get(state!.Flames[i])?.AddStatus(i<4?(ushort)2758:(ushort)2759,23f);
        state!.FlamesAssigned=true;
    }
    private void BeginAkhMorn()
    {
        var target=world!.Party.Get(state!.AkhMornTarget)!;
        nidhogg?.Cast(27974,target.Position,7.7f,targetId:target.GameObjectId,fireDelay:.267f);
    }
    private void SpawnFireballs(int wave)
    {
        for(var i=0;i<3;i++)
        {
            fireballs[wave,i]=Spawn(0x33B6,0,state!.Wroth.Fireballs(wave)[i],true,false);
            fireballs[wave,i]?.SetTargetable(false);
        }
        if(wave==1)for(var r=0;r<8;r++)state!.Destinations[r]=state.Wroth.Start;
    }
    private void BeginFireballs(int wave)
    {
        for(var i=0;i<3;i++)fireballs[wave,i]?.Cast(26409,castSeconds:5,omenDelay:5);
    }
    private void ResolveFireballs(int wave)
    {
        foreach(var origin in state!.Wroth.Fireballs(wave))
            foreach(var member in world!.Party.ActiveMembers())
            {
                var offset=member.Position-origin;
                if(MathF.Abs(offset.X)<=3&&MathF.Abs(offset.Z)<=44||MathF.Abs(offset.Z)<=3&&MathF.Abs(offset.X)<=44)
                    Hit(member,$"第 {wave+1} 組烈焰十字爆：離開火球的橫列與直行");
            }
    }
    private void ClearFireballs()
    {
        for(var wave=0;wave<3;wave++)for(var i=0;i<3;i++)
        {
            fireballs[wave,i]?.Despawn();
            fireballs[wave,i]=null;
        }
    }
    private void HideFireballs(int wave)
    {
        for(var i=0;i<3;i++)fireballs[wave,i]?.SetVisible(false);
    }
}
