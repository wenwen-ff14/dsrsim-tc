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
        var playMusic = !Plugin.Config.SuppressBgm;
        if (ImGui.Checkbox("背景音樂：邪龍急襲", ref playMusic))
        {
            Plugin.Config.SuppressBgm = !playMusic;
            Plugin.Config.Save();
        }
        if (playMusic && Plugin.GameInstance.Bgm.IsActive)
        {
            ImGui.SameLine();
            if (ImGui.Button("重新播放音樂")) Plugin.GameInstance.Bgm.Restart();
        }
        ImGui.TextDisabled("配樂使用遊戲的背景音樂音量設定。");
        ImGui.TextDisabled("本關練習三輪數字跳躍、踩塔、內外圈、分攤、直線與龍槍。");
        ImGui.TextDisabled("四人數塔與雙連線尚未加入；時間軸及原生演出待實機校正。");
        ImGui.Checkbox("顯示站位提示", ref showHints);
        ImGui.SameLine();
        ImGui.Checkbox("顯示戰術圖", ref showMap);
        ImGui.Checkbox("固定隨機種子", ref fixedSeed);
        if (fixedSeed) { ImGui.SetNextItemWidth(150); ImGui.InputInt("種子", ref seed); }
        if (state == null || world == null) return;
        ImGui.Separator();
        ImGui.TextUnformatted($"數字龍　{state.Time:F1} 秒　種子 {state.Seed}");
        if (state.Complete) ImGui.TextUnformatted(state.Failed ? "本輪有失誤，可重置重練。" : "本輪完成！");
        if (showHints && !state.Complete)
        {
            var role = (int)world.Party.PlayerRole;
            if (state.Time >= 8)
                ImGui.TextUnformatted($"你是 {state.Order[role] + 1} 號；位置：{LaneName(state.Order[role], state.Lane[role])}");
            if (state.Time >= 10)
                ImGui.TextUnformatted(state.Direction[role] switch
                {
                    1 => "上箭頭：朝東，塔落在面前 15 公尺。",
                    -1 => "下箭頭：朝東，塔落在身後 15 公尺。",
                    _ => "無箭頭：塔落在原地。"
                });
            if (state.Time is >= 10.1f and < 24.5f or >= 31.6f and < 46f)
                ImGui.TextUnformatted(state.OutFirst[state.Time < 24.5f ? 0 : 1] ? "本輪：先外後內" : "本輪：先內後外");
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
