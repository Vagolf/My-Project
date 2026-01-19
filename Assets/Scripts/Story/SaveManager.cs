using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Story
{
    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    [Serializable]
    public class SaveData
    {
        public string id;              // unique id (Guid)
        public string name;            // display name
        public Difficulty difficulty;  // difficulty
        public int selectedStage;      // highest completed stage (0..3). Allow to play selectedStage+1
        public string createdAt;       // ISO time
        public string lastPlayedAt;    // ISO time
    }

    [Serializable]
    internal class SaveDataList
    {
        public List<SaveData> items = new List<SaveData>();
    }

    public static class SaveManager
    {
        private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "saves.json");
        private const string CurrentKey = "story.current";

        // Only allow one save slot
        public static List<SaveData> GetAll()
        {
            if (!File.Exists(FilePath)) return new List<SaveData>();
            try
            {
                var json = File.ReadAllText(FilePath);
                var wrap = JsonUtility.FromJson<SaveDataList>(json);
                if (wrap != null && wrap.items != null && wrap.items.Count > 0)
                {
                    // Only keep the first save
                    return new List<SaveData> { wrap.items[0] };
                }
                return new List<SaveData>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Read error: {e.Message}");
                return new List<SaveData>();
            }
        }

        private static void Persist(List<SaveData> list)
        {
            try
            {
                // Only persist the first save (if any)
                var wrap = new SaveDataList { items = list.Count > 0 ? new List<SaveData> { list[0] } : new List<SaveData>() };
                var json = JsonUtility.ToJson(wrap, true);
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Write error: {e.Message}");
            }
        }

        public static SaveData Create(string displayName, Difficulty difficulty)
        {
            var now = DateTime.UtcNow.ToString("o");
            var data = new SaveData
            {
                id = Guid.NewGuid().ToString("N"),
                name = string.IsNullOrWhiteSpace(displayName) ? "Save 1" : displayName.Trim(),
                difficulty = difficulty,
                selectedStage = 0, // none completed yet
                createdAt = now,
                lastPlayedAt = now
            };
            // Overwrite any previous save
            var saves = new List<SaveData> { data };
            Persist(saves);
            SetCurrentId(data.id);
            return data;
        }

        public static void Delete(string id)
        {
            var list = GetAll();
            var idx = list.FindIndex(s => s.id == id);
            if (idx >= 0)
            {
                list.RemoveAt(idx);
                Persist(list);
                if (GetCurrentId() == id) ClearCurrentId();
            }
        }

        public static void Touch(string id)
        {
            var list = GetAll();
            var s = list.Find(x => x.id == id);
            if (s != null)
            {
                s.lastPlayedAt = DateTime.UtcNow.ToString("o");
                Persist(list);
            }
        }

        public static void SetProgress(string id, int completedStage)
        {
            var list = GetAll();
            var s = list.Find(x => x.id == id);
            if (s != null)
            {
                s.selectedStage = Mathf.Clamp(Mathf.Max(s.selectedStage, completedStage), 0, 3);
                s.lastPlayedAt = DateTime.UtcNow.ToString("o");
                Persist(list);
            }
        }

        public static SaveData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var list = GetAll();
            return list.Find(s => s.id == id);
        }

        public static void SetCurrentId(string id)
        {
            PlayerPrefs.SetString(CurrentKey, id ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static string GetCurrentId() => PlayerPrefs.GetString(CurrentKey, string.Empty);
        public static SaveData GetCurrent() => GetById(GetCurrentId());
        public static void ClearCurrentId() => PlayerPrefs.DeleteKey(CurrentKey);
    }
}
