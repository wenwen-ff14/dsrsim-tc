using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
namespace AnoMech.Scenarios.Dsr.P5Wrath;
public sealed partial class DsrP5WrathScenario : IScenario
{
    public string Name => "風槍";
    public IPhase Phase => DsrZone.P5;
    public IReadOnlyList<IScenarioAi> AiStrats { get; } = [new DsrP5WrathAi()];
    private SimWorld? world;
    private DsrP5WrathState? state;
    private SimEnemy? boss, whiteDragon, darkDragon, vidofnir, leapKnight, grinnaux, charibert;
    private readonly SimEnemy?[] chargeKnights = new SimEnemy?[2];
    private readonly SimTether?[] tethers = new SimTether?[2];
    private readonly List<SimEventObject> twisterObjects = [];
    private bool fixedSeed, showHints = true, showMap = true;
    private int seed = 1;

    public void Run(SimWorld simWorld, int? selectedAi)
    {
        world = simWorld;
        state = new(fixedSeed ? seed : Random.Shared.Next());
        twisterObjects.Clear();
        Array.Clear(tethers);
        boss = Spawn(DsrP5WrathConstants.Thordan, 3632, Vector3.Zero, true);
        boss?.SetTargetable(true);
        whiteDragon = Spawn(DsrP5WrathConstants.Vedrfolnir, 11314, state.WhiteDragon, false);
        darkDragon = Spawn(DsrP5WrathConstants.Darkscale, 3983, state.Rotate(new(-24,0,0)), false);
        vidofnir = Spawn(DsrP5WrathConstants.Vidofnir, 3984, state.Rotate(new(24,0,0)), false);
        chargeKnights[0] = Knight(DsrConstants.Npc.Vellguine, state.KnightPosition(0));
        chargeKnights[1] = Knight(DsrConstants.Npc.Ignasse, state.KnightPosition(1));
        leapKnight = Knight(DsrConstants.Npc.Paulecrain, state.Rotate(new(-21,0,-10)));
        grinnaux = Knight(DsrConstants.Npc.Grinnaux, state.Grinnaux);
        charibert = Knight(DsrConstants.Npc.Charibert, state.Charibert);
        world.Events.Add(3f, () => boss?.Cast(DsrP5WrathConstants.Wrath, castSeconds:4));
        world.Events.Add(10.1f, () => { boss?.SetTargetable(false); boss?.PlayDeparture(DsrConstants.Timeline.KnightDeparture); });
        world.Events.Add(10.6f, () =>
        {
            boss?.SetVisible(false);
            whiteDragon?.SetVisible(true);
            foreach(var knight in chargeKnights) knight?.SetVisible(true);
            leapKnight?.SetVisible(true);
        });
        world.Events.Add(12.8f, Assign);
        world.Events.Add(13f, () => whiteDragon?.Cast(DsrP5WrathConstants.TwistingDive, state!.Rotate(new(0,0,24)), 6));
        world.Events.Add(18f, () =>
        {
            grinnaux?.SetVisible(true);
            charibert?.SetVisible(true);
            darkDragon?.SetVisible(true);
            vidofnir?.SetVisible(true);
        });
        world.Events.Add(19f, ResolveCharges);
        world.Events.Add(20.2f, ShowTwisters);
        world.Events.Add(21f, () =>
        {
            whiteDragon?.SetVisible(false);
            foreach(var knight in chargeKnights) knight?.SetVisible(false);
            leapKnight?.SetVisible(false);
        });
        world.Events.Add(22.2f, ClearTwisters);
        world.Events.Add(22.5f, Complete);
        DsrP5WrathAi.Tick(state, world);
    }
    private SimEnemy? Spawn(uint npc, uint name, Vector3 position, bool visible)
        => world!.SpawnEnemy(new EnemySpawnConfig(npc, NameId:name, Level:90,
            EnemyList:EnemyListMode.ScenarioVisible, IsVisible:visible,
            Placement:new(position,position.LengthSquared() > .001f ? MathF.Atan2(-position.X,-position.Z) : 0), DisableLookAt:true,
            WeaponDrawn:npc is DsrP5WrathConstants.Thordan or DsrConstants.Npc.Vellguine or DsrConstants.Npc.Paulecrain or DsrConstants.Npc.Ignasse or DsrConstants.Npc.Grinnaux or DsrConstants.Npc.Charibert));
    private SimEnemy? Knight(uint npc, Vector3 position)
    {
        var knight = Spawn(npc,DsrConstants.NameId(npc),position,false);
        knight?.QueueEntrance(DsrConstants.Timeline.KnightEntrance, .65f);
        return knight;
    }
    private void Assign()
    {
        state!.Assigned = true;
        world!.Party.Get(state.Blue)?.AttachLockonVfx(14,6.2f);
        for(var i=0;i<2;i++) tethers[i] = world.Tether(chargeKnights[i],world.Party.Get(state.TetherRoles[i]),DsrP5WrathConstants.Tether);
    }
    internal static bool InCharge(Vector3 p, Vector3 start, Vector3 end, float halfWidth)
    {
        var delta=end-start;
        if(delta.LengthSquared()<.01f) return false;
        var forward=Vector3.Normalize(delta);
        var offset=p-start;
        var along=Vector3.Dot(offset,forward);
        return along>=0 && along<=60 && MathF.Abs(offset.X*forward.Z-offset.Z*forward.X)<halfWidth;
    }
    private void ResolveCharges()
    {
        for(var i=0;i<2;i++)
        {
            var target=world!.Party.Get(state!.TetherRoles[i]);
            var source=state.KnightPosition(i);
            if(!target.IsAlive()) { Fail("風槍連線目標已倒下"); continue; }
            chargeKnights[i]?.Cast(DsrP5WrathConstants.SpiralPierce,target!.Position,0,target.GameObjectId);
            if(Vector3.Distance(source,target!.Position)<30) Hit(target,"風槍連線過短：交叉拉至對側南方");
            foreach(var other in world.Party.ActiveMembers())
                if(other!=target && InCharge(other.Position,source,target.Position,8)) Hit(other,"被騎士交叉衝鋒波及");
        }
        var blue=world!.Party.Get(state!.Blue);
        if(!blue.IsAlive()) Fail("風槍藍標目標已倒下");
        else
        {
            leapKnight?.Cast(DsrP5WrathConstants.SkywardLeap,blue!.Position,0,blue.GameObjectId);
            foreach(var other in world.Party.ActiveMembers())
                if(other!=blue && Vector3.DistanceSquared(other.Position,blue!.Position)<576) Hit(other,"被藍標跳躍範圍波及");
        }
        foreach(var member in world.Party.ActiveMembers())
            if(InCharge(member.Position,state.WhiteDragon,state.Rotate(new(0,0,24)),5)) Hit(member,"未躲開白龍旋風衝直線");
        for(var role=0;role<8;role++) state.Twisters[role]=world.Party.Get(role)?.Position??Vector3.Zero;
        foreach(var tether in tethers) tether?.Despawn();
        state.ChargesResolved=true;
    }
    private void ShowTwisters()
    {
        state!.TwistersActive=true;
        foreach(var position in state.Twisters)
        {
            var obj=world!.SpawnEventObject(new EventObjectSpawnConfig { EObjId=DsrP5WrathConstants.TwisterObject, Placement=new(position,0), Lifetime=2.1f });
            if(obj!=null) twisterObjects.Add(obj);
        }
        CheckTwisters();
    }
    private void CheckTwisters()
    {
        if(!state!.TwistersActive) return;
        foreach(var member in world!.Party.ActiveMembers())
            if(member.IsAlive() && state.Twisters.Any(p=>Vector3.DistanceSquared(p,member.Position)<4)) Hit(member,"踩到旋風：衝鋒後離開原位，勿穿過他人的旋風");
    }
    private void ClearTwisters()
    {
        state!.TwistersActive=false;
        foreach(var obj in twisterObjects) obj.Despawn();
        twisterObjects.Clear();
    }
    private void Complete()
    {
        state!.Complete=true;
        ClearTwisters();
        foreach(var tether in tethers) tether?.Despawn();
        foreach(var actor in chargeKnights.Concat(new[]{boss,whiteDragon,darkDragon,vidofnir,leapKnight,grinnaux,charibert})) actor?.SetVisible(false);
    }
    private void Hit(SimCharacter member,string reason) { state!.Failed=true; member.Die(reason); }
    private void Fail(string reason) { state!.Failed=true; world!.Party.WipeAllPlayers(reason); }
    public void Tick(float delta,float elapsed)
    {
        if(state==null || world==null || state.Complete) return;
        state.Time=elapsed;
        CheckTwisters();
        DsrP5WrathAi.Tick(state,world);
    }
}
