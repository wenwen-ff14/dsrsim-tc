using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P5Wrath;
void Check(bool condition,string message){if(!condition){Console.WriteLine(message);Environment.Exit(1);}}
foreach(var fps in new[]{30,60,144})
for(var seed=0;seed<200;seed++)
{
    var scenario=new DsrP5WrathScenario(); scenario.UseSeed(seed); scenario.UsePracticeTarget((seed / 10) % 5, (seed / 50));
    var world=new SimWorld(); SimCharacter.Failures.Clear(); scenario.Run(world,0);
    for(var f=1;f<=40*fps;f++)
    {
        SimCharacter.Time=f/(float)fps;
        if(SimCharacter.Time<17.4f) Check(world.Party.Slots.All(m=>!m.HasStatus(466)),"early thunder status");
        var before=world.Party.Slots.Select(m=>m.Position).ToArray();
        world.Events.Tick(1f/fps);
        foreach(var m in world.Party.Slots)m.Advance(1f/fps);
        scenario.Tick(1f/fps,SimCharacter.Time);
        if(SimCharacter.Time<26.294f)
        {
            Check(world.EventObjects.Count<=8,"Liquid Heaven ground appeared before projectile impact");
            Check(!world.Party.Get(scenario.State.Liquid)!.HasStatus(DsrP5WrathConstants.FireResistanceDown),"early fire vulnerability");
        }
        if(SimCharacter.Time>30.97f && SimCharacter.Time<33.9f)
            Check(world.Party.Get(scenario.State.Liquid)!.HasStatus(DsrP5WrathConstants.FireResistanceDown),"fire vulnerability missing after last impact");
        if(SimCharacter.Time>34f)
            Check(!world.Party.Get(scenario.State.Liquid)!.HasStatus(DsrP5WrathConstants.FireResistanceDown),"fire vulnerability did not expire");
        for(var r=0;r<8;r++)Check(Vector3.Distance(before[r],world.Party.Slots[r].Position)<=6f/fps+.001f,"teleport");
    }
    if(SimCharacter.Failures.Count>0) Console.WriteLine($"roles blue={scenario.State.Blue} green={scenario.State.Green} liquid={scenario.State.Liquid} altar={scenario.State.Altar} north={scenario.State.GrinnauxNorth} first={string.Join(";",SimCharacter.Failures.Take(8))}");
    Check(SimCharacter.Failures.Count==0,$"seed {seed}, fps {fps}: {string.Join(";",SimCharacter.Failures.Select(f=>f[(f.IndexOf("role"))..]).Distinct())}");
    var dragon=world.Enemies.Single(e=>e.BNpcBaseId==DsrP5WrathConstants.Vedrfolnir);
    Check(MathF.Abs(scenario.State.Grinnaux.Length()-11)<.001f&&MathF.Abs(scenario.State.Charibert.Length()-12)<.001f,"moon knight radius");
    Check(world.Enemies.SelectMany(e=>e.ActionTimelines).Count(t=>t.Id==2225)==5,"five original Liquid Heaven impacts");
    Check(dragon.Casts.Count(c=>c.Action==27537)==5,"white dragon must cast all five Liquid Heaven attacks");
    var liquidCasts=dragon.Casts.Where(c=>c.Action==27537).ToArray();
    var loggedCasts=new[]{25.219f,26.384f,27.548f,28.711f,29.874f};
    for(var i=0;i<5;i++)Check(MathF.Abs(liquidCasts[i].Time-loggedCasts[i])<=1f/fps+.001f,"Liquid Heaven cast differs from log interval");
    Check(world.Enemies.Where(e=>e!=dragon).All(e=>e.Casts.All(c=>c.Action!=27537)),"wrong Liquid Heaven source");
    Check(world.Enemies.Where(e=>e.BNpcBaseId is DsrP5WrathConstants.Vedrfolnir or DsrP5WrathConstants.Darkscale or DsrP5WrathConstants.Vidofnir).All(e=>e.Entrances.Count==1),"dragon entrance missing");
    Check(world.Enemies.SelectMany(e=>e.Omens).Any(o=>o.Action==25306 && o.Delay==4.2f),"moon omen timing");
    Check(world.Party.Slots.Sum(m=>m.Vfx.Count(v=>v.Time>=17.452f && v.Path.Contains("dk05th")))==2,"thunder VFX timing");
    Check(scenario.State.Complete&&world.Events.IsEmpty,"incomplete");
    Check(world.EventObjects.Count==13&&world.EventObjects.All(e=>!e.Active),"twister cleanup");
    Check(world.Enemies.All(e=>!e.Active&&e.NameId!=0),"named actor cleanup");
}
Console.WriteLine("PASS: 200 seeds across both target groups at 30/60/144 FPS; rotating cardinal layouts, cross-tethers, blue spread, white dragon line, twister dodge, continuous NPC movement and cleanup.");
for(var role=0;role<8;role++)
{
    var s=new DsrP5WrathScenario();s.UseSeed(role);var w=new SimWorld();w.Party.Slots[role]=new SimPlayer{Role=role,Position=new(0,0,16)};SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=40*60;f++)
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
    for(var f=1;f<=40*60;f++)
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
foreach(var error in new[]{"mercy","dive","moon","lightning","liquid","altar"})
{
    var s=new DsrP5WrathScenario();s.UseSeed(7);var w=new SimWorld();SimCharacter.Failures.Clear();s.Run(w,0);
    Check(!w.Enemies.Any(e=>e.BNpcBaseId is DsrP5WrathConstants.Vedrfolnir or DsrP5WrathConstants.Darkscale or DsrP5WrathConstants.Vidofnir),"dragons preloaded before entrance");
    for(var f=1;f<=40*60;f++)
    {
        var t=f/60f;
        if(error=="mercy" && t>24.2f && t<25.3f) w.Party.Slots[s.State.Blue].Position=w.Party.Slots[s.State.TetherRoles[0]].Position;
        if(error=="dive" && t>26f && t<26.15f) w.Party.Slots[s.State.Green].Position=s.State.Grinnaux;
        if(error=="moon" && t>32.15f && t<32.3f) w.Party.Slots[0].Position=Vector3.Zero;
        if(error=="lightning" && t>32.15f && t<32.3f) w.Party.Slots[s.State.Thunder[0]].Position=w.Party.Slots[s.State.Thunder[1]].Position;
        if(error=="liquid" && t>27 && w.EventObjects.Count>8) w.Party.Slots[0].Position=w.EventObjects[8].Config.Placement.Position;
        if(error=="altar" && t>28.8f && t<29.3f) w.Party.Slots[0].Position=s.State.SpreadPosition(s.State.Altar);
        SimCharacter.Time=t;
        w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,t);
    }
    var expected=error switch{"mercy"=>"八方劍","dive"=>"龍衝","moon"=>"月環","lightning"=>"雷光鏈","liquid"=>"蒼天火液",_=>"聖壇火光"};
    if(error=="lightning") expected="雷光鏈";
    Check(SimCharacter.Failures.Any(f=>f.Contains(expected)),"missing followup failure "+error);
}
Console.WriteLine("PASS: full Wrath at 3 frame rates; all followup failure checks, deferred dragon spawning, green/thunder combinations and actor cleanup.");
{
    var s=new DsrP5WrathScenario();s.UseSeed(7);var w=new SimWorld();SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=29*60;f++)
    {
        var t=f/60f;SimCharacter.Time=t;
        w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);
        if(t>26.32f && w.EventObjects.Count>8)w.Party.Get(s.State.Liquid)!.Position=w.EventObjects[8].Config.Placement.Position;
        s.Tick(1f/60,t);
        if(t<28.36f)Check(!SimCharacter.Failures.Any(f=>f.Contains("蒼天火液")),"ground hit during landing grace period");
    }
    Check(SimCharacter.Failures.Any(f=>f.Contains("蒼天火液")),"standing in active liquid must fail");
}
Console.WriteLine("PASS: log cast intervals, delayed landing, vulnerability refresh/expiry and ground activation grace.");
for(var first=0;first<5;first++)
for(var second=0;second<4;second++)
for(var role=0;role<8;role++)
for(var seed=0;seed<100;seed++)
{
    if(first!=0&&second==2)continue;
    var s=new DsrP5WrathState(seed,role,first,second);
    Check(first switch {0=>true,1=>s.Blue==role,2=>s.TetherRoles[0]==role,3=>s.TetherRoles[1]==role,4=>s.Green==role,_=>false},"first group assignment missing");
    Check(second switch {0=>true,1=>s.Thunder.Contains(role),2=>s.Liquid==role,3=>s.Altar==role,_=>false},"second group assignment missing");
    Check(new[]{s.Blue}.Concat(s.TetherRoles).Concat(s.EastRoles).Distinct().Count()==8,"duplicate initial role");
    Check(s.EastRoles.Contains(s.Green)&&s.EastRoles.Contains(s.Liquid)&&s.Green!=s.Liquid,"invalid east assignments");
    Check(!s.Thunder.Contains(s.Liquid)&&!s.Thunder.Contains(s.Altar),"incompatible thunder assignment");
    var repeated=new DsrP5WrathState(seed,role,first,second);
    Check(s.Blue==repeated.Blue&&s.Green==repeated.Green&&s.Liquid==repeated.Liquid&&s.Thunder.SequenceEqual(repeated.Thunder),"seed must reproduce target assignments");
}
Console.WriteLine("PASS: both assignment groups for all 8 player roles and valid combinations.");
foreach(var fps in new[]{30,60,144})
{
    var s=new DsrP5WrathScenario();var w=new SimWorld();s.UseSeed(0);s.Run(w,0);s.PrepareContactCheck(w);
    SimCharacter.Failures.Clear();
    for(var f=0;f<fps/2;f++)s.ContactTick(1f/fps);
    Check(SimCharacter.Failures.Count==0,"overlapping circles accelerated contact damage");
    w.Party.Slots[0].Position=new(30,0,0);s.ContactTick(1f/fps);
    w.Party.Slots[0].Position=Vector3.Zero;
    for(var f=0;f<fps*.6f;f++)s.ContactTick(1f/fps);
    Check(SimCharacter.Failures.Count==0,"leaving fire did not reset contact grace");
    for(var f=0;f<fps*.5f;f++)s.ContactTick(1f/fps);
    Check(SimCharacter.Failures.Any(x=>x.Contains("蒼天火液")),"continuous fire exposure did not fail");
    SimCharacter.Failures.Clear();s.State.Time=8;s.ContactTick(1f/fps);
    Check(SimCharacter.Failures.Count==0,"expired fire still caused damage");
}
Console.WriteLine("PASS: short contact, overlapping fires, exit/reentry, continuous contact and expiry at 30/60/144 FPS.");
namespace AnoMech.Scenarios.Dsr.P5Wrath
{
    public sealed partial class DsrP5WrathScenario
    {
        internal DsrP5WrathState State=>state!;
        internal void PrepareContactCheck(SimWorld simWorld)
        {
            state!.Time=2;groundHazards.Clear();Array.Clear(liquidContact);
            groundHazards.Add((Vector3.Zero,0,true));groundHazards.Add((Vector3.Zero,0,true));
            for(var r=0;r<8;r++)simWorld.Party.Slots[r].Position=r==0?Vector3.Zero:new(30,0,0);
        }
        internal void ContactTick(float dt)=>CheckGround(dt);
        internal void UseSeed(int value){validationSeed=value;}
        internal void UsePracticeTarget(int first, int second){practiceTarget=second==2?0:first;followupTarget=second;}
    }
}
