using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
namespace AnoMech.Scenarios.Dsr.P5Death;
internal sealed class DsrP5DeathAi : IScenarioAi
{
    public string Name=>"tuuf／死宣北、無死宣南";
    public static Vector3 Destination(DsrP5DeathState s,int role)
    {
        if(s.MeteorsActive)return role==7?Vector3.Zero:s.FinalDirection(role)*12f;
        if(s.Knocked)return s.HasDoom(role)&&!s.Cleansed[role]
            ?s.CleansePositions.MinBy(p=>Vector3.DistanceSquared(p,s.FinalDirection(role)*19.2f))
            :s.FinalDirection(role)*19.2f;
        if(s.SymbolsAssigned)return s.FinalDirection(role)*2.5f;
        if(s.Time>=27.4f)return s.SymbolBait(role);
        if(s.Time>=26.1f&&!s.HasDoom(role))return s.Rotate(new(Array.IndexOf(s.Clean,role)<2?-9:9,0,7));
        if(s.SpreadsResolved)
        {
            var p=s.Spread(role);
            return p+Vector3.Normalize(s.Hammer-p)*(s.HasDoom(role)?5f:2.2f);
        }
        if(s.Time>=16f)return s.Spread(role);
        return s.Rotate(new((role-3.5f)*2.5f,0,s.Assigned?(s.HasDoom(role)?-2:2):0));
    }
    public static void Tick(DsrP5DeathState s,SimWorld world)
    {
        if(!s.FormationVisible || s.Complete || s.Knocked && s.Time<38.7f)return;
        for(var role=0;role<8;role++)
            if(world.Party.Get(role) is SimPartyNpc npc && npc.IsAlive())
            {
                if(role==7&&s.LimitBreakCasting)continue;
                var target=Destination(s,role);
                if(s.Destinations[role]==target)continue;
                s.Destinations[role]=target;
                npc.MoveTo(target,6,s.SymbolsAssigned&&!s.Knocked?s.SafeFacing(target):null);
            }
    }
}
