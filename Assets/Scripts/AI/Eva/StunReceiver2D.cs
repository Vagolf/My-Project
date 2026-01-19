using System.Collections;
using UnityEngine;

public class StunReceiver2D : MonoBehaviour
{
    private bool stunned;

    public void Stun(float seconds)
    {
        if (!gameObject.activeInHierarchy) return;
        if (stunned) return;
        StartCoroutine(StunRoutine(seconds));
    }

    private IEnumerator StunRoutine(float seconds)
    {
        stunned = true;

        // ปิดสคริปต์ควบคุม (ถ้ามี)
        var player = GetComponent<Player>();
        if (player != null) player.enabled = false;

        var roman = GetComponent<Roman>();
        if (roman != null) roman.enabled = false;

        // หยุดการเคลื่อนที่
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        yield return new WaitForSecondsRealtime(seconds);

        // เปิดกลับ
        if (player != null) player.enabled = true;
        if (roman != null) roman.enabled = true;

        stunned = false;
    }
}
