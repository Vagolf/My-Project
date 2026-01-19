using UnityEngine;

public class RomanDamage : MonoBehaviour
{
    public float HP = 1200f;
    private float comboCooldown = 6f;
    private float ultimateCooldown = 14f;
    private float comboTimer = 0f;
    private float ultimateTimer = 0f;

    public void NormalAttack(GameObject enemy)
    {
        enemy.GetComponent<HealthEnemy>()?.TakeDamage(120f);
    }

    public void ComboAttack(GameObject enemy)
    {
        if (comboTimer <= 0f)
        {
            enemy.GetComponent<HealthEnemy>()?.TakeDamage(250f);
            comboTimer = comboCooldown;
        }
    }

    public void UltimateAttack(GameObject enemy)
    {
        if (ultimateTimer <= 0f)
        {
            enemy.GetComponent<HealthEnemy>()?.TakeDamage(600f);
            ultimateTimer = ultimateCooldown;
        }
    }

    private void Update()
    {
        if (comboTimer > 0f) comboTimer -= Time.deltaTime;
        if (ultimateTimer > 0f) ultimateTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.J))
            NormalAttack(FindEnemy());
        if (Input.GetKeyDown(KeyCode.K))
            ComboAttack(FindEnemy());
        if (Input.GetKeyDown(KeyCode.L))
            UltimateAttack(FindEnemy());
    }

    private GameObject FindEnemy()
    {
        return GameObject.FindWithTag("Enemy");
    }
}