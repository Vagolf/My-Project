using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTimer : MonoBehaviour
{
    [Header("ตั้งค่าการเปลี่ยน Scene")]
    [Tooltip("เวลาที่ต้องรอก่อนเปลี่ยน Scene (วินาที)")]
    public float waitTime = 5f;

    [Tooltip("ชื่อ Scene ถัดไปที่ต้องการให้โหลด")]
    public string nextSceneName = "MainMenu";

    private void Start()
    {
        // เริ่มนับเวลาทันทีที่ Object นี้ทำงาน (เช่น เปิดเข้ามาใน Scene นี้)
        StartCoroutine(WaitAndLoadScene());
    }

    private IEnumerator WaitAndLoadScene()
    {
        // รอเวลาตามที่กำหนด
        yield return new WaitForSeconds(waitTime);

        // โหลด Scene ถัดไป
        SceneManager.LoadScene(nextSceneName);
    }
}