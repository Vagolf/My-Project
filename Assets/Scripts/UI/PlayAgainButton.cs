using UnityEngine;
using UnityEngine.SceneManagement;

// Attach this to the "Play Again" button on the defeat screen.
public class PlayAgainButton : MonoBehaviour
{
    [Header("Load Options")]
    [Tooltip("Use LoadSceneAsync instead of LoadScene")]
    [SerializeField] private bool useAsyncLoad = false;
    [Tooltip("Set Time.timeScale before reloading (defeat flow often pauses at 0)")]
    [SerializeField] private bool resetTimeScale = true;
    [SerializeField] private float timeScaleOnReload = 1f;

    [Header("Optional: close this panel before reload")]
    [SerializeField] private GameObject defeatPanel;

    [Header("Back to Menu Settings")]
    [Tooltip("ชื่อ Scene เมนูหลักที่ต้องการกลับไป")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // ==============================================
    // Bind this to the "Play Again" Button.onClick
    // ==============================================
    public void PlayAgain()
    {
        if (defeatPanel != null) defeatPanel.SetActive(false);

        if (resetTimeScale)
            Time.timeScale = timeScaleOnReload;

        // Clear any global pause flag if your project uses it
        try { PauseGame.GameIsPause = false; } catch { }

        var scene = SceneManager.GetActiveScene();
        if (useAsyncLoad)
            SceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
    }

    // ==============================================
    // 🔥 ฟังก์ชันใหม่: Bind this to the "Back" Button.onClick
    // ==============================================
    public void BackToMenu()
    {
        if (resetTimeScale)
            Time.timeScale = timeScaleOnReload;

        try { PauseGame.GameIsPause = false; } catch { }

        // สั่งให้ระบบจำค่าว่าต้องเปิด Panel ล่าสุดค้างไว้ (หน้า PvE)
        PlayerPrefs.SetInt("ShowLastPanel", 1); // เปลี่ยน "ShowLastPanel" ให้ตรงกับ Key ของ MainMenuLastPanelRestorer ของคุณถ้าจำเป็น
        PlayerPrefs.Save();

        // โหลดกลับไปที่หน้า Main Menu
        if (useAsyncLoad)
            SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }
}