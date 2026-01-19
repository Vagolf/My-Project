using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Story;

// Attach this to your Victory object. It reads name from an InputField or TMP_InputField
// and reads time from VictoryTimeDisplay (or set manually) then stores to JSON via RunResultsStore.
public class VictoryReporter : MonoBehaviour
{
    [Header("Inputs")]
    [Tooltip("Player name input (legacy UI)")] [SerializeField] private TMP_InputField nameInput;
#if TMP_PRESENT
    [Tooltip("Player name input (TextMeshPro)")] [SerializeField] private TMPro.TMP_InputField tmpNameInput;
#endif
    [Tooltip("Optional explicit difficulty string; if empty, tries to read from GameManager")] [SerializeField] private string difficultyOverride;
    [Tooltip("If set, time will be read from this component; otherwise set via API")] [SerializeField] private VictoryTimeDisplay timeDisplay;

    private float cachedSeconds = -1f;

    // Call this from a button (e.g., Save / Continue)
    public void SaveVictory()
    {
        float seconds = GetSeconds();
        string playerName = GetPlayerName();
        string difficulty = GetDifficulty();
        int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        RunResultsStore.Ensure().AddResult(playerName, seconds, sceneIndex, difficulty);
        Debug.Log($"[VictoryReporter] Saved result: {playerName} {seconds:F2}s scene={sceneIndex} diff={difficulty}");

        // Update story progress (unlock next stage) if StageMeta present
        var save = Story.SaveManager.GetCurrent();
        var meta = Object.FindObjectOfType<Story.StageMeta>();
        if (save != null && meta != null)
        {
            Story.SaveManager.SetProgress(save.id, Mathf.Clamp(meta.stageNumber, 0, 3));
            Story.SaveManager.Touch(save.id);
            Debug.Log($"[VictoryReporter] Progress updated to stage {meta.stageNumber} for save '{save.name}'.");
        }
    }

    public void SetTimeSeconds(float seconds)
    {
        cachedSeconds = Mathf.Max(0f, seconds);
    }

    private float GetSeconds()
    {
        if (cachedSeconds >= 0f) return cachedSeconds;
        if (timeDisplay != null)
        {
            // Try to read from RoundManager static if available
            return RoundManager.LastPlayerWinTime;
        }
        return RoundManager.LastPlayerWinTime;
    }

    private string GetPlayerName()
    {
#if TMP_PRESENT
        if (tmpNameInput != null) return tmpNameInput.text;
#endif
        if (nameInput != null) return nameInput.text;
        return "Player";
    }

    private string GetDifficulty()
    {
        // 1) Explicit override (normalize to Easy/Normal/Hard)
        if (!string.IsNullOrWhiteSpace(difficultyOverride))
        {
            return NormalizeDifficulty(difficultyOverride);
        }

        // 2) Use Story save difficulty if available
        var currentSave = SaveManager.GetCurrent();
        if (currentSave != null)
        {
            return currentSave.difficulty.ToString(); // Easy / Normal / Hard
        }

        // 3) Fallback: infer from a tag on a manager or scene object
        var gm = FindObjectOfType<GameManagerScript>();
        if (gm != null)
        {
            var tag = gm.gameObject.tag;
            if (string.Equals(tag, "Easy", System.StringComparison.OrdinalIgnoreCase)) return "Easy";
            if (string.Equals(tag, "Hard", System.StringComparison.OrdinalIgnoreCase)) return "Hard";
            return "Normal";
        }

        // Default
        return "Normal";
    }

    private string NormalizeDifficulty(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Normal";
        var v = value.Trim();
        if (string.Equals(v, "Easy", System.StringComparison.OrdinalIgnoreCase)) return "Easy";
        if (string.Equals(v, "Normal", System.StringComparison.OrdinalIgnoreCase)) return "Normal";
        if (string.Equals(v, "Hard", System.StringComparison.OrdinalIgnoreCase)) return "Hard";
        // accept short forms
        if (string.Equals(v, "E", System.StringComparison.OrdinalIgnoreCase)) return "Easy";
        if (string.Equals(v, "N", System.StringComparison.OrdinalIgnoreCase)) return "Normal";
        if (string.Equals(v, "H", System.StringComparison.OrdinalIgnoreCase)) return "Hard";
        return "Normal";
    }
}
