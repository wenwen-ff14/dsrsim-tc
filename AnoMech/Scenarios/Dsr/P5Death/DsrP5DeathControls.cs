using System;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr.P5Death;

public sealed partial class DsrP5DeathScenario
{
    internal bool PracticeActive=>state!=null&&!state.Complete;
    internal bool PracticeReady=>state is { Complete:false, LimitBreakCasting:false, LimitBreakUsed:false };
    internal bool PracticeCasting=>state?.LimitBreakCasting==true;
    internal bool PracticeUsed=>state?.LimitBreakUsed==true;
    internal float PracticeCastElapsed=>state?.LimitBreakElapsed??0;
    internal Vector3 PracticeTarget=>state?.LimitBreakTarget??default;
    internal void CancelPracticeCast()
    {
        if(state?.LimitBreakCasting!=true)return;
        state.LimitBreakCasting=false;
        state.LimitBreakMessage="LB2 讀條中斷，可以重新施放。";
        Array.Clear(state.Destinations);
    }
    public void RequestLimitBreak()
    {
        if(state==null||world==null||state.Complete||!state.MeteorsActive)return;
        if((int)world.Party.PlayerRole!=7)
        {state.LimitBreakMessage="本練習由 D4 使用 LB2，請選擇 D4 職責。";return;}
        var selected=Plugin.TargetManager.Target?.EntityId;
        var index=Array.FindIndex(meteors,m=>m!=null&&m.EntityId==selected);
        if(index<0||state.MeteorDestroyed[index])
        {state.LimitBreakMessage="請先選取一顆隕石，LB2 將以它為中心施放。";return;}
        TryLimitBreak(state.MeteorPositions[index]);
    }

}
