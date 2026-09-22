using System;
using System.Collections.Generic;

namespace AnoMech.Core.Map;

internal sealed class MapEffectResets
{
    private readonly Dictionary<byte, uint> pending = new();

    public void Track(byte index, uint state) => pending[index] = state;

    public void Reset(Action<uint, byte> apply)
    {
        foreach (var (index, state) in pending) apply(state, index);
        pending.Clear();
    }
}
