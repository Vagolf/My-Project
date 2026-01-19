using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Simple viewer that populates a Text with results filtered by current scene or all.
public class ResultsViewer : MonoBehaviour
{
    [SerializeField] private Text targetText;
    [SerializeField] private bool filterByCurrentScene = true;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        var store = RunResultsStore.Ensure();
        List<RunResultsStore.RunResult> list;
        if (filterByCurrentScene)
        {
            int idx = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            list = store.GetByScene(idx);
        }
        else
        {
            list = new List<RunResultsStore.RunResult>(store.GetAll());
        }

        list.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var r in list)
        {
            sb.AppendLine($"{r.sceneName} [{r.difficulty}] {r.playerName} - {FormatTime(r.timeSeconds)}");
        }
        if (targetText != null) targetText.text = sb.ToString();
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }
}
