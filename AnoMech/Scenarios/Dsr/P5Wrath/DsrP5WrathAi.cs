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
        if(state.FinaleResolved) return state.SafePosition(role);
        if(state.MercyResolved)
        {
            if(role==state.Green && !state.DiveLocked) return state.SpreadPosition(role);
            if(role==state.Liquid || role==state.Altar) return state.BaitDestination(role);
            return state.SafePosition(role);
        }
        if(state.GreenAssigned && state.Time>=22.2f) return state.SpreadPosition(role);
        if(state.GreenAssigned && state.Time>=20.2f) return Vector3.Normalize(state.SpreadPosition(role))*15;
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
