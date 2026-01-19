using System.Collections;
using UnityEngine;

public class EvaTornadoUltimate : MonoBehaviour
{
    private Transform followTarget;
    private LayerMask targetMask;

    private float damage;
    private int hits;
    private float interval;
    private float radius;

    public void Init(Transform target, LayerMask mask, float dmg, int hitCount, float tickInterval, float hitRadius)
    {
        followTarget = target;
        targetMask = mask;
        damage = dmg;
        hits = Mathf.Max(1, hitCount);
        interval = Mathf.Max(0.05f, tickInterval);
        radius = Mathf.Max(0.1f, hitRadius);

        StartCoroutine(DamageRoutine());
    }

    private IEnumerator DamageRoutine()
    {
        for (int i = 0; i < hits; i++)
        {
            if (followTarget != null)
            {
                // ให้พายุอยู่ใกล้เป้าหมาย (เหมือนล็อคเป้าตี)
                transform.position = followTarget.position;
            }

            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, radius, targetMask);
            foreach (var c in cols)
            {
                var hp = c.GetComponent<HealthCh>() ?? c.GetComponentInParent<HealthCh>();
                if (hp != null) hp.TakeDamage(damage);
            }

            yield return new WaitForSecondsRealtime(interval);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
