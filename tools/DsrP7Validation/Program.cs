using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P7DragonKing;

void Check(bool condition,string message){if(!condition)throw new Exception(message);}
foreach(var fps in new[]{30,60,144})
for(var seed=0;seed<32;seed++)
for(var sword=0;sword<3;sword++)
for(var plan=0;plan<3;plan++)
{
    var scenario=new DsrP7DragonKingScenario();scenario.Configure(seed,sword,plan);
    var world=new SimWorld();SimCharacter.Failures.Clear();scenario.Run(world,0);
    for(var frame=1;frame<=232*fps;frame++)
    {
        var time=frame/(float)fps;SimCharacter.Time=time;
        var before=world.Party.Slots.Select(m=>m.Position).ToArray();
        world.Events.Tick(1f/fps);
        if(scenario.State.MechanicIndex is 1 or 4 or 7 && !scenario.State.FaceTank)
            Check(MathF.Abs(MathF.Sin(scenario.State.MechanicFacing))<.025f,"all three Exaflares must face A/C with NPC tanks");
        foreach(var member in world.Party.Slots)member.Advance(1f/fps);
        scenario.Tick(1f/fps,time);
        if(scenario.State.FaceTank&&!scenario.State.Enrage)
            Check(Vector3.Dot(scenario.State.Destinations[0],scenario.State.Destinations[1])>0&&
                Vector3.Distance(scenario.State.Destinations[0],scenario.State.Destinations[1])>3.9f,"Trinity tank destinations share a side without overlap");
        if(scenario.State.TowerRound>0)
        {
            bool[][] expected=[[false,false,false],[false,false,true],[false,true,true]];
            Check(scenario.State.SoloTowers==expected[plan][scenario.State.TowerRound-1],"selected tower sequence applies to the correct round");
            if(scenario.State.SoloTowers)
            {
                var assigned=Enumerable.Range(0,8).GroupBy(scenario.State.TowerForRole).OrderBy(g=>g.Key).ToArray();
                Check(assigned.Select(g=>g.Count()).SequenceEqual(new[]{6,1,1}),"116 has six red, solo red and solo blue");
                Check(assigned[0].All(r=>r>=2)&&assigned[1].Single()<2&&assigned[2].Single()<2,"six non-tanks use red; each tank solos a different tower");
                Check(assigned[2].Single()==(plan==2&&scenario.State.TowerRound==3?1:0),"blue solo tank alternates for consecutive 116 rounds");
            }
        }
        for(var r=0;r<8;r++)Check(Vector3.Distance(before[r],world.Party.Slots[r].Position)<=6f/fps+.003f,"NPC teleported");
    }
    Check(SimCharacter.Failures.Count==0,$"seed={seed} sword={sword} plan={plan} fps={fps}: {string.Join(";",SimCharacter.Failures.Take(15))}");
    Check(scenario.State.Complete&&scenario.State.TrinityHits==16&&world.Events.IsEmpty,"full timeline completion");
    Check(scenario.State.Sacrificed.Count(x=>x)==6&&world.Enemies.All(e=>!e.Active),"enrage sacrifice and cleanup");
    Check(world.Enemies.Count==20,"bounded native actor count");
    var casts=world.Enemies.SelectMany(e=>e.CastTargets).Where(c=>c.Action==28060).ToArray();
    Check(casts.Length==9&&casts.All(c=>c.Target.HasValue),"initial Exaflares deliver native effects to a registered actor");
    var omens=world.Enemies.SelectMany(e=>e.Omens).ToArray();
    foreach(var (action,delay) in new[]{(28058u,0f),(28114u,2f),(28115u,4f)})
        Check(omens.Count(o=>o.Action==action&&o.Delay==delay)==2,"Gigaflare hints appear sequentially in both sets");
}
Console.WriteLine("PASS: 32 seeds × 3 sword modes × 3 tower plans × 30/60/144 FPS; same-side tanks, A/C Exaflares, 332/116 roles, all mechanics and cleanup.");
for(var role=0;role<8;role++)
for(var plan=0;plan<3;plan++)
{
    var scenario=new DsrP7DragonKingScenario();scenario.Configure(17,0,plan);
    var world=new SimWorld();world.Party.Slots[role]=new SimPlayer{Role=role,Position=new(0,0,16)};
    SimCharacter.Failures.Clear();scenario.Run(world,0);
    foreach(var enemy in world.Enemies)enemy.Role=100+world.Enemies.IndexOf(enemy);
    for(var frame=1;frame<=232*60;frame++)
    {
        var time=frame/60f;SimCharacter.Time=time;
        var player=world.Party.Slots[role];
        if(!scenario.State.Sacrificed[role])player.MoveTo(scenario.State.Destinations[role]);
        var commands=player.MoveCommands;
        world.Events.Tick(1f/60);
        if(role<2&&scenario.State.ExpectedTank==role&&scenario.State.MainTank!=role)
            Check(scenario.OnTankAction(7533,world.Enemies[0].GameObjectId),"executed Provoke takes aggro");
        foreach(var member in world.Party.Slots)member.Advance(1f/60);
        scenario.Tick(1f/60,time);
        Check(player.MoveCommands==commands,"AI must not move player");
    }
    Check(SimCharacter.Failures.Count==0,$"scripted role {role}: {string.Join(";",SimCharacter.Failures.Take(8))}");
}
Console.WriteLine("PASS: all eight scripted player roles; target-validated manual tank swaps.");
foreach(var mode in new[]{"stationary","missed-swap","wrong-target"})
{
    var scenario=new DsrP7DragonKingScenario();scenario.Configure(9,1);
    var world=new SimWorld();var role=mode=="stationary"?2:1;
    world.Party.Slots[role]=new SimPlayer{Role=role,Position=new(0,0,16)};
    SimCharacter.Failures.Clear();scenario.Run(world,0);
    foreach(var enemy in world.Enemies)enemy.Role=100+world.Enemies.IndexOf(enemy);
    for(var frame=1;frame<=100*60;frame++)
    {
        var time=frame/60f;SimCharacter.Time=time;
        if(mode!="stationary")world.Party.Slots[role].MoveTo(scenario.State.Destinations[role]);
        world.Events.Tick(1f/60);
        if(mode=="wrong-target")Check(!scenario.OnTankAction(7533,999),"wrong Provoke target rejected");
        foreach(var member in world.Party.Slots)member.Advance(1f/60);
        scenario.Tick(1f/60,time);
    }
    Check(scenario.State.Failed&&SimCharacter.Failures.Count>0,$"{mode} must fail");
    if(mode!="stationary")Check(SimCharacter.Failures.Any(s=>s.Contains("耐性重複")),"missed swap caught by vulnerability");
}
Console.WriteLine("PASS: stationary player, missed tank swaps and wrong targets fail.");
for(var pattern=0;pattern<512;pattern++)
foreach(var sword in new[]{1,2})
foreach(var facing in new[]{0f,MathF.PI})
{
    var scenario=new DsrP7DragonKingScenario();scenario.Configure(0,sword);var world=new SimWorld();scenario.Run(world,0);
    SimCharacter.Failures.Clear();var assigned=false;
    for(var frame=1;frame<=24*60;frame++)
    {
        SimCharacter.Time=frame/60f;
        if(!assigned)scenario.State.Facing=facing;
        world.Events.Tick(1f/60);
        if(!assigned&&scenario.State.MechanicIndex==1)
        {
            assigned=true;
            for(var i=0;i<3;i++)scenario.State.ExaflareRotations[i]=((pattern>>(3*i))&7)*MathF.PI/4+facing-MathF.PI;
        }
        foreach(var member in world.Party.Slots)member.Advance(1f/60);
        scenario.Tick(1f/60,frame/60f);
    }
    Check(SimCharacter.Failures.Count==0,$"exaflare pattern {pattern}, sword {sword}, facing {facing}: {string.Join(";",SimCharacter.Failures.Take(3))}");
}
Console.WriteLine("PASS: all 512 three-way Exaflare orientations with both swords, north/south boss facing and continuous walking.");
{
    var scenario=new DsrP7DragonKingScenario();scenario.Configure(1,0);var world=new SimWorld();world.Party.Slots[0]=new SimPlayer{Role=0};scenario.Run(world,0);
    foreach(var enemy in world.Enemies)enemy.Role=100+world.Enemies.IndexOf(enemy);
    Check(!scenario.OnTankAction(7537,world.Enemies[0].GameObjectId)&&scenario.State.MainTank==0,"Shirk boss rejected");
    Check(scenario.OnTankAction(7537,1)&&scenario.State.MainTank==1,"Shirk co-tank transfers first aggro");
    Check(scenario.State.Destinations[0]==new Vector3(0,0,-6)&&scenario.State.Destinations[1]==new Vector3(0,0,-10),"tank destinations remain on the same side after swap");
    Check(world.Enemies[0].EnmityTank==1,"Shirk updates enemy enmity");
    Check(scenario.OnTankAction(7533,world.Enemies[0].GameObjectId)&&scenario.State.MainTank==0,"Provoke retakes first aggro");
    Check(world.Enemies[0].EnmityTank==0,"Provoke updates enemy enmity");
    scenario.State.Complete=true;Check(!scenario.OnTankAction(7533,world.Enemies[0].GameObjectId),"completed scenario ignores actions");
}
Console.WriteLine("PASS: Shirk target validation, Provoke retake, tank repositioning and completion guard.");
foreach(var plan in new[]{1,2})
{
    var scenario=new DsrP7DragonKingScenario();scenario.Configure(2,1,plan);var world=new SimWorld();scenario.Run(world,0);
    SimCharacter.Failures.Clear();
    var hit=plan==1?191.043f:111.021f;
    for(var frame=1;frame<=(hit+1)*60;frame++)
    {
        var time=frame/60f;SimCharacter.Time=time;
        if(time>hit-.2f&&time<hit+.2f)
            world.Party.Get(scenario.State.BlueTowerTank)!.SetPosition(scenario.State.Towers[1]);
        world.Events.Tick(1f/60);
        foreach(var member in world.Party.Slots)member.Advance(1f/60);
        scenario.Tick(1f/60,time);
    }
    Check(SimCharacter.Failures.Any(f=>f.Contains("塔人數或職能錯誤")),"116 missing blue tank must fail instead of bypassing tower validation");
}
Console.WriteLine("PASS: both new tower plans reject a missing solo blue-tower tank.");

namespace AnoMech.Scenarios.Dsr.P7DragonKing
{
    public sealed partial class DsrP7DragonKingScenario
    {
        internal DsrP7DragonKingState State=>state!;
        internal void Configure(int seed,int sword,int plan=0){validationSeed=seed;swordChoice=sword;towerPlan=plan;}
    }
}
