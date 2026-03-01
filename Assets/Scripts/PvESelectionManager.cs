using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PvESelectionManager : MonoBehaviour
{
    [Header("สถานะที่เลือกปัจจุบัน (ห้ามพิมพ์เอง ระบบจะจำให้)")]
    public string selectedPlayer = "";
    public string selectedEnemy = "";

    [Header("สีไฮไลท์ปุ่ม")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.cyan;

    [Header("ปุ่มฝั่งซ้าย (Player)")]
    public Image[] playerButtons;
    // ใส่ชื่อตัวละครให้ตรงกับลำดับปุ่ม (0=Kaisa, 1=Eva, 2=Roman, 3=Dusan)
    public string[] playerNames = { "Kaisa", "Eva", "Roman", "Dusan" };

    [Header("ปุ่มฝั่งขวา (Enemy)")]
    public Image[] enemyButtons;
    public string[] enemyNames = { "Kaisa", "Eva", "Roman", "Dusan" };

    // ---------------------------------------------------
    // ฟังก์ชันนี้เอาไปผูกกับปุ่มฝั่งซ้ายทั้ง 4 ปุ่ม
    // ---------------------------------------------------
    public void SelectPlayer(int index)
    {
        selectedPlayer = playerNames[index]; // จำชื่อตัวละคร
        UpdateButtonColors(playerButtons, index); // เปลี่ยนสีปุ่ม
        Debug.Log("เลือก Player: " + selectedPlayer);
    }

    // ---------------------------------------------------
    // ฟังก์ชันนี้เอาไปผูกกับปุ่มฝั่งขวาทั้ง 4 ปุ่ม
    // ---------------------------------------------------
    public void SelectEnemy(int index)
    {
        selectedEnemy = enemyNames[index];
        UpdateButtonColors(enemyButtons, index);
        Debug.Log("เลือก Enemy: " + selectedEnemy);
    }

    // ระบบเปลี่ยนสีปุ่ม
    private void UpdateButtonColors(Image[] buttons, int selectedIndex)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].color = (i == selectedIndex) ? selectedColor : normalColor;
            }
        }
    }

    // ---------------------------------------------------
    // ฟังก์ชันนี้เอาไปผูกกับปุ่ม Fight
    // ---------------------------------------------------
    public void OnFightButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedPlayer) || string.IsNullOrEmpty(selectedEnemy))
        {
            Debug.LogWarning("กรุณาเลือกตัวละครทั้งฝั่ง Player และ Enemy ก่อนกด Fight!");
            return;
        }

        // 🔥 ตรงนี้คือเงื่อนไขการเข้า Scene 
        // สมมติว่าตั้งชื่อ Scene ใน Unity ไว้แบบนี้ "Fight_Kaisa_vs_Roman"
        string sceneName = "Fight_" + selectedPlayer + "_vs_" + selectedEnemy;

        Debug.Log("กำลังโหลดฉาก: " + sceneName);

        // เอาเครื่องหมาย // ออกเมื่อคุณสร้าง Scene รอไว้ใน Build Settings แล้ว
        SceneManager.LoadScene(sceneName); 
    }
}