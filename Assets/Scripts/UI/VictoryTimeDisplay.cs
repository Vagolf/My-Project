using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach this to the Victory object (the same GameObject assigned to RoundManager.onPlayerWinObject)
// Displays the elapsed match time when the player wins.
public class VictoryTimeDisplay : MonoBehaviour
{
    [Header("UI Text Targets")]
    [Tooltip("Standard UI Text to show the victory time (optional if using TMP)")]
    [SerializeField] private Text uiText;
    [Tooltip("TextMeshProUGUI to show the victory time (optional if using legacy Text)")]
    [SerializeField] private TextMeshProUGUI tmpText;

    [Header("Format Settings")]
    [Tooltip("Time format string, e.g. {0:00}:{1:00} for mm:ss")]
    [SerializeField] private string timeFormat = "{0:00}:{1:00}"; // mm:ss only
    [Tooltip("Prefix label shown before the time")] [SerializeField] private string prefix = "";

    private void OnEnable()
    {
        // If we got enabled after a match already ended, try to populate from the static cache
        if (RoundManager.LastPlayerWinTime > 0f)
        {
            SetTime(RoundManager.LastPlayerWinTime);
        }
    }

    // Called by RoundManager via SendMessage (default method name)
    public void OnMatchWinTime(float seconds)
    {
        SetTime(seconds);
    }

    private void SetTime(float seconds)
    {
        string text = FormatTime(seconds);
        if (uiText != null) uiText.text = text;
        if (tmpText != null) tmpText.text = text;
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        int hundredths = Mathf.FloorToInt((seconds - totalSeconds) * 100f);
        // {0}=mm, {1}=ss, {2}=ff, {3}=seconds float
        string core = string.Format(timeFormat, minutes, secs, hundredths, seconds);
        return string.IsNullOrEmpty(prefix) ? core : (prefix + core);
    }
}
