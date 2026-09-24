using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
namespace AnoMech.Scenarios.Dsr.P5Death;
public sealed partial class DsrP5DeathScenario : IScenario
{
    public string Name=>"死刻";
    public IPhase Phase=>DsrZone.P5;
    public IReadOnlyList<IScenarioAi> AiStrats {get;}=[new DsrP5DeathAi()];
    private SimWorld? world;
    private DsrP5DeathState? state;
    private int? validationSeed=null;
    private int doomPreference=0;
    private bool showHints=true,showMap=true,twistersActive,cleansesActive;
    private SimEnemy? boss,hammer,white,dark,spear,charibert,grinnaux;
    private readonly List<SimEnemy> actors=[];
    private readonly List<SimEventObject> objects=[];
    private readonly List<(int First,int Second,SimTether Tether)> chains=[];
    private readonly Vector3[] chargeStarts=new Vector3[3];
    private readonly float[] doomExpires=new float[8];
    private static readonly ushort[] Statuses=[2976,769];
    public void Run(SimWorld simWorld,int? selectedAi)
    {
        world=simWorld;state=new(validationSeed??Random.Shared.Next(),(int)world.Party.PlayerRole,doomPreference);
        actors.Clear();objects.Clear();chains.Clear();Array.Clear(doomExpires);
        twistersActive=cleansesActive=false;
        boss=Spawn(0x3143,3632,Vector3.Zero,true);
        boss?.SetTargetable(true);
        world.Events.Add(3f,()=>boss?.Cast(27538,castSeconds:3.7f,fireDelay:.279f));
        world.Events.Add(9f,()=>{boss?.SetTargetable(false);boss?.Cast(DsrConstants.Action.Teleport,castSeconds:0,targetId:boss.GameObjectId);});
        world.Events.Add(9.6f,()=>boss?.SetVisible(false));
        world.Events.Add(10.1f,()=>
        {
            boss?.SetPosition(state.Boss);
            boss?.HoldFacing(MathF.Atan2(-state.Boss.X,-state.Boss.Z));
            boss?.SetVisible(true);
            boss?.Cast(DsrConstants.Action.Reappear,castSeconds:0,targetId:boss.GameObjectId);
        });
        world.Events.Add(11.6f,SpawnFormation);
        world.Events.Add(13.956f,()=>dark?.Cast(27540,castSeconds:0));
        world.Events.Add(14.760f,()=>AssignDoom(0,14.760f));
        world.Events.Add(14.895f,()=>AssignDoom(1,14.895f));
        world.Events.Add(15.029f,()=>AssignDoom(2,15.029f));
        world.Events.Add(15.162f,()=>AssignDoom(3,15.162f));
        world.Events.Add(16.190f,()=>hammer?.Cast(25558,castSeconds:5.7f,fireDelay:.289f));
        world.Events.Add(18.068f,StartCharges);
        world.Events.Add(22.179f,()=>ResolveRing(0));
        world.Events.Add(24.056f,ResolveCharges);
        world.Events.Add(24.100f,()=>ResolveRing(1));
        world.Events.Add(24.145f,SnapshotCleanses);
        world.Events.Add(24.234f,ResolveSpreads);
        world.Events.Add(24.4f,()=>grinnaux=Spawn(DsrConstants.Npc.Grinnaux,3639,Vector3.Zero,true));
        world.Events.Add(25.250f,ShowTwisters);
        world.Events.Add(25.978f,()=>ResolveRing(2));
        world.Events.Add(27.350f,()=>twistersActive=false);
        world.Events.Add(27.860f,()=>ResolveRing(3));
        world.Events.Add(29.738f,()=>ResolveRing(4));
        world.Events.Add(30.2f,()=>{white?.Despawn();dark?.Despawn();spear?.Despawn();hammer?.Despawn();});
        world.Events.Add(31f,()=>charibert=Spawn(DsrConstants.Npc.Charibert,3642,state.Rotate(new(0,0,24)),true));
        world.Events.Add(32.139f,AssignSymbols);
        world.Events.Add(32.239f,()=>
        {
            BeginKnightCast(charibert,25310,6.7f,.275f);
        });
        world.Events.Add(33.357f,()=>BeginKnightCast(grinnaux,DsrConstants.Action.Knockback,3.7f,.280f));
        world.Events.Add(34.117f,ShowCleanses);
        world.Events.Add(36.175f,AttachChains);
        world.Events.Add(37.381f,ResolveGazes);
        world.Events.Add(37.962f,Knockback);
        world.Events.Add(39.840f,ResolveFlames);
        world.Events.Add(40.5f,CheckChains);
        world.Events.Add(41.3f,CheckDooms);
        world.Events.Add(42.24f,SpawnMeteors);
        DsrP5DeathAi.Tick(state,world);
    }
    private static void BeginKnightCast(SimEnemy? knight,uint action,float castSeconds,float fireDelay)
    {
        if(knight==null)return;
        knight.AddVfx("vfx/common/eff/mon_eisyo03t.avfx",castSeconds+fireDelay);
        knight.Cast(action,castSeconds:castSeconds,targetId:knight.GameObjectId,fireDelay:fireDelay);
    }
    private SimEnemy? Spawn(uint npc,uint name,Vector3 position,bool visible)
    {
        var actor=world!.SpawnEnemy(new EnemySpawnConfig(npc,NameId:name,Level:90,EnemyList:EnemyListMode.ScenarioVisible,IsVisible:false,Placement:new(position,0),DisableLookAt:true,WeaponDrawn:npc is 0x3143 or 0x315D or 0x3130 or 0x313B or 0x313A));
        if(actor!=null)
        {
            actors.Add(actor);
            if(visible){actor.QueueEntrance(DsrConstants.Timeline.KnightEntrance,.65f);actor.SetVisible(true);}
        }
        return actor;
    }
    private void SpawnFormation()
    {
        hammer=Spawn(0x315D,3641,state!.Hammer,true);
        chargeStarts[0]=state.Rotate(new(0,0,-24));
        chargeStarts[1]=state.Rotate(DsrP5DeathState.Polar(60,24));
        chargeStarts[2]=state.Rotate(DsrP5DeathState.Polar(-60,24));
        dark=Spawn(0x3156,3983,chargeStarts[0],true);
        white=Spawn(0x3166,11314,chargeStarts[1],true);
        spear=Spawn(DsrConstants.Npc.Zephirin,3633,chargeStarts[2],true);
        world!.Map.AddEffect(0x00020001,(byte)state.EyeIndex,resetFlags:0x00080004);
        state.FormationVisible=true;
    }
    private void AssignDoom(int index,float time)
    {
        var role=state!.Dooms[index];state.Assigned=true;
        world!.Party.Get(role)?.AddStatus(2976,26,playEffects:false);
        var target=world.Party.Get(role);
        if(target!=null)
            Spawn(0x3156,3983,chargeStarts[0],false)?.Cast(27540,target.Position,0,target.GameObjectId);
        doomExpires[role]=time+26;
    }
    private void StartCharges()
    {
        dark?.Cast(27533,-chargeStarts[0],5.7f,fireDelay:.288f);
        white?.Cast(27531,-chargeStarts[1],5.7f,fireDelay:.288f);
        spear?.Cast(27539,-chargeStarts[2],5.7f,fireDelay:.288f);
        boss?.Cast(25548,castSeconds:5.4f,fireDelay:.275f);
    }
    private void ResolveRing(int ring)
    {
        if(ring>0)hammer?.Cast((uint)(25558+ring),state!.Hammer,0);
        foreach(var member in world!.Party.ActiveMembers())
        {
            var distance=Vector3.Distance(member.Position,state!.Hammer);
            if(distance>=ring*6&&distance<(ring+1)*6)Hit(member,"沉重衝擊：等待圓環判定後再進入");
        }
    }
    private void ResolveCharges()
    {
        for(var role=0;role<8;role++)state!.SpreadSnapshots[role]=world!.Party.Get(role)?.Position??Vector3.Zero;
        foreach(var member in world!.Party.ActiveMembers())
            for(var i=0;i<3;i++)
            {
                var dir=Vector3.Normalize(chargeStarts[i]);
                if(MathF.Abs(member.Position.X*dir.Z-member.Position.Z*dir.X)<(i==0?10:5))Hit(member,"死刻衝鋒：前往八方預站位，避開三條直線");
            }
    }
    private void SnapshotCleanses()
    {
        for(var i=0;i<4;i++)
        {
            var position=world!.Party.Get(state!.Clean[i])?.Position??Vector3.Zero;
            state.CleansePositions[i]=position;
            Spawn(0x3157,3984,position,false)?.Cast(27542,position,9.7f,fireDelay:.272f);
        }
    }
    private void ResolveSpreads()
    {
        ResolvePlayerCircles(25549,5,"百雷重疊：八方散開");
        state!.SpreadsResolved=true;
    }
    private void ResolvePlayerCircles(uint action,float radius,string failure)
    {
        var positions=Enumerable.Range(0,8).Select(r=>world!.Party.Get(r)?.Position??Vector3.Zero).ToArray();
        for(var role=0;role<8;role++)
        {
            var target=world!.Party.Get(role);if(target==null)continue;
            Spawn(DsrConstants.Npc.Helper,3632,positions[role],false)?.Cast(action,positions[role],0,target.GameObjectId);
            for(var other=0;other<8;other++)
                if(other!=role&&Vector3.Distance(positions[role],positions[other])<radius)Hit(target,failure);
        }
    }
    private void ShowTwisters()
    {
        twistersActive=true;
        foreach(var p in state!.SpreadSnapshots)AddObject(0x1E8910,p,2.1f);
    }
    private void AddObject(uint id,Vector3 p,float lifetime)
    {
        var obj=world!.SpawnEventObject(new EventObjectSpawnConfig{EObjId=id,Placement=new(p,0),Lifetime=lifetime});
        if(obj!=null)objects.Add(obj);
    }
    private void AssignSymbols()
    {
        var s=state!;
        var pairs=from a in s.Dooms from b in s.Dooms where a<b select (a,b);
        var pair=pairs.MaxBy(p=>Vector3.DistanceSquared(world!.Party.Get(p.a)!.Position,world.Party.Get(p.b)!.Position));
        float X(int r){var p=world!.Party.Get(r)!.Position;return p.X*MathF.Cos(s.Rotation)+p.Z*MathF.Sin(s.Rotation);}
        var circles=new[]{pair.a,pair.b}.OrderBy(X).ToArray();
        for(var i=0;i<2;i++){s.Symbols[circles[i]]=281;s.FinalDirections[circles[i]]=s.Rotate(new(i==0?-1:1,0,0));}
        var inner=s.Dooms.Except(circles).OrderBy(X).ToArray();
        var marks=new uint[]{282,283};s.Random.Shuffle(marks);
        for(var i=0;i<2;i++){s.Symbols[inner[i]]=marks[i];s.FinalDirections[inner[i]]=s.Rotate(Vector3.Normalize(new Vector3(i==0?-12:12,0,15)));}
        var cleanMarks=new uint[]{282,283,284,284};s.Random.Shuffle(cleanMarks);
        for(var i=0;i<4;i++)s.Symbols[s.Clean[i]]=cleanMarks[i];
        var crosses=s.Clean.Where(r=>s.Symbols[r]==284).OrderBy(X).ToArray();
        for(var i=0;i<2;i++)s.FinalDirections[crosses[i]]=s.Rotate(new(0,0,i==0?-1:1));
        foreach(var role in s.Clean.Where(r=>s.Symbols[r]!=284))s.FinalDirections[role]=-s.FinalDirections[inner.First(r=>s.Symbols[r]==s.Symbols[role])];
        for(var role=0;role<8;role++)world!.Party.Get(role)?.AttachLockonVfx(s.Symbols[role],7.701f);
        s.SymbolsAssigned=true;
    }
    private void ShowCleanses()
    {
        cleansesActive=true;
        foreach(var p in state!.CleansePositions)AddObject(0x1EB685,p,7.9f);
    }
    private void AttachChains()
    {
        for(uint icon=281;icon<=284;icon++)
        {
            var roles=Enumerable.Range(0,8).Where(r=>state!.Symbols[r]==icon).ToArray();
            var a=world!.Party.Get(roles[0]);var b=world.Party.Get(roles[1]);
            a?.AddStatus(769,15);b?.AddStatus(769,15);
            chains.Add((roles[0],roles[1],world.Tether(a,b,9)));
        }
    }
    private void ResolveGazes()
    {
        foreach(var member in world!.Party.ActiveMembers())
        {
            var facing=new Vector3(MathF.Sin(member.Rotation),0,MathF.Cos(member.Rotation));
            if(Vector3.Dot(facing,Vector3.Normalize(state!.Eye-member.Position))>.7071f||Vector3.Dot(facing,Vector3.Normalize(state.Boss-member.Position))>.7071f)Hit(member,"死刻雙視線：背對托爾丹與龍眼");
        }
        Spawn(DsrConstants.Npc.Helper,3632,state!.Eye,false)?.Cast(25554,state.Eye,0);
        world!.Map.AddEffect(0x00080004,(byte)state.EyeIndex);
    }
    private void Knockback()
    {
        state!.Knocked=true;
        Array.Clear(state.Destinations);
        foreach(var member in world!.Party.ActiveMembers())
        {
            if(member.Position.Length()<.1f||member.Position.Length()+16>21)Hit(member,"死刻擊退：靠近中心，避免被擊退至場外");
            else (member as ISimPartyMember)?.Knockback(Vector3.Zero,16);
        }
    }
    private void ResolveFlames()=>ResolvePlayerCircles(25311,10,"天火重疊：按 PS 標記分散擊退");
    private void CheckChains()
    {
        foreach(var chain in chains)
        {
            Hit(world!.Party.Get(chain.First)!,"烈焰鏈未拉斷：向場邊拉開相同標記");
            Hit(world.Party.Get(chain.Second)!,"烈焰鏈未拉斷：向場邊拉開相同標記");
            chain.Tether.Despawn();
        }
        chains.Clear();
    }
    private void CheckDooms()
    {
        foreach(var role in state!.Dooms)if(!state.Cleansed[role])Hit(world!.Party.Get(role)!,"死亡宣告未解除：擊退後進入白圈");
    }
    private void Complete()
    {
        state!.Complete=true;
        foreach(var actor in actors)actor.Despawn();
        foreach(var obj in objects)obj.Despawn();
        foreach(var chain in chains)chain.Tether.Despawn();chains.Clear();
        foreach(var member in world!.Party.ActiveMembers())foreach(var id in Statuses)member.RemoveStatus(id);
        world.Map.AddEffect(0x00080004,(byte)state.EyeIndex);
    }
    private void Hit(SimCharacter member,string reason){if(!member.IsAlive())return;state!.Failed=true;member.Die(reason);}
    public void Tick(float delta,float elapsed)
    {
        if(state==null||world==null||state.Complete)return;
        state.Time=elapsed;
        if(twistersActive)foreach(var member in world.Party.ActiveMembers())
            if(state.SpreadSnapshots.Any(p=>Vector3.DistanceSquared(p,member.Position)<4))Hit(member,"踩中死刻旋風：散開判定後離開原位");
        for(var i=chains.Count-1;i>=0;i--)
        {
            var chain=chains[i];var a=world.Party.Get(chain.First)!;var b=world.Party.Get(chain.Second)!;
            if(Vector3.Distance(a.Position,b.Position)<32)continue;
            chain.Tether.Despawn();a.RemoveStatus(769);b.RemoveStatus(769);chains.RemoveAt(i);
        }
        foreach(var role in state.Dooms)
        {
            if(doomExpires[role]==0||state.Cleansed[role])continue;
            var member=world.Party.Get(role)!;
            if(cleansesActive&&state.CleansePositions.Any(p=>Vector3.DistanceSquared(p,member.Position)<=DsrP5DeathState.CleanseRadius*DsrP5DeathState.CleanseRadius))
            {state.Cleansed[role]=true;member.RemoveStatus(2976);}
            else if(elapsed>=doomExpires[role])Hit(member,"死亡宣告到期：未進入白圈");
        }
        TickLimitBreak(delta);
        DsrP5DeathAi.Tick(state,world);
    }
}
