using System;
using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Scenarios;

internal static class ShuffleExtensions
{
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Enumerate();

        IEnumerable<T> Enumerate()
        {
            var items = source.ToArray();
            Random.Shared.Shuffle(items);
            foreach (var item in items)
                yield return item;
        }
    }
}
