using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("絕龍詩模擬器設定###AnoMechConfig")
    {
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var onInn = configuration.OpenSimMenuOnInn;
        if (ImGui.Checkbox("進入旅館時開啟模擬器選單", ref onInn))
        {
            configuration.OpenSimMenuOnInn = onInn;
            configuration.Save();
        }

        var playBgm = !configuration.SuppressBgm;
        if (ImGui.Checkbox("播放英傑（P2 背景音樂）", ref playBgm))
        {
            configuration.SuppressBgm = !playBgm;
            configuration.Save();
        }

        var userActions = configuration.EnableUserActions;
        if (ImGui.Checkbox("模擬自身技能效果", ref userActions))
        {
            configuration.EnableUserActions = userActions;
            configuration.Save();
            if (userActions) Plugin.UserActions.Enable();
            else Plugin.UserActions.Disable();
        }

        if (configuration.EnableUserActions)
        {
            var threshold = configuration.CastInterruptThreshold;
            ImGui.SetNextItemWidth(90 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputFloat("滑步施法容許時間（秒）", ref threshold, 0.05f, 0.1f, "%.2f"))
            {
                configuration.CastInterruptThreshold = Math.Clamp(threshold, 0f, 5f);
                configuration.Save();
            }
        }

        ImGui.Separator();

        var logging = configuration.EnableEventLogging;
        if (ImGui.Checkbox("啟用事件紀錄", ref logging))
        {
            configuration.EnableEventLogging = logging;
            configuration.Save();
            if (logging) Plugin.LogManager.Open();
            else Plugin.LogManager.Close();
        }
        ImGui.SameLine();
        if (ImGui.Button("開啟紀錄資料夾"))
            Plugin.LogManager.OpenLogsFolder();

#if DEBUG
        ImGui.Separator();

        var safeMode = configuration.SafeMode;
        if (ImGui.Checkbox("安全模式（除錯）", ref safeMode))
        {
            configuration.SafeMode = safeMode;
            configuration.Save();
        }
        if (safeMode)
            ImGui.TextWrapped(
                "安全模式會在模擬場景中隔離伺服器訊息。" +
                "此時不會顯示隊員加入、離隊、準備確認" +
                "或副本配對通知。");
        else
            ImGui.TextWrapped(
                "關閉安全模式後，仍可收到" +
                "隊伍更新、準備確認與配對通知，但較容易干擾" +
                "模擬場景。模擬期間仍會阻擋向伺服器" +
                "傳送操作。");
#endif
    }
}
