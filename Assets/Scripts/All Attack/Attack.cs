using System.Collections;
using UnityEngine;

public class Attack : MonoBehaviour
{
    public enum AttackType { Normal, Special, Ultimate }

    [Header("Character Settings")]
    [Tooltip("เลือกตัวละคร 0-5")]
    [SerializeField] private int characterIndex = 0;

    [Header("Attack Points")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Cooldowns")]
    [SerializeField] private float[] normalCooldowns = new float[6] { 0.2f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f };
    [SerializeField] private float[] specialCooldowns = new float[6] { 2f, 2f, 2f, 2f, 2f, 2f };
    [SerializeField] private float[] ultimateCooldowns = new float[6] { 8f, 8f, 8f, 8f, 8f, 8f };

    [Header("Damage Table")]
    [SerializeField] private int[] normalDamages = new int[6] { 100, 2, 1, 2, 1, 2 };
    [SerializeField] private int[] specialDamages = new int[6] { 2, 4, 3, 4, 3, 4 };
    [SerializeField] private int[] ultimateDamages = new int[6] { 7, 8, 7, 8, 7, 8 };

    [Header("Sound")]
    [SerializeField] private AudioSource normalAttackSound;
    [SerializeField] private AudioSource specialAttackSound;
    [SerializeField] private AudioSource ultimateAttackSound;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {

        // ลบการเรียกใช้ปุ่ม J, K, L ออกแล้ว
        // ให้ใช้การเรียก DoAttack จากสคริปต์อื่นแทน
    }

    private void DoAttack(AttackType type)
    {
        // Trigger animation
        anim.SetTrigger(type.ToString().ToLower());

        // Play sound
        switch (type)
        {
            case AttackType.Normal:
                normalAttackSound?.Play();
                break;
            case AttackType.Special:
                specialAttackSound?.Play();
                break;
            case AttackType.Ultimate:
                ultimateAttackSound?.Play();
                break;
        }

        // Damage value
        int damage = 1;
        switch (type)
        {
            case AttackType.Normal:
                damage = normalDamages[characterIndex];
                break;
            case AttackType.Special:
                damage = specialDamages[characterIndex];
                break;
            case AttackType.Ultimate:
                damage = ultimateDamages[characterIndex];
                break;
        }

        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    // Gizmo drawing removed per request
    private void Deactivte()
    {
        gameObject.SetActive(false);
    }
}

