using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

internal sealed class DsrP7DragonKingAi : IScenarioAi
{
    public string Name=>"中文攻略／Tuuf（可選踩塔配置）";
    public static void Tick(DsrP7DragonKingState state,SimWorld world)
    {
        if(state.Complete)return;
        for(var r=0;r<8;r++)
            if(!state.Sacrificed[r]&&world.Party.Get(r) is SimPartyNpc npc&&npc.IsAlive()&&state.LastDestinations[r]!=state.Destinations[r])
            {
                state.LastDestinations[r]=state.Destinations[r];
                npc.MoveTo(state.Destinations[r],6);
            }
    }
}
