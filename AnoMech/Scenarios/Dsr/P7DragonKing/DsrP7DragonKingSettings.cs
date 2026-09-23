using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

public sealed partial class DsrP7DragonKingScenario
{
    public void DrawSettings()
    {
        ImGui.TextWrapped("中文攻略／Tuuf：三輪百京火光、死亡輪迴（5／6／7 下）、兩輪十億火光及最後無盡頓悟劍。");
        ImGui.Combo("冰火劍",ref swordChoice,["隨機","火劍（鋼鐵）","冰劍（月環）"],3);
        ImGui.Combo("三輪踩塔配置",ref towerPlan,["332／332／332","332／332／116","332／116／116"],3);
        ImGui.Checkbox("坦克挑釁／退避換坦練習",ref manualTanks);
        ImGui.TextWrapped("設定下次開始生效。選 MT／ST 並使用坦克職業：每兩下三劍一體後換坦；挑釁須選中托爾丹，退避須選中另一坦。NPC 接手時會自動挑釁，輪到玩家時等你操作。未勾選則自動換坦。");
        ImGui.TextWrapped("116：六人左前紅塔、一坦右前紅塔、一坦後方藍塔。首次 116 由 MT 單吃藍塔，連續第二次改 ST；只驗證站位，不檢查無敵／減傷。最近者輪替：D1→D2、D3→D4、H1→H2。最後犧牲塔：H1/H2/ST → D3/D4/MT。");
        ImGui.TextWrapped("本階段從托爾丹可選取後開始；練習站位、判定與換坦，未模擬輸出血量及減傷治療門檻。");
        var music=!Plugin.Config.SuppressBgm;
        if(ImGui.Checkbox("背景音樂：Revenge Twofold（交響樂版）",ref music)){Plugin.Config.SuppressBgm=!music;Plugin.Config.Save();}
        if(state==null)return;
        ImGui.Separator();
        ImGui.TextUnformatted($"P7　{state.Time:F1} 秒　一仇：{DsrP7DragonKingState.RoleName(state.MainTank)}");
        ImGui.TextWrapped(state.Hint);
        if(!string.IsNullOrEmpty(state.TankMessage))ImGui.TextWrapped(state.TankMessage);
        ImGui.TextUnformatted($"MT：暗 {state.DarkStacks[0]}／光 {state.LightStacks[0]}　ST：暗 {state.DarkStacks[1]}／光 {state.LightStacks[1]}");
        ImGui.TextWrapped("普攻雙坦盡量同側，MT 距王 6 碼、ST 10 碼，保持 4 碼間距。地火前同側靠 A 或 C，讓王保持南北面向；玩家坦克請自行移動。選中王時，小隊列表顯示模擬仇恨順位與比例，不計算實際輸出。");
        if(state.Failed)ImGui.TextWrapped("本輪有失誤，可重置重練。");
    }
}
