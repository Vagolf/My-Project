using UnityEngine;

// Put this on a root object in the MainMenu scene. It restores the last active
// panel/object that was open the last time you were on MainMenu.
public class MainMenuLastPanelRestorer : MonoBehaviour
{
    // PlayerPrefs keys
    public const string PrefsLastPanelKey = "mainmenu.last.panel";
    public const string PrefsShowLastKey = "mainmenu.show.last";

    [Header("Auto Restore On Start")] 
    [SerializeField] private bool autoRestoreOnStart = true;

    private void Start()
    {
        if (!autoRestoreOnStart) return;
        TryRestoreLastIfRequested();
    }

    public void TryRestoreLastIfRequested()
    {
        int want = PlayerPrefs.GetInt(PrefsShowLastKey, 0);
        if (want == 1)
        {
            PlayerPrefs.SetInt(PrefsShowLastKey, 0); // consume flag
            PlayerPrefs.Save();
            RestoreLastActivePanel();
        }
    }

    public void RestoreLastActivePanel()
    {
        var last = PlayerPrefs.GetString(PrefsLastPanelKey, string.Empty);
        if (string.IsNullOrEmpty(last)) return;
        var go = GameObject.Find(last);
        if (go != null)
        {
            go.SetActive(true);
        }
    }
}
