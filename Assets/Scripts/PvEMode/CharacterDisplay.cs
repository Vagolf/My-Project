using UnityEngine;
using UnityEngine.UI;

public class CharacterDisplay : MonoBehaviour
{
    public Image[] playerImages; // 6 ภาพสำหรับ Player
    public Image[] enemyImages;  // 6 ภาพสำหรับ Enemy

    // แสดงตัวละครฝั่ง Player ตาม index (0-5)
    public void ShowPlayer(int index, Sprite characterSprite)
    {
        if (index >= 0 && index < playerImages.Length && playerImages[index] != null)
        {
            playerImages[index].sprite = characterSprite;
            playerImages[index].gameObject.SetActive(true);
        }
    }

    // แสดงตัวละครฝั่ง Enemy ตาม index (0-5)
    public void ShowEnemy(int index, Sprite characterSprite)
    {
        if (index >= 0 && index < enemyImages.Length && enemyImages[index] != null)
        {
            enemyImages[index].sprite = characterSprite;
            enemyImages[index].gameObject.SetActive(true);
        }
    }

    // ซ่อนภาพทั้งหมด (ถ้าต้องการ)
    public void HideAll()
    {
        foreach (var img in playerImages)
            if (img != null) img.gameObject.SetActive(false);
        foreach (var img in enemyImages)
            if (img != null) img.gameObject.SetActive(false);
    }
}