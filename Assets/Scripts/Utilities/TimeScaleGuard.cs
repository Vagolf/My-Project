using UnityEngine;

// Global safeguard: make sure time is resumed after scene loads.
// This helps if some UI was active by mistake and paused time (Time.timeScale = 0).
public static class TimeScaleGuard
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureResumed()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
            try { PauseGame.GameIsPause = false; } catch { }
            Debug.Log("[TimeScaleGuard] Detected timeScale=0 on scene load. Reset to 1.");
        }
    }
}
