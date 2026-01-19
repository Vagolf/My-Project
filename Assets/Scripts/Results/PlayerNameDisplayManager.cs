using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// ???????????????????????????? ?????????????
/// ??????????????? Top 5 ??????????????????
/// </summary>
public class PlayerNameDisplayManager : MonoBehaviour
{
    [Header("Character Selection")]
    [SerializeField] private ButtonGroupColorManager characterButtonGroup;
    [SerializeField] private int romanSceneIndex = 1;
    [SerializeField] private int evaSceneIndex = 2;
    [SerializeField] private int dusanSceneIndex = 3;

    [Header("Player Name Display UI")]
    [SerializeField] private TextMeshProUGUI romanPlayersText;
    [SerializeField] private TextMeshProUGUI evaPlayersText;
    [SerializeField] private TextMeshProUGUI dusanPlayersText;

    [Header("Button References")]
    [SerializeField] private ButtonTextColor romanButton;
    [SerializeField] private ButtonTextColor evaButton;
    [SerializeField] private ButtonTextColor dusanButton;

    [Header("Update Button")]
    [SerializeField] private UnityEngine.UI.Button updateButton;

    [Header("Display Options")]
    [SerializeField] private int maxPlayersToShow = 5;
    [SerializeField] private string noDataMessage = "No players found";

    private RunResultsStore resultsStore;

    private void Start()
    {
        resultsStore = RunResultsStore.Ensure();
        
        if (updateButton != null)
            updateButton.onClick.AddListener(UpdateAllDisplays);
            
        // ??????????????????
        UpdateAllDisplays();
    }

    /// <summary>
    /// ?????????????????????????
    /// </summary>
    public void UpdateAllDisplays()
    {
        UpdateCharacterPlayerNames(romanSceneIndex, romanPlayersText, "Roman");
        UpdateCharacterPlayerNames(evaSceneIndex, evaPlayersText, "Eva");
        UpdateCharacterPlayerNames(dusanSceneIndex, dusanPlayersText, "Dusan");
    }

    /// <summary>
    /// ??????????????????????????????????????
    /// </summary>
    private void UpdateCharacterPlayerNames(int sceneIndex, TextMeshProUGUI targetText, string characterName)
    {
        if (targetText == null) return;

        var results = resultsStore.GetByScene(sceneIndex);
        
        if (results == null || results.Count == 0)
        {
            targetText.text = $"{characterName}:\n{noDataMessage}";
            return;
        }

        var sortedResults = results.OrderBy(r => r.timeSeconds).Take(maxPlayersToShow).ToList();
        
        string displayText = $"{characterName} Top {Mathf.Min(sortedResults.Count, maxPlayersToShow)}:\n";
        
        for (int i = 0; i < sortedResults.Count; i++)
        {
            displayText += $"{i + 1}. {sortedResults[i].playerName}\n";
        }
        
        targetText.text = displayText.TrimEnd('\n');
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
                UpdateCharacterPlayerNames(romanSceneIndex, romanPlayersText, "Roman");
                break;
            case 2: // Eva
                UpdateCharacterPlayerNames(evaSceneIndex, evaPlayersText, "Eva");
                break;
            case 3: // Dusan
                UpdateCharacterPlayerNames(dusanSceneIndex, dusanPlayersText, "Dusan");
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
    /// ??????? scene index ?????????????????????
    /// </summary>
    public void SetCharacterSceneIndices(int roman, int eva, int dusan)
    {
        romanSceneIndex = roman;
        evaSceneIndex = eva;
        dusanSceneIndex = dusan;
    }
}