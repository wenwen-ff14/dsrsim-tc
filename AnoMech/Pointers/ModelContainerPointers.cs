using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace AnoMech.Pointers;

internal static unsafe class ModelContainerPointers
{
    // Named in newer ClientStructs; API 13 exposes only the surrounding model fields.
    public static ref byte ModeAttributeFlags(ModelContainer* model) => ref *((byte*)model + 0x22);
    public static byte ModelScaleId(ModelContainer* model) => *((byte*)model + 0x21);

}
