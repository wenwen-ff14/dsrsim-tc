using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P5Death;
void Check(bool value,string message){if(!value)throw new Exception(message);}
foreach(var fps in new[]{30,60,144})
for(var seed=0;seed<100;seed++)
{
    var s=new DsrP5DeathScenario();s.UseSeed(seed);var w=new SimWorld();SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=43*fps;f++)
    {
        var t=f/(float)fps;SimCharacter.Time=t;
        var before=w.Party.Slots.Select(m=>m.Position).ToArray();
        w.Events.Tick(1f/fps);foreach(var m in w.Party.Slots)m.Advance(1f/fps);s.Tick(1f/fps,t);
        if(t<37.95f||t>38.8f)for(var r=0;r<8;r++)Check(Vector3.Distance(before[r],w.Party.Slots[r].Position)<=6f/fps+.001f,"NPC teleport");
    }
    Check(SimCharacter.Failures.Count==0,$"seed={seed} fps={fps}: {string.Join(";",SimCharacter.Failures.Take(10))}");
    Check(s.State.Complete&&w.Events.IsEmpty,"incomplete timeline");
    Check(s.State.Dooms.All(r=>s.State.Cleansed[r]),"uncleansed doom");
    Check(w.Party.Slots.All(m=>!m.HasStatus(2976)&&!m.HasStatus(769)),"status cleanup");
    Check(w.Enemies.All(e=>!e.Active)&&w.EventObjects.Count==12&&w.EventObjects.All(o=>!o.Active),"actor cleanup");
    Check(Enumerable.Range(281,4).All(id=>s.State.Symbols.Count(v=>v==id)==2),"PS pairs");
}
Console.WriteLine("PASS: Death of the Heavens 100 seeds at 30/60/144 FPS; NPC routes, rings, dives, spreads, twisters, gaze, knockback, chains, doom cleanse and cleanup.");
for(var role=0;role<8;role++)
{
    var s=new DsrP5DeathScenario();s.UseSeed(role*11);var w=new SimWorld();w.Party.Slots[role]=new SimPlayer{Role=role,Position=new(0,0,16)};SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=43*60;f++)
    {
        var t=f/60f;SimCharacter.Time=t;var member=w.Party.Slots[role];
        if(!s.State.Knocked||t>=38.7f)
        {
            var target=DsrP5DeathAi.Destination(s.State,role);
            member.MoveTo(target,6,s.State.SymbolsAssigned&&!s.State.Knocked?s.State.SafeFacing(target):null);
        }
        var commands=member.MoveCommands;
        w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,t);
        Check(member.MoveCommands==commands,"scenario moved player");
    }
    Check(SimCharacter.Failures.Count==0,$"scripted player {role}: {string.Join(";",SimCharacter.Failures.Take(3))}");
}
foreach(var failure in new[]{"ring","charge","spread","twister","gaze","wall","flame","doom","chain"})
{
    var s=new DsrP5DeathScenario();s.UseSeed(7);var w=new SimWorld();SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=43*60;f++)
    {
        var t=f/60f;SimCharacter.Time=t;
        if(failure=="ring"&&t>22.1f&&t<22.3f)w.Party.Slots[0].Position=s.State.Hammer;
        if(failure=="charge"&&t>24&&t<24.1f)w.Party.Slots[0].Position=Vector3.Zero;
        if(failure=="spread"&&t>24.2f&&t<24.3f)w.Party.Slots[0].Position=w.Party.Slots[1].Position;
        if(failure=="twister"&&t>25.2f&&t<26)w.Party.Slots[0].Position=s.State.SpreadSnapshots[0];
        if(failure=="gaze"&&t>37.3f&&t<37.5f)w.Party.Slots[0].Face(s.State.Boss);
        if(failure=="wall"&&t>37.9f&&t<38)w.Party.Slots[0].Position=new(15,0,0);
        if(failure=="flame"&&t>39.7f&&t<40)w.Party.Slots[0].Position=w.Party.Slots[1].Position;
        if(failure=="doom"&&t>37.9f)w.Party.Slots[s.State.Dooms[0]].Position=s.State.Rotate(new(0,0,-19));
        if(failure=="chain"&&t>37.9f)foreach(var m in w.Party.Slots)m.Position=new(2,0,0);
        w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,t);
    }
    var message=failure switch{"ring"=>"沉重衝擊","charge"=>"死刻衝鋒","spread"=>"百雷","twister"=>"旋風","gaze"=>"雙視線","wall"=>"場外","flame"=>"天火","doom"=>"死亡宣告",_=>"烈焰鏈"};
    Check(SimCharacter.Failures.Any(x=>x.Contains(message)),"missing failure: "+failure);
}
Console.WriteLine("PASS: all 8 scripted player roles; player ownership; nine mechanic failure cases.");
namespace AnoMech.Scenarios.Dsr.P5Death
{
    public sealed partial class DsrP5DeathScenario
    {
        internal DsrP5DeathState State=>state!;
        internal void UseSeed(int value)=>validationSeed=value;
    }
}
