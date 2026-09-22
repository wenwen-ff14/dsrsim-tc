using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;



namespace AnoMech.Scenarios.Dsr.P2Sanctity;

public sealed partial class DsrP2SanctityScenario
{
    private string poseDiagnostics = "請先開始練習，等候衝鋒騎士出現。";

    private void CapturePoseDiagnostics()
    {
        var report = new System.Text.StringBuilder("AnoMech DSR pose diagnostics\n");
        foreach (var enemy in new[] { boss, darkKnight, knights[0], knights[1] })
            if (enemy != null) report.AppendLine(enemy.DescribePose());
        poseDiagnostics = report.ToString();
        Plugin.Log.Information(poseDiagnostics);
    }

    public void DrawSettings()
    {
        ImGui.TextUnformatted("打法：tuuf／Elemental　同職能換組、南北隕石");
        var playMusic = !Plugin.Config.SuppressBgm;
        if (ImGui.Checkbox("背景音樂：英傑", ref playMusic))
        {
            Plugin.Config.SuppressBgm = !playMusic;
            Plugin.Config.Save();
        }
        if (playMusic && Plugin.GameInstance.Bgm.IsActive)
        {
            ImGui.SameLine();
            if (ImGui.Button("重新播放音樂")) Plugin.GameInstance.Bgm.Restart();
        }
        ImGui.TextDisabled("配樂與隕石音效使用遊戲的背景音樂／音效音量設定。");
        ImGui.Checkbox("顯示站位提示", ref showHints);
        ImGui.SameLine(); ImGui.Checkbox("顯示戰術圖", ref showMap);
        ImGui.Checkbox("固定隨機種子（重複同一組點名）", ref fixedSeed);
        if (ImGui.Button("複製騎士動作診斷")) ImGui.SetClipboardText(poseDiagnostics);
        if (fixedSeed) { ImGui.SetNextItemWidth(150); ImGui.InputInt("種子", ref seed); }
        ImGui.SetNextItemWidth(180);
        ImGui.Combo("騎士方向", ref direction, ["隨機", "順時針", "逆時針"], 3);
        ImGui.SetNextItemWidth(180);
        ImGui.BeginDisabled(meteorAngleSelection != 0);
        ImGui.Combo("隕石點名", ref meteorPreference, ["隨機", "坦克／補師", "輸出", "保證點名自己"], 4);
        ImGui.EndDisabled();
        ImGui.SetNextItemWidth(180);
        ImGui.Combo("自己隕石跑動角度", ref meteorAngleSelection, ["隨機", "120°", "150°", "180°", "210°", "240°"], 6);
        ImGui.TextDisabled("指定角度會保證點名自己；角度是你沿外圈順時針跑的路線。");
        ImGui.TextDisabled("種子、方向、點名與跑動角度選項於下次開始生效。");
        ImGui.TextDisabled("外圈擊退請使用親疏自行／沉穩詠唱，並啟用「模擬自身技能效果」。");
        if (state == null || world?.Party.Player == null) return;
        ImGui.Separator();
        string[] stages = ["準備", "分攤劍與雙視線", "騎士衝鋒與白球", "隕石換位、兩人冰圈", "第一輪踩塔", "順時針放隕石、準備擊退", "第二輪踩塔完成", "聖仗練習結束"];
        ImGui.TextUnformatted($"{stages[(int)state.Stage]}　{time:F1} 秒　種子 {state.Seed}");
        if (state.FirstTowersVisible || state.Stage >= SanctityStage.FirstTowers)
            ImGui.TextUnformatted(state.HasMeteor((int)world.Party.PlayerRole)
                ? $"本輪自己隕石跑動角度：{state.MeteorArc((int)world.Party.PlayerRole):F0}°（順時針）"
                : "本輪你沒有隕石點名。");
        if (state.Stage == SanctityStage.Complete)
            ImGui.TextUnformatted(state.Failed ? "本輪有失誤；可重置後重練。" : "本輪完成！");
        if (showHints && state.Stage != SanctityStage.Complete)
        {
            var role = (int)world.Party.PlayerRole;
            if (state.Stage is SanctityStage.Swords or SanctityStage.Charges)
                ImGui.TextUnformatted($"你是第 {state.Groups[role] + 1} 組；{(state.Clockwise ? "順" : "逆")}時針，先避開王與龍眼視線，再等前方白球爆炸。");
            else if (state.Stage >= SanctityStage.Pairs)
            {
                ImGui.TextUnformatted($"你去{DsrP2SanctityState.Directions[state.Quadrants[role]]}；{(state.HasMeteor(role) ? "你有隕石，沿外圈順時針跑，記得開疾跑與防擊退" : "你沒有隕石")}。");
                if (state.FirstTowersVisible || state.Stage >= SanctityStage.FirstTowers)
                    ImGui.TextUnformatted(state.StartsInside(role) ? "內塔靠中心踩；之後從中心對準第二輪塔吃擊退。" : "外塔靠場邊踩；之後開防擊退。");
            }
        }
        if (showMap) DrawMap();
    }

    private void DrawMap()
    {
        var s = state!;
        var scale = 5.5f * ImGuiHelpers.GlobalScale;
        var size = 260 * ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos() + new Vector2(size / 2, size / 2);
        var draw = ImGui.GetWindowDrawList();
        Vector2 Map(Vector3 p) => origin + new Vector2(p.X, p.Z) * scale;
        void Circle(Vector3 p, float r, uint color) => draw.AddCircle(Map(p), r * scale, color, 48, 1.5f);
        draw.AddCircle(origin, 21 * scale, 0xFFAAAAAA, 90, 2);
        draw.AddText(origin + new Vector2(-6, -23 * scale), 0xFFFFFFFF, "北");
        if (s.Stage is SanctityStage.Swords or SanctityStage.Charges)
        {
            draw.AddText(Map(Vector3.Normalize(s.EyePosition) * 19), 0xFF66AAFF, "眼");
            draw.AddText(Map(Vector3.Normalize(s.BossPosition) * 19), 0xFF66AAFF, "王");
            draw.AddText(Map(s.DarkKnightPosition), 0xFFAA66FF, "暗");
            if (s.Stage == SanctityStage.Charges)
                for (var k = 0; k < 2; k++)
                    for (var i = s.Explosions; i < Math.Min(9, s.Explosions + 3); i++)
                        Circle(s.SpherePoints[k][i], 9, 0x666666FF);
        }
        if (s.FirstTowersVisible)
            foreach (var tower in s.FirstTowers) Circle(tower, 3, 0xFF00DDEE);
        if (s.SecondTowersVisible)
            for (var r = 0; r < 8; r++) Circle(s.SecondTower(r), 3, 0xFF00DDEE);
        foreach (var p in ice) Circle(p, 7, 0x88FFAA55);
        if (s.FireVisible)
            for (var q = 0; q < 4; q++)
                Circle(DsrP2SanctityState.Polar(q * 90 + 45, DsrConstants.Geometry.FireCenterRadius), 7, 0x886666FF);
        foreach (var p in meteors) Circle(p, 2.5f, 0xFF6666FF);
        if (showHints && s.Stage != SanctityStage.Complete)
            Circle(DsrP2SanctityAi.Destination(s, (int)world!.Party.PlayerRole), 1, 0xFF55FF55);
        foreach (var (role, member) in world!.Party.FilledSlots())
        {
            var p = Map(member.Position);
            var color = member is SimPlayer ? 0xFF55FF55u : 0xFFFFFFFFu;
            draw.AddCircleFilled(p, 3, color);
            draw.AddText(p + new Vector2(3, 0), color, DsrP2SanctityState.Roles[(int)role]);
            if (member is SimPlayer)
                draw.AddLine(p, p + new Vector2(MathF.Sin(member.Rotation), MathF.Cos(member.Rotation)) * 12, color, 2);
        }
        ImGui.Dummy(new(size, size));
        ImGui.TextDisabled("綠點：你／提示位置　黃圈：塔　紅圈：危險區");
    }
}




