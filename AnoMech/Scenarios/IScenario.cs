using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios;

// The mechanic timeline for one encounter fragment. Shared identity lives on the owning
// IZone (via Phase.Zone) and IPhase; a scenario declares only what is its own.
public interface IScenario
{
    string Name { get; }

    // Its phase — usually `=> TopZone.P5`.
    IPhase Phase { get; }

    bool SupportsSolo => false;

    // Selectable strats. Run's selectedAi indexes this (null = solo); region buttons derive
    // from each strat's IScenarioAi.Group.
    IReadOnlyList<IScenarioAi> AiStrats { get; }

    void Run(SimWorld world, int? selectedAi);
    void Tick(float delta, float elapsed) { }
    void DrawSettings() { }

    // Whether the scenario has reached its own natural end, for Game's mechanic-streak
    // tracking. Default covers scenarios whose whole timeline lives on world.Events (the
    // common case: nothing to override). A scenario that schedules its mechanic on a private
    // EventScheduler instead (e.g. one kept immune to EventTimeScale for real-time precision)
    // must override this to check that queue instead, since world.Events would otherwise
    // look permanently empty from the first tick.
    bool IsFinished(SimWorld world) => world.Events.IsEmpty;
}
