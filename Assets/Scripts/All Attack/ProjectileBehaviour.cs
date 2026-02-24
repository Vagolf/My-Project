using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    public float Speed = 18f;
    public float Damage = 75f;

    private Vector2 moveDirection = Vector2.zero;
    private LayerMask opponentLayer;

    // ✅ ฟังก์ชันรับค่าจาก EvaAI
    public void Init(Vector2 dir, float spd, float dmg, LayerMask targetL, float kb, float stun)
    {
        moveDirection = dir.normalized;
        Speed = spd;
        Damage = dmg;
        opponentLayer = targetL;

        // 🔥 แก้ปัญหาภาพกลับด้านโดยใช้ SpriteRenderer (หลีกเลี่ยงการตีกับ Animator)
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            if (moveDirection.x > 0)
            {
                // ถ้ายิงไปทางขวา -> ไม่ต้องพลิกภาพ (ถ้าภาพต้นฉบับหันขวาอยู่แล้ว)
                sprite.flipX = false;
            }
            else if (moveDirection.x < 0)
            {
                // ถ้ายิงไปทางซ้าย -> ติ๊กพลิกภาพแกน X
                sprite.flipX = true;
            }
        }

        // ตั้งเวลาทำลายตัวเอง
        Destroy(gameObject, 5f);
    }

    private void Update()
    {

        if (Timer.GateBlocked)
        {
            Destroy(gameObject);
            return;
        }

        // 🔥 แก้ปัญหาไปทางซ้ายอย่างเดียว: บังคับให้พุ่งตามทิศทางที่รับมาจาก EvaAI เท่านั้น
        if (moveDirection != Vector2.zero)
        {
            transform.Translate(moveDirection * Speed * Time.deltaTime, Space.World);
        }
        else
        {
            // ถ้าไม่ได้ยิงผ่าน EvaAI (เทสลากลงฉากเอง) ให้มันพุ่งไปทางซ้าย
            transform.Translate(Vector3.left * Speed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HealthCh playerHealth = collision.GetComponent<HealthCh>();

        if (playerHealth != null)
        {
            // ทำดาเมจ
            playerHealth.TakeDamage(Damage);
            // ชน Player แล้วหายไป
            Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // ชนกำแพง/พื้น แล้วหายไป
            Destroy(gameObject);
        }
    }
}