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

    [Header("Crouch Guard")]
    [Tooltip("Block damage while crouching up to N times, with recharge cooldown per charge")]
    [SerializeField] private bool enableCrouchGuard = true;
    [SerializeField] private int maxCrouchBlocks = 3;
    [SerializeField] private float crouchGuardCooldown = 5f;
    private int availableCrouchBlocks;
    private float crouchRechargeTimer;

    // Events to notify listeners about crouch guard changes
    public event System.Action<int, int> OnCrouchBlockUsed;      // (available, max)
    public event System.Action<int, int> OnCrouchBlockRestored; // (available, max)

    private void Start()
    {
        isDead = false;
        currentHealth = startingHealth;
        healthBar.setMaxHealth(startingHealth);
        healthBar.SetHealth(currentHealth);
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();

        // init crouch guard
        availableCrouchBlocks = maxCrouchBlocks;
        crouchRechargeTimer = 0f;
    }

    private void Update()
    {
        // For test damage
        if (Input.GetKeyDown(KeyCode.E))
            TakeDamage(200);

        // Recharge crouch guard one charge every cooldown interval when not full
        if (enableCrouchGuard && availableCrouchBlocks < maxCrouchBlocks)
        {
            crouchRechargeTimer += Time.deltaTime;
            if (crouchRechargeTimer >= crouchGuardCooldown)
            {
                crouchRechargeTimer -= crouchGuardCooldown;
                availableCrouchBlocks = Mathf.Min(maxCrouchBlocks, availableCrouchBlocks + 1);
                OnCrouchBlockRestored?.Invoke(availableCrouchBlocks, maxCrouchBlocks);
            }
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
            var p = GetComponent<Player>();
            if (p != null) p.enabled = false;
            var r = GetComponent<Roman>();
            if (r != null) r.enabled = false;
            isDead = true;
        }
    }

    public void TakeDamage(float _damage)
    {
        if (isDead) return;

        // crouch guard: block damage when crouching or on crouch input
        if (enableCrouchGuard && CanBlockViaCrouch())
        {
            if (availableCrouchBlocks > 0)
            {
                availableCrouchBlocks--;
                // reset recharge timer to start recharging this spent charge
                if (availableCrouchBlocks < maxCrouchBlocks)
                    crouchRechargeTimer = 0f;
                // optionally flash/block feedback here
                OnCrouchBlockUsed?.Invoke(availableCrouchBlocks, maxCrouchBlocks);
                return; // ignore this damage entirely
            }
        }

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

    private bool CanBlockViaCrouch()
    {
        // check crouch state either from Animator or character scripts
        bool crouchState = anim != null && anim.GetBool("crouch");
        var roman = GetComponent<Roman>();
        if (roman != null) crouchState = crouchState || roman.isCrouching;
        var player = GetComponent<Player>();
        if (player != null) crouchState = crouchState || player.isCrouching;
        // also allow immediate press S to block
        bool crouchKey = Input.GetKey(KeyCode.S);
        return (crouchState || crouchKey);
    }

    private IEnumerator HurtRoutine()
    {
        isHurting = true;
        anim.SetTrigger("hurt");
        yield return new WaitForSeconds(hurtAnimDuration);
        isHurting = false;
    }

    // Revive and restore to full for a new round
    public void ResetForNewRound()
    {
        // stop any running routines
        StopAllCoroutines();
        isHurting = false;
        isDead = false;
        gameObject.SetActive(true);
        currentHealth = startingHealth;
        if (healthBar != null)
        {
            healthBar.setMaxHealth(startingHealth);
            healthBar.SetHealth(currentHealth);
        }
        if (spriteRend != null)
        {
            spriteRend.enabled = true;
            spriteRend.color = Color.white;
        }
        if (anim != null)
        {
            // clear common states
            anim.ResetTrigger("die");
            anim.ResetTrigger("hurt");
            anim.Update(0f);
        }
        var player = GetComponent<Player>();
        if (player != null) player.enabled = true;
        var roman = GetComponent<Roman>();
        if (roman != null) roman.enabled = true;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        // reset crouch guard charges
        availableCrouchBlocks = maxCrouchBlocks;
        crouchRechargeTimer = 0f;
    }
}

