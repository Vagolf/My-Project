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
        if (!isDead)
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
            }
        }
    }
}
