using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// No Thai!!!
// iFeature ???
public class HealthCh : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float startingHealth;

    public HealthBar healthBar; // Reference to the HealthBar script
    
    private bool isDead;
    public float currentHealth { get; private set; }
    private Animator anim;

    [Header("iFrames")]
    [SerializeField] private float iFramesDuration;
    [SerializeField] private int numberOfFlashes;
    private SpriteRenderer spriteRend;
    private bool isInvulnerable = false;

    private void Start()
    {
        isDead = false;
        currentHealth = startingHealth;
        healthBar.setMaxHealth(startingHealth);
        healthBar.SetHealth(currentHealth);
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();    
    }

    private void Update()
    {
        // For test damage
        if (Input.GetKeyDown(KeyCode.E))
            TakeDamage(200);
    }

    public void SetHealth(float healthChange)
    {
        currentHealth += healthChange;
        currentHealth = Mathf.Clamp(currentHealth, 0, startingHealth);
        healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0 && !isDead)
        {
            anim.SetTrigger("die");
            isDead = true;
        }
    }

    public void TakeDamage(float _damage)
    {
        if (!isInvulnerable)
        {
            currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);
            healthBar.SetHealth(currentHealth);

            if (currentHealth <= 0 && !isDead)
            {
                anim.SetTrigger("die");
                isDead = true;
            }
            else
            {
                anim.SetTrigger("hurt");
                StartCoroutine(Invunerability());
            }
        }
    }

    private IEnumerator Invunerability()
    {
        isInvulnerable = true;
        Physics2D.IgnoreLayerCollision(1, 0, true); // Layer number Player and Enemy
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRend.color = new Color(1, 0, 0, 0.5f);
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
            spriteRend.color = Color.white;
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
        }
        Physics2D.IgnoreLayerCollision(1, 0, false); // Layer number Player and Enemy
        isInvulnerable = false;
    }
}
