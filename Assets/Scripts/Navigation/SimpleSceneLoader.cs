using UnityEngine;
using UnityEngine.SceneManagement;

// Simple loader with 4 scene name slots. Bind the LoadX methods to UI Buttons.
public class SimpleSceneLoader : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string sceneName1;
    [SerializeField] private string sceneName2;
    [SerializeField] private string sceneName3;
    [SerializeField] private string sceneName4;

    [Header("Options")]
    [SerializeField] private bool resumeTimeOnLoad = true;
    [SerializeField] private float resumeTimeScale = 1f;
    [SerializeField] private bool useAsync = false;

    public void LoadScene1() => LoadByName(sceneName1);
    public void LoadScene2() => LoadByName(sceneName2);
    public void LoadScene3() => LoadByName(sceneName3);
    public void LoadScene4() => LoadByName(sceneName4);

    public void LoadByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SimpleSceneLoader] Empty scene name.");
            return;
        }
        if (resumeTimeOnLoad) Time.timeScale = resumeTimeScale;
        if (useAsync) SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        else SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
