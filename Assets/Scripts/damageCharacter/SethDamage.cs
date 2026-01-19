using UnityEngine;

public class SethDamage : MonoBehaviour
{
    public float HP = 1000f;
    private float comboCooldown = 3f;
    private float ultimateCooldown = 10f;
    private float comboTimer = 0f;
    private float ultimateTimer = 0f;

    public void NormalAttack(GameObject enemy)
    {
        enemy.GetComponent<HealthEnemy>()?.TakeDamage(80f);
    }

    public void ComboAttack(GameObject enemy)
    {
        if (comboTimer <= 0f)
        {
            enemy.GetComponent<HealthEnemy>()?.TakeDamage(60f);
            enemy.GetComponent<HealthEnemy>()?.TakeDamage(60f);
            comboTimer = comboCooldown;
        }
    }

    public void UltimateAttack(GameObject enemy)
    {
        if (ultimateTimer <= 0f)
        {
            for (int i = 0; i < 5; i++)
                enemy.GetComponent<HealthEnemy>()?.TakeDamage(100f);
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