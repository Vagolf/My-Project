using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Story
{
    public class SaveStoryPanel : MonoBehaviour
    {
        [Header("Panels (same scene)")]
        [SerializeField] private GameObject panelSaveStory;
        [SerializeField] private GameObject panelCreateSave;
        [SerializeField] private GameObject panelChooseStation;

        [Header("Save list UI")]
        [SerializeField] private Transform content;
        [SerializeField] private GameObject saveItemPrefab; // Button + TMP_Text
        [SerializeField] private Button createNewButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text Name;

        private void Awake()
        {
            if (createNewButton) createNewButton.onClick.AddListener(OpenCreateSave);
            if (backButton) backButton.onClick.AddListener(BackToMenu);
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!content || !saveItemPrefab) return;
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

            List<SaveData> saves = SaveManager.GetAll();
            if (saves.Count > 0)
            {
                var s = saves[0];
                // Set the Name TMP_Text field if assigned
                if (Name != null) Name.text = s.name;

                var go = Instantiate(saveItemPrefab, content);
                var btn = go.GetComponentInChildren<Button>();
                var texts = go.GetComponentsInChildren<TMP_Text>(true);
                // Primary line: save name only
                if (texts != null && texts.Length > 0)
                {
                    TMP_Text nameText = null;
                    TMP_Text progressText = null;
                    foreach (var t in texts)
                    {
                        if (nameText == null && t.name.ToLower().Contains("name")) nameText = t;
                        if (progressText == null && t.name.ToLower().Contains("progress")) progressText = t;
                    }
                    if (nameText == null) nameText = texts[0];
                    nameText.text = s.name;

                    int maxPlayable = Mathf.Clamp(s.selectedStage + 1, 1, 3);
                    if (progressText != null)
                    {
                        progressText.text = $"เล่นได้ถึงด่าน {maxPlayable}";
                    }
                    else if (texts.Length > 1)
                    {
                        texts[1].text = $"เล่นได้ถึงด่าน {maxPlayable}";
                    }
                }
                if (btn) btn.onClick.AddListener(() => SelectSave(s.id));
            }
        }

        private void SelectSave(string id)
        {
            SaveManager.SetCurrentId(id);
            SaveManager.Touch(id);
            // switch panel to ChooseStation
            if (panelSaveStory) panelSaveStory.SetActive(false);
            if (panelChooseStation) panelChooseStation.SetActive(true);
            if (panelCreateSave) panelCreateSave.SetActive(false);
        }

        private void OpenCreateSave()
        {
            if (panelSaveStory) panelSaveStory.SetActive(false);
            if (panelCreateSave) panelCreateSave.SetActive(true);
            if (panelChooseStation) panelChooseStation.SetActive(false);
        }

        private void BackToMenu()
        {
            // just deactivate this panel to reveal parent/menu panel in same scene
            if (panelSaveStory) panelSaveStory.SetActive(false);
        }
    }
}
