using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class PauseExitController : MonoBehaviour
{
    [Header("UI Refs")]
    [Tooltip("Root object of the Pause Menu UI")] [SerializeField] private GameObject pauseMenuRoot;
    [Tooltip("Confirmation panel to show when pressing Exit")] [SerializeField] private GameObject confirmPanel;

    [Header("Target Scene")] 
    [Tooltip("Scene name to load when confirming exit (used if Build Index < 0)")] [SerializeField] private string targetSceneName = "MainMenu";
    [Tooltip("Scene build index to load when confirming exit (>= 0 overrides name)")] [SerializeField] private int targetSceneBuildIndex = -1;

    [Header("Time Scale Handling")] 
    [Tooltip("Set time scale before loading the scene (useful if you paused with timeScale=0)")] [SerializeField] private bool setTimeScaleOnLoad = true;
    [SerializeField] private float resumeTimeScale = 1f;
    [Header("Loading Options")]
    [Tooltip("Use LoadSceneAsync to change scenes")] [SerializeField] private bool useAsyncLoad = false;

    // Called by Exit button on the pause menu
    public void OnExitButton()
    {
        if (confirmPanel != null) confirmPanel.SetActive(true); // overlay on top of pause UI
    }

    // Called by Yes button on the confirmation panel
    public void OnConfirmYes()
    {
        // Ensure gameplay time is resumed before switching scenes
        Time.timeScale = 1f;
        // Load MainMenu by build index (0)
        SceneManager.LoadScene(0);
    }

    private int GetBuildIndexForSceneName(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    // Called by No button on the confirmation panel
    public void OnConfirmNo()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        // leave pause menu as-is (still visible)
    }
}
