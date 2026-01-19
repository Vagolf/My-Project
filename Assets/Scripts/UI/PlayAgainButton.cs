using UnityEngine;
using UnityEngine.SceneManagement;

// Attach this to the "Play Again" button on the defeat screen.
// When pressed, it resets timeScale and reloads the current scene so the match starts fresh.
public class PlayAgainButton : MonoBehaviour
{
    [Header("Load Options")] 
    [Tooltip("Use LoadSceneAsync instead of LoadScene")] 
    [SerializeField] private bool useAsyncLoad = false;
    [Tooltip("Set Time.timeScale before reloading (defeat flow often pauses at 0)")] 
    [SerializeField] private bool resetTimeScale = true;
    [SerializeField] private float timeScaleOnReload = 1f;

    [Header("Optional: close this panel before reload")] 
    [SerializeField] private GameObject defeatPanel;

    // Bind this to the Button.onClick
    public void PlayAgain()
    {
        if (defeatPanel != null) defeatPanel.SetActive(false);

        if (resetTimeScale)
            Time.timeScale = timeScaleOnReload;

        // Clear any global pause flag if your project uses it
        try { PauseGame.GameIsPause = false; } catch {}

        var scene = SceneManager.GetActiveScene();
        if (useAsyncLoad)
            SceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
    }
}
