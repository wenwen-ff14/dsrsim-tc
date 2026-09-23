using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P7DragonKing;

void Check(bool condition,string message){if(!condition)throw new Exception(message);}
foreach(var fps in new[]{30,60,144})
for(var seed=0;seed<32;seed++)
for(var sword=0;sword<3;sword++)
{
    var scenario=new DsrP7DragonKingScenario();scenario.Configure(seed,sword);
    var world=new SimWorld();SimCharacter.Failures.Clear();scenario.Run(world,0);
    for(var frame=1;frame<=232*fps;frame++)
    {
        var time=frame/(float)fps;SimCharacter.Time=time;
        var before=world.Party.Slots.Select(m=>m.Position).ToArray();
        world.Events.Tick(1f/fps);
        foreach(var member in world.Party.Slots)member.Advance(1f/fps);
        scenario.Tick(1f/fps,time);
        for(var r=0;r<8;r++)Check(Vector3.Distance(before[r],world.Party.Slots[r].Position)<=6f/fps+.003f,"NPC teleported");
    }
    Check(SimCharacter.Failures.Count==0,$"seed={seed} sword={sword} fps={fps}: {string.Join(";",SimCharacter.Failures.Take(15))}");
    Check(scenario.State.Complete&&scenario.State.TrinityHits==16&&world.Events.IsEmpty,"full timeline completion");
    Check(scenario.State.Sacrificed.Count(x=>x)==6&&world.Enemies.All(e=>!e.Active),"enrage sacrifice and cleanup");
    Check(world.Enemies.Count==20,"bounded native actor count");
}
Console.WriteLine("PASS: 32 seeds × 3 sword modes × 30/60/144 FPS; walking NPCs, all mechanics, 16 Trinity hits, sacrifices and cleanup.");
for(var role=0;role<8;role++)
{
    var scenario=new DsrP7DragonKingScenario();scenario.Configure(17,0);
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
    Check(scenario.State.Destinations[1]==new Vector3(0,0,-10)&&scenario.State.Destinations[0]==new Vector3(7,0,-7),"tank destinations follow aggro swap");
    Check(scenario.OnTankAction(7533,world.Enemies[0].GameObjectId)&&scenario.State.MainTank==0,"Provoke retakes first aggro");
    scenario.State.Complete=true;Check(!scenario.OnTankAction(7533,world.Enemies[0].GameObjectId),"completed scenario ignores actions");
}
Console.WriteLine("PASS: Shirk target validation, Provoke retake, tank repositioning and completion guard.");

namespace AnoMech.Scenarios.Dsr.P7DragonKing
{
    public sealed partial class DsrP7DragonKingScenario
    {
        internal DsrP7DragonKingState State=>state!;
        internal void Configure(int seed,int sword){validationSeed=seed;swordChoice=sword;}
    }
}
