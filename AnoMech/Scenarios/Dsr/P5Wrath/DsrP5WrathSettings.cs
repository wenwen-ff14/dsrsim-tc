using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
namespace AnoMech.Scenarios.Dsr.P5Wrath;
public sealed partial class DsrP5WrathScenario
{
    public void DrawSettings()
    {
        ImGui.TextUnformatted("tuuf／白龍為北、連線交叉至南側");
        ImGui.TextWrapped("藍標與交叉連線 → 旋風 → 八方劍散開 → 綠標到持杖騎士背後引導龍衝 → 連續地火 → 持斧騎士月環與雷光鏈 → 古代爆震。");
        var music=!Plugin.Config.SuppressBgm;
        if(ImGui.Checkbox("背景音樂：Heavensward",ref music)){Plugin.Config.SuppressBgm=!music;Plugin.Config.Save();}
        ImGui.Checkbox("顯示站位提示",ref showHints);
        ImGui.SameLine();
        ImGui.Checkbox("顯示戰術圖",ref showMap);
        if(ImGui.Combo("第一組點名",ref practiceTarget,"隨機\0放大圈\0龍連線1\0龍連線2\0龍衝引導\0") && practiceTarget != 0 && followupTarget == 2)
            followupTarget = 0;
        if(ImGui.Combo("第二組點名",ref followupTarget,"隨機\0雷點名\0五火\0四火\0") && followupTarget == 2)
            practiceTarget = 0;
        ImGui.TextWrapped("兩組可分別指定，開始或重置後生效。五火不能與第一組指定點名重疊，選五火會將第一組改為隨機。");
        ImGui.SliderFloat("火圈接觸緩衝",ref liquidContactGrace,.5f,2f,"%.1f 秒");
        ImGui.TextDisabled("連續停留超過緩衝才判定失敗；離開火圈重算。此為練習容錯。");
        if(state==null || world==null) return;
        ImGui.TextUnformatted($"P5-風槍　{state.Time:F1} 秒");
        if(state.Complete) ImGui.TextUnformatted(state.Failed?"本輪有失誤，可重置重練。":"風槍練習完成。");
        else if(showHints)
        {
            var role=(int)world.Party.PlayerRole;
            ImGui.TextWrapped(state.MercyResolved ? "前往持斧騎士：雷點名站安全區外側；地火點名持續移動，勿將地火帶進集合點。" : state.GreenAssigned ? (role==state.Green ? "綠標：到持杖騎士背後場邊引導龍衝，鎖定後再離開。" : "躲旋風後八方散開，與隊友隔兩格，避免八方劍重疊。") : !state.Assigned?"等待白龍與騎士出現，白龍為相對北。":state.ChargesResolved?"離開原位躲旋風；不要走進其他人的旋風。":role==state.Blue?"藍標：到白龍左側西北，遠離其他人。":role==state.TetherRoles[0]||role==state.TetherRoles[1]?"連線：交叉拉至相對南側，勿讓直線掃到隊友。":"無點名：相對東側分散。");
        }
        if(showMap) DrawMap();
    }
    private void DrawMap()
    {
        var scale=5*ImGuiHelpers.GlobalScale;
        var size=280*ImGuiHelpers.GlobalScale;
        var center=ImGui.GetCursorScreenPos()+new Vector2(size/2);
        var draw=ImGui.GetWindowDrawList();
        Vector2 Map(Vector3 p)=>center+new Vector2(p.X,p.Z)*scale;
        draw.AddCircle(center,21*scale,0xFFAAAAAA,80,2);
        draw.AddText(center+new Vector2(-5,-26*scale),0xFFFFFFFF,"北");
        draw.AddText(Map(state!.WhiteDragon),0xFFFFFFFF,"白龍");
        if(state.TwistersActive) foreach(var p in state.Twisters) draw.AddCircle(Map(p),2*scale,0xFF55DD55,24,2);
        string[] names=["MT","ST","H1","H2","D1","D2","D3","D4"];
        for(var r=0;r<8;r++)
        {
            var m=world!.Party.Get(r);
            if(m==null) continue;
            var p=Map(m.Position);
            draw.AddCircleFilled(p,3,r==(int)world.Party.PlayerRole?0xFF55FF55u:0xFFFFFFFFu);
            draw.AddText(p+new Vector2(3,0),0xFFFFFFFF,names[r]);
        }
        if(state.Assigned&&!state.ChargesResolved)
        {
            for(var i=0;i<2;i++) if(world!.Party.Get(state.TetherRoles[i]) is {} m) draw.AddLine(Map(state.KnightPosition(i)),Map(m.Position),0xFFFFAA55,2);
            if(world!.Party.Get(state.Blue) is {} blue) draw.AddCircle(Map(blue.Position),5,0xFFFFAA55,24,2);
        }
        if(showHints&&!state.Complete) draw.AddCircle(Map(DsrP5WrathAi.Destination(state,(int)world!.Party.PlayerRole)),scale,0xFF55FF55,24,2);
        ImGui.Dummy(new Vector2(size));
    }
}
