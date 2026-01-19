using System.Linq;
using TMPro;
using UnityEngine;
using System.IO;

// Displays a 2-column Top-5 table (names + times) for the selected character (by sceneIndex)
// Selection is resolved from ButtonGroupColorManager and three character buttons (Roman/Eva/Dusan)
// Press the assigned Check button to refresh from JSON (RunResultsStore)
public class Top5ResultsTable : MonoBehaviour
{
    [Header("Selection (buttons -> scene index)")]
    [SerializeField] private ButtonGroupColorManager characterButtonGroup;
    [SerializeField] private ButtonTextColor romanButton;
    [SerializeField] private ButtonTextColor evaButton;
    [SerializeField] private ButtonTextColor dusanButton;
    [SerializeField] private int romanSceneIndex = 1;
    [SerializeField] private int evaSceneIndex = 2;
    [SerializeField] private int dusanSceneIndex = 3;

    [Header("Check Action")]
    [SerializeField] private UnityEngine.UI.Button checkButton;

    [Header("Table Slots (5 rows)")]
    [Tooltip("5 TextMeshProUGUI for player names (rows 1..5)")]
    [SerializeField] private TextMeshProUGUI[] nameSlots = new TextMeshProUGUI[5];
    [Tooltip("5 TextMeshProUGUI for times (rows 1..5)")]
    [SerializeField] private TextMeshProUGUI[] timeSlots = new TextMeshProUGUI[5];

    [Header("Display Options")]
    [SerializeField] private int maxRows = 5;
    [SerializeField] private string timeFormat = "{0:00}:{1:00}"; // mm:ss
    [SerializeField] private string noDataMessage = "No records found";
    
    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private RunResultsStore store;
    private int lastSelectedSceneIndex = -1; // fallback if group selection isn't detected

    private void Awake()
    {
        store = RunResultsStore.Ensure();
        if (checkButton != null)
            checkButton.onClick.AddListener(OnCheckClicked);
        // clear on start
        ClearAllRows();
        TryLogInit();
    }

    public void OnCheckClicked()
    {
        int sceneIndex = ResolveSelectedSceneIndex();
        Log($"OnCheck -> sceneIndex={sceneIndex} (map: R={romanSceneIndex}, E={evaSceneIndex}, D={dusanSceneIndex})");
        LogSelectionStates();
        if (sceneIndex < 0 && lastSelectedSceneIndex >= 0)
        {
            Log($"Using fallback lastSelectedSceneIndex={lastSelectedSceneIndex}");
            sceneIndex = lastSelectedSceneIndex;
        }
        if (sceneIndex < 0)
        {
            ClearAllRows();
            SetRowActive(0, true);
            SetRowText(0, "-", "Please select a character");
            return;
        }
        DumpSceneResults(sceneIndex, "Before Fill");
        FillFromScene(sceneIndex);
    }

    // Alternative entry points if??????????????????????????? (???????????? selected)
    public void ShowSelected()
    {
        OnCheckClicked();
    }

    public void ShowRoman()
    {
        Log($"ShowRoman -> sceneIndex={romanSceneIndex}");
        lastSelectedSceneIndex = romanSceneIndex;
        DumpSceneResults(romanSceneIndex, "ShowRoman");
        FillFromScene(romanSceneIndex);
    }

    public void ShowEva()
    {
        Log($"ShowEva -> sceneIndex={evaSceneIndex}");
        lastSelectedSceneIndex = evaSceneIndex;
        DumpSceneResults(evaSceneIndex, "ShowEva");
        FillFromScene(evaSceneIndex);
    }

    public void ShowDusan()
    {
        Log($"ShowDusan -> sceneIndex={dusanSceneIndex}");
        lastSelectedSceneIndex = dusanSceneIndex;
        DumpSceneResults(dusanSceneIndex, "ShowDusan");
        FillFromScene(dusanSceneIndex);
    }

    public void ShowForSceneIndex(int sceneIndex)
    {
        Log($"ShowForSceneIndex -> sceneIndex={sceneIndex}");
        lastSelectedSceneIndex = sceneIndex;
        DumpSceneResults(sceneIndex, "ShowForSceneIndex");
        FillFromScene(sceneIndex);
    }

    // Wire these to the character buttons' OnClick in case the selection manager isn't used
    public void SelectRoman() { lastSelectedSceneIndex = romanSceneIndex; Log($"SelectRoman set lastSelectedSceneIndex={lastSelectedSceneIndex}"); }
    public void SelectEva() { lastSelectedSceneIndex = evaSceneIndex; Log($"SelectEva set lastSelectedSceneIndex={lastSelectedSceneIndex}"); }
    public void SelectDusan() { lastSelectedSceneIndex = dusanSceneIndex; Log($"SelectDusan set lastSelectedSceneIndex={lastSelectedSceneIndex}"); }

    private void FillFromScene(int sceneIndex)
    {
        var list = store.GetByScene(sceneIndex) ?? new System.Collections.Generic.List<RunResultsStore.RunResult>();
        var top = list
            .OrderBy(r => r.timeSeconds)
            .Take(Mathf.Min(maxRows, 5))
            .ToList();

        ClearAllRows();

        if (top.Count == 0)
        {
            SetRowActive(0, true);
            SetRowText(0, "-", noDataMessage);
            Log($"No data for scene {sceneIndex}");
            return;
        }

        for (int i = 0; i < top.Count; i++)
        {
            var r = top[i];
            SetRowActive(i, true);
            string nameText = string.IsNullOrEmpty(r.playerName) ? "Player" : r.playerName;
            string timeText = FormatTime(r.timeSeconds);
            SetRowText(i, nameText, timeText);
            Log($"Row {i+1}: name='{nameText}', time={r.timeSeconds:F2}s ({timeText}) scene={r.sceneIndex} diff={r.difficulty}");
        }
    }

    private int ResolveSelectedSceneIndex()
    {
        if (characterButtonGroup == null)
            return -1;
        if (romanButton != null && characterButtonGroup.IsCurrentSelected(romanButton)) return romanSceneIndex;
        if (evaButton != null && characterButtonGroup.IsCurrentSelected(evaButton)) return evaSceneIndex;
        if (dusanButton != null && characterButtonGroup.IsCurrentSelected(dusanButton)) return dusanSceneIndex;
        return -1;
    }

    private void ClearAllRows()
    {
        for (int i = 0; i < 5; i++)
        {
            SetRowActive(i, false);
            SetRowText(i, string.Empty, string.Empty);
        }
        Log("Cleared all rows");
    }

    private void SetRowActive(int index, bool active)
    {
        if (index < 0 || index >= 5) return;
        if (nameSlots != null && index < nameSlots.Length && nameSlots[index] != null)
            nameSlots[index].gameObject.SetActive(active);
        if (timeSlots != null && index < timeSlots.Length && timeSlots[index] != null)
            timeSlots[index].gameObject.SetActive(active);
    }

    private void SetRowText(int index, string name, string time)
    {
        if (index < 0 || index >= 5) return;
        if (nameSlots != null && index < nameSlots.Length && nameSlots[index] != null)
            nameSlots[index].text = name ?? string.Empty;
        if (timeSlots != null && index < timeSlots.Length && timeSlots[index] != null)
            timeSlots[index].text = time ?? string.Empty;
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return string.Format(timeFormat, minutes, secs);
    }

    private void TryLogInit()
    {
        if (!verboseLog) return;
        try { Log($"Results JSON: {Path.Combine(Application.persistentDataPath, "run_results.json")}"); } catch {}
        var all = store.GetAll();
        Log($"Store ready. All results count = {all.Count}");
        try
        {
            int cr = store.GetByScene(romanSceneIndex)?.Count ?? 0;
            int ce = store.GetByScene(evaSceneIndex)?.Count ?? 0;
            int cd = store.GetByScene(dusanSceneIndex)?.Count ?? 0;
            Log($"Buckets -> Roman({romanSceneIndex})={cr}, Eva({evaSceneIndex})={ce}, Dusan({dusanSceneIndex})={cd}");
        }
        catch {}
        if (characterButtonGroup == null) Log("WARN: characterButtonGroup is null");
        if (romanButton == null) Log("WARN: romanButton is null");
        if (evaButton == null) Log("WARN: evaButton is null");
        if (dusanButton == null) Log("WARN: dusanButton is null");
        if (nameSlots == null || nameSlots.Length < 5) Log("WARN: nameSlots not set to 5");
        if (timeSlots == null || timeSlots.Length < 5) Log("WARN: timeSlots not set to 5");
    }

    private void DumpSceneResults(int sceneIndex, string title)
    {
        if (!verboseLog) return;
        var list = store.GetByScene(sceneIndex);
        Log($"Dump ({title}) scene={sceneIndex} count={(list==null?0:list.Count)}");
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            Log($"[{i}] name='{r.playerName}' time={r.timeSeconds:F3}s diff={r.difficulty} sceneIdx={r.sceneIndex} scene='{r.sceneName}' date={r.dateIso}");
        }
    }

    private void LogSelectionStates()
    {
        if (!verboseLog || characterButtonGroup == null) return;
        bool r = romanButton != null && characterButtonGroup.IsCurrentSelected(romanButton);
        bool e = evaButton != null && characterButtonGroup.IsCurrentSelected(evaButton);
        bool d = dusanButton != null && characterButtonGroup.IsCurrentSelected(dusanButton);
        Log($"Selected -> Roman={r}, Eva={e}, Dusan={d}");
    }

    private void Log(string msg)
    {
        if (verboseLog) Debug.Log($"[Top5ResultsTable] {msg}");
    }
}
