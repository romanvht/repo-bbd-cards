using UnityEngine;

namespace BbdCards.Helpers;

internal static class DebugSpawn
{
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
