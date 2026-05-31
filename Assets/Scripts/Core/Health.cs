using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int currentHP = 10;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    private void Awake()
    {
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP < 0)
            currentHP = 0;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHP += amount;
        if (currentHP > maxHP)
            currentHP = maxHP;
    }

    public void IncreaseMaxHP(int amount, bool fullHeal)
    {
        maxHP += amount;

        if (fullHeal)
            currentHP = maxHP;
        else
            currentHP = Mathf.Min(currentHP, maxHP);
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
