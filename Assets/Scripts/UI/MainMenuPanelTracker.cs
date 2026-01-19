using UnityEngine;

// Attach this to each MainMenu panel GameObject you want to be remembered.
// When enabled, it stores its GameObject name as the last-opened panel.
public class MainMenuPanelTracker : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerPrefs.SetString(MainMenuLastPanelRestorer.PrefsLastPanelKey, gameObject.name);
        PlayerPrefs.Save();
    }
}
