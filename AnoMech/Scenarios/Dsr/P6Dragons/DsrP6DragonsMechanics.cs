using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public sealed partial class DsrP6DragonsScenario
{
    private void BeginBreath(bool second)
    {
        state!.SetBreath(second);
        var glow=second?state.SecondGlow:DsrP6Glow.Nidhogg;
        nidhogg?.Cast(glow==DsrP6Glow.Hraesvelgr?27954u:27955u,castSeconds:6f,fireDelay:second?.263f:.261f);
        hraesvelgr?.Cast(glow==DsrP6Glow.Nidhogg?27956u:27957u,castSeconds:6f,fireDelay:second?.263f:.261f);
        Spawn(DsrConstants.Npc.Helper,4954,Vector3.Zero,false)?.Cast(27960,castSeconds:6.7f,fireDelay:.277f);
        foreach(var tether in tethers)tether.Despawn();tethers.Clear();
        for(var r=2;r<8;r++)
            if(world!.Party.Get(r) is {} member)
            {
                tethers.Add(world.Tether(member,state.Fire[r]?nidhogg:hraesvelgr,state.Fire[r]?(ushort)194:(ushort)195));
            }
    }
    private void ResolveBlizzard()
    {
        foreach(var member in world!.Party.ActiveMembers())
            if(member.Position.LengthSquared()>400&&member.Position.LengthSquared()<1225)Hit(member,"暴雪環：站進中央圓形安全區");
    }
    private static bool InCone(Vector3 p,Vector3 origin,Vector3 target)
    {
        var v=p-origin;var direction=target-origin;
        return v.LengthSquared()<.01f||Vector3.Dot(Vector3.Normalize(v),Vector3.Normalize(direction))>=MathF.Cos(MathF.PI/18);
    }
    private void ResolveBreath(bool second)
    {
        foreach(var tether in tethers)tether.Despawn();tethers.Clear();
        for(var r=2;r<8;r++)breathTargets[r]=world!.Party.Get(r)!.Position;
        for(var r=2;r<8;r++)
        {
            var origin=state!.Fire[r]?DsrP6DragonsState.Nidhogg:DsrP6DragonsState.Hraesvelgr;
            Effect(state.Fire[r]?27958u:27959u,origin,breathTargets[r],world!.Party.Get(r));
        }
        for(var r=2;r<8;r++)
        {
            var member=world!.Party.Get(r)!;var fire=0;var ice=0;
            for(var j=2;j<8;j++)
            {
                var origin=state!.Fire[j]?DsrP6DragonsState.Nidhogg:DsrP6DragonsState.Hraesvelgr;
                if(InCone(member.Position,origin,breathTargets[j])){if(state.Fire[j])fire++;else ice++;}
            }
            if(second)
            {
                if(fire+ice!=1)Hit(member,"冰火二：散開，避免重疊吐息");
                member.AddStatus(state!.Fire[r]?(ushort)2898:(ushort)2899,10.956f);
            }
            else
            {
                member.RemoveStatus(2898);member.RemoveStatus(2899);
                if(fire!=1||ice!=1)
                {
                    if(fire>0&&ice==0)member.AddStatus(2898,10.956f);
                    if(ice>0&&fire==0)member.AddStatus(2899,10.956f);
                    Hit(member,"冰火一：必須各承受一次冰與火");
                }
            }
        }
        ResolveBreathTanks(second?state!.SecondGlow:DsrP6Glow.Nidhogg);
    }
    private void ResolveBreathTanks(DsrP6Glow glow)
    {
        if(glow==DsrP6Glow.Both)
        {
            for(var r=0;r<2;r++)
            {
                var target=world!.Party.Get(r)!;
                Effect(r==0?27961u:27962u,r==0?DsrP6DragonsState.Nidhogg:DsrP6DragonsState.Hraesvelgr,target.Position,target);
                if(Vector3.Distance(target.Position,world.Party.Get(1-r)!.Position)>6)Hit(target,"雙龍吐息：雙坦在場中重疊分攤");
                for(var j=2;j<8;j++)
                    if(Vector3.Distance(target.Position,world.Party.Get(j)!.Position)<=6)Hit(world.Party.Get(j)!,"雙龍吐息：遠離雙坦分攤");
            }
            return;
        }
        var nidhoggGlows=glow==DsrP6Glow.Nidhogg;
        var tank=world!.Party.Get(nidhoggGlows?1:0)!;
        var origin=nidhoggGlows?DsrP6DragonsState.Nidhogg:DsrP6DragonsState.Hraesvelgr;
        Effect(27965,nidhoggGlows?DsrP6DragonsState.Hraesvelgr:DsrP6DragonsState.Nidhogg,tank.Position,tank);
        foreach(var member in world.Party.ActiveMembers())
            if(member!=tank&&Vector3.Distance(member.Position,tank.Position)<15)Hit(member,"交錯吐息：遠離未發光龍的坦克大型死刑");
        Effect(nidhoggGlows?27963u:27964u,origin,Vector3.Zero);
        foreach(var member in world.Party.ActiveMembers())
        {
            var offset=member.Position-origin;
            var forward=nidhoggGlows?offset.X:-offset.X;
            if(forward>=0&&MathF.Abs(offset.Z)<=forward*MathF.Tan(MathF.PI/12))Hit(member,"龍之吐息：離開發光龍正前方");
        }
    }
    private void BeginStacks()
    {
        state!.SetStacks();
        nidhogg?.Cast(27971,castSeconds:7.7f,fireDelay:.262f);
        hraesvelgr?.Cast(27969,castSeconds:7.7f,fireDelay:.262f);
    }
    private void ResolveStacks()
    {
        for(var r=2;r<=3;r++)
        {
            var target=world!.Party.Get(r)!;
            (r==2?nidhogg:hraesvelgr)?.Cast(r==2?27972u:27970u,target.Position,0,targetId:target.GameObjectId);
            var members=world.Party.ActiveMembers().Where(m=>Vector3.Distance(m.Position,target.Position)<=4).ToArray();
            if(members.Length!=4)foreach(var member in members)Hit(member,"無盡輪迴：需要四人分攤");
        }
    }
    private void PrepareWings(bool second)
    {
        RestoreDragons();state!.SetWings(second);
        if(!second){nidhogg?.SetTargetable(false);nidhogg?.SetPosition(new Vector3(11,0,-34));nidhogg?.HoldFacing(0);}
    }
    private void ResolveWings(bool second)
    {
        var targets=world!.Party.ActiveMembers().OrderByDescending(m=>Vector3.DistanceSquared(m.Position,DsrP6DragonsState.Hraesvelgr)).Take(2).ToArray();
        for(var i=0;i<targets.Length;i++)
        {
            plumeTargets[i]=targets[i].Position;
            plumeRoles[i]=Enumerable.Range(0,8).First(r=>world.Party.Get(r)==targets[i]);
        }
        Effect(second?27944u:27941u,new(22,0,second?-11:11),new(-30,0,second?-11:11));
        foreach(var member in world.Party.ActiveMembers())
            if(second?member.Position.Z<=0:member.Position.Z>=0)Hit(member,"神聖之翼：前往未發光翅膀側");
        for(var i=0;i<targets.Length;i++)
        {
            Effect(27945,DsrP6DragonsState.Hraesvelgr,plumeTargets[i],targets[i]);
            if(plumeRoles[i]>=2)Hit(targets[i],"神聖之羽：雙坦必須誘導最遠目標");
            foreach(var member in world.Party.ActiveMembers())
                if(member!=targets[i]&&Vector3.Distance(member.Position,plumeTargets[i])<10)Hit(member,"神聖之羽：遠離坦克死刑範圍");
        }
    }
    private void ResolveLine(Vector3 origin,Vector3 target,float halfWidth,string reason)
    {
        var d=Vector3.Normalize(target-origin);
        foreach(var member in world!.Party.ActiveMembers())
        {
            var p=member.Position-origin;
            if(Vector3.Dot(p,d)>=0&&MathF.Abs(p.X*d.Z-p.Z*d.X)<halfWidth)Hit(member,reason);
        }
    }
    private void ApplyVow(int role,float remaining=34.1f)
    {
        state!.VowOwner=role;
        world!.Party.Get(role)?.AddStatus(2896,remaining);
        if(startAt==0)
        {
            var target=world.Party.Get(role)!;
            nidhogg?.Cast(27952,target.Position,0,targetId:target.GameObjectId);
            foreach(var member in world.Party.ActiveMembers())
                if(member!=world.Party.Get(role)&&Vector3.Distance(member.Position,world.Party.Get(role)!.Position)<5)Hit(member,"滅殺的誓言：DPS 點名時須散開");
        }
    }
    private void PassVow(int receiver)
    {
        if(state!.VowOwner<0)return;
        var owner=world!.Party.Get(state.VowOwner)!;
        var recipients=world.Party.ActiveMembers().Where(m=>m!=owner&&Vector3.Distance(m.Position,owner.Position)<5).ToArray();
        if(recipients.Length!=1||recipients[0]!=world.Party.Get(receiver))Hit(owner,"滅殺的誓言：只與指定接毒者重疊");
        if(world.Party.Get(receiver)!.HasStatus(2897))Hit(owner,"滅殺的贖罪尚未結束，不能傳給該玩家");
        Effect(27953,owner.Position,owner.Position,owner);
        owner.RemoveStatus(2896);owner.AddStatus(2897,100);
        state.VowOwner=receiver;world.Party.Get(receiver)?.AddStatus(2896,34.1f);
        state.SetIdle();
    }
    private void PrepareDoubleDive()
    {
        nidhogg?.SetTargetable(true);hraesvelgr?.SetTargetable(true);
        nidhogg?.SetPosition(new Vector3(-10,0,-34));nidhogg?.HoldFacing(0);
        hraesvelgr?.SetPosition(new Vector3(10,0,-34));hraesvelgr?.HoldFacing(0);
        state!.SetDoubleDive();
    }
    private void ResolveDoubleDive()
    {
        for(var r=2;r<8;r++)
        {
            var member=world!.Party.Get(r)!;
            if(MathF.Abs(member.Position.X)<1||state!.Fire[r]!=(member.Position.X>0))Hit(member,"雙龍俯衝：火站白龍側、冰站黑龍側");
        }
        for(var side=0;side<2;side++)
        {
            var first=world!.Party.ActiveMembers().Where(m=>side==0?m.Position.X<0:m.Position.X>0).MinBy(m=>m.Position.Z);
            if(first!=world.Party.Get(side)&&first!=null)Hit(first,"雙龍俯衝：坦克需站在該側最前方");
        }
    }
    private void TransformThermalStatuses()
    {
        state!.ThermalActive=true;
        for(var r=2;r<8;r++)
        {
            var member=world!.Party.Get(r)!;
            member.RemoveStatus(2898);member.RemoveStatus(2899);
            member.AddStatus(state.Fire[r]?(ushort)960:(ushort)3480,30);
        }
        state.Hint="熱病／凍結：停止動作，等雙龍俯衝命中、狀態解除後再往北。";
    }
    private void ClearThermalStatuses()
    {
        state!.ThermalActive=false;
        foreach(var (_,member) in world!.Party.FilledSlots())
        {
            member.RemoveStatus(2898);member.RemoveStatus(2899);
            member.RemoveStatus(960);member.RemoveStatus(3480);
        }
    }
    private void ResolveTouchdown()
    {
        Effect(28903,Vector3.Zero,Vector3.Zero);
        Effect(28903,new(0,0,25),new(0,0,25));
        foreach(var member in world!.Party.ActiveMembers())
            if(member.Position.LengthSquared()<400||Vector3.DistanceSquared(member.Position,new(0,0,25))<400)Hit(member,"雙龍空降：靠北遠離落點");
    }
    private void PrepareWroth()
    {
        state!.Hint="十字火：避開白龍、遠離第二組火球；等命中特效後橫移，再轉彎走 L 型。";
        ClearFireballs();
        for(var r=0;r<8;r++)state.Destinations[r]=Vector3.Zero;
    }
    private void PlayAkhMornHit()
    {
        var target=world!.Party.Get(state!.AkhMornTarget)!;
        nidhogg?.Cast(27975,target.Position,0,targetId:target.GameObjectId);
    }
    private void ResolveAkhMorn(int hit)
    {
        var target=world!.Party.Get(state!.AkhMornTarget)!;var position=target.Position;
        foreach(var member in world.Party.ActiveMembers())
            if(Vector3.Distance(member.Position,position)>6)Hit(member,"死亡輪迴：八人集合分攤");
        akhMornPositions[hit]=position;
        puddleHits.Add((position,state!.Time+1.5f));
        for(var r=0;r<8;r++)state.Destinations[r]=state.Wroth.NextStack(hit);
    }
    private void SpawnAkhMornPuddle(int hit)
    {
        var puddle=world!.SpawnEventObject(new EventObjectSpawnConfig{EObjId=0x1EB683,Placement=new(akhMornPositions[hit],0)});
        if(puddle!=null)puddles.Add(puddle);
    }
    private void ResolveHot(bool wings)
    {
        if(wings)
        {
            Effect(27948,new(-22,0,-14.5f),new(30,0,-14.5f));
            Effect(27948,new(-22,0,14.5f),new(30,0,14.5f));
        }
        else Effect(27950,DsrP6DragonsState.Nidhogg,new(30,0,0));
        foreach(var member in world!.Party.ActiveMembers())
            if(wings?MathF.Abs(member.Position.Z)>=4:MathF.Abs(member.Position.Z)<8)Hit(member,wings?"燃燒之翼：靠近中央橫線":"燃燒之尾：離開中央橫線");
    }
    private void ResolveFlames()
    {
        state!.FlamePositioning=false;
        for(var i=0;i<6;i++)
        {
            var member=world!.Party.Get(state!.FlameOrder[i])!;
            Effect(i<4?29739u:29740u,member.Position,member.Position,member);
            var others=world.Party.ActiveMembers().Where(m=>m!=member&&Vector3.Distance(m.Position,member.Position)<(i<4?5:4)).ToArray();
            if(i<4){foreach(var other in others)Hit(other,"復仇之炎：散開");}
            else if(others.Length!=1||others[0]!=world.Party.Get(state.FlameOrder[i+2]))Hit(member,"同歸於盡之炎：與無標玩家二人分攤");
            member.RemoveStatus(i<4?(ushort)2758:(ushort)2759);
        }
        foreach(var puddle in puddles)puddle.Despawn();puddles.Clear();puddleHits.Clear();
        ClearFlameMarks();
    }
}
