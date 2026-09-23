using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dsr.P6Dragons;

public sealed partial class DsrP6DragonsScenario
{
    public void DrawSettings()
    {
        ImGui.TextWrapped("P6 開發版：目前提供冰火 1、第一次坦死刑、第二次坦死刑及固定式冰火 2。十字火與全流程尚待核對火球資料，未開放。");
        ImGui.TextWrapped("冰火 2 使用中文攻略固定式：冰火連線隨機，職責站位固定；單龍發光為另一側坦克單吃，雙龍發光為雙坦場中分攤。冰火 1 與翅膀死刑仍使用指定紀錄分支。坦克減傷與雙龍血量差尚未模擬。");
        var music=!Plugin.Config.SuppressBgm;
        if(ImGui.Checkbox("背景音樂：Dragonsong",ref music)){Plugin.Config.SuppressBgm=!music;Plugin.Config.Save();}
        if(state==null)return;
        ImGui.TextUnformatted($"P6　{state.Time:F1} 秒");
        ImGui.TextWrapped(state.Complete?(state.Failed?"本輪有失誤，可重置重練。":"本段完成。"):state.Hint);
    }
}
