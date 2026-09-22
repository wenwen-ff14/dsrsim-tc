using System;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.Game;

public sealed unsafe class Bgm : IDisposable
{
    // The inn can keep its own event music above the content scene. Borrow that
    // scene for practice, then return its previous track when practice ends.
    private const uint MusicSceneId = 0;
    private ushort current;
    private ushort previous;
    private bool captured;
    private long retryAfter;

    public void Play(ushort bgmId)
    {
        if (bgmId == 0) return;
        var system = BGMSystem.Instance();
        if (system == null) return;
        foreach (var scene in system->Scenes)
        {
            if (scene.SceneId != MusicSceneId) continue;
            if (!captured)
            {
                previous = scene.BgmId;
                captured = true;
            }
            if (bgmId == current && scene.BgmId == bgmId &&
                scene.PlayState == BGMSystem.PlayState.Playing) return;
            break;
        }
        if (bgmId == current && Environment.TickCount64 < retryAfter) return;
        BGMSystem.SetBGM(bgmId, MusicSceneId);
        current = bgmId;
        retryAfter = Environment.TickCount64 + 2000;
    }

    public bool IsActive => current != 0;

    public void Restart()
    {
        var track = current;
        if (track == 0) return;
        var system = BGMSystem.Instance();
        if (system == null) return;
        system->ResetBGM(MusicSceneId);
        current = 0;
        Play(track);
    }

    public void Reset()
    {
        var system = BGMSystem.Instance();
        if (current != 0 && system != null)
            foreach (var scene in system->Scenes)
                if (scene.SceneId == MusicSceneId && scene.BgmId == current)
                {
                    if (captured && previous != 0) BGMSystem.SetBGM(previous, MusicSceneId);
                    else system->ResetBGM(MusicSceneId);
                    break;
                }
        current = previous = 0;
        captured = false;
        retryAfter = 0;
    }

    public void Dispose() => Reset();
}
