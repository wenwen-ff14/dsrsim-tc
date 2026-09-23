using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public sealed partial class DsrP6DragonsScenario(DsrP6Section section=DsrP6Section.Full) : IScenario
{
    public string Name=>"機制流程";
    private DsrP6Section firstSection=section==DsrP6Section.Full?DsrP6Section.Breath1:section;
    private DsrP6Section lastSection=section==DsrP6Section.Full?DsrP6Section.Breath2:section;
    private static string SectionName(DsrP6Section value)=>value switch
    {
        DsrP6Section.Breath1=>"冰火 1（含分攤）",
        DsrP6Section.Wings1=>"第一次坦死刑",
        DsrP6Section.Wroth=>"十字火（含分攤）",
        DsrP6Section.Wings2=>"第二次坦死刑",
        DsrP6Section.Breath2=>"冰火 2（固定式、雙龍俯衝）",
        _=>"全流程",
    };
    public IPhase Phase=>DsrZone.P6;
    public IReadOnlyList<IScenarioAi> AiStrats {get;}=[new DsrP6DragonsAi()];
    private SimWorld? world;
    private DsrP6DragonsState? state;
    private SimEnemy? nidhogg,hraesvelgr;
    private readonly List<SimEnemy> actors=[];
    private readonly List<SimTether> tethers=[];
    private readonly List<SimEventObject> puddles=[];
    private readonly List<(Vector3 Position,float ActiveAt)> puddleHits=[];
    private readonly Vector3[] breathTargets=new Vector3[8];
    private readonly Vector3[] akhMornPositions=new Vector3[4];
    private readonly Vector3[] plumeTargets=new Vector3[2];
    private readonly int[] plumeRoles=new int[2];
    private float startAt,endAt;
    private static readonly ushort[] StatusIds=[2896,2897,2898,2899,960,3480,2758,2759];
    private int? validationSeed=null;
    public void SetRange(DsrP6Section first,DsrP6Section last)
    {
        if(first<DsrP6Section.Breath1||last>DsrP6Section.Breath2||last<first)
            throw new ArgumentOutOfRangeException(nameof(first));
        firstSection=first;lastSection=last;
    }

    public void Run(SimWorld simWorld,int? selectedAi)
    {
        world=simWorld;state=new(validationSeed??Random.Shared.Next());
        actors.Clear();tethers.Clear();puddles.Clear();puddleHits.Clear();
        var runFirst=firstSection;var runLast=lastSection;
        startAt=runFirst switch{DsrP6Section.Wings1=>35,DsrP6Section.Wroth=>58,DsrP6Section.Wings2=>105,DsrP6Section.Breath2=>126,_=>0};
        endAt=runLast switch{DsrP6Section.Breath1=>35,DsrP6Section.Wings1=>57,DsrP6Section.Wroth=>104,DsrP6Section.Wings2=>125,_=>162};
        nidhogg=Spawn(0x3144,3458,DsrP6DragonsState.Nidhogg,true);
        hraesvelgr=Spawn(0x3145,4954,DsrP6DragonsState.Hraesvelgr,true);
        nidhogg?.HoldFacing(MathF.PI/2);hraesvelgr?.HoldFacing(-MathF.PI/2);
        nidhogg?.SetTargetable(true);hraesvelgr?.SetTargetable(true);
        if(startAt>0)
        {
            ApplyVow(runFirst switch{DsrP6Section.Wroth=>0,DsrP6Section.Wings2=>1,DsrP6Section.Breath2=>state.FirstVow==4?5:4,_=>state.FirstVow},
                runFirst switch{DsrP6Section.Wings1=>21.109f,DsrP6Section.Wings2=>19.264f,DsrP6Section.Breath2=>32.334f,_=>32.192f});
            if(startAt>90)
            {
                world.Party.Get(state.FirstVow)?.AddStatus(2897,156.109f-startAt);
                world.Party.Get(0)?.AddStatus(2897,190.192f-startAt);
            }
            if(startAt>124)world.Party.Get(1)?.AddStatus(2897,224.264f-startAt);
        }
        world.Events.Add(0f,()=>state.SetBreath(false));
        world.Events.Add(7.770f,()=>BeginBreath(false));
        world.Events.Add(14.747f,ResolveBlizzard);
        world.Events.Add(14.836f,()=>ResolveBreath(false));
        world.Events.Add(16f,()=>state.SetVowSpread());
        world.Events.Add(22.031f,()=>ApplyVow(state.FirstVow));
        world.Events.Add(22.6f,()=>state.SetStacks());
        world.Events.Add(25.207f,BeginStacks);
        world.Events.Add(33.393f,ResolveStacks);
        world.Events.Add(37f,()=>PrepareWings(false));
        world.Events.Add(39.023f,()=>hraesvelgr?.Cast(27940,castSeconds:7.2f,fireDelay:.270f));
        world.Events.Add(42.600f,()=>nidhogg?.Cast(27966,new Vector3(11,0,40),4.7f,fireDelay:.267f));
        world.Events.Add(47.567f,()=>ResolveLine(new(11,0,-34),new(11,0,40),11,"邪炎俯衝"));
        world.Events.Add(47.656f,()=>ResolveWings(false));
        world.Events.Add(50f,()=>{nidhogg?.QueueEntrance(DsrConstants.Timeline.KnightEntrance,.65f);RestoreDragons();state.SetVowPass(0);});
        world.Events.Add(56.109f,()=>PassVow(0));
        world.Events.Add(58f,PrepareWroth);
        world.Events.Add(59.148f,()=>nidhogg?.Cast(27973,castSeconds:2.2f,fireDelay:.259f));
        world.Events.Add(63.127f,ApplyFlames);
        world.Events.Add(63.18f,DepartWrothHraesvelgr);
        world.Events.Add(63.85f,()=>hraesvelgr?.SetVisible(false));
        world.Events.Add(64f,ShowWrothDive);
        world.Events.Add(64.468f,BeginAkhMorn);
        world.Events.Add(65f,()=>SpawnFireballs(0));
        world.Events.Add(67f,()=>SpawnFireballs(1));
        world.Events.Add(67.512f,()=>hraesvelgr?.Cast(27967,new Vector3(11,0,40),4.7f,fireDelay:.267f));
        world.Events.Add(70f,()=>SpawnFireballs(2));
        world.Events.Add(70.754f,()=>BeginFireballs(0));
        world.Events.Add(72.435f,()=>ResolveAkhMorn(0));
        world.Events.Add(72.479f,()=>ResolveLine(new(11,0,-34),new(11,0,40),11,"聖力俯衝"));
        world.Events.Add(72.720f,()=>BeginFireballs(1));
        world.Events.Add(73.5f,ReturnWrothHraesvelgr);
        world.Events.Add(73.735f,()=>SpawnAkhMornPuddle(0));
        world.Events.Add(74.090f,()=>ResolveAkhMorn(1));
        world.Events.Add(75.390f,()=>SpawnAkhMornPuddle(1));
        world.Events.Add(75.654f,()=>ResolveAkhMorn(2));
        world.Events.Add(75.754f,()=>ResolveFireballs(0));
        world.Events.Add(75.774f,()=>BeginFireballs(2));
        world.Events.Add(76.8f,()=>HideFireballs(0));
        world.Events.Add(76.954f,()=>SpawnAkhMornPuddle(2));
        world.Events.Add(77.220f,()=>ResolveAkhMorn(3));
        world.Events.Add(77.720f,()=>ResolveFireballs(1));
        world.Events.Add(78.520f,()=>SpawnAkhMornPuddle(3));
        world.Events.Add(78.8f,()=>HideFireballs(1));
        world.Events.Add(78.874f,()=>{RestoreDragons();nidhogg?.Cast(27949,castSeconds:5.2f,fireDelay:.261f);});
        world.Events.Add(80.774f,()=>ResolveFireballs(2));
        world.Events.Add(80.85f,()=>state.SetFlames());
        world.Events.Add(81.8f,()=>HideFireballs(2));
        world.Events.Add(82f,ClearFireballs);
        world.Events.Add(85.365f,()=>ResolveHot(false));
        world.Events.Add(86.212f,ResolveFlames);
        world.Events.Add(86.4f,()=>state.SetVowPass(1,true));
        world.Events.Add(90.192f,()=>PassVow(1));
        world.Events.Add(90.4f,()=>state.SetStacks());
        world.Events.Add(94.532f,BeginStacks);
        world.Events.Add(102.714f,ResolveStacks);
        world.Events.Add(105f,()=>PrepareWings(true));
        world.Events.Add(107.228f,()=>hraesvelgr?.Cast(27943,castSeconds:7.2f,fireDelay:.269f));
        world.Events.Add(109.239f,()=>nidhogg?.Cast(27947,castSeconds:5.2f,fireDelay:.258f));
        world.Events.Add(115.501f,()=>ResolveWings(true));
        world.Events.Add(115.725f,()=>ResolveHot(true));
        world.Events.Add(116f,()=>state.SetVowPass(state.FirstVow==4?5:4));
        world.Events.Add(124.264f,()=>PassVow(state.FirstVow==4?5:4));
        world.Events.Add(126f,()=>{RestoreDragons();state.SetBreath(true);});
        world.Events.Add(128.289f,()=>BeginBreath(true));
        world.Events.Add(135.268f,ResolveBlizzard);
        world.Events.Add(135.358f,()=>ResolveBreath(true));
        world.Events.Add(137f,PrepareDoubleDive);
        world.Events.Add(142.914f,()=>{nidhogg?.Cast(27966,new Vector3(-10,0,40),4.7f,fireDelay:.263f);hraesvelgr?.Cast(27967,new Vector3(10,0,40),4.7f,fireDelay:.263f);});
        world.Events.Add(146.314f,TransformThermalStatuses);
        world.Events.Add(147.877f,ResolveDoubleDive);
        world.Events.Add(148.813f,ClearThermalStatuses);
        world.Events.Add(149f,()=>{for(var r=0;r<8;r++)state.Destinations[r]=new(0,0,-20.5f);});
        world.Events.Add(154.044f,()=>{nidhogg?.SetTargetable(true);hraesvelgr?.SetTargetable(true);nidhogg?.SetPosition(Vector3.Zero);hraesvelgr?.SetPosition(new Vector3(0,0,25));nidhogg?.Cast(27968,castSeconds:0);hraesvelgr?.Cast(27968,castSeconds:0);});
        world.Events.Add(154.938f,ResolveTouchdown);
        world.Events.Add(155f,()=>{state.SetVowPass(6);if(state.VowOwner>=0)state.Destinations[state.VowOwner]=new(0,0,-20.5f);state.Destinations[6]=new(0,0,-20.5f);});
        world.Events.Add(158.334f,()=>PassVow(6));
        world.Events.Add(162f,Complete);
        if(runLast==DsrP6Section.Breath1)world.Events.Add(35f,Complete);
        if(runLast==DsrP6Section.Wings1)world.Events.Add(57f,Complete);
        if(runLast==DsrP6Section.Wroth)world.Events.Add(104f,Complete);
        if(runLast==DsrP6Section.Wings2)world.Events.Add(125f,Complete);
        world.Events.SelectWindow(startAt,endAt);
        DsrP6DragonsAi.Tick(state,world);
    }
    private SimEnemy? Spawn(uint npc,uint name,Vector3 position,bool visible,bool enemyList=true)
    {
        var enemy=world!.SpawnEnemy(new EnemySpawnConfig(npc,NameId:name,Level:90,EnemyList:visible&&enemyList?EnemyListMode.ScenarioVisible:EnemyListMode.Never,IsVisible:visible,Placement:new(position,0),DisableLookAt:true));
        if(enemy!=null)actors.Add(enemy);
        return enemy;
    }
    private void Effect(uint action,Vector3 origin,Vector3 target,SimCharacter? recipient=null,float rotation=0)
    {
        var helper=Spawn(DsrConstants.Npc.Helper,3458,origin,false);
        helper?.SetRotation(rotation);
        helper?.Cast(action,target,0,targetId:recipient?.GameObjectId);
    }
    private void RestoreDragons()
    {
        nidhogg?.SetPosition(DsrP6DragonsState.Nidhogg);nidhogg?.HoldFacing(MathF.PI/2);nidhogg?.SetVisible(true);
        hraesvelgr?.SetPosition(DsrP6DragonsState.Hraesvelgr);hraesvelgr?.HoldFacing(-MathF.PI/2);hraesvelgr?.SetVisible(true);
        nidhogg?.SetTargetable(true);hraesvelgr?.SetTargetable(true);
    }
    private void Hit(SimCharacter member,string reason){if(member.IsAlive()){state!.Failed=true;member.Die(reason);}}
    private void Complete()
    {
        state!.Complete=true;
        ClearFireballs();
        foreach(var tether in tethers)tether.Despawn();
        foreach(var actor in actors)actor.Despawn();
        foreach(var puddle in puddles)puddle.Despawn();
        foreach(var (_,member) in world!.Party.FilledSlots())foreach(var id in StatusIds)member.RemoveStatus(id);
    }
    public void Tick(float delta,float elapsed)
    {
        if(state==null||world==null||state.Complete)return;
        state.Time=elapsed+startAt;
        if(state.ThermalActive)
            for(var r=2;r<8;r++)
                if(state.Fire[r]&&world.Party.Get(r) is SimPlayer { IsActing:true } player)Hit(player,"熱病：俯衝命中解除狀態前停止移動、技能與自動攻擊");
        foreach(var p in puddleHits)
            if(state.Time>=p.ActiveAt)
                foreach(var member in world.Party.ActiveMembers())
                    if(Vector3.DistanceSquared(member.Position,p.Position)<36)Hit(member,"停留於死亡輪迴火圈");
        DsrP6DragonsAi.Tick(state,world);
    }
}
