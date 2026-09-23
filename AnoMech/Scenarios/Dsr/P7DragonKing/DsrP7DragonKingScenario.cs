using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

public sealed partial class DsrP7DragonKingScenario : IScenario
{
    public string Name=>"全流程／換坦練習";
    public IPhase Phase=>DsrZone.P7;
    public IReadOnlyList<IScenarioAi> AiStrats {get;}=[new DsrP7DragonKingAi()];
    private SimWorld? world;
    private DsrP7DragonKingState? state;
    private SimEnemy? boss,sword;
    private readonly SimEnemy?[] exaflares=new SimEnemy?[9],towers=new SimEnemy?[3],gigaflares=new SimEnemy?[3],trinity=new SimEnemy?[3];
    private readonly List<SimEnemy> actors=[];
    private readonly int[] trinityTargets=new int[3];
    private readonly Vector3[] trinityPositions=new Vector3[3];
    private int? validationSeed=null;
    private bool manualTanks=true;
    private int swordChoice;
    private static readonly ushort[] StatusIds=[3135,3136,2940];

    public void Run(SimWorld simWorld,int? selectedAi)
    {
        world=simWorld;state=new(validationSeed??Random.Shared.Next()){ManualTanks=manualTanks,SwordChoice=swordChoice};
        actors.Clear();
        boss=Spawn(0x3148,true);
        boss?.SetTargetable(true);boss?.HoldFacing(MathF.PI);
        sword=Spawn(DsrConstants.Npc.Helper);
        for(var i=0;i<exaflares.Length;i++)exaflares[i]=Spawn(DsrConstants.Npc.Helper);
        for(var i=0;i<3;i++){towers[i]=Spawn(DsrConstants.Npc.Helper);gigaflares[i]=Spawn(DsrConstants.Npc.Helper);trinity[i]=Spawn(DsrConstants.Npc.Helper);}
        world.Events.Add(4.194f,()=>BeginExaflares(0.280f));
        world.Events.Add(11.074f,()=>ResolveExaflares(0));
        world.Events.Add(11.342f,ResolveSword);
        world.Events.Add(11.392f,()=>state.SetAll(state.Relative(new(0,0,8))));
        world.Events.Add(12.995f,()=>ResolveExaflares(1));
        world.Events.Add(14.870f,()=>ResolveExaflares(2));
        world.Events.Add(14.880f,()=>PrepareTrinity(4));
        world.Events.Add(16.750f,()=>ResolveExaflares(3));
        world.Events.Add(18.358f,PlayTrinity);
        world.Events.Add(18.671f,()=>ResolveExaflares(4));
        world.Events.Add(19.431f,SnapshotTrinity);
        world.Events.Add(19.744f,()=>ResolveTrinity(false));
        world.Events.Add(20.548f,()=>ResolveExaflares(5));
        world.Events.Add(22.382f,PlayTrinity);
        world.Events.Add(23.455f,SnapshotTrinity);
        world.Events.Add(23.768f,()=>ResolveTrinity(true));
        world.Events.Add(25.424f,()=>BeginTowers(0.264f));
        world.Events.Add(32.088f,()=>ResolveTowers(0));
        world.Events.Add(32.221f,ResolveSword);
        world.Events.Add(32.271f,()=>state.SetTowerPositions(true));
        world.Events.Add(33.517f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(34.322f,()=>ResolveTowers(1));
        world.Events.Add(34.588f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(35.393f,()=>ResolveTowers(2));
        world.Events.Add(35.660f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(36.465f,()=>ResolveTowers(3));
        world.Events.Add(36.732f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(37.537f,()=>ResolveTowers(4));
        world.Events.Add(37.837f,()=>PrepareTrinity(6));
        world.Events.Add(42.857f,PlayTrinity);
        world.Events.Add(43.930f,SnapshotTrinity);
        world.Events.Add(44.243f,()=>ResolveTrinity(false));
        world.Events.Add(46.879f,PlayTrinity);
        world.Events.Add(47.952f,SnapshotTrinity);
        world.Events.Add(48.265f,()=>ResolveTrinity(true));
        world.Events.Add(49.918f,()=>BeginGigaflares(0.282f,0.262f,0.288f));
        world.Events.Add(58.900f,()=>ResolveGigaflare(0));
        world.Events.Add(59.034f,ResolveSword);
        world.Events.Add(59.084f,()=>MoveAfterGigaflare(0));
        world.Events.Add(62.880f,()=>ResolveGigaflare(1));
        world.Events.Add(62.930f,()=>MoveAfterGigaflare(1));
        world.Events.Add(66.906f,()=>ResolveGigaflare(2));
        world.Events.Add(66.956f,()=>MoveAfterGigaflare(2));
        world.Events.Add(67.406f,()=>PrepareTrinity(2));
        world.Events.Add(76.072f,PlayTrinity);
        world.Events.Add(77.145f,SnapshotTrinity);
        world.Events.Add(77.458f,()=>ResolveTrinity(false));
        world.Events.Add(80.097f,PlayTrinity);
        world.Events.Add(81.170f,SnapshotTrinity);
        world.Events.Add(81.483f,()=>ResolveTrinity(true));
        world.Events.Add(83.137f,()=>BeginExaflares(0.277f));
        world.Events.Add(90.014f,()=>ResolveExaflares(0));
        world.Events.Add(90.283f,ResolveSword);
        world.Events.Add(90.333f,()=>state.SetAll(state.Relative(new(0,0,8))));
        world.Events.Add(91.938f,()=>ResolveExaflares(1));
        world.Events.Add(93.817f,()=>ResolveExaflares(2));
        world.Events.Add(93.827f,()=>PrepareTrinity(4));
        world.Events.Add(95.694f,()=>ResolveExaflares(3));
        world.Events.Add(97.300f,PlayTrinity);
        world.Events.Add(97.568f,()=>ResolveExaflares(4));
        world.Events.Add(98.373f,SnapshotTrinity);
        world.Events.Add(98.686f,()=>ResolveTrinity(false));
        world.Events.Add(99.446f,()=>ResolveExaflares(5));
        world.Events.Add(101.324f,PlayTrinity);
        world.Events.Add(102.397f,SnapshotTrinity);
        world.Events.Add(102.710f,()=>ResolveTrinity(true));
        world.Events.Add(104.363f,()=>BeginTowers(0.258f));
        world.Events.Add(111.021f,()=>ResolveTowers(0));
        world.Events.Add(111.155f,ResolveSword);
        world.Events.Add(111.205f,()=>state.SetTowerPositions(true));
        world.Events.Add(112.452f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(113.257f,()=>ResolveTowers(1));
        world.Events.Add(113.524f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(114.329f,()=>ResolveTowers(2));
        world.Events.Add(114.596f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(115.401f,()=>ResolveTowers(3));
        world.Events.Add(115.669f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(116.474f,()=>ResolveTowers(4));
        world.Events.Add(116.741f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(117.546f,()=>ResolveTowers(5));
        world.Events.Add(117.846f,()=>PrepareTrinity(6));
        world.Events.Add(122.869f,PlayTrinity);
        world.Events.Add(123.942f,SnapshotTrinity);
        world.Events.Add(124.255f,()=>ResolveTrinity(false));
        world.Events.Add(126.894f,PlayTrinity);
        world.Events.Add(127.967f,SnapshotTrinity);
        world.Events.Add(128.280f,()=>ResolveTrinity(true));
        world.Events.Add(129.934f,()=>BeginGigaflares(0.284f,0.267f,0.290f));
        world.Events.Add(138.918f,()=>ResolveGigaflare(0));
        world.Events.Add(139.052f,ResolveSword);
        world.Events.Add(139.102f,()=>MoveAfterGigaflare(0));
        world.Events.Add(142.901f,()=>ResolveGigaflare(1));
        world.Events.Add(142.951f,()=>MoveAfterGigaflare(1));
        world.Events.Add(146.924f,()=>ResolveGigaflare(2));
        world.Events.Add(146.974f,()=>MoveAfterGigaflare(2));
        world.Events.Add(147.424f,()=>PrepareTrinity(2));
        world.Events.Add(156.089f,PlayTrinity);
        world.Events.Add(157.162f,SnapshotTrinity);
        world.Events.Add(157.475f,()=>ResolveTrinity(false));
        world.Events.Add(160.110f,PlayTrinity);
        world.Events.Add(161.183f,SnapshotTrinity);
        world.Events.Add(161.496f,()=>ResolveTrinity(true));
        world.Events.Add(163.148f,()=>BeginExaflares(0.281f));
        world.Events.Add(170.029f,()=>ResolveExaflares(0));
        world.Events.Add(170.298f,ResolveSword);
        world.Events.Add(170.348f,()=>state.SetAll(state.Relative(new(0,0,8))));
        world.Events.Add(171.953f,()=>ResolveExaflares(1));
        world.Events.Add(173.831f,()=>ResolveExaflares(2));
        world.Events.Add(173.841f,()=>PrepareTrinity(4));
        world.Events.Add(175.708f,()=>ResolveExaflares(3));
        world.Events.Add(177.318f,PlayTrinity);
        world.Events.Add(177.587f,()=>ResolveExaflares(4));
        world.Events.Add(178.391f,SnapshotTrinity);
        world.Events.Add(178.704f,()=>ResolveTrinity(false));
        world.Events.Add(179.465f,()=>ResolveExaflares(5));
        world.Events.Add(181.341f,PlayTrinity);
        world.Events.Add(182.414f,SnapshotTrinity);
        world.Events.Add(182.727f,()=>ResolveTrinity(true));
        world.Events.Add(184.382f,()=>BeginTowers(0.261f));
        world.Events.Add(191.043f,()=>ResolveTowers(0));
        world.Events.Add(191.178f,ResolveSword);
        world.Events.Add(191.228f,()=>state.SetTowerPositions(true));
        world.Events.Add(192.472f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(193.277f,()=>ResolveTowers(1));
        world.Events.Add(193.544f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(194.349f,()=>ResolveTowers(2));
        world.Events.Add(194.616f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(195.421f,()=>ResolveTowers(3));
        world.Events.Add(195.688f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(196.493f,()=>ResolveTowers(4));
        world.Events.Add(196.758f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(197.563f,()=>ResolveTowers(5));
        world.Events.Add(197.831f,()=>boss?.Cast(28053,castSeconds:0));
        world.Events.Add(198.636f,()=>ResolveTowers(6));
        world.Events.Add(198.936f,()=>PrepareTrinity(6));
        world.Events.Add(203.956f,PlayTrinity);
        world.Events.Add(205.029f,SnapshotTrinity);
        world.Events.Add(205.342f,()=>ResolveTrinity(false));
        world.Events.Add(207.977f,PlayTrinity);
        world.Events.Add(209.050f,SnapshotTrinity);
        world.Events.Add(209.363f,()=>ResolveTrinity(true));
        world.Events.Add(211.017f,BeginEnrage);
        world.Events.Add(221.698f,()=>ResolveSacrifice(0));
        world.Events.Add(224.898f,()=>ResolveSacrifice(1));
        world.Events.Add(228.098f,Enrage);
        world.Events.Add(230.100f,Complete);
        DsrP7DragonKingAi.Tick(state,world);
    }
    private SimEnemy? Spawn(uint id,bool visible=false)
    {
        var actor=world!.SpawnEnemy(new EnemySpawnConfig(id,NameId:11319,Level:90,EnemyList:visible?EnemyListMode.ScenarioVisible:EnemyListMode.Never,IsVisible:visible,Placement:new(Vector3.Zero,MathF.PI),DisableLookAt:true,WeaponDrawn:visible));
        if(actor!=null)actors.Add(actor);
        return actor;
    }
    private void Begin(uint action,float castSeconds)
    {
        state!.FaceTank=false;state.MechanicFacing=state.Facing;
        state.Fire=state.SwordChoice==0?state.Random.Next(2)==0:state.SwordChoice==1;
        state.MechanicIndex++;
        boss?.HoldFacing(state.MechanicFacing);
        boss?.RemoveStatus(2056);
        boss?.AddStatusParam(2056,state.Fire?0x12A:0x12B,castSeconds+1.5f);
        boss?.Cast(action,castSeconds:castSeconds,fireDelay:.28f);
        state.Hint=state.Fire?"火劍：站在目標圈外。":"冰劍：站在目標圈內。";
    }
    private void ResolveSword()
    {
        sword?.Cast(state!.Fire?28049u:28050u,castSeconds:0);
        foreach(var (_,m) in world!.Party.FilledSlots())
        {
            var distance=m.Position.Length();
            if(state!.Fire?distance<8:distance>8)Hit(m,state.Fire?"阿斯卡隆之焰：離開 8 碼鋼鐵":"阿斯卡隆之冰：進入 8 碼月環安區");
        }
        boss?.RemoveStatus(2056);
    }
    private void BeginExaflares(float fireDelay)
    {
        Begin(28059,5.7f);
        Vector3[] origins=[new(-6.9282f,0,-4),new(6.9282f,0,-4),new(0,0,8)];
        for(var i=0;i<3;i++)
        {
            state!.Exaflares[i]=state.Relative(origins[i]);
            state.ExaflareRotations[i]=state.Random.Next(8)*MathF.PI/4+state.MechanicFacing-MathF.PI;
            exaflares[i]?.SetPosition(state.Exaflares[i]);
            exaflares[i]?.HoldFacing(state.ExaflareRotations[i]);
            exaflares[i]?.Cast(28060,castSeconds:6.6f,fireDelay:fireDelay,targetId:exaflares[i]!.GameObjectId);
        }
        state!.SetAll(state.Relative(new(0,0,state.Fire?15:1.5f)));
        state.Hint+=" 百京火光：第一下後進後方亮點，第二下後再向後退。";
    }
    private void ResolveExaflares(int step)
    {
        for(var origin=0;origin<3;origin++)
        for(var ray=0;ray<(step==0?1:3);ray++)
        {
            var position=state!.Exaflares[origin]+DsrP7DragonKingState.Radial(state.ExaflareRotations[origin]+(ray-1)*MathF.PI/2,7*step);
            if(step>0)
            {
                var actor=exaflares[origin*3+ray];
                actor?.SetPosition(position);actor?.Cast(28061,position,0);
            }
            foreach(var (_,m) in world!.Party.FilledSlots())
                if(Vector3.DistanceSquared(m.Position,position)<36)Hit(m,"百京火光劍：地火命中");
        }
        if(step==0)state!.SetAll(state.Relative(new(0,0,state.Fire?9.2f:6.5f)));
        if(step==1)state!.SetAll(state.Relative(new(0,0,15)));
    }
    private void BeginTowers(float fireDelay)
    {
        Begin(28051,5.7f);
        SetTowers();
        for(var i=0;i<3;i++)towers[i]?.Cast(29452u+(uint)i,castSeconds:6.4f,fireDelay:fireDelay);
        state!.SetTowerPositions(false);
        state.Hint+=" 死亡輪迴劍：左前 H1/D1/D3，右前 H2/D2/D4，後方雙坦；劍判定後向內。";
    }
    private void SetTowers()
    {
        Vector3[] positions=[new(-6.9282f,0,-4),new(6.9282f,0,-4),new(0,0,8)];
        for(var i=0;i<3;i++)
        {
            state!.Towers[i]=state.Relative(positions[i]);
            towers[i]?.SetPosition(state.Towers[i]);
        }
    }
    private void ResolveTowers(int hit)
    {
        for(var i=0;i<3;i++)
        {
            if(hit>0)towers[i]?.Cast(i==2?28055u:28054u,castSeconds:0);
            var roles=Enumerable.Range(0,8).Where(r=>world!.Party.Get(r).IsAlive()&&Vector3.DistanceSquared(world.Party.Get(r)!.Position,state!.Towers[i])<=16).ToArray();
            var required=i==2?2:3;
            if(roles.Length<required||(i==2&&roles.Any(r=>r>=2)))
                FailAll($"死亡輪迴劍第 {hit+1} 下：{(i==2?"雙坦":i==0?"左前":"右前")}塔人數或職能錯誤");
        }
        for(var r=0;r<8;r++)
            if(world!.Party.Get(r) is {} member&&!state!.Towers.Any(p=>Vector3.DistanceSquared(member.Position,p)<=16))
                Hit(member,"死亡輪迴劍：未進塔");
    }
    private void BeginGigaflares(float firstDelay,float secondDelay,float thirdDelay)
    {
        Begin(28057,7.7f);
        var start=state!.Random.Next(8)*MathF.PI/4;
        var direction=state.Random.Next(2)==0?1:-1;
        uint[] actions=[28058,28114,28115];
        float[] delays=[firstDelay,secondDelay,thirdDelay];
        for(var i=0;i<3;i++)
        {
            state.Gigaflares[i]=DsrP7DragonKingState.Radial(start+direction*i*2*MathF.PI/3,14);
            gigaflares[i]?.SetPosition(state.Gigaflares[i]);
            gigaflares[i]?.Cast(actions[i],castSeconds:8.7f+4*i,fireDelay:delays[i],omenDelay:2*i);
        }
        state.SetAll(-Vector3.Normalize(state.Gigaflares[0])*(state.Fire?10:7));
        state.Hint+=" 十億火光：遠離第一個光柱，依序往已爆炸側繞。";
    }
    private void ResolveGigaflare(int hit)
    {
        foreach(var (_,m) in world!.Party.FilledSlots())
            if(Vector3.DistanceSquared(m.Position,state!.Gigaflares[hit])<400)Hit(m,"十億火光劍：距離爆心不足 20 碼");
    }
    private void MoveAfterGigaflare(int hit)
    {
        if(hit<2)state!.SetAll(-Vector3.Normalize(state.Gigaflares[hit+1])*10);
    }
    private void PrepareTrinity(int role)=>state!.SetTrinity(role);
    private void PlayTrinity()=>boss?.Cast(28062,castSeconds:0);
    private void SnapshotTrinity()
    {
        trinityTargets[0]=state!.MainTank;trinityTargets[1]=1-state.MainTank;
        trinityTargets[2]=Enumerable.Range(0,8).Where(r=>!state.Sacrificed[r]&&world!.Party.Get(r).IsAlive())
            .OrderBy(r=>world!.Party.Get(r)!.Position.LengthSquared()).FirstOrDefault(-1);
        for(var i=0;i<3;i++)
        {
            var target=trinityTargets[i]>=0?world!.Party.Get(trinityTargets[i]):null;
            if(target==null)continue;
            trinityPositions[i]=target.Position;
            trinity[i]?.SetPosition(Vector3.Zero);
            trinity[i]?.Cast(28063u+(uint)i,target.Position,0,targetId:target.GameObjectId);
        }
    }
    private void ResolveTrinity(bool second)
    {
        state!.ExpireStatuses();
        for(var i=0;i<3;i++)
        {
            var role=trinityTargets[i];
            if(role<0){FailAll("三劍一體：缺少承受者");continue;}
            var member=world!.Party.Get(role)!;
            if(i==2&&role!=state.TrinityRole)Hit(member,$"三劍一體：本次應由 {DsrP7DragonKingState.RoleName(state.TrinityRole)} 引導");
            if((i!=1&&state.DarkStacks[role]>=2)||(i!=0&&state.LightStacks[role]>=2)||(i==2&&state.PhysicalUntil[role]>state.Time))
                Hit(member,"三劍一體：耐性重複累積，未正確換坦／輪替");
            for(var other=0;other<8;other++)
                if(other!=role&&world.Party.Get(other) is {} m&&Vector3.DistanceSquared(m.Position,trinityPositions[i])<9)Hit(m,"三劍一體：與承受者重疊");
            if(i!=1){state.DarkStacks[role]++;state.DarkUntil[role]=state.Time+37;member.AddStatus(3136,37,state.DarkStacks[role],true);}
            if(i!=0){state.LightStacks[role]++;state.LightUntil[role]=state.Time+37;member.AddStatus(3135,37,state.LightStacks[role],true);}
            if(i==2){state.PhysicalUntil[role]=state.Time+60;member.AddStatus(2940,60);}
        }
        state.TrinityHits++;
        if(!second)state.SetTrinity(state.TrinityRole+1);
        else BeginTankSwap();
    }
    private void BeginTankSwap()
    {
        state!.ExpectedTank=1-state.ExpectedTank;
        var incoming=world!.Party.Get(state.ExpectedTank);
        if(!state.ManualTanks||incoming is SimPartyNpc)
        {
            state.MainTank=state.ExpectedTank;
            state.TankMessage=$"{DsrP7DragonKingState.RoleName(state.MainTank)} 挑釁接手。";
        }
        else state.TankMessage=$"輪到你挑釁托爾丹；下一組三劍一體前接手一仇。";
        state.SetTrinity(state.TrinityRole);
    }
    public bool OnTankAction(uint actionId,ulong targetId)
    {
        if(state==null||world==null||state.Complete||state.Enrage||!state.ManualTanks)return false;
        var role=(int)world.Party.PlayerRole;
        if(role>1||world.Party.Get(role) is not SimPlayer player||!player.IsAlive())return false;
        if(actionId==7533&&boss!=null&&targetId==boss.GameObjectId)
        {
            state.MainTank=role;
            state.TankMessage=$"{DsrP7DragonKingState.RoleName(role)} 挑釁：取得一仇。";
        }
        else if(actionId==7537&&world.Party.Get(1-role) is {} other&&targetId==other.GameObjectId)
        {
            if(state.MainTank==role)state.MainTank=1-role;
            state.TankMessage=$"{DsrP7DragonKingState.RoleName(role)} 退避給 {DsrP7DragonKingState.RoleName(1-role)}。";
        }
        else return false;
        if(state.FaceTank)state.SetTankPositions();
        boss?.SetTarget(world.Party.Get(state.MainTank),false);
        boss?.SetTankEnmity(world.Party,state.MainTank);
        return true;
    }
    private void BeginEnrage()
    {
        state!.FaceTank=false;state.MechanicFacing=state.Facing;state.Enrage=true;
        boss?.HoldFacing(state.MechanicFacing);boss?.Cast(28206,castSeconds:9.7f,fireDelay:.281f);
        SetTowers();
        for(var i=0;i<3;i++)towers[i]?.Cast(29455u+(uint)i,castSeconds:10.4f,fireDelay:.281f);
        SetSacrifice(0);
    }
    private void SetSacrifice(int wave)
    {
        int[] roles=wave==0?[2,3,1]:[6,7,0];
        state!.SetAll(Vector3.Zero);
        for(var i=0;i<3;i++)state.Destinations[roles[i]]=state.Towers[i];
        state.Hint=wave==0?"無盡頓悟劍：H1、H2、ST 進塔；其餘人在塔外待命。":"第二輪：D3、D4、MT 進塔；D1、D2 留在場中。";
    }
    private void ResolveSacrifice(int wave)
    {
        int[] expected=wave==0?[2,3,1]:[6,7,0];
        if(wave>0)boss?.Cast(28207,castSeconds:0);
        for(var i=0;i<3;i++)
        {
            if(wave>0)towers[i]?.Cast(28208,castSeconds:0);
            var roles=Enumerable.Range(0,8).Where(r=>!state!.Sacrificed[r]&&world!.Party.Get(r).IsAlive()&&Vector3.DistanceSquared(world.Party.Get(r)!.Position,state.Towers[i])<=16).ToArray();
            if(roles.Length!=1||roles[0]!=expected[i]){FailAll("無盡頓悟劍：犧牲塔承受者錯誤");continue;}
            state!.Sacrificed[expected[i]]=true;
            if(world!.Party.Get(expected[i]) is ISimPartyMember member)member.OnKilled();
        }
        if(wave==0)SetSacrifice(1);
        else {state!.SetAll(Vector3.Zero);state.Hint="第三輪不踩塔；D1、D2 繼續攻擊，等待狂暴演出。";}
    }
    private void Enrage()
    {
        boss?.Cast(28207,castSeconds:0);
        towers[0]?.Cast(28209,castSeconds:0);
        foreach(var (_,m) in world!.Party.FilledSlots())if(m is ISimPartyMember member)member.OnKilled();
        state!.Hint="狂暴演出結束；機制與換坦練習完成（未模擬 DPS 血量門檻）。";
    }
    private void Hit(SimCharacter member,string reason)
    {
        if(member.IsAlive()){state!.Failed=true;member.Die(reason);}
    }
    private void FailAll(string reason)
    {
        foreach(var (_,member) in world!.Party.FilledSlots())Hit(member,reason);
    }
    private void Complete()
    {
        state!.Complete=true;
        foreach(var actor in actors)actor.Despawn();
        foreach(var (_,member) in world!.Party.FilledSlots())foreach(var id in StatusIds)member.RemoveStatus(id);
    }
    public void Tick(float delta,float elapsed)
    {
        if(state==null||world==null||state.Complete)return;
        state.Time=elapsed;state.ExpireStatuses();
        boss?.SetTarget(world.Party.Get(state.MainTank),false);
        boss?.SetTankEnmity(world.Party,state.MainTank);
        if(state.FaceTank&&world.Party.Get(state.MainTank) is {} tank)
        {
            var target=MathF.Atan2(tank.Position.X,tank.Position.Z);
            var difference=MathF.IEEERemainder(target-state.Facing,2*MathF.PI);
            state.Facing+=Math.Clamp(difference,-delta*3,delta*3);
            boss?.HoldFacing(state.Facing);
        }
        DsrP7DragonKingAi.Tick(state,world);
    }
}
