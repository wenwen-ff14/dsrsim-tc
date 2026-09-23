using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AnoMech.Scenarios.Dsr.P4Eyes;

public sealed partial class DsrP4EyesScenario
{
    public void DrawSettings()
    {
        ImGui.TextUnformatted("tuuf／坦補紅線、DPS 藍線");
        var music = !Plugin.Config.SuppressBgm;
        if (ImGui.Checkbox("背景音樂：Contention", ref music))
        {
            Plugin.Config.SuppressBgm = !music;
            Plugin.Config.Save();
        }
        ImGui.TextWrapped("集合取得思念 → 換成坦補紅／DPS 藍 → 黃球長大兩次後坦補雙人撞球 → 換線 → DPS 單人撞藍球 → 西側四輪幻象俯衝。");
        ImGui.TextWrapped("俯衝換線順序：補師 → 坦克 → 第一輪被點的 DPS。由北起逆時針／順時針分配，H1、MT、較小編號 DPS 優先逆時針。");
        ImGui.TextWrapped("本關自動推進雙眼擊破，不需輸出；時間軸尚待實機校正。");
        ImGui.Checkbox("顯示站位提示", ref showHints);
        ImGui.SameLine();
        ImGui.Checkbox("顯示戰術圖", ref showMap);
        ImGui.Checkbox("固定隨機種子", ref fixedSeed);
        if (fixedSeed) ImGui.InputInt("種子", ref seed);
        if (state == null || world == null) return;
        ImGui.Separator();
        ImGui.TextUnformatted($"P4-雙眼　{state.Time:F1} 秒　幻象俯衝 {state.DiveCount}/4");
        if (state.Complete) ImGui.TextUnformatted(state.Failed ? "本輪有失誤，可重置重練。" : "雙眼練習完成！");
        else if (showHints)
        {
            var role = (int)world.Party.PlayerRole;
            if (!state.BuffsApplied) ImGui.TextUnformatted("到南側阿爾菲諾旁集合取得兩種思念。");
            else if (!state.ColorsAssigned) ImGui.TextUnformatted("預站位，等待紅藍連線。");
            else
            {
                ImGui.TextUnformatted(state.Red[role] ? "你目前是紅線" : "你目前是藍線");
                if (state.SwapCooldown[role] > 0) ImGui.TextUnformatted($"換線冷卻：{state.SwapCooldown[role]:F1} 秒");
                if (state.SwapTarget[role] >= 0)
                    ImGui.TextUnformatted($"前往 {RoleName(state.SwapTarget[role])} 接紅線。");
                else if (state.MirageStarted) ImGui.TextUnformatted(state.Red[role] ? "紅線分散；被俯衝後等待接線。" : "藍線在西側眼睛集合，等待換線。");
                else if (!state.YellowDone) ImGui.TextUnformatted(state.Red[role] != (role < 4) ? "顏色不符：到中間交換。" : state.YellowReady ? "黃球已長大兩次，坦補雙人撞球。" : "保持預站位，等待黃球長大兩次。");
                else if (!state.BlueDone) ImGui.TextUnformatted(state.BlueReady ? "DPS 取得紅線後單人撞藍球。" : "DPS 與對應坦補交換，等待藍球長大兩次。");
                else ImGui.TextUnformatted("移動到西側準備幻象俯衝。");
            }
        }
        if (showMap) DrawMap();
    }
    private static string RoleName(int role) => new[] { "MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4" }[role];
    private void DrawMap()
    {
        var scale = 5.5f * ImGuiHelpers.GlobalScale;
        var size = 260 * ImGuiHelpers.GlobalScale;
        var center = ImGui.GetCursorScreenPos() + new Vector2(size / 2);
        var draw = ImGui.GetWindowDrawList();
        Vector2 Map(Vector3 p) => center + new Vector2(p.X, p.Z) * scale;
        draw.AddCircle(center, 21 * scale, 0xFFAAAAAA, 80, 2);
        draw.AddText(center + new Vector2(-5, -23 * scale), 0xFFFFFFFF, "北");
        draw.AddCircle(Map(DsrP4EyesState.RedEye), 4 * scale, 0xFF5555FF, 40, 2);
        draw.AddCircle(Map(DsrP4EyesState.BlueEye), 4 * scale, 0xFFFFAA55, 40, 2);
        if (state!.ColorsAssigned && !state.MirageStarted)
            for (var i = 0; i < 6; i++)
                if (!state.OrbsPopped[i])
                    draw.AddCircle(Map(DsrP4EyesState.OrbPosition(i)), (i < 2 ? state.YellowReady : state.BlueReady) ? 2 * scale : scale,
                        i < 2 ? 0xFF00DDEE : 0xFFFFAA55, 30, 2);
        for (var role = 0; role < 8; role++)
        {
            var position = Map(state.Positions[role]);
            if (state.ColorsAssigned && !state.Complete)
                draw.AddLine(position, Map(state.Red[role] ? DsrP4EyesState.RedEye : DsrP4EyesState.BlueEye), state.Red[role] ? 0xFF5555FFu : 0xFFFFAA55u);
            draw.AddCircleFilled(position, 3, role == (int)world!.Party.PlayerRole ? 0xFF55FF55u : 0xFFFFFFFFu);
            draw.AddText(position + new Vector2(3, 0), 0xFFFFFFFF, RoleName(role));
        }
        if (showHints && !state.Complete) draw.AddCircle(Map(DsrP4EyesAi.Destination(state, (int)world!.Party.PlayerRole)), scale, 0xFF55FF55, 24, 2);
        ImGui.Dummy(new Vector2(size));
    }
}
