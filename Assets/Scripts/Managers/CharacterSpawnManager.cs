using UnityEngine;

public class CharacterSpawnManager : MonoBehaviour
{
    [Header("Selection (indices)")]
    [SerializeField] private int selectedPlayerIndex = 0;
    [SerializeField] private int selectedEnemyIndex = 0;
    [SerializeField] private bool overrideSelectionFromPrefs = true;
    [SerializeField] private string playerPrefKey = "SelectedPlayerIdx";
    [SerializeField] private string enemyPrefKey = "SelectedEnemyIdx";

    [Header("Scene Characters (enable/disable)")]
    [Tooltip("Existing Player objects in scene (pick one by index)")]
    [SerializeField] private GameObject[] playerObjects;
    [Tooltip("Existing Enemy objects in scene (pick one by index)")]
    [SerializeField] private GameObject[] enemyObjects;

    [Header("Spawn Points")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;

    [Header("Options")]
    [SerializeField] private bool setTagsOnSpawn = true; // ensure tag Player/Enemy

    public static GameObject PlayerInstance { get; private set; }
    public static GameObject EnemyInstance { get; private set; }

    private void Awake()
    {
        if (overrideSelectionFromPrefs)
        {
            if (PlayerPrefs.HasKey(playerPrefKey)) selectedPlayerIndex = Mathf.Clamp(PlayerPrefs.GetInt(playerPrefKey), 0, Mathf.Max(0, playerObjects != null ? playerObjects.Length - 1 : 0));
            if (PlayerPrefs.HasKey(enemyPrefKey)) selectedEnemyIndex = Mathf.Clamp(PlayerPrefs.GetInt(enemyPrefKey), 0, Mathf.Max(0, enemyObjects != null ? enemyObjects.Length - 1 : 0));
        }
    }

    private void Start()
    {
        SpawnAll();
    }

    public void SaveSelectionToPrefs()
    {
        PlayerPrefs.SetInt(playerPrefKey, selectedPlayerIndex);
        PlayerPrefs.SetInt(enemyPrefKey, selectedEnemyIndex);
        PlayerPrefs.Save();
    }

    public void SetSelection(int playerIdx, int enemyIdx)
    {
        selectedPlayerIndex = Mathf.Clamp(playerIdx, 0, Mathf.Max(0, playerObjects != null ? playerObjects.Length - 1 : 0));
        selectedEnemyIndex = Mathf.Clamp(enemyIdx, 0, Mathf.Max(0, enemyObjects != null ? enemyObjects.Length - 1 : 0));
    }

    public void SpawnAll()
    {
        // enable selected and disable others (player)
        PlayerInstance = ActivateOne(playerObjects, selectedPlayerIndex, playerSpawnPoint, setTagsOnSpawn ? "Player" : null);
        // enable selected and disable others (enemy)
        EnemyInstance = ActivateOne(enemyObjects, selectedEnemyIndex, enemySpawnPoint, setTagsOnSpawn ? "Enemy" : null);

        // inform RoundManager if exists
        var rm = FindObjectOfType<RoundManager>();
        if (rm != null)
        {
            rm.player = PlayerInstance;
            rm.enemy = EnemyInstance;
        }
    }

    private void ZeroBody(GameObject go)
    {
        if (!go) return;
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = Vector2.zero;
    }

    private GameObject ActivateOne(GameObject[] list, int index, Transform spawnPoint, string tag)
    {
        if (list == null || list.Length == 0) return null;
        index = Mathf.Clamp(index, 0, list.Length - 1);
        GameObject active = null;
        for (int i = 0; i < list.Length; i++)
        {
            var go = list[i];
            if (go == null) continue;
            bool enable = i == index;
            if (enable)
            {
                if (tag != null) go.tag = tag;
                // move to spawn position (preserve Z)
                if (spawnPoint != null)
                {
                    var p = go.transform.position;
                    var sp = spawnPoint.position;
                    go.transform.position = new Vector3(sp.x, sp.y, p.z);
                }
                go.SetActive(true);
                ZeroBody(go);
                active = go;
            }
            else
            {
                if (tag != null && go.tag == tag)
                    go.tag = "Untagged"; // avoid systems finding the wrong actor
                go.SetActive(false);
            }
        }
        return active;
    }
}
