using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;

namespace AnoMech.Helpers;

public static unsafe class GameObjectHelper
{
    public static void WriteName(GameObject* obj, string name)
    {
        Utf8NameEncoder.Write(name, obj->Name);
    }
}
