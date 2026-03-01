using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 🔥 ต้องมี using นี้ถึงจะโหลดฉากได้

public class PvEButtonManager : MonoBehaviour
{
    private List<PvEButton> allButtons = new List<PvEButton>();

    [Header("สถานะการเลือกปัจจุบัน")]
    public string selectedPlayer = "";
    public string selectedEnemy = "";

    public void RegisterButton(PvEButton btn)
    {
        if (!allButtons.Contains(btn))
        {
            allButtons.Add(btn);
        }
    }

    public void OnButtonPressed(PvEButton clickedButton)
    {
        // 1. เคลียร์สีปุ่มอื่นในกลุ่มเดียวกัน
        foreach (PvEButton btn in allButtons)
        {
            if (btn.groupName == clickedButton.groupName && btn != clickedButton)
            {
                btn.ResetColor();
            }
        }

        // 2. 🔥 เก็บข้อมูลว่าตอนนี้เลือกใครอยู่
        if (clickedButton.groupName == "Player")
        {
            selectedPlayer = clickedButton.characterName;
        }
        else if (clickedButton.groupName == "Enemy")
        {
            selectedEnemy = clickedButton.characterName;
        }
    }

    // 🔥 ฟังก์ชันนี้เอาไปผูกกับ OnClick ของปุ่ม Fight
    public void OnFightButtonClicked()
    {
        // เช็คว่าเลือกตัวละครครบทั้ง 2 ฝั่งหรือยัง
        if (string.IsNullOrEmpty(selectedPlayer) || string.IsNullOrEmpty(selectedEnemy))
        {
            Debug.LogWarning("กรุณาเลือกตัวละครทั้งฝั่ง Player และ Enemy ก่อนกด Fight!");
            return;
        }

        // หาชื่อ Scene ที่จะโหลดตามเงื่อนไข
        string sceneToLoad = GetSceneName(selectedPlayer, selectedEnemy);

        Debug.Log($"กำลังเข้าสู่ฉาก: {sceneToLoad} (Player: {selectedPlayer} VS Enemy: {selectedEnemy})");
        SceneManager.LoadScene(sceneToLoad);
    }

    // 🔥 ฟังก์ชันตรวจสอบเงื่อนไข (ว่าเลือกใครชนใคร แล้วไปฉากไหน)
    private string GetSceneName(string player, string enemy)
    {
        // แก้ไขเงื่อนไขและชื่อ Scene ด้านล่างนี้ให้ตรงกับในโปรเจกต์ของคุณได้เลย
        if (player == "Kaisa" && enemy == "Roman")
        {
            return "Stage_Kaisa_VS_Roman"; // ชื่อฉากใน Unity
        }
        else if (player == "Kaisa" && enemy == "Eva")
        {
            return "Stage_Kaisa_VS_Eva";
        }
        else if (player == enemy)
        {
            // ถ้าเลือกตัวเดียวกันมาสู้กัน (กระจก)
            return "Stage_Mirror";
        }

        // ถ้าขี้เกียจเขียน if-else ทุกตัว สามารถตั้งชื่อ Scene ใน Unity ให้เป็นสูตรแบบนี้ได้เลย
        // เช่น ฉากชื่อ "Fight_Roman_Dusan"
        return $"Fight_{player}_{enemy}";
    }
}