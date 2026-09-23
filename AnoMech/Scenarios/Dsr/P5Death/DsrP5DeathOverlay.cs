using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

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

    public void DrawOverlay()
    {
        if(state==null||world==null||state.Complete)return;
        var draw=ImGui.GetForegroundDrawList();
        var radius=12*ImGuiHelpers.GlobalScale;
        if(state.SymbolsAssigned&&state.Time<39.840f)
            for(var role=0;role<8;role++)
            {
                var member=world.Party.Get(role);
                if(member==null||!Plugin.GameGui.WorldToScreen(world.Coordinates.ToGlobal(member.Position+new Vector3(0,2.8f,0)),out var p))continue;
                var symbol=state.Symbols[role];
                uint color=symbol switch{281=>0xFF5555FF,282=>0xFF55FF55,283=>0xFFFF77FF,_=>0xFFFFAA55};
                foreach(var outline in new[]{true,false})
                {
                    var c=outline?0xFF111111:color;var width=outline?6f:3f;
                    switch(symbol)
                    {
                        case 281:draw.AddCircle(p,radius,c,32,width);break;
                        case 282:draw.AddTriangle(p+new Vector2(0,-radius),p+new Vector2(radius,radius),p+new Vector2(-radius,radius),c,width);break;
                        case 283:draw.AddRect(p-new Vector2(radius),p+new Vector2(radius),c,0,ImDrawFlags.None,width);break;
                        case 284:
                            draw.AddLine(p-new Vector2(radius),p+new Vector2(radius),c,width);
                            draw.AddLine(p+new Vector2(radius,-radius),p+new Vector2(-radius,radius),c,width);break;
                    }
                }
            }
        if(state.Time>=32.239f&&state.Time<37.381f&&Plugin.GameGui.WorldToScreen(world.Coordinates.ToGlobal(state.Boss+new Vector3(0,6,0)),out var eye))
        {
            for(var i=0;i<32;i++)
            {
                Vector2 Point(int n){var a=n*MathF.Tau/32;return eye+new Vector2(MathF.Cos(a)*24,MathF.Sin(a)*12)*ImGuiHelpers.GlobalScale;}
                draw.AddLine(Point(i),Point(i+1),0xFFEEEEFF,3);
            }
            draw.AddCircleFilled(eye,6*ImGuiHelpers.GlobalScale,0xFF5555FF,20);
        }
    }
}
