using UnityEngine;

public class PlayerCombatStats : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private int weaponLevel = 0;
    private const int maxWeaponLevel = 4;

    [Header("Combat")]
    [SerializeField] private float attackSpeedPercent = 0f;
    [SerializeField] private float attackPowerPercent = 0f;

    public int WeaponLevel => weaponLevel;
    public int MaxWeaponLevel => maxWeaponLevel;

    public float AttackSpeedPercent => attackSpeedPercent;
    public float AttackPowerPercent => attackPowerPercent;

    public float AttackSpeedMultiplier => 1f + attackSpeedPercent;
    public float AttackPowerMultiplier => 1f + attackPowerPercent;

    public WeaponType CurrentWeaponType
    {
        get
        {
            switch (weaponLevel)
            {
                case 0: return WeaponType.Bow;
                case 1: return WeaponType.Pistol;
                case 2: return WeaponType.Rifle;
                case 3: return WeaponType.Shotgun;
                default: return WeaponType.Laser;
            }
        }
    }

    public string WeaponName
    {
        get
        {
            switch (CurrentWeaponType)
            {
                case WeaponType.Bow: return "활";
                case WeaponType.Pistol: return "권총";
                case WeaponType.Rifle: return "라이플";
                case WeaponType.Shotgun: return "샷건";
                case WeaponType.Laser: return "레이저";
                default: return "활";
            }
        }
    }

    public void ApplyRunContextStats()
    {
        weaponLevel = Mathf.Clamp(RunContext.WeaponUpgradeLevel, 0, maxWeaponLevel);
        attackSpeedPercent = Mathf.Max(0f, RunContext.AttackSpeedBonusPercent);
        attackPowerPercent = Mathf.Max(0f, RunContext.AttackPowerBonusPercent);
    }

    public bool CanUpgradeWeapon()
    {
        return weaponLevel < maxWeaponLevel;
    }

    public void UpgradeWeapon()
    {
        if (!CanUpgradeWeapon())
            return;

        weaponLevel++;
    }

    public void SetWeaponLevel(int newLevel)
    {
        weaponLevel = Mathf.Clamp(newLevel, 0, maxWeaponLevel);
    }

    public void AddAttackSpeedPercent(float amount)
    {
        attackSpeedPercent += amount;
    }

    public void AddAttackPowerPercent(float amount)
    {
        attackPowerPercent += amount;
    }
}
