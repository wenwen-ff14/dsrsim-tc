namespace AnoMech.Core.UserActions;

// A condition on the action just used — drives status removal (JobActions.StatusClearedOnAction).
// Authored via the factory helpers in JobActions (ActionId(...), AnyWeaponskill()).
internal interface IActionPredicate
{
    bool Matches(uint actionId);
}

internal sealed class ActionIdPredicate(uint id) : IActionPredicate
{
    public bool Matches(uint actionId) => actionId == id;
}

internal sealed class AnyWeaponskillPredicate : IActionPredicate
{
    public bool Matches(uint actionId)
    {
        var sheet = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        return sheet.TryGetRow(actionId, out var row) && row.ActionCategory.RowId == 3;
    }
}

// Any GCD — Weaponskill (3) or Spell (2). For procs consumed by the "next spell/GCD".
internal sealed class AnyGcdPredicate : IActionPredicate
{
    public bool Matches(uint actionId)
    {
        var sheet = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        return sheet.TryGetRow(actionId, out var row) && row.ActionCategory.RowId is 2 or 3;
    }
}
