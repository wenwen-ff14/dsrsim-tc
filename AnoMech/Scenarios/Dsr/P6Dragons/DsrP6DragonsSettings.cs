using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public sealed partial class DsrP6DragonsScenario
{
    public void DrawSettings()
    {
        var first=(int)firstSection-1;var last=(int)lastSection-1;
        string[] sections=["冰火 1（含分攤）","第一次坦死刑","十字火（含分攤）","第二次坦死刑","冰火 2（固定式、雙龍俯衝）"];
        if(ImGui.Combo("開始機制",ref first,sections,sections.Length))SetRange((DsrP6Section)(first+1),(DsrP6Section)(System.Math.Max(first,last)+1));
        if(ImGui.Combo("結束機制",ref last,sections,sections.Length))SetRange((DsrP6Section)(System.Math.Min((int)firstSection-1,last)+1),(DsrP6Section)(last+1));
        ImGui.TextWrapped($"下次開始：{SectionName(firstSection)} → {SectionName(lastSection)}");
        ImGui.TextWrapped("十字火：中央先出、後兩組對角隨機；白龍西／中／東及南／北隨機。等命中特效後走 L 型，黑龍側可繞目標圈。火球時間與位置仍待原始 ACT／影片精校。");
        ImGui.TextWrapped("冰火 2 使用中文攻略固定式：冰火連線隨機，職責站位固定；單龍發光為另一側坦克單吃，雙龍發光為雙坦場中分攤。冰火 1 與翅膀死刑仍使用指定紀錄分支。坦克減傷與雙龍血量差尚未模擬。");
        var music=!Plugin.Config.SuppressBgm;
        if(ImGui.Checkbox("背景音樂：Dragonsong",ref music)){Plugin.Config.SuppressBgm=!music;Plugin.Config.Save();}
        if(state==null)return;
        ImGui.TextUnformatted($"P6　{state.Time:F1} 秒");
        ImGui.TextWrapped(state.Complete?(state.Failed?"本輪有失誤，可重置重練。":"本段完成。"):state.Hint);
    }
}
