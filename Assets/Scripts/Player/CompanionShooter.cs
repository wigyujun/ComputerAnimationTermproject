using UnityEngine;

public class CompanionShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private PlayerCombatStats playerStats;

    [Header("Attack Settings")]
    [SerializeField] private float fireInterval = 0.55f;
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private float projectileSpeed = 10f;

    private float fireTimer = 0f;

    private void Awake()
    {
        if (firePoint == null)
        {
            Transform found = transform.Find("FirePoint");
            if (found != null)
                firePoint = found;
        }

        if (playerStats == null)
        {
            playerStats = GetComponentInParent<PlayerCombatStats>();
        }
    }

    private void Update()
    {
        fireTimer += Time.deltaTime;

        float finalInterval = fireInterval;

        if (playerStats != null)
            finalInterval /= playerStats.AttackSpeedMultiplier;

        if (fireTimer >= finalInterval)
        {
            Fire();
            fireTimer = 0f;
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null)
            return;

        Transform shootPoint = firePoint != null ? firePoint : transform;

        GameObject projectileObj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetDirection(Vector2.up);
            projectile.SetSpeed(projectileSpeed);

            int finalDamage = baseDamage;
            if (playerStats != null)
                finalDamage = Mathf.RoundToInt(baseDamage * playerStats.AttackPowerMultiplier);

            projectile.SetDamage(finalDamage);
        }
    }
}
