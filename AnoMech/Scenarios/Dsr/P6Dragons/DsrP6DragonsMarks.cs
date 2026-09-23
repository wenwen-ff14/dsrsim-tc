using System;
using System.Linq;
using AnoMech.Core;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public sealed partial class DsrP6DragonsScenario
{
    private static readonly Sign[] FlameSigns=[Sign.Attack1,Sign.Attack2,Sign.Attack3,Sign.Attack4,Sign.Bind1,Sign.Bind2,Sign.Ignore1,Sign.Ignore2];
    private void ApplyFlameMarks()
    {
        if(!state!.SystemMarks)return;
        ClearFlameMarks();
        for(var i=0;i<8;i++)
            if(world!.Party.Get(state.Flames[i]) is {} member)Markings.Set(FlameSigns[i],member.GameObjectId);
    }
    private static void ClearFlameMarks()
    {
        foreach(var sign in FlameSigns)Markings.Clear(sign);
    }
    private void UpdateFlamePositions()
    {
        if(!state!.SystemMarks)
        {
            var order=new int[8];
            for(var i=0;i<8;i++)
            {
                order[i]=-1;
                for(var r=0;r<8;r++)
                    if(world!.Party.Get(r) is {} member&&Markings.IsSetOn(FlameSigns[i],member.GameObjectId))order[i]=r;
                var groupStart=i<4?0:i<6?4:6;
                var groupSize=i<4?4:2;
                if(order[i]<0||Array.IndexOf(state.Flames,order[i],groupStart,groupSize)<0)
                {
                    state.Hint="等待手標：散開攻擊 1～4、分攤鎖鏈 1～2、無點名禁止 1～2；同號鎖鏈與禁止配對。";
                    return;
                }
            }
            if(order.Distinct().Count()!=8)return;
            state.FlameOrder=order;
        }
        state.SetFlames();
    }
}
