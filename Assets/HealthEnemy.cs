using System.Collections;
using UnityEngine;

public class HealthEnemy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] public float startingHealth;

    public HealthBar healthBar; // Reference to the HealthBar script

    private bool isDead;
    public float currentHealth { get; private set; }
    private Animator anim;

    [Header("iFrames")]
    [SerializeField] private float iFramesDuration = 0.5f;
    [SerializeField] private int numberOfFlashes = 3;
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
        if (startingHealth < 0) // For test damage
        {
            
            Debug.Log("Enemy health is below zero, taking damage.");
        }
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

    public void TakeDamage(float damage)
    {
        if (!isInvulnerable && !isDead)
        {
            currentHealth = Mathf.Clamp(currentHealth - damage, 0, startingHealth);
            healthBar.SetHealth(currentHealth);

            if (currentHealth <= 0 && !isDead)
            {
                anim.SetTrigger("die");
                isDead = true;
            }
            else
            {
                anim.SetTrigger("hurt");
                StartCoroutine(Invulnerability());
            }
        }
    }

    private IEnumerator Invulnerability()
    {
        isInvulnerable = true;
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRend.color = new Color(1, 0, 0, 0.5f);
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
            spriteRend.color = Color.white;
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
        }
        isInvulnerable = false;
    }
}
