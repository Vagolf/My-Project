using UnityEngine;

public class EvaProjectile : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float lifeTime = 3f;

    private Vector2 moveDir;
    private float speed;
    private float damage;
    private LayerMask targetMask;

    private float knockbackForce;
    private float stunDuration;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 dir, float projSpeed, float projDamage, LayerMask mask, float knockback, float stunTime)
    {
        moveDir = dir.normalized;
        speed = projSpeed;
        damage = projDamage;
        targetMask = mask;
        knockbackForce = knockback;
        stunDuration = stunTime;

        // ยิงไปตามทิศ
        if (rb != null)
        {
            rb.velocity = moveDir * speed;
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // check layer
        if (((1 << other.gameObject.layer) & targetMask) == 0) return;

        // damage to Player
        var hp = other.GetComponent<HealthCh>() ?? other.GetComponentInParent<HealthCh>();
        if (hp != null)
        {
            hp.TakeDamage(damage);

            // knockback
            if (knockbackForce > 0f)
            {
                var targetRb = hp.GetComponent<Rigidbody2D>();
                if (targetRb != null)
                {
                    targetRb.velocity = new Vector2(0f, targetRb.velocity.y);
                    targetRb.AddForce(new Vector2(moveDir.x * knockbackForce, 2f), ForceMode2D.Impulse);
                }
            }

            // stun
            if (stunDuration > 0f)
            {
                var stun = hp.GetComponent<StunReceiver2D>() ?? hp.GetComponentInParent<StunReceiver2D>();
                if (stun != null)
                    stun.Stun(stunDuration);
            }
        }

        Destroy(gameObject);
    }
}
