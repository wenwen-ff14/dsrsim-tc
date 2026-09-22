using System;
using System.Text;

namespace AnoMech.Helpers;

internal static class Utf8NameEncoder
{
    public static void Write(string name, Span<byte> destination)
    {
        destination.Clear();
        if (destination.Length < 2) return;
        // Leave the terminator and never truncate in the middle of a UTF-8 character.
        Encoding.UTF8.GetEncoder().Convert(name.AsSpan(), destination[..^1], true,
            out _, out _, out _);
    }
}
