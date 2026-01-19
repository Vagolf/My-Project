using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.IO;

/// <summary>
/// ??????????????????????????????????? ??????? 5 ???????? TextMeshPro ??????
/// </summary>
public class CharacterStatsDisplayManager : MonoBehaviour
{
    [Header("Character Selection")]
    [SerializeField] private ButtonGroupColorManager characterButtonGroup;
    [SerializeField] private int romanSceneIndex = 1;
    [SerializeField] private int evaSceneIndex = 2;
    [SerializeField] private int dusanSceneIndex = 3;

    [Header("Roman Display (Top 5)")]
    [SerializeField] private TextMeshProUGUI romanRank1Text;
    [SerializeField] private TextMeshProUGUI romanRank2Text;
    [SerializeField] private TextMeshProUGUI romanRank3Text;
    [SerializeField] private TextMeshProUGUI romanRank4Text;
    [SerializeField] private TextMeshProUGUI romanRank5Text;

    [Header("Eva Display (Top 5)")]
    [SerializeField] private TextMeshProUGUI evaRank1Text;
    [SerializeField] private TextMeshProUGUI evaRank2Text;
    [SerializeField] private TextMeshProUGUI evaRank3Text;
    [SerializeField] private TextMeshProUGUI evaRank4Text;
    [SerializeField] private TextMeshProUGUI evaRank5Text;

    [Header("Dusan Display (Top 5)")]
    [SerializeField] private TextMeshProUGUI dusanRank1Text;
    [SerializeField] private TextMeshProUGUI dusanRank2Text;
    [SerializeField] private TextMeshProUGUI dusanRank3Text;
    [SerializeField] private TextMeshProUGUI dusanRank4Text;
    [SerializeField] private TextMeshProUGUI dusanRank5Text;

    [Header("Button References")]
    [SerializeField] private ButtonTextColor romanButton;
    [SerializeField] private ButtonTextColor evaButton;
    [SerializeField] private ButtonTextColor dusanButton;

    [Header("Check Button")]
    [SerializeField] private UnityEngine.UI.Button checkButton;

    [Header("Display Options")]
    [SerializeField] private string timeFormat = "{0:00}:{1:00}"; // mm:ss
    [SerializeField] private string noDataMessage = "No Data";
    [SerializeField] private bool showPlayerNameAndTime = true; // ???????????????????
    [SerializeField] private bool showDifficulty = false; // ????????????????
    
    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private RunResultsStore resultsStore;
    private TextMeshProUGUI[] romanTexts;
    private TextMeshProUGUI[] evaTexts;
    private TextMeshProUGUI[] dusanTexts;

    private void Start()
    {
        resultsStore = RunResultsStore.Ensure();
        Log($"Store ready. All results count = {resultsStore.GetAll().Count}");
        try { Log($"Results JSON: {Path.Combine(Application.persistentDataPath, "run_results.json")}" ); } catch {}
        TryLogSceneBuckets();
        
        // ??? TextMeshPro ???? Array
        romanTexts = new TextMeshProUGUI[] { romanRank1Text, romanRank2Text, romanRank3Text, romanRank4Text, romanRank5Text };
        evaTexts = new TextMeshProUGUI[] { evaRank1Text, evaRank2Text, evaRank3Text, evaRank4Text, evaRank5Text };
        dusanTexts = new TextMeshProUGUI[] { dusanRank1Text, dusanRank2Text, dusanRank3Text, dusanRank4Text, dusanRank5Text };
        
        if (checkButton != null)
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        else Log("WARN: checkButton is null (hook your Check button)");
        if (characterButtonGroup == null) Log("WARN: characterButtonGroup is null");
        if (romanButton == null) Log("WARN: romanButton is null");
        if (evaButton == null) Log("WARN: evaButton is null");
        if (dusanButton == null) Log("WARN: dusanButton is null");
            
        // ??????????????????????
        HideAllDisplays();
    }

    /// <summary>
    /// ???????????????? Check
    /// </summary>
    public void OnCheckButtonClicked()
    {
        int selectedScene = GetSelectedCharacterSceneIndex();
        Log($"OnCheck -> selectedScene={selectedScene} (map: Roman={romanSceneIndex}, Eva={evaSceneIndex}, Dusan={dusanSceneIndex})");
        LogSelectionStates();
        if (selectedScene > -1) DumpSceneResults(selectedScene, "Before display dump");
        
        // ??????????????
        HideAllDisplays();
        
        switch (selectedScene)
        {
            case 1: // Roman
                DisplayCharacterStats(romanSceneIndex, romanTexts, "Roman");
                break;
            case 2: // Eva
                DisplayCharacterStats(evaSceneIndex, evaTexts, "Eva");
                break;
            case 3: // Dusan
                DisplayCharacterStats(dusanSceneIndex, dusanTexts, "Dusan");
                break;
            default:
                Debug.Log("[CharacterStats] No character selected");
                break;
        }
    }

    /// <summary>
    /// ???????????????????????????
    /// </summary>
    private void DisplayCharacterStats(int sceneIndex, TextMeshProUGUI[] textArray, string characterName)
    {
        var results = resultsStore.GetByScene(sceneIndex);
        Log($"Display for {characterName} sceneIndex={sceneIndex} -> rawCount={ (results!=null?results.Count:0) }");
        
        if (results == null || results.Count == 0)
        {
            // ??????????????????????
            for (int i = 0; i < textArray.Length; i++)
            {
                if (textArray[i] != null)
                {
                    textArray[i].gameObject.SetActive(true);
                    textArray[i].text = i == 0 ? $"{characterName}: {noDataMessage}" : "";
                }
                else Log($"WARN: textArray[{i}] is null for {characterName}");
            }
            Log($"No data for scene {sceneIndex}. Showing noData message.");
            return;
        }

        // ????????????????????? (????????????)
        var sortedResults = results.OrderBy(r => r.timeSeconds).Take(5).ToList();
        for (int li = 0; li < sortedResults.Count; li++)
        {
            var rr = sortedResults[li];
            Log($"Top[{li+1}] {rr.playerName} time={rr.timeSeconds:F2}s scene={rr.sceneIndex} diff={rr.difficulty}");
        }
        
        // ???????? TextMeshPro ??????
        for (int i = 0; i < textArray.Length; i++)
        {
            if (textArray[i] != null)
            {
                textArray[i].gameObject.SetActive(true);
                
                if (i < sortedResults.Count)
                {
                    var result = sortedResults[i];
                    string displayText = FormatResultText(i + 1, result);
                    textArray[i].text = displayText;
                    Log($"Row {i+1} -> '{displayText}'");
                }
                else
                {
                    textArray[i].text = ""; // ????????????????
                    Log($"Row {i+1} -> hidden (no data) ");
                }
            }
            else Log($"WARN: textArray[{i}] is null for {characterName}");
        }
    }

    /// <summary>
    /// ?????????????????????????
    /// </summary>
    private string FormatResultText(int rank, RunResultsStore.RunResult result)
    {
        string formattedTime = FormatTime(result.timeSeconds);
        
        if (showPlayerNameAndTime)
        {
            string text = $"{rank}. {result.playerName} - {formattedTime}";
            if (showDifficulty)
                text += $" ({result.difficulty})";
            return text;
        }
        else
        {
            return $"{rank}. {formattedTime}";
        }
    }

    /// <summary>
    /// ????????????????????????????
    /// </summary>
    private int GetSelectedCharacterSceneIndex()
    {
        if (characterButtonGroup == null) return -1;

        if (romanButton != null && IsButtonSelected(romanButton))
            return romanSceneIndex;
        if (evaButton != null && IsButtonSelected(evaButton))
            return evaSceneIndex;
        if (dusanButton != null && IsButtonSelected(dusanButton))
            return dusanSceneIndex;

        return -1;
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
    /// ????????????????????
    /// </summary>
    private void HideAllDisplays()
    {
        HideTextArray(romanTexts);
        HideTextArray(evaTexts);
        HideTextArray(dusanTexts);
        Log("All displays hidden");
    }

    /// <summary>
    /// ???? TextMeshPro ?? Array
    /// </summary>
    private void HideTextArray(TextMeshProUGUI[] textArray)
    {
        foreach (var text in textArray)
        {
            if (text != null)
                text.gameObject.SetActive(false);
        }
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
    /// ??????? scene index ?????????????????????
    /// </summary>
    public void SetCharacterSceneIndices(int roman, int eva, int dusan)
    {
        romanSceneIndex = roman;
        evaSceneIndex = eva;
        dusanSceneIndex = dusan;
        Log($"Scene indices set -> Roman={romanSceneIndex}, Eva={evaSceneIndex}, Dusan={dusanSceneIndex}");
    }

    /// <summary>
    /// ???????????? Roman ?????? (????????????????????)
    /// </summary>
    public void ShowRomanStats()
    {
        HideAllDisplays();
        DisplayCharacterStats(romanSceneIndex, romanTexts, "Roman");
    }

    /// <summary>
    /// ???????????? Eva ??????
    /// </summary>
    public void ShowEvaStats()
    {
        HideAllDisplays();
        DisplayCharacterStats(evaSceneIndex, evaTexts, "Eva");
    }

    /// <summary>
    /// ???????????? Dusan ??????
    /// </summary>
    public void ShowDusanStats()
    {
        HideAllDisplays();
        DisplayCharacterStats(dusanSceneIndex, dusanTexts, "Dusan");
    }

    private void Log(string msg)
    {
        if (verboseLog)
            Debug.Log($"[CharacterStatsDisplay] {msg}");
    }

    private void LogSelectionStates()
    {
        if (!verboseLog || characterButtonGroup == null) return;
        bool r = romanButton != null && characterButtonGroup.IsCurrentSelected(romanButton);
        bool e = evaButton != null && characterButtonGroup.IsCurrentSelected(evaButton);
        bool d = dusanButton != null && characterButtonGroup.IsCurrentSelected(dusanButton);
        Log($"Selected states -> Roman={r}, Eva={e}, Dusan={d}");
    }

    private void TryLogSceneBuckets()
    {
        if (!verboseLog) return;
        try
        {
            int cr = resultsStore.GetByScene(romanSceneIndex)?.Count ?? 0;
            int ce = resultsStore.GetByScene(evaSceneIndex)?.Count ?? 0;
            int cd = resultsStore.GetByScene(dusanSceneIndex)?.Count ?? 0;
            Log($"Bucket counts -> Roman(scene {romanSceneIndex})={cr}, Eva(scene {evaSceneIndex})={ce}, Dusan(scene {dusanSceneIndex})={cd}");
        }
        catch { }
    }

    private void DumpSceneResults(int sceneIndex, string title)
    {
        if (!verboseLog) return;
        var list = resultsStore.GetByScene(sceneIndex);
        Log($"Dump ({title}) scene={sceneIndex} count={(list==null?0:list.Count)}");
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            Log($"[{i}] name='{r.playerName}' time={r.timeSeconds:F3}s diff={r.difficulty} sceneIdx={r.sceneIndex} scene='{r.sceneName}' date={r.dateIso}");
        }
    }
}