using UnityEngine;
using UnityEngine.SceneManagement; // ใช้สำหรับเปลี่ยนซีนตอนจบ

public class CreditRoll : MonoBehaviour
{
    [Header("Settings")]
    public float scrollSpeed = 50f; // ความเร็วในการไหลขึ้น
    public float endYPosition = 2000f; // ตำแหน่ง Y ที่บอกว่า "จบแล้ว" (ปรับให้พอดีกับความยาวข้อความ)

    [Header("Next Scene")]
    public string mainMenuSceneName = "MainMenu"; // ชื่อฉากเมนูหลักที่จะให้กลับไป

    private RectTransform rectTransform;

    void Start()
    {
        // ดึงคอมโพเนนต์ RectTransform ของตัวหนังสือมาใช้งาน
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // สั่งให้เลื่อนขึ้นข้างบนเรื่อยๆ (Vector2.up) ตามความเร็ว
        rectTransform.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        // ถ้ายกตัวหนังสือขึ้นไปจนถึงจุดที่กำหนดไว้ (endYPosition)
        if (rectTransform.anchoredPosition.y >= endYPosition)
        {
            EndCutscene();
        }
    }

    // ฟังก์ชันตอนเครดิตไหลจบ
    private void EndCutscene()
    {
        Debug.Log("เครดิตจบแล้ว! กำลังกลับหน้าเมนู...");

        // โหลดกลับหน้าเมนูหลัก (อย่าลืมเพิ่มซีนใน File -> Build Settings ด้วยนะครับ)
        // SceneManager.LoadScene(mainMenuSceneName); 
    }
}