using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to a Victory UI object. Wire its public methods to buttons:
// - NextStation(): go to next scene in Build Settings, or back to MainMenu if current is the last.
// - BackToMainMenu(): go to MainMenu and ask it to show the last-opened panel.
public class VictoryNextStation : MonoBehaviour
{
    [Header("Main Menu Target")]
    [Tooltip("Build index of MainMenu scene (0 by default)")]
    [SerializeField] private int mainMenuBuildIndex = 0;
    [Tooltip("Optional MainMenu scene name (used if buildIndex < 0)")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Loading Options")]
    [SerializeField] private bool useAsyncLoad = true;
    [SerializeField] private bool resetTimeScaleOnLoad = true;
    [SerializeField] private float timeScale = 1f;

    private void ResumeTimeIfNeeded()
    {
        if (resetTimeScaleOnLoad) Time.timeScale = timeScale;
        // clear pause flags if any
        try { PauseGame.GameIsPause = false; } catch { }
    }

    // Bind to "Next Station" button on Victory screen
    public void NextStation()
    {
        ResumeTimeIfNeeded();
        var curr = SceneManager.GetActiveScene().buildIndex;
        int count = SceneManager.sceneCountInBuildSettings;
        int next = curr + 1;
        if (next >= 0 && next < count)
        {
            if (useAsyncLoad) SceneManager.LoadSceneAsync(next, LoadSceneMode.Single);
            else SceneManager.LoadScene(next, LoadSceneMode.Single);
        }
        else
        {
            // At last scene -> go to main menu
            LoadMainMenu();
        }
    }

    // Bind to "Back" button on Victory screen
    public void BackToMainMenu()
    {
        // Hint the MainMenu to restore last-opened panel this time
        PlayerPrefs.SetInt(MainMenuLastPanelRestorer.PrefsShowLastKey, 1);
        PlayerPrefs.Save();
        LoadMainMenu();
    }

    private void LoadMainMenu()
    {
        ResumeTimeIfNeeded();
        if (mainMenuBuildIndex >= 0)
        {
            if (useAsyncLoad) SceneManager.LoadSceneAsync(mainMenuBuildIndex, LoadSceneMode.Single);
            else SceneManager.LoadScene(mainMenuBuildIndex, LoadSceneMode.Single);
        }
        else if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            if (useAsyncLoad) SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);
            else SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
        }
        else
        {
            // fallback to 0
            if (useAsyncLoad) SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            else SceneManager.LoadScene(0, LoadSceneMode.Single);
        }
    }
}
