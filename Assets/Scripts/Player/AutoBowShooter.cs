using UnityEngine;

public class AutoBowShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerCombatStats playerCombatStats;

    [Header("Projectile By Weapon Level")]
    [SerializeField] private GameObject level0ProjectilePrefab; // 0 = 목재 활
    [SerializeField] private GameObject level1ProjectilePrefab; // 1 = 권총
    [SerializeField] private GameObject level2ProjectilePrefab; // 2 = 라이플
    [SerializeField] private GameObject level3ProjectilePrefab; // 3 = 산탄총
    [SerializeField] private GameObject level4ProjectilePrefab; // 4 = 레이저

    [Header("Shot Settings")]
    [SerializeField] private float baseShotInterval = 0.3f;
    [SerializeField] private Vector2 shootDirection = Vector2.up;

    [Header("Shotgun Settings")]
    [SerializeField] private int shotgunPelletCount = 3;
    [SerializeField] private float shotgunSpreadAngle = 15f;

    private GameObject currentProjectilePrefab;
    private float timer = 0f;
    private int currentWeaponLevel = 0;
    private float currentShotInterval = 0.3f;

    private void Awake()
    {
        if (playerCombatStats == null)
            playerCombatStats = GetComponentInParent<PlayerCombatStats>();
    }

    private void Start()
    {
        SetWeaponLevel(RunContext.WeaponUpgradeLevel);
        RefreshCombatStats();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= currentShotInterval)
        {
            timer = 0f;
            Shoot();
        }
    }

    // 영구 강화치/전투 스탯을 반영해 실제 발사 간격을 다시 계산한다.
    public void RefreshCombatStats()
    {
        float attackSpeedMultiplier;

        if (playerCombatStats != null)
            attackSpeedMultiplier = Mathf.Max(0.01f, playerCombatStats.AttackSpeedMultiplier);
        else
            attackSpeedMultiplier = Mathf.Max(0.01f, RunContext.GetAttackSpeedMultiplier());

        currentShotInterval = baseShotInterval / attackSpeedMultiplier;
    }

    // 현재 무기 단계에 맞는 투사체 프리팹과 기본 공격 세팅을 갱신한다.
    public void SetWeaponLevel(int level)
    {
        currentWeaponLevel = Mathf.Clamp(level, 0, 4);
        currentProjectilePrefab = GetProjectilePrefabByLevel(currentWeaponLevel);

        Debug.Log(
            $"[AutoBowShooter] Weapon level = {currentWeaponLevel} ({GetWeaponLabel(currentWeaponLevel)}), " +
            $"prefab = {(currentProjectilePrefab != null ? currentProjectilePrefab.name : "NULL")}, " +
            $"baseDamage = {GetWeaponBaseDamage(currentWeaponLevel)}"
        );
    }

    private GameObject GetProjectilePrefabByLevel(int level)
    {
        switch (level)
        {
            case 0: return level0ProjectilePrefab;
            case 1: return level1ProjectilePrefab;
            case 2: return level2ProjectilePrefab; // 라이플
            case 3: return level3ProjectilePrefab; // 산탄총
            case 4: return level4ProjectilePrefab;
            default: return level0ProjectilePrefab;
        }
    }

    // 무기 종류에 따라 단발/산탄 발사 방식을 분기하는 실제 공격 진입점이다.
    private void Shoot()
    {
        if (currentProjectilePrefab == null || firePoint == null)
            return;

        Vector2 baseDirection = shootDirection.normalized;

        // 3 = 산탄총
        if (currentWeaponLevel == 3)
            FireShotgun(baseDirection);
        else
            FireSingle(baseDirection);
    }

    private void FireSingle(Vector2 direction)
    {
        GameObject proj = Instantiate(currentProjectilePrefab, firePoint.position, Quaternion.identity);

        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.SetDirection(direction.normalized);
            projectile.SetDamage(GetFinalDamage());
        }
    }

    private void FireShotgun(Vector2 baseDirection)
    {
        if (shotgunPelletCount <= 1)
        {
            FireSingle(baseDirection);
            return;
        }

        float startAngle = -shotgunSpreadAngle;
        float step = (shotgunSpreadAngle * 2f) / (shotgunPelletCount - 1);

        for (int i = 0; i < shotgunPelletCount; i++)
        {
            float angle = startAngle + (step * i);
            Vector2 spreadDirection = RotateVector(baseDirection, angle);
            FireSingle(spreadDirection);
        }
    }

    // 무기 기본 공격력과 공격력 배수를 합쳐 최종 데미지를 계산한다.
    private int GetFinalDamage()
    {
        float attackPowerMultiplier;

        if (playerCombatStats != null)
            attackPowerMultiplier = playerCombatStats.AttackPowerMultiplier;
        else
            attackPowerMultiplier = RunContext.GetAttackPowerMultiplier();

        int baseWeaponDamage = GetWeaponBaseDamage(currentWeaponLevel);
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseWeaponDamage * attackPowerMultiplier));
        return finalDamage;
    }

    private int GetWeaponBaseDamage(int weaponLevel)
    {
        switch (weaponLevel)
        {
            case 0: return 1; // 활
            case 1: return 2; // 권총
            case 2: return 3; // 라이플
            case 3: return 1; // 산탄총 (펠릿당 데미지)
            case 4: return 4; // 레이저
            default: return 1;
        }
    }

    private Vector2 RotateVector(Vector2 dir, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        return new Vector2(
            dir.x * cos - dir.y * sin,
            dir.x * sin + dir.y * cos
        ).normalized;
    }

    private string GetWeaponLabel(int level)
    {
        switch (level)
        {
            case 0: return "목재 활";
            case 1: return "권총";
            case 2: return "라이플";
            case 3: return "산탄총";
            case 4: return "레이저";
            default: return "알 수 없음";
        }
    }
}
