using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P5Death;
void Check(bool value,string message){if(!value)throw new Exception(message);}
foreach(var fps in new[]{30,60,144})
for(var seed=0;seed<100;seed++)
{
    var s=new DsrP5DeathScenario();s.UseSeed(seed);var w=new SimWorld();SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=59*fps;f++)
    {
        var t=f/(float)fps;SimCharacter.Time=t;
        var before=w.Party.Slots.Select(m=>m.Position).ToArray();
        w.Events.Tick(1f/fps);foreach(var m in w.Party.Slots)m.Advance(1f/fps);s.Tick(1f/fps,t);
        if(t<11.6f)for(var r=0;r<8;r++)Check(before[r]==w.Party.Slots[r].Position,"NPC moved before knights appeared");
        if(t>=24.5f&&t<42.2f)
        {
            var knight=w.Enemies.Single(e=>e.BNpcBaseId==0x313A);
            Check(knight.Active&&knight.Visible&&knight.Position==Vector3.Zero,"Grinnaux enters center immediately after charges and stays until meteor cleanup");
            if(t<33.357f)Check(!knight.Casts.Any(c=>c.Action==25308),"early arrival must not shorten PS preparation");
        }
        if(t<37.95f||t>38.8f)for(var r=0;r<8;r++)Check(Vector3.Distance(before[r],w.Party.Slots[r].Position)<=6f/fps+.001f,"NPC teleport");
        if(t>=25.3f&&t<27.35f)
            foreach(var role in new[]{s.State.Clean[0],s.State.Clean[3]})
                foreach(var doom in s.State.Dooms)
                {
                    var start=before[role];var end=w.Party.Slots[role].Position;
                    var segment=end-start;var length=segment.LengthSquared();
                    var point=s.State.SpreadSnapshots[doom];
                    var fraction=length<.000001f?0:Math.Clamp(Vector3.Dot(point-start,segment)/length,0,1);
                    Check(Vector3.Distance(start+segment*fraction,point)>=4,$"outer non-doom route needs clearance from doom twister: seed={seed}, role={role}, time={t:F3}");
                }
    }
    Check(SimCharacter.Failures.Count==0,$"seed={seed} fps={fps}: {string.Join(";",SimCharacter.Failures.Take(10))}");
    Check(!s.State.Complete&&s.State.MeteorsActive&&w.Events.IsEmpty,"incomplete timeline");
    Check(!s.State.LimitBreakUsed&&!s.State.MeteorDestroyed.Any(x=>x),"NPC meteor clear");
    Check(s.State.MeteorPositions.All(p=>MathF.Abs(p.Length()-13)<.001f),"meteor ring radius");
    Check(MathF.Abs(s.State.Boss.Length()-23)<.001f,"Thordan outside arena");
    Check(s.State.MeteorPositions.Count(p=>Vector3.Distance(p,s.State.MeteorPositions[0])<=10)==3,"LB2 must cover three adjacent meteors");
    Check(s.State.MeteorPositions.All(p=>p.Length()>10),"LB2 centered in arena must not clear the meteor ring");
    Check(w.Enemies.SelectMany(e=>e.Casts).Count(c=>c.Action==204)==0&&!w.Enemies.SelectMany(e=>e.Casts).Any(c=>c.Action==205),"native LB2 action, not LB3");
    Check(!w.Enemies.Any(e=>e.Vfx.Any(v=>v.Path=="vfx/common/eff/mon_eisyo01et.avfx")),"no extra Thordan eye VFX");
    Check(!w.Enemies.SelectMany(e=>e.Casts).Any(c=>c.Action is 25552 or 25553),"no Thordan eye cast or release effect");
    Check(w.Enemies.SelectMany(e=>e.Casts).Count(c=>c.Action==27540)==5,"Deathstorm cast plus four native target hits");
    Check(w.Party.Slots.All(m=>m.LockonVfx.Contains(s.State.Symbols[m.Role])),"native Playstation markers");
    Check(s.State.Dooms.All(r=>s.State.Cleansed[r]),"uncleansed doom");
    Check(w.Party.Slots.All(m=>!m.HasStatus(2976)&&!m.HasStatus(769)),"status cleanup");
    Check(w.Enemies.Count(e=>e.Active)==8&&w.EventObjects.Count==12&&w.EventObjects.All(o=>!o.Active),"actor cleanup");
    var hammer=w.Enemies.Single(e=>e.BNpcBaseId==0x315D);
    Check(!hammer.Casts.Any(c=>c.Action==25557)&&hammer.Vfx.Count==0,"additional hammer presentation removed");
    Check(hammer.Casts.Select(c=>c.Action).SequenceEqual(new uint[]{25558,25559,25560,25561,25562}),"original five ring actions preserved");
    float[] impactTimes=[22.179f,24.100f,25.978f,27.860f,29.738f];
    for(var ring=0;ring<5;ring++)
    {
        var cast=hammer.Casts[ring];
        var release=cast.Time+(cast.Duration??0)+cast.FireDelay;
        Check(MathF.Abs(release-impactTimes[ring])<=1f/fps+.002f,"wave release matches ring damage time");
    }
    foreach(var entry in new[]{(Npc:0x313Au,Action:25308u,Start:33.357f,Duration:3.7f),(Npc:0x313Bu,Action:25310u,Start:32.239f,Duration:6.7f)})
    {
        var knight=w.Enemies.Single(e=>e.BNpcBaseId==entry.Npc);
        var cast=knight.Casts.Single(c=>c.Action==entry.Action);
        Check(MathF.Abs(cast.Time-entry.Start)<=1f/fps+.001f&&cast.Duration==entry.Duration,"native knight cast timing preserved");
        Check(knight.CastTargets.Single(c=>c.Action==entry.Action).Target==knight.GameObjectId,"knight native release has a valid self target");
        Check(knight.Vfx.Count(v=>v.Path=="vfx/common/eff/mon_eisyo03t.avfx")==1,"knight charging VFX added once");
        Check(knight.Entrances.Count==1&&!knight.Active,"knight entrance and cleanup");
    }
    Check(Enumerable.Range(281,4).All(id=>s.State.Symbols.Count(v=>v==id)==2),"PS pairs");
}
Console.WriteLine("PASS: Death of the Heavens 100 seeds at 30/60/144 FPS; NPC routes, rings, dives, spreads, twisters, gaze, knockback, chains, doom cleanse and cleanup.");
Console.WriteLine("PASS: first/last non-doom paths keep at least 4y from doom twister snapshots, including between-frame movement segments.");
for(var role=0;role<8;role++)
{
    var s=new DsrP5DeathScenario();s.UseSeed(role*11);var w=new SimWorld();w.Party.Slots[role]=new SimPlayer{Role=role,Position=new(0,0,16)};SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=59*60;f++)
    {
        var t=f/60f;SimCharacter.Time=t;var member=w.Party.Slots[role];
        if(!s.State.Knocked||t>=38.7f)
        {
            var target=DsrP5DeathAi.Destination(s.State,role);
            member.MoveTo(target,6,s.State.SymbolsAssigned&&!s.State.Knocked?s.State.SafeFacing(target):null);
        }
        if(role==7&&t>46&&!s.State.LimitBreakUsed&&!s.State.LimitBreakCasting)s.TryLimitBreak(s.State.MeteorPositions[0]);
        var commands=member.MoveCommands;
        w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,t);
        Check(member.MoveCommands==commands,"scenario moved player");
    }
    Check(SimCharacter.Failures.Count==0,$"scripted player {role}: {string.Join(";",SimCharacter.Failures.Take(3))}");
}
foreach(var failure in new[]{"ring","charge","spread","twister","gaze","wall","flame","doom","chain"})
{
    var s=new DsrP5DeathScenario();s.UseSeed(7);var w=new SimWorld();SimCharacter.Failures.Clear();s.Run(w,0);
    for(var f=1;f<=59*60;f++)
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
foreach(var mode in new[]{"correct","miss","interrupt","retry","unused","late"})
{
    var s=new DsrP5DeathScenario();s.UseSeed(12);var w=new SimWorld();
    var player=new SimPlayer{Role=7,Position=new(0,0,16)};w.Party.Slots[7]=player;
    SimCharacter.Failures.Clear();s.Run(w,0);var started=false;var interrupted=false;
    for(var f=1;f<=59*60;f++)
    {
        var t=f/60f;SimCharacter.Time=t;
        if(!s.State.Knocked||t>=38.7f)
            player.MoveTo(DsrP5DeathAi.Destination(s.State,7),6,s.State.SymbolsAssigned&&!s.State.Knocked?s.State.SafeFacing(player.Position):null);
        if(mode!="unused"&&!started&&t>=(mode=="late"?55:46))
        {
            Check(!s.TryLimitBreak(new(100,0,100)),"out-of-range LB accepted");
            Check(s.TryLimitBreak(s.State.MeteorPositions[mode=="miss"?4:0]),"LB not started");started=true;
            Check(!s.TryLimitBreak(s.State.MeteorPositions[0]),"duplicate cast accepted");
        }
        if((mode=="interrupt"||mode=="retry")&&!interrupted&&t>46.5f)
        {player.Position+=new Vector3(1,0,0);interrupted=true;}
        if(mode=="retry"&&interrupted&&t>48&&!s.State.LimitBreakCasting&&!s.State.LimitBreakUsed)
            Check(s.TryLimitBreak(s.State.MeteorPositions[0]),"retry rejected");
        w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,t);
    }

    var destroyed=mode is "interrupt" or "unused"?0:3;
    Check(s.State.MeteorDestroyed.Count(x=>x)==destroyed,"meteor result: "+mode);
    Check(!SimCharacter.Failures.Any(x=>x.Contains("隕石未擊破")),"meteor failure: "+mode);
    Check(!s.State.Complete&&w.Enemies.Count(e=>e.Active)==8-destroyed,"LB cleanup: "+mode);
}
Console.WriteLine("PASS: manual D4 LB hit/miss, movement interruption/retry, no cast, late cast, range and duplicate-cast checks.");
{
    var s=new DsrP5DeathScenario();s.UseSeed(4);var w=new SimWorld();s.Run(w,0);
    for(var f=1;f<=2050;f++)
    {w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,f/60f);}
    var role=s.State.Dooms[0];var member=w.Party.Slots[role];
    member.Position=s.State.CleansePositions[0]+new Vector3(1.2f,0,0);s.Tick(0,34.2f);
    Check(!s.State.Cleansed[role]&&member.HasStatus(2976),"cleansed outside small white circle");
    member.Position=s.State.CleansePositions[0]+new Vector3(.8f,0,0);s.Tick(0,34.2f);
    Check(s.State.Cleansed[role]&&!member.HasStatus(2976),"did not cleanse inside white circle");
}
Console.WriteLine("PASS: doom remains outside the small white circle and clears only after stepping inside.");
foreach(var expected in new[]{0,1,2,3})
{
    var s=new DsrP5DeathScenario();s.UseSeed(12);var w=new SimWorld();
    w.Party.Slots[7]=new SimPlayer{Role=7,Position=Vector3.Zero};s.Run(w,0);
    s.State.MeteorsActive=true;
    var target=expected switch
    {
        0=>Vector3.Zero,
        1=>Vector3.Normalize(s.State.MeteorPositions[0])*22,
        2=>Vector3.Normalize(s.State.MeteorPositions[0]+s.State.MeteorPositions[1])*20,
        _=>s.State.MeteorPositions[0],
    };
    Check(s.TryLimitBreak(target),$"LB rejected {expected}-target placement");
    s.AdvanceLimitBreak(2.9f);
    Check(s.State.LimitBreakCasting&&!s.State.LimitBreakUsed&&!s.State.MeteorDestroyed.Any(x=>x),"LB resolved before cast finished");
    s.AdvanceLimitBreak(.11f);
    Check(s.State.LimitBreakUsed&&s.State.MeteorDestroyed.Count(x=>x)==expected,$"LB hit count {expected}");
}
{
    var s=new DsrP5DeathScenario();s.UseSeed(12);var w=new SimWorld();
    w.Party.Slots[7]=new SimPlayer{Role=7,Position=Vector3.Zero};s.Run(w,0);
    Check(s.TryLimitBreak(s.State.MeteorPositions[0]),"full LB unavailable before meteors");
    s.AdvanceLimitBreak(3.01f);
    Check(s.State.LimitBreakUsed&&!s.State.MeteorDestroyed.Any(x=>x),"early LB hit nonexistent meteors");
    Check(!s.TryLimitBreak(Vector3.Zero),"empty LB can be reused");
    s.AdvanceLimitBreak(1.01f);
    Check(s.TryLimitBreak(Vector3.Zero),"practice LB did not refill");
}
Console.WriteLine("PASS: full LB available before meteors; three-second cast; zero/one/two/three meteor hits despawn only hit targets; empty gauge rejected and refilled.");
foreach(var role in new[]{6,7})foreach(var action in new[]{204u,4239u})foreach(var angle in new[]{0f,1.1f,3.4f})
{
    Vector3 Rotate(Vector3 p)=>Vector3.Transform(p,Quaternion.CreateFromAxisAngle(Vector3.UnitY,angle));
    var s=new DsrP5DeathScenario();var w=new SimWorld();
    var player=new SimPlayer{Role=role,Position=Vector3.Zero};w.Party.Slots[role]=player;s.Run(w,0);
    s.State.LimitBreakAction=action;s.State.MeteorsActive=true;
    Vector3[] points=[new(0,0,10.5f),new(2.4f,0,29),new(2.6f,0,10),new(0,0,-10),new(0,0,31),new(0,0,20),new(9.5f,0,20),new(-2.4f,0,1)];
    for(var i=0;i<8;i++)s.State.MeteorPositions[i]=Rotate(points[i]);
    Check(!s.TryLimitBreak(Rotate(new(0,0,31))),"LB range gate");
    if(action==4239)Check(!s.TryLimitBreak(Vector3.Zero),"zero-direction line rejected");
    Check(s.TryLimitBreak(Rotate(new(0,0,20))),"both roles can use either supported job family");
    w.Party.Slots[role==6?7:6].Position=new(12,0,12);
    s.AdvanceLimitBreak(2.9f);
    Check(s.State.LimitBreakCasting&&!s.State.MeteorDestroyed.Any(x=>x),"other ranged moving must not interrupt caster");
    s.AdvanceLimitBreak(.11f);
    var expected=action==4239?new[]{0,1,5,7}:new[]{0,1,5,6};
    Check(Enumerable.Range(0,8).Where(i=>s.State.MeteorDestroyed[i]).SequenceEqual(expected),$"line/circle hit geometry role {role}, action {action}, rotation {angle}");
    s.AdvanceLimitBreak(1.01f);
    Check(s.TryLimitBreak(Rotate(new(0,0,20))),"LB refills for both roles");
    player.Position+=new Vector3(1,0,0);s.AdvanceLimitBreak(.1f);
    Check(!s.State.LimitBreakCasting&&!s.State.LimitBreakUsed,"active caster movement interrupts without consuming gauge");
    Check(s.TryLimitBreak(Rotate(new(0,0,20))),"retry after movement interruption");
    player.Active=false;s.AdvanceLimitBreak(.1f);
    Check(!s.State.LimitBreakCasting&&!s.State.LimitBreakUsed,"caster death interrupts");
}
for(var role=0;role<6;role++)
{
    var s=new DsrP5DeathScenario();var w=new SimWorld();
    w.Party.Slots[role]=new SimPlayer{Role=role};s.Run(w,0);
    Check(!s.TryLimitBreak(new(0,0,10)),"non-ranged practice role rejected");
}
Console.WriteLine("PASS: D3/D4 caster and physical-ranged LB2, rotated circle/line hit geometry, cast timing, range, correct caster interruption, death, refill and role restrictions.");
namespace AnoMech.Scenarios.Dsr.P5Death
{
    public sealed partial class DsrP5DeathScenario
    {
        internal DsrP5DeathState State=>state!;
        internal void UseSeed(int value)=>validationSeed=value;
        internal void AdvanceLimitBreak(float delta)=>TickLimitBreak(delta);
    }
}
