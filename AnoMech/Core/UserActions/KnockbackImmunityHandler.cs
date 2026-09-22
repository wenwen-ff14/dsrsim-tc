using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

internal sealed unsafe class KnockbackImmunityHandler : IUserActionHandler
{
    public void OnAction(ActionType actionType, uint actionId)
    {
        if (actionType != ActionType.Action) return;
        var status = actionId switch { 7548 => (ushort)1209, 7559 => (ushort)160, _ => (ushort)0 };
        if (status != 0) Plugin.GameInstance?.Player?.AddStatus(status, 6);
    }

    public void OnScenarioStart()
    {
        var manager = ActionManager.Instance();
        if (manager == null) return;
        foreach (var action in new uint[] { 7548, 7559 })
        {
            var group = manager->GetRecastGroup((int)ActionType.Action, action);
            if (group < 0) continue;
            var recast = manager->GetRecastGroupDetail(group);
            if (recast == null) continue;
            recast->IsActive = false;
            recast->Elapsed = 0;
        }
    }
}
