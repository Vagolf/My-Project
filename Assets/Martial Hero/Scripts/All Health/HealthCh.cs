using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthCh : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float startingHealth;

    public HealthBar healthBar; // Reference to the HealthBar script
    
    private bool isDead;
    public float currentHealth { get; private set; }
    private Animator anim;
    private SpriteRenderer spriteRend;

    // Prevent hurt trigger spamming
    private bool isHurting = false;
    [SerializeField] private float hurtAnimDuration = 0.3f; // Adjust to match your hurt animation length

    [Header("GameManager")]
    [SerializeField] private GameManagerScript gameManager; // Reference to the GameManager script

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
            GetComponent<Player>().enabled = false;
            isDead = true;
        }
    }

    public void TakeDamage(float _damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);
        healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0 && !isDead)
        {
            anim.SetTrigger("die");
            isDead = true;
        }
        else
        {
            if (!isHurting)
                StartCoroutine(HurtRoutine());
        }
    }

    private IEnumerator HurtRoutine()
    {
        isHurting = true;
        anim.SetTrigger("hurt");
        yield return new WaitForSeconds(hurtAnimDuration);
        isHurting = false;
    }
}
