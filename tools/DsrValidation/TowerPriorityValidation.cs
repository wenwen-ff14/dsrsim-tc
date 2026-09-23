using AnoMech.Scenarios.Dsr.P2Sanctity;
using System.Numerics;

internal static class TowerPriorityValidation
{
    public static void Run()
    {
        int[] priority = [1, 0, 2];
        var layouts = 0;
        var retainedUnequalArcs = 0;
        for (var north = 1; north <= 6; north++)
        for (var east = 1; east <= 6; east++)
        for (var south = 1; south <= 6; south++)
        for (var west = 1; west <= 6; west++)
        {
            int[] masks = [north, east, south, west];
            if (masks.Sum(m => BitOperations.PopCount((uint)m)) is not (5 or 6)) continue;
            layouts++;
            var baseline = masks.Select(m => priority.First(t => (m & (1 << t)) != 0)).ToArray();
            var result = DsrP2SanctityState.ChooseOuterTowers(masks);
            if (result[1] != baseline[1] || result[3] != baseline[3]) throw new Exception("E/W tower priority changed");
            if (Math.Abs(baseline[0] - baseline[2]) < 2)
            {
                if (!result.SequenceEqual(baseline)) throw new Exception("Non-cursed layout was optimized to 180 degrees");
                if (baseline[0] != baseline[2]) retainedUnequalArcs++;
            }
            else
            {
                var canEscape = (from n in priority from s in priority
                    where (north & (1 << n)) != 0 && (south & (1 << s)) != 0 && Math.Abs(n - s) < 2
                    select n).Any();
                if ((Math.Abs(result[0] - result[2]) < 2) != canEscape)
                    throw new Exception("Cursed tower adjustment did not follow available towers");
            }
            for (var q = 0; q < 4; q++)
                if ((masks[q] & (1 << result[q])) == 0) throw new Exception("Assigned a missing tower");
        }
        if (layouts != 810 || retainedUnequalArcs == 0) throw new Exception("Incomplete tower layout coverage");
        var regression = DsrP2SanctityState.ChooseOuterTowers([3, 1, 1, 3]);
        if (regression[0] != 1 || regression[2] != 0) throw new Exception("Center tower must keep priority even when 180 degrees is possible");
        Console.WriteLine($"Tuuf towers: all {layouts} layouts; {retainedUnequalArcs} retain 150/210-degree priority; only cursed patterns adjust.");
    }
}
