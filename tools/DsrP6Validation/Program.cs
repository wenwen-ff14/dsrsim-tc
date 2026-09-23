using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P6Dragons;
using AnoMech.Scenarios.Dsr.P5Death;
using AnoMech.Core.Game;
void Check(bool condition,string message){if(!condition)throw new Exception(message);}
var scheduler=new EventScheduler();var fired=new List<int>();
scheduler.Tick(20);
scheduler.Add(4,()=>fired.Add(4));scheduler.Add(5,()=>fired.Add(5));
scheduler.Add(7,()=>fired.Add(7));scheduler.Add(7,()=>fired.Add(70));scheduler.Add(8,()=>fired.Add(8));
scheduler.SelectWindow(5,7);scheduler.Tick(0);
Check(fired.SequenceEqual(new[]{5}),"selected window start must fire at current time");
scheduler.Tick(1.9f);Check(fired.Count==1,"window must preserve relative delay");
scheduler.Tick(.11f);Check(fired.SequenceEqual(new[]{5,7,70})&&scheduler.IsEmpty,"inclusive end, stable event order, excluded handlers");
Console.WriteLine("PASS: scheduler windows preserve order and boundaries after a nonzero clock origin.");
for(var role=0;role<8;role++)for(var preference=1;preference<=2;preference++)for(var seed=0;seed<100;seed++)
{
 var s=new DsrP5DeathState(seed,role,preference);
 Check(s.HasDoom(role)==(preference==1),"doom preference");
 Check(s.Dooms.Length==4&&s.Clean.Length==4&&s.Dooms.Concat(s.Clean).Distinct().Count()==8,"doom partition");
}
Console.WriteLine("PASS: all eight roles, both doom preferences, 100 seeds preserve four/four grouping.");
foreach(var section in new[]{DsrP6Section.Breath1,DsrP6Section.Wings1,DsrP6Section.Wings2,DsrP6Section.Breath2,DsrP6Section.Wroth,DsrP6Section.Full})
foreach(var fps in new[]{30,60,144})for(var seed=0;seed<20;seed++)
{
 var s=new DsrP6DragonsScenario(section);s.UseSeed(seed);var w=new SimWorld();SimCharacter.Failures.Clear();s.Run(w,0);
 for(var frame=1;frame<=180*fps;frame++)
 {
  var time=frame/(float)fps;SimCharacter.Time=time;var before=w.Party.Slots.Select(m=>m.Position).ToArray();
  w.Events.Tick(1f/fps);foreach(var member in w.Party.Slots)member.Advance(1f/fps);s.Tick(1f/fps,time);
  for(var r=0;r<8;r++)Check(Vector3.Distance(before[r],w.Party.Slots[r].Position)<=6f/fps+.002f,"NPC teleport");
 }
 Check(SimCharacter.Failures.Count==0,$"{section} {seed} {fps}: {string.Join(";",SimCharacter.Failures.Take(8))}");
 Check(s.State.Complete&&w.Events.IsEmpty&&w.Enemies.All(e=>!e.Active),"completion and actor cleanup");
}
Console.WriteLine("PASS: all five P6 sections and full sequence, 20 seeds at 30/60/144 FPS, walking NPCs, outcomes and cleanup.");
foreach(var section in new[]{DsrP6Section.Breath1,DsrP6Section.Wings1,DsrP6Section.Wings2,DsrP6Section.Breath2,DsrP6Section.Wroth,DsrP6Section.Full})
for(var role=0;role<8;role++)
{
 var s=new DsrP6DragonsScenario(section);s.UseSeed(role*11);var w=new SimWorld();
 w.Party.Slots[role]=new SimPlayer{Role=role,Position=new(0,0,16)};
 SimCharacter.Failures.Clear();s.Run(w,0);
 for(var frame=1;frame<=180*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;var player=w.Party.Slots[role];
  player.MoveTo(s.State.Destinations[role]);var commands=player.MoveCommands;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
  Check(player.MoveCommands==commands,"scenario must not move the real player");
 }
 Check(SimCharacter.Failures.Count==0,$"{section} scripted role {role}: {string.Join(";",SimCharacter.Failures.Take(5))}");
}
Console.WriteLine("PASS: all eight player roles can follow each section and the full sequence without NPC control of the player.");
foreach(var section in new[]{DsrP6Section.Breath1,DsrP6Section.Wings1,DsrP6Section.Wings2,DsrP6Section.Breath2,DsrP6Section.Wroth,DsrP6Section.Full})
{
 var s=new DsrP6DragonsScenario(section);s.UseSeed(7);var w=new SimWorld();
 w.Party.Slots[2]=new SimPlayer{Role=2,Position=Vector3.Zero};SimCharacter.Failures.Clear();s.Run(w,0);
 for(var frame=1;frame<=180*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
 }
 Check(s.State.Failed&&SimCharacter.Failures.Count>0,$"{section} stationary player must fail");
}
foreach(var acting in new[]{false,true})
{
 var s=new DsrP6DragonsScenario(DsrP6Section.Breath2);s.UseSeed(0);var w=new SimWorld();
 var player=new SimPlayer{Role=2,Position=new(0,0,16)};w.Party.Slots[2]=player;
 SimCharacter.Failures.Clear();s.Run(w,0);var sawThermal=false;
 s.State.SecondFire[2]=true;s.State.SecondFire[3]=false;
 for(var r=4;r<8;r++)s.State.SecondFire[r]=r<6;
 for(var frame=1;frame<=180*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;player.MoveTo(s.State.Destinations[2]);
  player.IsActing=acting&&time>20.4f&&time<21;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
  if(time>20.4f&&time<21)
  {
   sawThermal=true;Check(player.HasStatus(960)&&w.Party.Slots[3].HasStatus(3480),"native Pyretic and Deep Freeze states");
  }
  if(time>22.9f)Check(!player.HasStatus(960)&&!w.Party.Slots[3].HasStatus(3480),"dive clears thermal states");
 }
 Check(sawThermal&&s.State.Failed==acting,"Pyretic must punish actions only while active");
}
Console.WriteLine("PASS: native thermal conversion, action penalty and post-dive removal.");
var fixedCases=0;
foreach(var glow in Enum.GetValues<DsrP6Glow>())
for(var mask=0;mask<64;mask++)
{
 if(System.Numerics.BitOperations.PopCount((uint)mask)!=3)continue;
 var s=new DsrP6DragonsScenario(DsrP6Section.Breath2);s.UseSeed(0);var w=new SimWorld();
 SimCharacter.Failures.Clear();s.Run(w,0);s.State.SecondGlow=glow;
 for(var r=2;r<8;r++)s.State.SecondFire[r]=(mask&(1<<(r-2)))!=0;
 for(var frame=1;frame<=180*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
 }
 Check(SimCharacter.Failures.Count==0,$"fixed {glow} {mask}: {string.Join(";",SimCharacter.Failures.Take(5))}");
 var actions=w.Enemies.SelectMany(e=>e.Casts).Select(c=>c.Action).ToArray();
 Check(actions.Contains(glow==DsrP6Glow.Hraesvelgr?27954u:27955u)&&actions.Contains(glow==DsrP6Glow.Nidhogg?27956u:27957u),"glow cast animations");
 Check(glow==DsrP6Glow.Both?actions.Contains(27961u)&&actions.Contains(27962u)&&!actions.Contains(27965u):actions.Contains(27965u)&&actions.Contains(glow==DsrP6Glow.Nidhogg?27963u:27964u),"tank branch native actions");
 Check(s.State.Complete,"fixed strat completion");fixedCases++;
}
Console.WriteLine($"PASS: {fixedCases} fixed-strat runs cover all 20 fire/ice assignments and three glow branches.");
foreach(var glow in Enum.GetValues<DsrP6Glow>())
{
 var s=new DsrP6DragonsScenario(DsrP6Section.Breath2);s.UseSeed(0);var w=new SimWorld();
 var role=glow==DsrP6Glow.Nidhogg?1:0;
 w.Party.Slots[role]=new SimPlayer{Role=role,Position=glow==DsrP6Glow.Both?new(-14.8f,0,-12.7f):Vector3.Zero};
 SimCharacter.Failures.Clear();s.Run(w,0);s.State.SecondGlow=glow;
 for(var frame=1;frame<=10*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
 }
 Check(s.State.Failed,$"{glow}: incorrect tank stack/spread must fail");
}
Console.WriteLine("PASS: split tanks fail the double-glow stack; central solo tank cleaves fail both single-glow branches.");
for(var first=1;first<=5;first++)for(var last=first;last<=5;last++)
{
 var s=new DsrP6DragonsScenario();s.SetRange((DsrP6Section)first,(DsrP6Section)last);
 s.UseSeed(first*10+last);var w=new SimWorld();SimCharacter.Failures.Clear();s.Run(w,0);
 Check(w.Enemies.Take(2).All(e=>e.Targetable),"dragons targetable on entry");
 for(var frame=1;frame<=180*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
 }
 Check(s.State.Complete&&w.Events.IsEmpty&&w.Enemies.All(e=>!e.Active)&&w.EventObjects.All(e=>!e.Active),$"range {first}-{last}: cleanup");
 Check(SimCharacter.Failures.Count==0,$"range {first}-{last}: {string.Join(";",SimCharacter.Failures.Take(4))}");
 var hasWroth=first<=3&&last>=3;
 Check(w.Enemies.Count(e=>e.BNpcBaseId==0x33B6)==(hasWroth?9:0),"nine fireballs only in selected Wroth window");
 Check(w.Enemies.SelectMany(e=>e.Casts).Count(c=>c.Action==26409)==(hasWroth?9:0),"three waves of native cross casts");
 if(first==1||hasWroth)Check(w.Party.Slots[2].LockonVfx.Contains(62)&&w.Party.Slots[3].LockonVfx.Contains(62),"healer stack markers");
}
var invalidRange=false;
try{new DsrP6DragonsScenario().SetRange(DsrP6Section.Breath2,DsrP6Section.Breath1);}catch(ArgumentOutOfRangeException){invalidRange=true;}
Check(invalidRange,"reversed range rejected");
Console.WriteLine("PASS: all 15 start/end windows, targetable dragons, healer stack markers, nine fireballs, native cross casts and cleanup.");
{
 var s=new DsrP6DragonsScenario(DsrP6Section.Wroth);s.UseSeed(0);var w=new SimWorld();
 var player=new SimPlayer{Role=2,Position=new(0,0,16)};w.Party.Slots[2]=player;
 SimCharacter.Failures.Clear();s.Run(w,0);
 for(var frame=1;frame<=19*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  player.MoveTo(s.State.Destinations[2]);
  if(time>17.7f&&time<17.9f)player.SetPosition(new(0,0,13));
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
 }
 Check(SimCharacter.Failures.Any(f=>f.Contains("第 1 組烈焰十字爆")),"first cross must hit its vertical lane");
}
Console.WriteLine("PASS: entering a fireball cross lane fails at its scheduled explosion.");
namespace AnoMech.Scenarios.Dsr.P6Dragons
{
 public sealed partial class DsrP6DragonsScenario
 {
  internal DsrP6DragonsState State=>state!;
  internal void UseSeed(int seed)=>validationSeed=seed;
 }
}
