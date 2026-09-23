using System;
using System.Numerics;
using AnoMech.Core.SimObjects;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AnoMech.Scenarios.Dsr.P3Wyrmhole;

public sealed partial class DsrP3WyrmholeScenario
{
    public void DrawSettings()
    {
        ImGui.TextUnformatted("tuuf／Elemental：Easthogg（箭頭朝東）");
        ImGui.Combo("自己的麻將", ref playerNumber, ["隨機", "一號", "二號", "三號"], 4);
        ImGui.Combo("自己的箭頭", ref playerArrows, ["隨機", "無箭頭", "有箭頭（隨機方向）", "上箭頭", "下箭頭"], 5);
        ImGui.TextDisabled("選項於下一次開始或重置時生效。");
        var playMusic = !Plugin.Config.SuppressBgm;
        if (ImGui.Checkbox("背景音樂：邪龍急襲", ref playMusic))
        {
            Plugin.Config.SuppressBgm = !playMusic;
            Plugin.Config.Save();
        }
        ImGui.TextDisabled("配樂使用遊戲的背景音樂音量設定。");
        ImGui.TextDisabled("本關練習尼德霍格麻將、兩次普攻、隨機龍槍與最後四座人數塔。");
        ImGui.TextDisabled("四塔：坦近戰依順時針 → 逆時針 → 對角補位；補遠留原塔。");
        ImGui.TextDisabled("西北 MT＋D3／東北 ST＋D4／西南 D1＋H1／東南 D2＋H2");
        ImGui.TextDisabled("時間軸依 FFLogs 第 42 場校正；四塔後包含坦克接線、五次普攻與固定 C 點騰龍槍。");
        ImGui.Checkbox("顯示站位提示", ref showHints);
        ImGui.SameLine();
        ImGui.Checkbox("顯示戰術圖", ref showMap);
        ImGui.Checkbox("固定隨機種子", ref fixedSeed);
        if (fixedSeed) { ImGui.SetNextItemWidth(150); ImGui.InputInt("種子", ref seed); }
        if (state == null || world == null) return;
        ImGui.Separator();
        ImGui.TextUnformatted($"P3-尼德霍格　{state.Time:F1} 秒　種子 {state.Seed}");
        if (state.Complete) ImGui.TextUnformatted(state.Failed ? "本輪有失誤，可重置重練。" : "本輪完成！");
        if (showHints && !state.Complete)
        {
            var role = (int)world.Party.PlayerRole;
            if (!state.NumbersAssigned) ImGui.TextUnformatted("準備：八方預站位，等待數字點名。");
            if (state.TethersActive)
                ImGui.TextUnformatted(role == 0 ? "MT：走進本體連線接線，回王腳下。" : role == 1
                    ? "ST：走進分身連線接線，回王腳下。" : "補遠引導槍後往內躲避，遠離坦克接線範圍。");
            else if (state.TethersResolved)
                ImGui.TextUnformatted("接線已結算：五次普攻後，固定 C 點騰龍槍。");
            else if (state.Time >= 58.115f)
            {
                string[] names = ["西北", "東北", "東南", "西南"];
                var tower = state.FinalTowersVisible || state.FinalTowersResolved
                    ? state.FinalTowerAssignment(role) : DsrP3WyrmholeState.FinalTowerHome(role);
                ImGui.TextUnformatted(state.FinalTowersResolved ? "最後四塔已結算。" : state.FinalTowersVisible
                    ? $"最後四塔：前往{names[tower]}，需要 {state.FinalTowerCounts[tower]} 人。"
                    : $"最後四塔：{names[tower]}預站位，等待塔出現。");
            }
            if (state.NumbersAssigned && state.Time < 46f)
                ImGui.TextUnformatted($"你是 {state.Order[role] + 1} 號；位置：{LaneName(state.Order[role], state.ArrowsAssigned ? state.Lane[role] : state.NumberLane[role])}");
            if (state.ArrowsAssigned && state.Time < 46f)
                ImGui.TextUnformatted(state.Direction[role] switch
                {
                    1 => "上箭頭：朝東，塔落在面前 15 公尺。",
                    -1 => "下箭頭：朝東，塔落在身後 15 公尺。",
                    _ => "無箭頭：塔落在原地。"
                });
            if (state.Time is >= 10.105f and < 24.455f or >= 31.606f and < 46f)
                ImGui.TextUnformatted(state.OutFirst[state.Time < 24.455f ? 0 : 1] ? "本輪：先外後內" : "本輪：先內後外");
        }
        if (showMap) DrawMap();
    }

    private static string LaneName(int order, int lane) => order == 1
        ? lane == 0 ? "西北" : "東北"
        : lane switch { 0 => "西", 1 => "南", _ => "東" };

    private void DrawMap()
    {
        var s = state!;
        var scale = 5.5f * ImGuiHelpers.GlobalScale;
        var size = 260f * ImGuiHelpers.GlobalScale;
        var center = ImGui.GetCursorScreenPos() + new Vector2(size / 2);
        var draw = ImGui.GetWindowDrawList();
        Vector2 Map(Vector3 position) => center + new Vector2(position.X, position.Z) * scale;
        draw.AddCircle(center, 21 * scale, 0xFFAAAAAA, 80, 2);
        draw.AddCircle(center, 8 * scale, 0x66888888, 64, 1);
        draw.AddText(center + new Vector2(-5, -23 * scale), 0xFFFFFFFF, "北");
        for (var wave = 0; wave < 3; wave++)
            if (s.TowersVisible[wave])
                foreach (var tower in s.Towers[wave]) draw.AddCircle(Map(tower), 5 * scale, 0xFF00DDEE, 40, 2);
        if (s.FinalTowersVisible)
            for (var tower = 0; tower < 4; tower++)
            {
                var position = Map(DsrP3WyrmholeState.FinalTowerPosition(tower));
                draw.AddCircle(position, 5 * scale, 0xFF00DDEE, 40, 2);
                draw.AddText(position + new Vector2(-4, -18), 0xFF00DDEE, s.FinalTowerCounts[tower].ToString());
            }
        foreach (var line in lines)
        {
            var side = new Vector3(-line.Direction.Z, 0, line.Direction.X) * 4;
            var end = line.Source + line.Direction * 45;
            draw.AddQuad(Map(line.Source + side), Map(end + side), Map(end - side), Map(line.Source - side), 0xFF5555FF, 2);
        }
        if (showHints && !s.Complete)
            draw.AddCircle(Map(DsrP3WyrmholeAi.Destination(s, (int)world!.Party.PlayerRole)), scale, 0xFF55FF55, 20, 2);
        string[] roles = ["MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4"];
        foreach (var (role, member) in world!.Party.FilledSlots())
        {
            var point = Map(member.Position);
            var color = member is SimPlayer ? 0xFF55FF55u : 0xFFFFFFFFu;
            draw.AddCircleFilled(point, 3, color);
            draw.AddText(point + new Vector2(4, 0), color, roles[(int)role]);
            if (member is SimPlayer)
                draw.AddLine(point, point + new Vector2(MathF.Sin(member.Rotation), MathF.Cos(member.Rotation)) * 12, color, 2);
        }
        ImGui.Dummy(new Vector2(size));
        ImGui.TextDisabled("綠圈：下一個位置　黃圈：塔　紅框：已鎖定直線");
    }
}
