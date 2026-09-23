using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P5Wrath;
void Check(bool condition,string message){if(!condition)throw new Exception(message);}
foreach(var fps in new[]{30,60,144})
for(var seed=0;seed<100;seed++)
{
    var scenario=new DsrP5WrathScenario(); scenario.UseSeed(seed);
    var world=new SimWorld(); SimCharacter.Failures.Clear(); scenario.Run(world,0);
    for(var f=1;f<=23*fps;f++)
    {
        SimCharacter.Time=f/(float)fps;
        var before=world.Party.Slots.Select(m=>m.Position).ToArray();
        world.Events.Tick(1f/fps);
        foreach(var m in world.Party.Slots)m.Advance(1f/fps);
        scenario.Tick(1f/fps,SimCharacter.Time);
        for(var r=0;r<8;r++)Check(Vector3.Distance(before[r],world.Party.Slots[r].Position)<=6f/fps+.001f,"teleport");
    }
    Check(SimCharacter.Failures.Count==0,$"seed {seed}, fps {fps}: {string.Join(";",SimCharacter.Failures.Distinct())}");
    Check(scenario.State.Complete&&world.Events.IsEmpty,"incomplete");
    Check(world.EventObjects.Count==8&&world.EventObjects.All(e=>!e.Active),"twister cleanup");
    Check(world.Enemies.All(e=>!e.Visible&&e.NameId!=0),"named actor cleanup");
}
Console.WriteLine("PASS: 100 seeds at 30/60/144 FPS; rotating cardinal layouts, cross-tethers, blue spread, white dragon line, twister dodge, continuous NPC movement and cleanup.");
for(var role=0;role<8;role++)
{
    var s=new DsrP5WrathScenario();s.UseSeed(role);var w=new SimWorld();w.Party.Slots[role]=new SimPlayer{Role=role,Position=new(0,0,16)};SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=23*60;f++)
    {
        w.Party.Slots[role].MoveTo(DsrP5WrathAi.Destination(s.State,role),6);
        var commands=w.Party.Slots[role].MoveCommands;
        w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,f/60f);
        Check(w.Party.Slots[role].MoveCommands==commands,"scenario moves player");
    }
    Check(SimCharacter.Failures.Count==0,"player role failure");
}
foreach(var error in new[]{"blue","charge","twister"})
{
    var s=new DsrP5WrathScenario();s.UseSeed(7);var w=new SimWorld();SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=23*60;f++)
    {
        var t=f/60f;
        if(t>=18.8f&&t<19.1f)
        {
            if(error=="blue")w.Party.Slots[s.State.EastRoles[0]].Position=w.Party.Slots[s.State.Blue].Position;
            if(error=="charge")w.Party.Slots[s.State.EastRoles[0]].Position=Vector3.Zero;
        }
        if(error=="twister"&&t>=20.15f)w.Party.Slots[0].Position=s.State.Twisters[0];
        w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,t);
    }
    var expected=error switch{"blue"=>"藍標","charge"=>"衝鋒",_=>"旋風"};
    Check(SimCharacter.Failures.Any(f=>f.Contains(expected)),"missing failure "+error);
}
Console.WriteLine("PASS: all 8 scripted player roles; wrong blue spread, charge overlap and standing on twister rejected.");
namespace AnoMech.Scenarios.Dsr.P5Wrath
{
    public sealed partial class DsrP5WrathScenario
    {
        internal DsrP5WrathState State=>state!;
        internal void UseSeed(int value){fixedSeed=true;seed=value;}
    }
}
