using UnityEngine;

public class AutoBowShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCombatStats playerStats;
    [SerializeField] private WeaponVisualController weaponVisual;
    [SerializeField] private Transform defaultFirePoint;

    [Header("Projectile Prefabs")]
    [SerializeField] private GameObject bowProjectilePrefab;
    [SerializeField] private GameObject pistolProjectilePrefab;
    [SerializeField] private GameObject rifleProjectilePrefab;
    [SerializeField] private GameObject shotgunProjectilePrefab;
    [SerializeField] private GameObject laserProjectilePrefab;

    private float fireTimer = 0f;

    private void Update()
    {
        fireTimer += Time.deltaTime;

        float finalFireInterval = GetCurrentFireInterval();

        if (Input.GetKey(KeyCode.Space) && fireTimer >= finalFireInterval)
        {
            Fire();
            fireTimer = 0f;
        }
    }

    private void Fire()
    {
        if (playerStats == null)
            return;

        Transform firePoint = defaultFirePoint;

        if (weaponVisual != null && weaponVisual.GetFirePoint() != null)
            firePoint = weaponVisual.GetFirePoint();

        if (firePoint == null)
            return;

        WeaponType currentWeapon = playerStats.CurrentWeaponType;

        switch (currentWeapon)
        {
            case WeaponType.Bow:
                SpawnProjectile(bowProjectilePrefab, firePoint.position, Vector2.up, 3, 10f, 0);
                break;

            case WeaponType.Pistol:
                SpawnProjectile(pistolProjectilePrefab, firePoint.position, Vector2.up, 2, 12f, 0);
                break;

            case WeaponType.Rifle:
                SpawnProjectile(rifleProjectilePrefab, firePoint.position, Vector2.up, 2, 15f, 0);
                break;

            case WeaponType.Shotgun:
                FireShotgun(firePoint.position, 2, 11f, 0);
                break;

            case WeaponType.Laser:
                SpawnProjectile(laserProjectilePrefab, firePoint.position, Vector2.up, 4, 18f, 3);
                break;
        }

        if (weaponVisual != null)
            weaponVisual.PlayShootMotion();
    }

    private float GetCurrentFireInterval()
    {
        if (playerStats == null)
            return 0.25f;

        float baseInterval = 0.25f;

        switch (playerStats.CurrentWeaponType)
        {
            case WeaponType.Bow:
                baseInterval = 0.45f;
                break;
            case WeaponType.Pistol:
                baseInterval = 0.25f;
                break;
            case WeaponType.Rifle:
                baseInterval = 0.14f;
                break;
            case WeaponType.Shotgun:
                baseInterval = 0.55f;
                break;
            case WeaponType.Laser:
                baseInterval = 0.10f;
                break;
        }

        return baseInterval / playerStats.AttackSpeedMultiplier;
    }

    private void SpawnProjectile(GameObject prefab, Vector3 spawnPosition, Vector2 direction, int damage, float speed, int pierceCount)
    {
        if (prefab == null)
            return;

        GameObject projectileObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetDirection(direction);
            projectile.SetDamage(GetFinalDamage(damage));
            projectile.SetSpeed(speed);
            projectile.SetPierceCount(pierceCount);
        }
    }

    private void FireShotgun(Vector3 spawnPosition, int damagePerPellet, float speed, int pierceCount)
    {
        if (shotgunProjectilePrefab == null)
            return;

        float[] angles = { -15f, 0f, 15f };

        foreach (float angle in angles)
        {
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;

            GameObject projectileObj = Instantiate(shotgunProjectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();

            if (projectile != null)
            {
                projectile.SetDirection(direction);
                projectile.SetDamage(GetFinalDamage(damagePerPellet));
                projectile.SetSpeed(speed);
                projectile.SetPierceCount(pierceCount);
            }
        }
    }

    private int GetFinalDamage(int baseDamage)
    {
        if (playerStats == null)
            return baseDamage;

        return Mathf.RoundToInt(baseDamage * playerStats.AttackPowerMultiplier);
    }
}
