using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // ต้องใช้สำหรับเปลี่ยนฉาก

public class PvESelectionManager : MonoBehaviour
{
    // กำหนดชื่อตัวละครให้เป็น Enum จะได้ไม่พิมพ์ผิด
    public enum Character { None, Kaisa, Roman, Eva, Dusan }

    [Header("สถานะที่เลือกปัจจุบัน")]
    public Character selectedPlayer = Character.None;
    public Character selectedEnemy = Character.None;

    [Header("UI ปุ่มเลือกตัวละคร (ลากปุ่มมาใส่ให้ตรงกับลำดับ)")]
    // ลำดับ: 0=Kaisa, 1=Roman, 2=Eva, 3=Dusan
    public Image[] playerButtons;
    public Image[] enemyButtons;

    [Header("สีไฮไลท์เมื่อถูกเลือก")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow; // สีตอนกดเลือก

    // ----------------------------------------------------
    // ฟังก์ชันสำหรับฝั่ง Player (ผูกกับปุ่มฝั่งซ้าย)
    // ----------------------------------------------------
    public void SelectPlayer(int characterIndex)
    {
        // 1=Kaisa, 2=Roman, 3=Eva, 4=Dusan (อิงตาม Enum ด้านบน, 0 คือ None)
        selectedPlayer = (Character)characterIndex;

        // อัปเดตสีปุ่มให้รู้ว่าเลือกตัวไหน
        UpdateVisuals(playerButtons, characterIndex - 1);
        Debug.Log("Player Selected: " + selectedPlayer);
    }

    // ----------------------------------------------------
    // ฟังก์ชันสำหรับฝั่ง Enemy (ผูกกับปุ่มฝั่งขวา)
    // ----------------------------------------------------
    public void SelectEnemy(int characterIndex)
    {
        selectedEnemy = (Character)characterIndex;
        UpdateVisuals(enemyButtons, characterIndex - 1);
        Debug.Log("Enemy Selected: " + selectedEnemy);
    }

    // ฟังก์ชันเปลี่ยนสีปุ่ม
    private void UpdateVisuals(Image[] buttons, int selectedIndex)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                // ถ้าเป็นปุ่มที่ถูกเลือก ให้เปลี่ยนเป็นสีที่ตั้งไว้ ถ้าไม่ใช่ให้เป็นสีปกติ
                buttons[i].color = (i == selectedIndex) ? selectedColor : normalColor;
            }
        }
    }

    // ----------------------------------------------------
    // ฟังก์ชันเมื่อกดปุ่ม Fight
    // ----------------------------------------------------
    public void OnFightButtonClicked()
    {
        // 1. เช็คว่าเลือกครบทั้ง 2 ฝั่งหรือยัง
        if (selectedPlayer == Character.None || selectedEnemy == Character.None)
        {
            Debug.LogWarning("กรุณาเลือกตัวละครให้ครบทั้ง Player และ Enemy!");
            // ตรงนี้คุณอาจจะทำ Popup แจ้งเตือนผู้เล่นเพิ่มเติมได้ครับ
            return;
        }

        // 2. นำข้อมูลไปเช็คเพื่อเข้า Scene
        string sceneToLoad = DetermineSceneName(selectedPlayer, selectedEnemy);

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log("กำลังโหลดฉาก: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("ไม่พบเงื่อนไข Scene สำหรับคู่นี้!");
        }
    }

    // ----------------------------------------------------
    // 🧠 ระบบตัดสินใจเลือก Scene (แก้ชื่อ Scene ตรงนี้ให้ตรงกับเกมของคุณ)
    // ----------------------------------------------------
    private string DetermineSceneName(Character player, Character enemy)
    {
        // วิธีที่ 1: ตั้งชื่อ Scene ให้เป็นระบบ เช่น "PvE_Kaisa_vs_Roman"
        // return $"PvE_{player}_vs_{enemy}"; 

        // วิธีที่ 2: ใช้ If-Else เช็คทีละคู่ (ยืดหยุ่นกว่าถ้าด่านใช้ชื่อไม่เหมือนกัน)
        if (player == Character.Kaisa && enemy == Character.Roman)
        {
            return "Stage1_Forest"; // เปลี่ยนเป็นชื่อ Scene จริงของคุณ
        }
        else if (player == Character.Kaisa && enemy == Character.Eva)
        {
            return "Stage2_Temple";
        }
        else if (player == Character.Roman && enemy == Character.Dusan)
        {
            return "Stage3_Castle";
        }
        // ตรวจสอบกรณีเลือกตัวเหมือนกัน (กระจก)
        else if (player == Character.Kaisa && enemy == Character.Kaisa)
        {
            return "Stage_Mirror_Kaisa";
        }

        // ถ้าใช้ชื่อ Scene ตามสูตรสำเร็จที่คาดเดาได้ เปิดบรรทัดนี้ทิ้งไว้เป็น Default ได้เลยครับ
        return $"Scene_{player}_vs_{enemy}";
    }
}