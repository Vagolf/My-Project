using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// ????????????????????????????????? ?????????????
/// ??????????????????? Top 5 ??????????????????
/// </summary>
public class PlayerTimeDisplayManager : MonoBehaviour
{
    [Header("Character Selection")]
    [SerializeField] private ButtonGroupColorManager characterButtonGroup;
    [SerializeField] private int romanSceneIndex = 1;
    [SerializeField] private int evaSceneIndex = 2;
    [SerializeField] private int dusanSceneIndex = 3;

    [Header("Roman Times (Top 5)")]
    [SerializeField] private TextMeshProUGUI romanRank1Text;
    [SerializeField] private TextMeshProUGUI romanRank2Text;
    [SerializeField] private TextMeshProUGUI romanRank3Text;
    [SerializeField] private TextMeshProUGUI romanRank4Text;
    [SerializeField] private TextMeshProUGUI romanRank5Text;

    [Header("Eva Times (Top 5)")]
    [SerializeField] private TextMeshProUGUI evaRank1Text;
    [SerializeField] private TextMeshProUGUI evaRank2Text;
    [SerializeField] private TextMeshProUGUI evaRank3Text;
    [SerializeField] private TextMeshProUGUI evaRank4Text;
    [SerializeField] private TextMeshProUGUI evaRank5Text;

    [Header("Dusan Times (Top 5)")]
    [SerializeField] private TextMeshProUGUI dusanRank1Text;
    [SerializeField] private TextMeshProUGUI dusanRank2Text;
    [SerializeField] private TextMeshProUGUI dusanRank3Text;
    [SerializeField] private TextMeshProUGUI dusanRank4Text;
    [SerializeField] private TextMeshProUGUI dusanRank5Text;

    [Header("Button References")]
    [SerializeField] private ButtonTextColor romanButton;
    [SerializeField] private ButtonTextColor evaButton;
    [SerializeField] private ButtonTextColor dusanButton;

    [Header("Update Button")]
    [SerializeField] private UnityEngine.UI.Button updateButton;

    [Header("Display Options")]
    [SerializeField] private int maxTimesToShow = 5;
    [SerializeField] private string timeFormat = "{0:00}:{1:00}"; // mm:ss
    [SerializeField] private string noDataMessage = "No records found";

    private RunResultsStore resultsStore;
    private TextMeshProUGUI[] romanTexts;
    private TextMeshProUGUI[] evaTexts;
    private TextMeshProUGUI[] dusanTexts;

    private void Start()
    {
        resultsStore = RunResultsStore.Ensure();
        
        if (updateButton != null)
            updateButton.onClick.AddListener(UpdateAllDisplays);

        // cache arrays of rank texts per character
        romanTexts = new TextMeshProUGUI[] { romanRank1Text, romanRank2Text, romanRank3Text, romanRank4Text, romanRank5Text };
        evaTexts = new TextMeshProUGUI[] { evaRank1Text, evaRank2Text, evaRank3Text, evaRank4Text, evaRank5Text };
        dusanTexts = new TextMeshProUGUI[] { dusanRank1Text, dusanRank2Text, dusanRank3Text, dusanRank4Text, dusanRank5Text };

        // initial refresh
        UpdateAllDisplays();
    }

    /// <summary>
    /// ?????????????????????????
    /// </summary>
    public void UpdateAllDisplays()
    {
        UpdateCharacterTimes(romanSceneIndex, romanTexts, "Roman");
        UpdateCharacterTimes(evaSceneIndex, evaTexts, "Eva");
        UpdateCharacterTimes(dusanSceneIndex, dusanTexts, "Dusan");
    }

    /// <summary>
    /// ????????????????????????????????????????????? (??? 5 ???????? 5 Text)
    /// </summary>
    private void UpdateCharacterTimes(int sceneIndex, TextMeshProUGUI[] rankTexts, string characterName)
    {
        if (rankTexts == null || rankTexts.Length == 0) return;

        var results = resultsStore.GetByScene(sceneIndex);

        if (results == null || results.Count == 0)
        {
            for (int i = 0; i < rankTexts.Length; i++)
            {
                if (rankTexts[i] == null) continue;
                rankTexts[i].gameObject.SetActive(true);
                rankTexts[i].text = i == 0 ? ($"{characterName}: {noDataMessage}") : string.Empty;
            }
            return;
        }

        var sortedResults = results.OrderBy(r => r.timeSeconds).Take(Mathf.Min(maxTimesToShow, rankTexts.Length)).ToList();

        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (rankTexts[i] == null) continue;
            rankTexts[i].gameObject.SetActive(true);
            if (i < sortedResults.Count)
            {
                var r = sortedResults[i];
                rankTexts[i].text = $"{i + 1}. {r.playerName} - {FormatTime(r.timeSeconds)}";
            }
            else
            {
                rankTexts[i].text = string.Empty;
            }
        }
    }

    /// <summary>
    /// ??????????????????????????
    /// </summary>
    public void UpdateSelectedCharacter()
    {
        int selectedScene = GetSelectedCharacterSceneIndex();

        switch (selectedScene)
        {
            case 1: // Roman
                UpdateCharacterTimes(romanSceneIndex, romanTexts, "Roman");
                break;
            case 2: // Eva
                UpdateCharacterTimes(evaSceneIndex, evaTexts, "Eva");
                break;
            case 3: // Dusan
                UpdateCharacterTimes(dusanSceneIndex, dusanTexts, "Dusan");
                break;
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
    }
}