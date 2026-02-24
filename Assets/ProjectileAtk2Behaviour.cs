using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class ProjectileAtk2Behaviour : MonoBehaviour
{
    [SerializeField] public float Speed = 14f;
    [SerializeField] public float Damage = 150f;
    [SerializeField] public float StunDuration = 1f; // เวลาในการ Stun
    private Vector2 moveDirection = Vector2.zero;
    private LayerMask opponentLayer;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    // ✅ ฟังก์ชันรับค่าจาก EvaAI (เหมือนกระสุนปกติ)
    public void Init(Vector2 dir, float spd, float dmg, LayerMask targetL, float kb, float stun)
    {
        moveDirection = dir.normalized;
        Speed = spd;
        Damage = dmg;
        opponentLayer = targetL;
        StunDuration = stun > 0 ? stun : 1f; // ถ้ารับค่ามาเป็น 0 ให้ใช้ 1 วินาทีเป็นค่าเริ่มต้น

        // พลิกภาพกระสุนโดยไม่ตีกับ Animator
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            if (moveDirection.x > 0) sprite.flipX = false;
            else if (moveDirection.x < 0) sprite.flipX = true;
        }

        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        if (Timer.GateBlocked)
        {
            Destroy(gameObject);
            return;
        }

        // พุ่งไปตามทิศทาง
        if (moveDirection != Vector2.zero)
        {
            transform.Translate(moveDirection * Speed * Time.deltaTime, Space.World);
        }
    }

    // ✅ ฟังก์ชันชน -> ดาเมจ -> Stun -> ทำลายตัวเอง
    private void OnTriggerEnter2D(Collider2D collision)
    {
        HealthCh playerHealth = collision.GetComponent<HealthCh>();

        if (playerHealth != null)
        {
            // 1. ทำดาเมจ
            playerHealth.TakeDamage(Damage);

            // 2. สั่งให้ Player ติด Stun 1 วินาที
            // ** ข้อนี้คุณต้องนำฟังก์ชัน ApplyStun ไปเขียนเพิ่มในสคริปต์ Player นะครับ (ดูวิธีทำด้านล่าง) **
            collision.SendMessage("ApplyStun", StunDuration, SendMessageOptions.DontRequireReceiver);

            // 3. ทำลายกระสุน
            Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    // ✅ ฟังก์ชันสำหรับ Animation Event
    // ==========================================
    public void TriggerAtkIdle()
    {
        if (anim != null)
        {
            anim.SetBool("atk-idle", true);
        }
    }
}