using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public sealed partial class DsrP6DragonsScenario
{
    public void DrawSettings()
    {
        ImGui.TextWrapped("P6 開發版：目前提供冰火 1、第一次坦死刑、第二次坦死刑及固定式冰火 2。十字火與全流程尚待核對火球資料，未開放。");
        ImGui.TextWrapped("目前重現指定紀錄的黑龍發光／白龍單坦死刑及抬頭最遠死刑組合。以站位練習為主；坦克減傷與雙龍血量差尚未模擬。");
        var music=!Plugin.Config.SuppressBgm;
        if(ImGui.Checkbox("背景音樂：Dragonsong",ref music)){Plugin.Config.SuppressBgm=!music;Plugin.Config.Save();}
        if(state==null)return;
        ImGui.TextUnformatted($"P6　{state.Time:F1} 秒");
        ImGui.TextWrapped(state.Complete?(state.Failed?"本輪有失誤，可重置重練。":"本段完成。"):state.Hint);
    }
}
