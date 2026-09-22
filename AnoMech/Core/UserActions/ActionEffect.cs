using System;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

// What a player action does client-side, standing in for the firewalled server response.
// Composable: leaf effects (Gauge / Status) do the work; wrappers (Combo / Random) gate
// them. Authored via the factory helpers in JobActions (Gauge(...), Status(...), etc.).
internal interface IActionEffect
{
    void Apply(ActionContext ctx);
}

// Per-dispatch state an effect may need.
internal sealed class ActionContext(uint actionId, SimPlayer player, Random rng)
{
    public uint ActionId { get; } = actionId;
    public SimPlayer Player { get; } = player;
    public Random Rng { get; } = rng;
}

// Leaf: add an amount to a job gauge (ResourceGauge.Add clamps to [0, Max]).
internal sealed unsafe class GaugeEffect(ResourceGauge gauge, int amount) : IActionEffect
{
    public void Apply(ActionContext ctx)
    {
        var jgm = JobGaugeManager.Instance();
        if (jgm != null) gauge.Add(jgm, amount);
    }
}

// Leaf: grant a status to the player for `duration` seconds (replaces any existing copy).
// `stacks` sets Status.Param — 0 for a plain buff, or the stack count for a stacking buff
// (Requiescat, Sacred Sight); we don't decrement per-consume yet, it just expires.
internal sealed class StatusEffect(ushort statusId, float duration, int stacks = 0) : IActionEffect
{
    public void Apply(ActionContext ctx)
    {
        ctx.Player.RemoveStatus(statusId);
        ctx.Player.AddStatusParam(statusId, stacks, duration);
    }
}

// Wrapper: apply the inner effects only when the action lands as a valid combo continuation.
internal sealed class ComboEffect(IActionEffect[] inner) : IActionEffect
{
    public void Apply(ActionContext ctx)
    {
        if (!PlayerCombo.IsActiveContinuation(ctx.ActionId)) return;
        foreach (var e in inner) e.Apply(ctx);
    }
}

// Wrapper: apply the inner effects with `chance` probability (procs, e.g. DNC feathers).
internal sealed class RandomEffect(float chance, IActionEffect[] inner) : IActionEffect
{
    public void Apply(ActionContext ctx)
    {
        if (ctx.Rng.NextSingle() >= chance) return;
        foreach (var e in inner) e.Apply(ctx);
    }
}
