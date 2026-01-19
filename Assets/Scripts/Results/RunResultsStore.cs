using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent JSON store for victory results.
// Saves: player name, elapsed time, scene id/name, difficulty, date.
public class RunResultsStore : MonoBehaviour
{
    [Serializable]
    public class RunResult
    {
        public string playerName;
        public float timeSeconds;
        public int sceneIndex;
        public string sceneName;
        public string difficulty;
        public string dateIso;
    }

    [Serializable]
    private class RunResultList
    {
        public List<RunResult> results = new List<RunResult>();
    }

    public static RunResultsStore Instance { get; private set; }

    private RunResultList _data = new RunResultList();
    private string _filePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _filePath = Path.Combine(Application.persistentDataPath, "run_results.json");
        LoadFromDisk();
    }

    // Add a new result and save to disk
    public void AddResult(string playerName, float timeSeconds, int sceneIndex, string difficulty)
    {
        string sceneName = SafeGetSceneName(sceneIndex);
        var r = new RunResult
        {
            playerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim(),
            timeSeconds = Mathf.Max(0f, timeSeconds),
            sceneIndex = sceneIndex,
            sceneName = sceneName,
            difficulty = string.IsNullOrWhiteSpace(difficulty) ? "Normal" : difficulty,
            dateIso = DateTime.UtcNow.ToString("o")
        };
        _data.results.Add(r);
        SaveToDisk();
    }

    public IReadOnlyList<RunResult> GetAll() => _data.results;

    public List<RunResult> GetByScene(int sceneIndex)
    {
        return _data.results.FindAll(r => r.sceneIndex == sceneIndex);
    }

    public void ClearAll()
    {
        _data.results.Clear();
        SaveToDisk();
    }

    private string SafeGetSceneName(int index)
    {
        try
        {
            if (index >= 0 && index < SceneManager.sceneCountInBuildSettings)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(index);
                if (!string.IsNullOrEmpty(path))
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    return string.IsNullOrEmpty(name) ? $"Scene_{index}" : name;
                }
            }
        }
        catch { }
        return $"Scene_{index}";
    }

    private void LoadFromDisk()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                var loaded = JsonUtility.FromJson<RunResultList>(json);
                if (loaded != null && loaded.results != null)
                    _data = loaded;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RunResultsStore] Load error: {e.Message}");
            _data = new RunResultList();
        }
    }

    private void SaveToDisk()
    {
        try
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RunResultsStore] Save error: {e.Message}");
        }
    }

    // Helper to ensure there's an instance in the scene when called from code
    public static RunResultsStore Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("RunResultsStore");
            Instance = go.AddComponent<RunResultsStore>();
        }
        return Instance;
    }
}
