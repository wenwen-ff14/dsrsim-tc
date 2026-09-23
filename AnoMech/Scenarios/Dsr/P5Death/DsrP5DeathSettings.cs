using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
namespace AnoMech.Scenarios.Dsr.P5Death;
public sealed partial class DsrP5DeathScenario
{
    public void DrawSettings()
    {
        ImGui.TextUnformatted("tuuf／死宣北、無死宣南、PS 2-2");
        ImGui.TextWrapped("以持錘騎士為北，MT ST H1 H2 D1 D2 D3 D4 由西向東排隊。死宣往北、無死宣往南，組內依左右順序散開。");
        var music=!Plugin.Config.SuppressBgm;
        if(ImGui.Checkbox("背景音樂：Heavensward",ref music)){Plugin.Config.SuppressBgm=!music;Plugin.Config.Save();}
        ImGui.Checkbox("顯示站位提示",ref showHints);ImGui.SameLine();ImGui.Checkbox("顯示戰術圖",ref showMap);
        ImGui.TextWrapped("包含死刻與隕石擊破；D4 使用熱鍵列的極限爆發，瞄準北側隕石放置 LB2。也可選取隕石後使用 /dsrsim lb。讀條中移動會中斷。");
        if(state==null||world==null)return;
        ImGui.TextUnformatted($"P5-死刻　{state.Time:F1} 秒");
        if(state.Complete)ImGui.TextUnformatted(state.Failed?"本輪有失誤，可重置重練。":"死刻練習完成。");
        else if(showHints)
        {
            var role=(int)world.Party.PlayerRole;
            ImGui.TextWrapped(state.MeteorsActive?"LB 滿條即可施放，讀條 3 秒；依落點計算命中數，不限制三顆。":state.Knocked?"向外拉斷鏈；有死亡宣告者進入白圈解除。":state.SymbolsAssigned?"同標記站對面，靠中心準備擊退；背對托爾丹與龍眼。":state.Time>=26.1f?"兩側死宣拉開誘導圓形；內側死宣往南；無死宣在中心偏北排隊。":state.SpreadsResolved?"死宣躲進已炸的第二環；無死宣先小幅移動躲旋風，第三環後再進入。":state.Assigned?(state.HasDoom(role)?"死亡宣告：往北分組，依左右順序站位。":"無死亡宣告：往南分組，依左右順序站位。"):"以持錘騎士為北，按職責由西向東排隊。");
        }
        if(state.MeteorsActive)
        {
            ImGui.TextWrapped(state.LimitBreakMessage);
            ImGui.TextUnformatted($"隕石判定剩餘：{System.MathF.Max(0,57.04f-state.Time):F1} 秒");
            if(state.LimitBreakCasting)ImGui.ProgressBar(state.LimitBreakElapsed/3f,new Vector2(-1,0),$"小型隕石 {state.LimitBreakElapsed:F1} / 3.0 秒");
            ImGui.BeginDisabled((int)world.Party.PlayerRole!=7||state.LimitBreakUsed||state.LimitBreakCasting);
            if(ImGui.Button("模擬 LB2（目前目標）"))RequestLimitBreak();
            ImGui.EndDisabled();
        }
        if(!showMap)return;
        var scale=5*ImGuiHelpers.GlobalScale;var size=280*ImGuiHelpers.GlobalScale;
        var center=ImGui.GetCursorScreenPos()+new Vector2(size/2);var draw=ImGui.GetWindowDrawList();
        Vector2 Map(Vector3 p)=>center+new Vector2(p.X,p.Z)*scale;
        draw.AddCircle(center,21*scale,0xFFAAAAAA,80,2);
        draw.AddText(Map(state.Hammer),0xFFFFFFFF,"錘");draw.AddText(Map(state.Boss),0xFFFF88FF,"王");
        draw.AddText(Map(Vector3.Normalize(state.Eye)*23),0xFFFF88FF,"眼");
        if(cleansesActive)foreach(var p in state.CleansePositions)draw.AddCircle(Map(p),DsrP5DeathState.CleanseRadius*scale,0xFFFFFFFF,32,2);
        if(state.MeteorsActive)for(var i=0;i<8;i++)
            if(!state.MeteorDestroyed[i])draw.AddCircleFilled(Map(state.MeteorPositions[i]),5*ImGuiHelpers.GlobalScale,0xFF55AAFF);
        string[] names=["MT","ST","H1","H2","D1","D2","D3","D4"];
        for(var role=0;role<8;role++)
        {
            var member=world.Party.Get(role);if(member==null)continue;
            var p=Map(member.Position);draw.AddCircleFilled(p,3,role==(int)world.Party.PlayerRole?0xFF55FF55u:0xFFFFFFFFu);
            draw.AddText(p+new Vector2(4,0),state.Assigned&&state.HasDoom(role)&&!state.Cleansed[role]?0xFF5555FFu:0xFFFFFFFFu,names[role]);
        }
        if(showHints&&!state.Complete)draw.AddCircle(Map(DsrP5DeathAi.Destination(state,(int)world.Party.PlayerRole)),scale,0xFF55FF55,24,2);
        ImGui.Dummy(new Vector2(size));
    }
}
