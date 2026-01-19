using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Put this on a panel in MainMenu. It lists saved results and can filter by scene and difficulty.
public class MainMenuResultsView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Dropdown sceneDropdown; // first option will be "All Scenes"
    [SerializeField] private Dropdown difficultyDropdown; // options: All, Easy, Normal, Hard (configure in Inspector)
    [SerializeField] private Text targetText;

    private void OnEnable()
    {
        EnsureDropdowns();
        PopulateScenes();
        Refresh();
    }

    public void Refresh()
    {
        var store = RunResultsStore.Ensure();
        var all = new List<RunResultsStore.RunResult>(store.GetAll());

        int selectedSceneIndex = GetSelectedSceneBuildIndex(); // -1 = all scenes
        string selectedDifficulty = GetSelectedDifficulty(); // null/empty = all

        // Filter
        if (selectedSceneIndex >= 0)
            all.RemoveAll(r => r.sceneIndex != selectedSceneIndex);
        if (!string.IsNullOrEmpty(selectedDifficulty) && !string.Equals(selectedDifficulty, "All"))
            all.RemoveAll(r => !string.Equals(r.difficulty, selectedDifficulty));

        // Sort best time first
        all.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));

        // Build text
        var sb = new StringBuilder();
        foreach (var r in all)
        {
            sb.AppendLine($"{r.sceneName} [{r.difficulty}] {r.playerName} - {FormatTime(r.timeSeconds)}");
        }
        if (targetText != null) targetText.text = sb.ToString();
    }

    public void OnSceneDropdownChanged(int _)
    {
        Refresh();
    }

    public void OnDifficultyDropdownChanged(int _)
    {
        Refresh();
    }

    private void EnsureDropdowns()
    {
        if (sceneDropdown == null)
        {
            Debug.LogWarning("[MainMenuResultsView] Scene dropdown is not assigned.");
        }
        if (difficultyDropdown == null)
        {
            Debug.LogWarning("[MainMenuResultsView] Difficulty dropdown is not assigned.");
        }
    }

    private void PopulateScenes()
    {
        if (sceneDropdown == null) return;
        sceneDropdown.ClearOptions();
        var options = new List<string> { "All Scenes" };
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            options.Add(string.IsNullOrEmpty(name) ? $"Scene_{i}" : name);
        }
        sceneDropdown.AddOptions(options);
        sceneDropdown.value = 0; // All Scenes
        sceneDropdown.RefreshShownValue();
    }

    private int GetSelectedSceneBuildIndex()
    {
        if (sceneDropdown == null) return -1;
        int idx = sceneDropdown.value; // 0 = All
        if (idx <= 0) return -1;
        // Dropdown index 1..N maps to buildIndex 0..N-1
        return idx - 1;
    }

    private string GetSelectedDifficulty()
    {
        if (difficultyDropdown == null || difficultyDropdown.options == null || difficultyDropdown.options.Count == 0)
            return "";
        return difficultyDropdown.options[difficultyDropdown.value].text;
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }
}
