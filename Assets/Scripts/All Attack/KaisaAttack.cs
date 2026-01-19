using UnityEngine;

public class KaisaAttack : AttackBase
{
    public Transform attackPoint;
    public LayerMask enemyLayers;

    private Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()       
    {
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.J)) // ?????????????????
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    public override void Attack()
    {
        // ??????????????????
        PlayAttackAnimation();

        // ???????????????????????
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            // ???????? TakeDamage ????????
            var health = enemy.GetComponent<HealthEnemy>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
            }
        }
    }

    public override void PlayAttackAnimation()
    {
        if (anim != null)
            anim.SetTrigger("attack");
    }

    // Gizmo drawing removed per request
}
