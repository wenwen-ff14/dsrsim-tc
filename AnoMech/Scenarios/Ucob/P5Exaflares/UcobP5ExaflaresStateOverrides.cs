namespace AnoMech.Scenarios.Ucob.P5Exaflares;

// User-controlled overrides for UcobP5ExaflaresState's randomized fields. Direction is where
// the whole set of lanes travels TO; null leaves it randomized at scenario start. Which two
// lanes fire together is always randomized, as it is in the fight.
public sealed class UcobP5ExaflaresStateOverrides
{
    public Direction? Direction { get; set; }
}
