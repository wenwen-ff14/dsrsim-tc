using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P6Dragons;

internal static class P6PatternValidation
{
    public static void Run(Action<bool,string> check)
    {
        foreach(var second in new[]{false,true})
        for(var variant=0;variant<(second?8:16);variant++)
        {
            var south=(variant&1)!=0;
            var far=(variant&2)!=0;
            var west=(variant&4)!=0;
            var fromSouth=(variant&8)!=0;
            var hotWing=(variant&4)!=0;
            var pattern=new DsrP6WingsPattern(south,far,west,fromSouth,hotWing);
            foreach(var fps in new[]{30,60,144})
            for(var role=0;role<8;role++)
            {
                var s=new DsrP6DragonsScenario(second?DsrP6Section.Wings2:DsrP6Section.Wings1);
                s.UseSeed(17);var w=new SimWorld();
                var player=new SimPlayer{Role=role,Position=new(0,0,16)};w.Party.Slots[role]=player;
                SimCharacter.Failures.Clear();s.Run(w,0);
                if(second)s.State.SecondWings=pattern;else s.State.FirstWings=pattern;
                for(var frame=1;frame<=25*fps;frame++)
                {
                    var time=frame/(float)fps;SimCharacter.Time=time;
                    player.MoveTo(s.State.Destinations[role]);
                    var before=w.Party.Slots.Select(m=>m.Position).ToArray();
                    w.Events.Tick(1f/fps);
                    foreach(var member in w.Party.Slots)member.Advance(1f/fps);
                    s.Tick(1f/fps,time);
                    for(var r=0;r<8;r++)check(Vector3.Distance(before[r],w.Party.Slots[r].Position)<=6f/fps+.002f,"wing NPCs/player walk without teleporting");
                    if(second&&time>2.4f&&time<4.2f)
                        check(s.State.Destinations.All(p=>MathF.Abs(p.Z)==2),"do not anticipate Hot Tail before its cast");
                    if(!second&&time>2.1f&&time<12)
                    {
                        var dragon=w.Enemies.First(e=>e.BNpcBaseId==0x3144);
                        check(dragon.Position==new Vector3(west?-11:11,0,fromSouth?34:-34),"dive origin matches lane and entry edge");
                        check(MathF.Abs(dragon.Rotation-(fromSouth?MathF.PI:0))<.001f,"south/north dive faces into arena");
                    }
                }
                check(!s.State.Failed&&s.State.Complete,$"wing {second}/{variant}, role={role}, fps={fps}: {string.Join(";",SimCharacter.Failures.Take(3))}");
                var actions=w.Enemies.SelectMany(e=>e.Casts).Select(c=>c.Action).ToArray();
                var cast=south?(far?27940u:27939u):(far?27943u:27942u);
                check(actions.Contains(cast)&&actions.Contains(south?27941u:27944u),"native head/wing cast and cleave agree");
                check(second?actions.Contains(hotWing?27947u:27949u)&&actions.Contains(hotWing?27948u:27950u):actions.Contains(27966u),"native Hot Wing/Tail or Cauterize branch");
                check(w.Enemies.SelectMany(e=>e.CastTargets).Where(c=>c.Action==27945).Select(c=>c.Target).Order().SequenceEqual(new uint?[]{0,1}),"both tankbusters target tanks");
            }

            foreach(var failure in second?new[]{"cleave","bait","splash","hot"}:new[]{"cleave","bait","splash","dive"})
            {
                var s=new DsrP6DragonsScenario(second?DsrP6Section.Wings2:DsrP6Section.Wings1);var w=new SimWorld();
                SimCharacter.Failures.Clear();s.Run(w,0);
                if(second)s.State.SecondWings=pattern;else s.State.FirstWings=pattern;
                for(var frame=1;frame<=13*60;frame++)
                {
                    var time=frame/60f;SimCharacter.Time=time;
                    if(time>(second?10:12))
                    {
                        var positions=Enumerable.Range(0,8).Select(r=>pattern.Position(r,second)).ToArray();
                        switch(failure)
                        {
                            case "cleave": positions[2]=new(positions[2].X,0,-positions[2].Z);break;
                            case "bait": (positions[0],positions[2])=(positions[2],positions[0]);break;
                            case "splash": positions[1]=positions[0];break;
                            case "hot": positions[2]=new(positions[2].X,0,(south?-1:1)*(hotWing?6:2));break;
                            case "dive": positions[2]=new(west?-11:11,0,positions[2].Z);break;
                        }
                        for(var r=0;r<8;r++){w.Party.Slots[r].SetPosition(positions[r]);w.Party.Slots[r].MoveTo(positions[r]);}
                    }
                    w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,time);
                }
                var message=failure switch{"cleave"=>"神聖之翼","bait"=>"雙坦必須誘導","splash"=>"遠離坦克死刑","hot"=>hotWing?"燃燒之翼":"燃燒之尾",_=>"邪炎俯衝"};
                check(SimCharacter.Failures.Any(f=>f.Contains(message)),$"wrong {failure} rejected: {second}/{variant}");
            }
        }
        Console.WriteLine("PASS: 16 first-wing and 8 second-wing patterns, eight player roles at 30/60/144 FPS, native actions and 96 negative cases.");

        foreach(var glow in Enum.GetValues<DsrP6Glow>())
        for(var mask=0;mask<64;mask++)
        {
            if(BitOperations.PopCount((uint)mask)!=3)continue;
            foreach(var role in new[]{6,7})
            foreach(var dx in new[]{-.3f,0,.3f})foreach(var dz in new[]{-.3f,0,.3f})
            {
                var s=new DsrP6DragonsScenario(DsrP6Section.Breath2);s.UseSeed(0);var w=new SimWorld();
                var player=new SimPlayer{Role=role};w.Party.Slots[role]=player;
                SimCharacter.Failures.Clear();s.Run(w,0);s.State.SecondGlow=glow;
                for(var r=2;r<8;r++)s.State.SecondFire[r]=(mask&(1<<(r-2)))!=0;
                for(var frame=1;frame<=10*60;frame++)
                {
                    var time=frame/60f;SimCharacter.Time=time;
                    player.MoveTo(s.State.Destinations[role]+new Vector3(dx,0,dz));
                    w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,time);
                }
                check(!s.State.Failed,$"ranged breath area: {glow}/{mask}, D{role-3}, {dx}/{dz}: {string.Join(";",SimCharacter.Failures)}");
            }
        }
        foreach(var role in new[]{6,7})
        {
            var s=new DsrP6DragonsScenario(DsrP6Section.Breath2);var w=new SimWorld();
            var player=new SimPlayer{Role=role};w.Party.Slots[role]=player;
            SimCharacter.Failures.Clear();s.Run(w,0);
            for(var frame=1;frame<=10*60;frame++)
            {
                var time=frame/60f;SimCharacter.Time=time;
                player.MoveTo(s.State.Destinations[role==6?3:2]);
                w.Events.Tick(1f/60);foreach(var m in w.Party.Slots)m.Advance(1f/60);s.Tick(1f/60,time);
            }
            check(SimCharacter.Failures.Any(f=>f.Contains("冰火二")),"actual healer/ranged overlap remains lethal");
        }
        Console.WriteLine("PASS: 1,080 D3/D4 offset runs across all tether/glow assignments; actual overlap still fails.");
    }
}
