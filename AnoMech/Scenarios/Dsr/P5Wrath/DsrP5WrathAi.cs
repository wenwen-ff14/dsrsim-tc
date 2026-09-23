using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
namespace AnoMech.Scenarios.Dsr.P5Wrath;
internal sealed class DsrP5WrathAi : IScenarioAi
{
    public string Name => "tuuf／白龍為北、交叉連線";
    public static Vector3 Destination(DsrP5WrathState state, int role)
    {
        if(!state.Assigned) return new(3*MathF.Sin(role*MathF.Tau/8),0,3*MathF.Cos(role*MathF.Tau/8));
        return state.ChargesResolved ? state.DodgePosition(role) : state.InitialPosition(role);
    }
    public static void Tick(DsrP5WrathState state, SimWorld world)
    {
        if(state.Complete) return;
        for(var role=0;role<8;role++)
            if(world.Party.Get(role) is SimPartyNpc npc && npc.IsAlive())
            {
                var target=Destination(state,role);
                if(state.Destinations[role]==target) continue;
                state.Destinations[role]=target;
                npc.MoveTo(target,6);
            }
    }
}
