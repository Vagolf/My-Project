using System.Collections;
using UnityEngine;

// Attach this to a child GameObject with a trigger collider representing the Ultimate damage volume.
// Enable this object only during the ultimate phase you want to capture enemies.
public class UltimateDamagePoint : MonoBehaviour
{
    [Tooltip("Tag ?????????????????????")] public string enemyTag = "Enemy";
    [Tooltip("??????????????????")] public int hitCount = 6;
    [Tooltip("???????????")] public float damagePerHit = 90f;
    [Tooltip("?????????????????? (??????) ??? unscaledTime")] public float interval = 0.15f;
    [Tooltip("?????????????? hurt ????")] public bool forceHurtLoop = true;

    private bool running;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (running) return;
        if (other.CompareTag(enemyTag))
        {
            var hp = other.GetComponent<HealthEnemy>();
            if (hp != null)
            {
                StartCoroutine(DoMultiHit(hp));
            }
        }
    }

    private IEnumerator DoMultiHit(HealthEnemy hp)
    {
        running = true;
        int applied = 0;
        while (applied < hitCount && hp != null)
        {
            hp.TakeDamageUltimate(damagePerHit);
            applied++;
            if (hp.currentHealth <= 0) break;
            float t = 0f;
            while (t < interval)
            {
                t += Time.unscaledDeltaTime; // unaffected by timeScale
                yield return null;
            }
        }
        running = false;
    }
}
