namespace AnoMech.Scenarios.Umad.P5Celestriad;

public enum CelestriadElementOverride { Random, Fire, Ice, Lightning, Free }

public enum CatastrophicVariantOverride { Random, Aero, Earth }

// User-controlled overrides for UmadP5CelestriadState's randomized fields. Bound by the
// scenario's settings UI; Random leaves the field randomized at scenario start. Same shape
// as UmadP5ExaflaresStateOverrides.
public sealed class UmadP5CelestriadStateOverrides
{
    public CelestriadElementOverride PlayerElement { get; set; } = CelestriadElementOverride.Random;
    public CatastrophicVariantOverride Set1 { get; set; } = CatastrophicVariantOverride.Random;
    public CatastrophicVariantOverride Set3 { get; set; } = CatastrophicVariantOverride.Random;
}
