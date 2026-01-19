using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace Story
{
    public class ChooseStationPanel : MonoBehaviour
    {
        [Header("Panels (same scene)")]
        [SerializeField] private GameObject panelSaveStory;
        [SerializeField] private GameObject panelCreateSave;
        [SerializeField] private GameObject panelChooseStation;

        [Header("Header UI")]
        [SerializeField] private TMP_Text saveNameText;

        [Header("Stage Select")]
        [SerializeField] private Button stage1Button;
        [SerializeField] private Button stage2Button;
        [SerializeField] private Button stage3Button;
        [SerializeField] private Image mapPreview;
        [SerializeField] private Sprite stage1Sprite;
        [SerializeField] private Sprite stage2Sprite;
        [SerializeField] private Sprite stage3Sprite;
        [Header("Optional: Multiple Previews (Roman/Eva/Dusan)")]
        [Tooltip("Assign 3 Images to show per selection (1=Roman, 2=Eva, 3=Dusan). If set, these will be toggled active; otherwise fallback to single sprite swap above.")]
        [SerializeField] private Image[] mapPreviews; // size 3

        [Header("Start")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        [Header("Start Mapping (by Character)")]
        [Tooltip("Build index for Roman (button 1). Set < 0 to use scene name instead.")]
        [SerializeField] private int romanBuildIndex = 1;
        [Tooltip("Build index for Eva (button 2). Set < 0 to use scene name instead.")]
        [SerializeField] private int evaBuildIndex = 2;
        [Tooltip("Build index for Dusan (button 3). Set < 0 to use scene name instead.")]
        [SerializeField] private int dusanBuildIndex = 3;
        [Tooltip("Optional scene name for Roman if build index < 0")] [SerializeField] private string romanSceneName;
        [Tooltip("Optional scene name for Eva if build index < 0")] [SerializeField] private string evaSceneName;
        [Tooltip("Optional scene name for Dusan if build index < 0")] [SerializeField] private string dusanSceneName;

        private int selectedStageIndex = 0; // 1..3

        private void Awake()
        {
            if (stage1Button) stage1Button.onClick.AddListener(() => SelectStage(1));
            if (stage2Button) stage2Button.onClick.AddListener(() => SelectStage(2));
            if (stage3Button) stage3Button.onClick.AddListener(() => SelectStage(3));
            if (startButton) startButton.onClick.AddListener(StartGame);
            if (backButton) backButton.onClick.AddListener(Back);
        }

        private void OnEnable()
        {
            // Ensure time is resumed when entering this panel (in case previous UI paused it)
            Time.timeScale = 1f;
            try { PauseGame.GameIsPause = false; } catch { }

            var save = SaveManager.GetCurrent();
            if (save == null) return;
            if (saveNameText) saveNameText.text = save.name;

            // lock stages beyond progress+1
            var maxPlayable = Mathf.Clamp(save.selectedStage + 1, 1, 3);
            if (stage1Button) stage1Button.interactable = 1 <= maxPlayable;
            if (stage2Button) stage2Button.interactable = 2 <= maxPlayable;
            if (stage3Button) stage3Button.interactable = 3 <= maxPlayable;

            // auto select first available
            selectedStageIndex = Mathf.Clamp(maxPlayable, 1, 3);
            SelectStage(selectedStageIndex);
        }

        private void SelectStage(int index)
        {
            selectedStageIndex = index;
            // Prefer multi-image previews if assigned
            if (mapPreviews != null && mapPreviews.Length >= 3)
            {
                for (int i = 0; i < mapPreviews.Length; i++)
                {
                    if (mapPreviews[i] == null) continue;
                    mapPreviews[i].gameObject.SetActive((i + 1) == index);
                }
            }
            else if (mapPreview)
            {
                // Fallback to single sprite switch
                mapPreview.sprite = index == 1 ? stage1Sprite : index == 2 ? stage2Sprite : stage3Sprite;
            }
        }

        private void StartGame()
        {
            var save = SaveManager.GetCurrent();
            if (save == null) return;
            // Always map by selected character (Roman/Eva/Dusan)
            Time.timeScale = 1f;
            try { PauseGame.GameIsPause = false; } catch { }
            switch (selectedStageIndex)
            {
                case 1:
                    if (romanBuildIndex >= 0) SceneManager.LoadScene(romanBuildIndex);
                    else if (!string.IsNullOrEmpty(romanSceneName)) SceneManager.LoadScene(romanSceneName);
                    break;
                case 2:
                    if (evaBuildIndex >= 0) SceneManager.LoadScene(evaBuildIndex);
                    else if (!string.IsNullOrEmpty(evaSceneName)) SceneManager.LoadScene(evaSceneName);
                    break;
                case 3:
                    if (dusanBuildIndex >= 0) SceneManager.LoadScene(dusanBuildIndex);
                    else if (!string.IsNullOrEmpty(dusanSceneName)) SceneManager.LoadScene(dusanSceneName);
                    break;
            }
        }

        private void Back()
        {
            if (panelChooseStation) panelChooseStation.SetActive(false);
            if (panelSaveStory) panelSaveStory.SetActive(true);
            if (panelCreateSave) panelCreateSave.SetActive(false);
        }
    }
}
