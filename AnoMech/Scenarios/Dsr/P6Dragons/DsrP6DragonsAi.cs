using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

internal sealed class DsrP6DragonsAi : IScenarioAi
{
    public string Name=>"雙龍站位練習";
    public static void Tick(DsrP6DragonsState state,SimWorld world)
    {
        if(state.Complete)return;
        for(var r=0;r<8;r++)
            if(world.Party.Get(r) is SimPartyNpc npc&&npc.IsAlive()&&state.LastDestinations[r]!=state.Destinations[r])
            {
                state.LastDestinations[r]=state.Destinations[r];
                npc.MoveTo(state.Destinations[r],6);
            }
    }
}
