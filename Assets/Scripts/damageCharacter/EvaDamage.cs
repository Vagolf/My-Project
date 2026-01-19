using UnityEngine;

public class EvaDamage : MonoBehaviour
{
    public float HP = 1100f;
    private float comboCooldown = 4f;
    private float ultimateCooldown = 10f;
    private float comboTimer = 0f;
    private float ultimateTimer = 0f;

    public void NormalAttack(GameObject enemy)
    {
        enemy.GetComponent<HealthEnemy>()?.TakeDamage(75f);
    }

    public void ComboAttack(GameObject enemy)
    {
        if (comboTimer <= 0f)
        {
            enemy.GetComponent<HealthEnemy>()?.TakeDamage(150f);
            comboTimer = comboCooldown;
        }
    }

    public void UltimateAttack(GameObject enemy)
    {
        if (ultimateTimer <= 0f)
        {
            for (int i = 0; i < 6; i++)
                enemy.GetComponent<HealthEnemy>()?.TakeDamage(80f);
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