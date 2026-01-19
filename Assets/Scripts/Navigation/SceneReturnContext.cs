using UnityEngine;

// Cross-scene context holder for returning to a previous scene and restoring state.
// Use DontDestroyOnLoad to persist while switching scenes.
public class SceneReturnContext : MonoBehaviour
{
    public static SceneReturnContext Instance { get; private set; }

    [System.Serializable]
    public class Payload
    {
        public string targetObjectName; // optional target object to receive restore call
        public string methodName = "OnRestoreFromPause"; // method called with string payload
        public string data; // custom JSON/string data
    }

    [Header("Sender Scene Info")]
    [SerializeField] private string senderSceneName = string.Empty;
    [SerializeField] private int senderSceneIndex = -1;
    [SerializeField] private Payload senderPayload = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void SetSender(string sceneName, int sceneIndex, Payload payload = null)
    {
        if (Instance == null)
        {
            var go = new GameObject("SceneReturnContext");
            Instance = go.AddComponent<SceneReturnContext>();
        }
        Instance.senderSceneName = sceneName;
        Instance.senderSceneIndex = sceneIndex;
        Instance.senderPayload = payload;
    }

    public static bool TryGet(out string sceneName, out int sceneIndex, out Payload payload)
    {
        if (Instance != null)
        {
            sceneName = Instance.senderSceneName;
            sceneIndex = Instance.senderSceneIndex;
            payload = Instance.senderPayload;
            return true;
        }
        sceneName = string.Empty;
        sceneIndex = -1;
        payload = null;
        return false;
    }
}
