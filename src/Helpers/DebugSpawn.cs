using UnityEngine;

namespace BbdCards.Helpers;

internal static class DebugSpawn
{
    /// <summary>Checks the spawn key and host state, then returns the active camera.</summary>
    public static bool TryGetCamera(bool enabled, KeyCode key, int count, out Camera camera)
    {
        camera = null!;

        if (!enabled || count == 0 || !Input.GetKeyDown(key))
        {
            return false;
        }

        if (!SemiFunc.IsMasterClientOrSingleplayer() || !LevelGenerator.Instance
            || !LevelGenerator.Instance.Generated || !ValuableDirector.instance || !SemiFunc.RunIsLevel())
        {
            return false;
        }

        camera = Camera.main;

        return camera;
    }
}
