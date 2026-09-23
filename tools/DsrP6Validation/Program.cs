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
Vector3[]? fixedRolePositions=null;
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
  if(time>3&&time<9.3f)
  {
   fixedRolePositions??=s.State.Destinations.Skip(2).ToArray();
   Check(s.State.Destinations.Skip(2).SequenceEqual(fixedRolePositions),"fixed-role destinations must not depend on tether colours or glow branch");
  }
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
foreach(var z in new[]{2f,3.8f,4.2f,6f,-2f})
{
 var s=new DsrP6DragonsScenario(DsrP6Section.Wings2);var w=new SimWorld();
 w.Party.Slots[2]=new SimPlayer{Role=2,Position=new(15,0,z)};
 SimCharacter.Failures.Clear();s.Run(w,0);
 for(var frame=1;frame<=11.8f*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
 }
 Check(s.State.Failed==(z<0||z>=4),$"second wings narrow south safe strip at Z={z}");
}
Console.WriteLine("PASS: second wings accepts the inner safe strip and rejects old Z=6 position and wrong half.");
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
 Check(w.Party.Slots.All(m=>!m.LockonVfx.Contains(62)),"no stack markers in any P6 range");
}
var invalidRange=false;
try{new DsrP6DragonsScenario().SetRange(DsrP6Section.Breath2,DsrP6Section.Breath1);}catch(ArgumentOutOfRangeException){invalidRange=true;}
Check(invalidRange,"reversed range rejected");
Console.WriteLine("PASS: all 15 start/end windows, targetable dragons, no stack markers, nine fireballs, native cross casts and cleanup.");
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
{
 var s=new DsrP6DragonsScenario(DsrP6Section.Breath1);s.UseSeed(0);var w=new SimWorld();s.Run(w,0);
 for(var frame=1;frame<=16*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
  if(time<14.8f)Check(w.Party.Slots.All(m=>!m.HasStatus(2898)&&!m.HasStatus(2899)),"tethers must not apply breath statuses before impact");
  if(time>15)Check(w.Party.Slots.All(m=>!m.HasStatus(2898)&&!m.HasStatus(2899)),"opposite breaths clear first-breath statuses");
 }
}
{
 var s=new DsrP6DragonsScenario(DsrP6Section.Wroth);s.UseSeed(0);var w=new SimWorld();s.Run(w,0);
 var black=w.Enemies[0];var white=w.Enemies[1];
 for(var frame=1;frame<=21*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
  Check(black.Visible&&black.Targetable&&black.Position==DsrP6DragonsState.Nidhogg&&black.Departures.Count==0,"black dragon stays attackable at home through Wroth");
  if(time<5.17f)Check(white.Position==DsrP6DragonsState.Hraesvelgr&&white.Targetable,"white dragon waits for Wroth debuffs");
  if(white.Departures.Count>0)Check(s.State.FlamesAssigned,"Wroth debuffs precede departure");
  if(time>5.2f&&time<15.49f)Check(!white.Targetable,"white dragon untargetable during departure and dive");
  if(time>15.6f)Check(white.Visible&&white.Targetable&&white.Position==DsrP6DragonsState.Hraesvelgr,"white dragon returns attackable after dive");
  if(time>14.5f&&time<15.7f)Check(w.EventObjects.Count==0,"first ground puddle must wait until after native hit effects");
  if(time>15.8f&&time<17.3f)
  {
   Check(w.EventObjects.Count==1,"first puddle appears after damage");
   Check(Vector3.Distance(w.EventObjects[0].Config.Placement.Position,s.State.Wroth.Start)<.1f,"puddle preserves stack snapshot instead of following moving target");
  }
 }
 Check(black.Casts.Count(c=>c.Action is 27974 or 27975)==4,"four black dragon stack hits");
 Check(white.Departures.Count==1&&white.Casts.Count(c=>c.Action==27967)==1,"only white dragon departs and dives once");
}
Console.WriteLine("PASS: first-breath status lifecycle and white-only Wroth departure, dive, return and targetability.");
{
 var s=new DsrP6DragonsScenario();s.UseSeed(0);var w=new SimWorld();s.Run(w,0);
 for(var frame=1;frame<=51*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
  if(time>22.1f&&time<22.5f)
  {
   Check(s.State.VowOwner>=4&&s.State.VowOwner<=7,"first poison selects a DPS");
   Check(w.Enemies[0].Casts.Count(c=>c.Action==27952)==1,"Nidhogg itself plays Mortal Vow");
   Check(w.Enemies.Skip(1).All(e=>e.Casts.All(c=>c.Action!=27952)),"no invisible helper for dragon poison animation");
  }
 }
 Check(w.Enemies[0].Position==DsrP6DragonsState.Nidhogg&&w.Enemies[0].Visible&&w.Enemies[0].Targetable&&w.Enemies[0].Entrances.Count==1,"Nidhogg returns from hidden dive with entrance animation");
}
Console.WriteLine("PASS: native Nidhogg poison source and post-tankbuster return.");
{
 var s=new DsrP6DragonsScenario(DsrP6Section.Breath1);s.UseSeed(0);var w=new SimWorld();
 var player=new SimPlayer{Role=2,Position=new(-14,0,-12)};w.Party.Slots[2]=player;s.Run(w,0);
 for(var frame=1;frame<=16*60;frame++)
 {
  var time=frame/60f;SimCharacter.Time=time;
  w.Events.Tick(1f/60);foreach(var member in w.Party.Slots)member.Advance(1f/60);s.Tick(1f/60,time);
  if(time<14.8f)Check(!player.HasStatus(2898)&&!player.HasStatus(2899),"isolated fire target has no early status");
  if(time>14.9f)Check(player.HasStatus(2898)&&!player.HasStatus(2899),"single fire hit applies Boiling only on impact");
 }
}
Console.WriteLine("PASS: isolated first-breath target receives its status only at impact.");
for(var variant=0;variant<24;variant++)foreach(var fps in new[]{30,60,144})
{
 var s=new DsrP6DragonsScenario(DsrP6Section.Wroth);s.UseSeed(variant);var w=new SimWorld();
 SimCharacter.Failures.Clear();s.Run(w,0);s.State.Wroth=new(variant);
 var pattern=s.State.Wroth;var before=new Vector3[8];
 for(var frame=1;frame<=46*fps;frame++)
 {
  var time=frame/(float)fps;SimCharacter.Time=time;
  for(var r=0;r<8;r++)before[r]=w.Party.Slots[r].Position;
  w.Events.Tick(1f/fps);foreach(var member in w.Party.Slots)member.Advance(1f/fps);s.Tick(1f/fps,time);
  foreach(var member in w.Party.Slots)
   Check(MathF.Abs(member.Position.X)<=21&&MathF.Abs(member.Position.Z)<=21,"all Wroth routes stay in square arena, including corners");
  if(time>14.5f&&time<15.3f)
   for(var r=0;r<8;r++)Check(Vector3.Distance(before[r],w.Party.Slots[r].Position)<.001f,"wait for native Akh Morn impact before stepping");
 }
 Check(SimCharacter.Failures.Count==0,$"Wroth variant {variant}/{fps}: {string.Join(";",SimCharacter.Failures.Take(4))}");
 Check(w.EventObjects.Count==4,"four native puddles per configuration");
 var p=w.EventObjects.Select(o=>o.Config.Placement.Position).ToArray();
 Check(MathF.Abs(p[0].Z-p[1].Z)<.01f&&MathF.Abs(p[1].Z-p[2].Z)<.01f,"first three drops run horizontally");
 Check(MathF.Abs(p[2].Z-p[3].Z)>5,"last drop turns inward for L route");
 Check(pattern.Fireballs(1).All(p=>MathF.Sign(p.X)==pattern.SecondX&&MathF.Sign(p.Z)==pattern.SecondZ),"second-wave quadrant");
 Check(pattern.Fireballs(2).All(p=>MathF.Sign(p.X)==-pattern.SecondX&&MathF.Sign(p.Z)==-pattern.SecondZ),"third wave is diagonally opposite");
 Check(w.Enemies[1].Casts.Count(c=>c.Action==27967)==1,"one white dragon dive for each variant");
}
Console.WriteLine("PASS: all 24 Wroth configurations at 30/60/144 FPS, cue-gated movement, L routes, four puddles, square bounds and outcomes.");
namespace AnoMech.Scenarios.Dsr.P6Dragons
{
 public sealed partial class DsrP6DragonsScenario
 {
  internal DsrP6DragonsState State=>state!;
  internal void UseSeed(int seed)=>validationSeed=seed;
 }
}
