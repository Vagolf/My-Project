using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Simple scene swapper utility. Attach to a persistent manager or any GameObject.
// Provides methods to change scenes by name or build index (sync/async) with optional fade.
public class SceneSwapper : MonoBehaviour
{
    [Header("Default Target (optional)")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private int targetSceneIndex = -1;

    [Header("TimeScale")] 
    [SerializeField] private bool resumeTimeOnSwap = true;
    [SerializeField] private float resumeTimeScale = 1f;

    [Header("Fade (optional)")]
    [Tooltip("CanvasGroup used for fade. Optional. Set alpha    to 0 initially.")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Async Load")] 
    [SerializeField] private bool useAsync = true;

    // Load the configured target (name preferred, then index)
    public void LoadConfigured()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
            SwapToName(targetSceneName);
        else if (targetSceneIndex >= 0)
            SwapToIndex(targetSceneIndex);
        else
            Debug.LogWarning("[SceneSwapper] No configured target scene.");
    }

    public void SwapToName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) { Debug.LogWarning("[SceneSwapper] Empty sceneName"); return; }
        StartCoroutine(Swap(() => LoadByName(sceneName)));
    }

    public void SwapToIndex(int buildIndex)
    {
        if (buildIndex < 0) { Debug.LogWarning("[SceneSwapper] Invalid buildIndex"); return; }
        StartCoroutine(Swap(() => LoadByIndex(buildIndex)));
    }

    public void Reload()
    {
        var active = SceneManager.GetActiveScene();
        StartCoroutine(Swap(() => LoadByIndex(active.buildIndex)));
    }

    private IEnumerator Swap(System.Action loader)
    {
        if (resumeTimeOnSwap) Time.timeScale = resumeTimeScale;
        if (fadeCanvas != null && fadeDuration > 0f)
        {
            yield return Fade(0f, 1f);
        }
        loader?.Invoke();
        // If loading async, wait until done before fade-in
        if (useAsync) yield return null; // allow async start
        // Delay one frame for new scene to initialize UI before fade-in
        if (fadeCanvas != null && fadeDuration > 0f)
        {
            yield return null;
            yield return Fade(1f, 0f);
        }
    }

    private void LoadByName(string sceneName)
    {
        if (useAsync) SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        else SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void LoadByIndex(int buildIndex)
    {
        if (useAsync) SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        else SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeCanvas == null) yield break;
        fadeCanvas.blocksRaycasts = true;
        float t = 0f;
        fadeCanvas.alpha = from;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        fadeCanvas.alpha = to;
        fadeCanvas.blocksRaycasts = to > 0.99f;
    }
}
