using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P4Eyes;
internal static class SwapWaitingValidation
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        foreach(var fps in new[]{30,60,144})
        {
            var s = new DsrP4EyesScenario(); var w = new SimWorld(); s.Run(w,0); s.PrepareColors();
            s.State.BuffsApplied=true; s.State.OrbsPopped[0]=s.State.OrbsPopped[1]=true;
            for(var r=0;r<8;r++) { s.State.Red[r]=r<4; w.Party.Slots[r].Position=r<4?DsrP4EyesState.YellowWait(r,1):DsrP4EyesState.Opening(r); }
            w.Party.Slots[4]=new SimPlayer{Role=4,Position=new(0,0,16)};
            for(var f=0;f<fps;f++) { s.Tick(1f/fps,41); foreach(var m in w.Party.Slots)m.Advance(1f/fps); }
            Check(w.Party.Slots[0].Position==DsrP4EyesState.YellowWait(0,1),"MT left before player exchanged");
            Check(!s.State.OrbExchangeDone[0],"exchange marked before contact");
            w.Party.Slots[4].Position=w.Party.Slots[0].Position;
            s.Tick(0,41);
            Check(s.State.OrbExchangeDone[0]&&s.State.OrbExchangeDone[4],"exchange not acknowledged");
            Check(w.Party.Slots[0].HasStatus(DsrP4EyesConstants.SwapLock)&&w.Party.Slots[4].HasStatus(DsrP4EyesConstants.SwapLock),"both participants need lock debuff");
            for(var f=0;f<fps;f++) { foreach(var m in w.Party.Slots)m.Advance(1f/fps); s.Tick(1f/fps,42); }
            Check(Vector3.Distance(w.Party.Slots[0].Position,w.Party.Slots[4].Position)>2,"NPC did not leave after contact");
            Check(!s.State.Red[0]&&s.State.Red[4],"premature repeat swap");

            var d = new DsrP4EyesScenario(); var v = new SimWorld(); d.Run(v,0); d.PrepareColors();
            d.State.BuffsApplied=d.State.MirageStarted=true; Array.Fill(d.State.OrbsPopped,true);
            for(var r=0;r<8;r++) { d.State.Red[r]=r>=4; v.Party.Slots[r].Position=r<4?DsrP4EyesState.BlueEye:DsrP4EyesState.DivePosition(d.State.DiveLane[r])+new Vector3(.5f,0,0); }
            v.Party.Slots[2]=new SimPlayer{Role=2,Position=DsrP4EyesState.BlueEye};
            v.Party.Slots[3]=new SimPlayer{Role=3,Position=DsrP4EyesState.BlueEye};
            d.DiveTest(); var target=d.State.SwapTarget[2]; var wait=v.Party.Slots[target].Position;
            for(var f=0;f<2*fps;f++){d.Tick(1f/fps,62);foreach(var m in v.Party.Slots)m.Advance(1f/fps);}
            Check(v.Party.Slots[target].Position==wait,"dive target left while player delayed");
            v.Party.Slots[2].Position=wait; d.Tick(0,62);
            Check(d.State.SwapWaitPosition[target]==null&&d.State.SwapTarget[2]==-1,"successful swap did not release waiter");
            v.Party.Slots[2].Position=new(0,0,18);
            d.Tick(2.999f,64.999f);
            Check(v.Party.Slots[2].HasStatus(DsrP4EyesConstants.SwapLock),"debuff expired before three seconds");
            d.Tick(.0011f,65.0001f);
            Check(!v.Party.Slots[2].HasStatus(DsrP4EyesConstants.SwapLock)&&d.State.SwapCooldown[2]==0,"debuff and lock did not expire together");
        }
        Console.WriteLine("PASS: delayed player exchange, NPC waits then departs; both swap participants receive 3-second debuff; expiry matches lock at 30/60/144 FPS.");
    }
}
