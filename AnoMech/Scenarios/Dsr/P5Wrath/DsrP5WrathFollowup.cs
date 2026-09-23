using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P5Wrath;

public sealed partial class DsrP5WrathScenario
{
    private readonly List<SimEnemy> mercyHelpers = [];
    private readonly List<SimEventObject> liquidObjects = [];
    private readonly Queue<Vector3> liquidImpacts = [];
    private readonly float[] liquidContact = new float[8];
    private float liquidContactGrace = 1f;
    private readonly List<(Vector3 Position,float At,bool Liquid)> groundHazards = [];

    private void ApplyThunder()
    {
        foreach(var role in state!.Thunder)
            if(world!.Party.Get(role) is {} target)
            {
                target.AddStatus(466,14.794f);
                target.AddVfx("vfx/common/eff/dk05th_stdn0t.avfx",2f,false);
            }
    }

    private void LockMercy()
    {
        state!.MercyLocked = true;
        mercyHelpers.Clear();
        for(var role=0;role<8;role++)
        {
            var target=world!.Party.Get(role);
            state.MercyTargets[role]=target?.Position??Vector3.Zero;
            var helper=Spawn(DsrP5WrathConstants.Thordan,3632,Vector3.Zero,false);
            if(helper!=null) mercyHelpers.Add(helper);
        }
    }

    private void ResolveMercy()
    {
        for(var role=0;role<8;role++)
        {
            var p=state!.MercyTargets[role];
            if(role<mercyHelpers.Count) mercyHelpers[role].Cast(25547,p,0);
            foreach(var member in world!.Party.ActiveMembers())
            {
                var dot=Vector3.Dot(Vector3.Normalize(p),Vector3.Normalize(member.Position));
                if(member!=world.Party.Get(role) && dot>MathF.Cos(MathF.PI/12)) Hit(member,"八方劍重疊：與其他玩家保持兩格以上間隔");
            }
        }
        state!.MercyResolved=true;
        charibert?.Cast(25572,castSeconds:3.2f,fireDelay:.291f);
    }

    private void LockDives()
    {
        state!.DiveLocked=true;
        state.DiveTarget=world!.Party.Get(state.Green)?.Position??Vector3.Zero;
        darkDragon?.Cast(27533,state.DiveTarget,5.7f,fireDelay:.296f);
        vidofnir?.Cast(27534,state.DiveTarget,5.7f,fireDelay:.296f);
    }

    private void ResolveDives()
    {
        foreach(var member in world!.Party.ActiveMembers())
            foreach(var dragon in new[]{darkDragon,vidofnir})
                if(dragon!=null && InCharge(member.Position,dragon.Position,state!.DiveTarget,10))
                    Hit(member,"被龍衝波及：綠標到持杖騎士背後引導，鎖定後前往持斧騎士");
    }

    private void DropLiquid()
    {
        var target=world!.Party.Get(state!.Liquid);
        if(target==null) return;
        whiteDragon?.Cast(27537,target.Position,0,target.GameObjectId);
        liquidImpacts.Enqueue(target.Position);
    }

    private void ResolveLiquid(float impactTime)
    {
        if(!liquidImpacts.TryDequeue(out var position)) return;
        world!.Party.Get(state!.Liquid)?.AddStatus(DsrP5WrathConstants.FireResistanceDown,3f);
        var obj=world.SpawnEventObject(new EventObjectSpawnConfig { EObjId=0x1EB684,Placement=new(position,0),Lifetime=8 });
        if(obj!=null) liquidObjects.Add(obj);
        groundHazards.Add((position,impactTime+1.1f,true));
    }

    private void DropAltar()
    {
        var target=world!.Party.Get(state!.Altar);
        if(target==null) return;
        var helper=Spawn(DsrConstants.Npc.Charibert,3642,target.Position,false);
        helper?.Cast(25573,target.Position,3.7f,fireDelay:.283f);
        if(helper!=null) mercyHelpers.Add(helper);
        groundHazards.Add((target.Position,state.Time+3.983f,false));
    }

    private void CheckGround(float delta)
    {
        Span<bool> touchingLiquid = stackalloc bool[8];
        touchingLiquid.Clear();
        for(var i=groundHazards.Count-1;i>=0;i--)
        {
            var hazard=groundHazards[i];
            if(hazard.Liquid && state!.Time>=hazard.At+6.9f)
            {
                groundHazards.RemoveAt(i);
                continue;
            }
            if(state!.Time<hazard.At) continue;
            for(var role=0;role<8;role++)
                if(world!.Party.Get(role) is {} member && member.IsAlive() && Vector3.Distance(member.Position,hazard.Position)<(hazard.Liquid?6:8))
                {
                    if(hazard.Liquid) touchingLiquid[role]=true;
                    else Hit(member,"未躲開聖壇火光");
                }
            if(!hazard.Liquid) groundHazards.RemoveAt(i);
        }
        for(var role=0;role<8;role++)
        {
            liquidContact[role]=touchingLiquid[role]?liquidContact[role]+delta:0;
            if(liquidContact[role]>=liquidContactGrace && world!.Party.Get(role) is {} member && member.IsAlive())
                Hit(member,"停留於蒼天火液：連續移動引導五次地火");
        }
    }

    private void ResolveFinale()
    {
        foreach(var member in world!.Party.ActiveMembers())
            if(Vector3.Distance(member.Position,state!.Grinnaux)>6) Hit(member,"未進入持斧騎士的月環安全區");
        foreach(var role in state!.Thunder)
        {
            var target=world.Party.Get(role);
            if(target==null) continue;
            target.RemoveStatus(466);
            var helper=Spawn(DsrP5WrathConstants.Thordan,3632,target.Position,false);
            helper?.Cast(27536,target.Position,0,target.GameObjectId);
            if(helper!=null) mercyHelpers.Add(helper);
            foreach(var other in world.Party.ActiveMembers())
                if(other!=target && Vector3.Distance(other.Position,target.Position)<5) Hit(other,"被雷光鏈波及：雷點名站月環安全區外側");
        }
        state.FinaleResolved=true;
        darkDragon?.Despawn();
        vidofnir?.Despawn();
    }
}
