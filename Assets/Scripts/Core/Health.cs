using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int currentHP = 10;

    [Header("State")]
    [SerializeField] private bool invincible = false;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsInvincible => invincible;

    private void Awake()
    {
        maxHP = Mathf.Max(1, maxHP);
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    public void SetHP(int newCurrentHP, int newMaxHP)
    {
        maxHP = Mathf.Max(1, newMaxHP);
        currentHP = Mathf.Clamp(newCurrentHP, 0, maxHP);
    }

    public void SetCurrentHP(int value)
    {
        currentHP = Mathf.Clamp(value, 0, maxHP);
    }

    public void SetMaxHP(int value, bool fullHeal = false)
    {
        maxHP = Mathf.Max(1, value);

        if (fullHeal)
            currentHP = maxHP;
        else
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    public void SetInvincible(bool value)
    {
        invincible = value;
    }

    public void TakeDamage(int damage)
    {
        if (invincible)
            return;

        if (damage <= 0)
            return;

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
