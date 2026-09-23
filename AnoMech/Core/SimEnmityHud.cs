using System;
using System.Collections.Generic;
using AnoMech.Core.SimObjects;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AnoMech.Core;

internal sealed unsafe class SimEnmityHud : IDisposable
{
    private Hate? originalHate;
    private Hater? originalHater;
    private SimEnemy? selected;
    private bool pendingClear;

    public SimEnmityHud() => Plugin.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "_PartyList", DrawPartyEnmity);

    public void Refresh(IEnumerable<SimEnemy> enemies)
    {
        var ui = UIState.Instance();
        var targetSystem = TargetSystem.Instance();
        if (ui == null || targetSystem == null) return;
        selected = null;
        var count = 0;
        var target = targetSystem->GetTargetObjectId();
        foreach (var enemy in enemies)
        {
            if (!enemy.IsActive || !enemy.HasEnmity || !enemy.InEnemyList || count >= 32) continue;
            originalHate ??= ui->Hate;
            originalHater ??= ui->Hater;
            ref var row = ref ui->Hater.Haters[count++];
            row = default;
            row.EntityId = enemy.EntityId;
            row.NameString = enemy.DisplayName;
            foreach (var entry in enemy.Enmity)
                if (entry.Member is SimPlayer) row.Enmity = entry.Percent;
            if (enemy.GameObjectId == target) selected = enemy;
        }
        if (count == 0) { Clear(); return; }
        ui->Hater.HaterCount = count;
        ui->Hate = default;
        if (selected != null)
        {
            ui->Hate.HateTargetId = selected.EntityId;
            for (var rank = 1; rank <= 8; rank++)
                foreach (var entry in selected.Enmity)
                    if (entry.Rank == rank && entry.Member is {} member)
                        ui->Hate.HateInfo[ui->Hate.HateArrayLength++] = new HateInfo { EntityId = member.EntityId, Enmity = entry.Percent };
        }
        pendingClear = true;
        DirtyParty();
    }

    public void Clear()
    {
        pendingClear |= originalHate != null;
        var ui = UIState.Instance();
        if (ui != null)
        {
            if (originalHate is {} hate) ui->Hate = hate;
            if (originalHater is {} hater) ui->Hater = hater;
        }
        originalHate = null;
        originalHater = null;
        selected = null;
        if (pendingClear) DirtyParty();
    }

    private static void DirtyParty()
    {
        var stage = AtkStage.Instance();
        if (stage == null) return;
        var array = stage->GetNumberArrayData(NumberArrayType.PartyList);
        if (array != null) array->UpdateState = 1;
    }

    private void DrawPartyEnmity(AddonEvent type, AddonArgs args)
    {
        if (originalHate == null && !pendingClear) return;
        if (args is not AddonRequestedUpdateArgs update) return;
        var arrays = (NumberArrayData**)update.NumberArrayData;
        if (arrays == null || arrays[(int)NumberArrayType.PartyList] == null) return;
        var party = (PartyListNumberArray*)arrays[(int)NumberArrayType.PartyList]->IntArray;
        if (party == null) return;
        // API 13 names this slot Unk7; the upstream layout identifies it as the leader index.
        arrays[(int)NumberArrayType.PartyList]->IntArray[7] = -1;
        for (var i = 0; i < 8; i++)
        {
            ref var row = ref party->PartyMembers[i];
            row.EnmityLevel = 0;
            row.EnmityPercent = 0;
            if (selected == null) continue;
            foreach (var entry in selected.Enmity)
                if (entry.Member is {} member && member.EntityId == (uint)row.ContentId)
                {
                    row.EnmityLevel = entry.Rank;
                    row.EnmityPercent = entry.Percent;
                    if (entry.Rank == 1) arrays[(int)NumberArrayType.PartyList]->IntArray[7] = i;
                }
        }
        pendingClear = false;
    }

    public void Dispose()
    {
        Clear();
        Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "_PartyList", DrawPartyEnmity);
    }
}
