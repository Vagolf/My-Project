using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// ???????????????????????? JSON ??????????????????
/// ????????????? RunResultsStore ????????? TextMeshPro
/// </summary>
public class ResultsDisplayManager : MonoBehaviour
{
    [Header("Character Selection")]
    [SerializeField] private ButtonGroupColorManager characterButtonGroup;
    [SerializeField] private int romanSceneIndex = 1;
    [SerializeField] private int evaSceneIndex = 2;
    [SerializeField] private int dusanSceneIndex = 3;

    [Header("Display UI")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI noDataText; // ????????????????????

    [Header("Button References")]
    [Tooltip("???? Roman (index 0 ???????)")]
    [SerializeField] private ButtonTextColor romanButton;
    [Tooltip("???? Eva (index 1 ???????)")]
    [SerializeField] private ButtonTextColor evaButton;
    [Tooltip("???? Dusan (index 2 ???????)")]
    [SerializeField] private ButtonTextColor dusanButton;

    [Header("Check Button")]
    [SerializeField] private UnityEngine.UI.Button checkButton;

    [Header("Display Options")]
    [SerializeField] private bool showBestTimeOnly = true; // ??????????????????????
    [SerializeField] private bool showAllDifficulties = false; // ???????????????????
    [SerializeField] private string timeFormat = "{0:00}:{1:00}"; // mm:ss
    [SerializeField] private string noDataMessage = "No records found";

    private RunResultsStore resultsStore;
    private int currentSelectedSceneIndex = -1;

    private void Start()
    {
        resultsStore = RunResultsStore.Ensure();
        
        // ??????? Check
        if (checkButton != null)
            checkButton.onClick.AddListener(OnCheckButtonClicked);
            
        // ??????????????????????
        SetUIVisibility(false);
    }

    /// <summary>
    /// ???????????????? Check
    /// </summary>
    public void OnCheckButtonClicked()
    {
        int selectedScene = GetSelectedCharacterSceneIndex();
        if (selectedScene == -1)
        {
            ShowNoSelection();
            return;
        }

        currentSelectedSceneIndex = selectedScene;
        DisplayResultsForScene(selectedScene);
    }

    /// <summary>
    /// ????????????????????????????
    /// </summary>
    private int GetSelectedCharacterSceneIndex()
    {
        if (characterButtonGroup == null) return -1;

        // ?????????????????????????????????????
        if (romanButton != null && IsButtonSelected(romanButton))
            return romanSceneIndex;
        if (evaButton != null && IsButtonSelected(evaButton))
            return evaSceneIndex;
        if (dusanButton != null && IsButtonSelected(dusanButton))
            return dusanSceneIndex;

        return -1; // ?????????????
    }

    /// <summary>
    /// ?????????????????????????????
    /// </summary>
    private bool IsButtonSelected(ButtonTextColor button)
    {
        if (button == null || characterButtonGroup == null) return false;
        return characterButtonGroup.IsCurrentSelected(button);
    }

    /// <summary>
    /// ?????????????????? scene ????????
    /// </summary>
    private void DisplayResultsForScene(int sceneIndex)
    {
        var results = resultsStore.GetByScene(sceneIndex);
        
        if (results == null || results.Count == 0)
        {
            ShowNoData();
            return;
        }

        if (showBestTimeOnly)
        {
            DisplayBestResult(results);
        }
        else
        {
            DisplayAllResults(results);
        }
    }

    /// <summary>
    /// ?????????????????????? - ???? 5 ?????????
    /// </summary>
    private void DisplayBestResult(List<RunResultsStore.RunResult> results)
    {
        var sortedResults = results.OrderBy(r => r.timeSeconds).Take(5).ToList();
        
        SetUIVisibility(true);
        
        // ?????????????????????????????
        string nameText = "Top 5 Players:\n";
        for (int i = 0; i < sortedResults.Count; i++)
        {
            nameText += $"{i + 1}. {sortedResults[i].playerName}\n";
        }
        
        // ??????????????????????
        string timeDisplayText = "Times:\n";
        for (int i = 0; i < sortedResults.Count; i++)
        {
            timeDisplayText += $"{FormatTime(sortedResults[i].timeSeconds)}\n";
        }
        
        if (playerNameText != null)
            playerNameText.text = nameText.TrimEnd('\n');
            
        if (timeText != null)
            timeText.text = timeDisplayText.TrimEnd('\n');
    }

    /// <summary>
    /// ?????????????????? (??????????? UI ?????????)
    /// </summary>
    private void DisplayAllResults(List<RunResultsStore.RunResult> results)
    {
        var sortedResults = results.OrderBy(r => r.timeSeconds).ToList();
        var displayText = "";
        
        for (int i = 0; i < Mathf.Min(sortedResults.Count, 5); i++) // ??????? 5 ?????????
        {
            var result = sortedResults[i];
            displayText += $"{i + 1}. {result.playerName} - {FormatTime(result.timeSeconds)}";
            if (!showAllDifficulties)
                displayText += $" ({result.difficulty})";
            displayText += "\n";
        }
        
        SetUIVisibility(true);
        
        if (playerNameText != null)
            playerNameText.text = "Top Players:";
            
        if (timeText != null)
            timeText.text = displayText.TrimEnd('\n');
    }

    /// <summary>
    /// ???????????????????????????
    /// </summary>
    private void ShowNoData()
    {
        SetUIVisibility(false);
        if (noDataText != null)
        {
            noDataText.gameObject.SetActive(true);
            noDataText.text = $"{GetSelectedCharacterName()}: {noDataMessage}";
        }
    }

    /// <summary>
    /// ??????????????????????????????????
    /// </summary>
    private void ShowNoSelection()
    {
        SetUIVisibility(false);
        if (noDataText != null)
        {
            noDataText.gameObject.SetActive(true);
            noDataText.text = "Please select a character first";
        }
    }

    /// <summary>
    /// ???????????????? UI
    /// </summary>
    private void SetUIVisibility(bool showResults)
    {
        if (playerNameText != null)
            playerNameText.gameObject.SetActive(showResults);
        if (timeText != null)
            timeText.gameObject.SetActive(showResults);
        if (noDataText != null)
            noDataText.gameObject.SetActive(!showResults);
    }

    /// <summary>
    /// ?????????????????? mm:ss
    /// </summary>
    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return string.Format(timeFormat, minutes, secs);
    }

    /// <summary>
    /// ??????????????????????
    /// </summary>
    private string GetSelectedCharacterName()
    {
        switch (currentSelectedSceneIndex)
        {
            case 1: return "Roman";
            case 2: return "Eva";
            case 3: return "Dusan";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// ??????????????? manual (?????????????????)
    /// </summary>
    public void RefreshDisplay()
    {
        if (currentSelectedSceneIndex != -1)
        {
            DisplayResultsForScene(currentSelectedSceneIndex);
        }
    }

    /// <summary>
    /// ??????? scene index ????????????????????? (???????? Inspector ???)
    /// </summary>
    public void SetCharacterSceneIndices(int roman, int eva, int dusan)
    {
        romanSceneIndex = roman;
        evaSceneIndex = eva;
        dusanSceneIndex = dusan;
    }
}