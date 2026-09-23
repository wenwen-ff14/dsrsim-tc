using System;

namespace AnoMech.Scenarios.Dsr.P5Death;

public sealed partial class DsrP5DeathScenario
{
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
