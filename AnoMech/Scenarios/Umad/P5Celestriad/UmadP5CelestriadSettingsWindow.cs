using System;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Umad.P5Celestriad;

// Settings panel for the P5 Celestriad scenario: force the player's own element/free
// assignment and each Catastrophic Choice set's variant, or leave them randomized.
// Mirrors UmadP5ExaflaresSettingsWindow (SettingsGrid two-column layout, "Auto" reset).
public sealed class UmadP5CelestriadSettingsWindow
{
    public UmadP5CelestriadStateOverrides Overrides { get; } = new();

    private static readonly string[] ElementLabels = ["Random", "Fire", "Ice", "Lightning", "Free"];
    private static readonly string[] VariantLabels = ["Random", "Aero (green)", "Earth (brown)"];

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerElement = CelestriadElementOverride.Random;
            Overrides.Set1 = CatastrophicVariantOverride.Random;
            Overrides.Set3 = CatastrophicVariantOverride.Random;
        }

        if (SettingsGrid.Begin("##p5celestriad"))
        {
            DrawRow("Your element:", "##playerelement", ElementLabels, (int)Overrides.PlayerElement,
                v => Overrides.PlayerElement = (CelestriadElementOverride)v);
            DrawRow("Set 1 variant:", "##set1variant", VariantLabels, (int)Overrides.Set1,
                v => Overrides.Set1 = (CatastrophicVariantOverride)v);
            DrawRow("Set 3 variant:", "##set3variant", VariantLabels, (int)Overrides.Set3,
                v => Overrides.Set3 = (CatastrophicVariantOverride)v);
            SettingsGrid.End();
        }
    }

    private static void DrawRow(string label, string id, string[] labels, int current, Action<int> set)
    {
        SettingsGrid.Row(label);
        var idx = current;
        ImGui.SetNextItemWidth(140);
        if (ImGui.Combo(id, ref idx, labels, labels.Length))
            set(idx);
    }
}
