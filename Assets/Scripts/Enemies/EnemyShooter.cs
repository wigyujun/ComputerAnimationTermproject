using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform playerTarget;

    [Header("Attack Settings")]
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private bool aimAtPlayer = true;

    private float fireTimer = 0f;

    private void Awake()
    {
        if (firePoint == null)
        {
            Transform found = transform.Find("FirePoint");
            if (found != null)
            {
                firePoint = found;
            }
        }

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        fireTimer += Time.deltaTime;

        if (fireTimer >= fireInterval)
        {
            Fire();
            fireTimer = 0f;
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("EnemyShooter: projectilePrefab 비어 있음");
            return;
        }

        Transform shootPoint = firePoint != null ? firePoint : transform;

        Vector2 direction = Vector2.down;

        if (aimAtPlayer && playerTarget != null)
        {
            direction = (playerTarget.position - shootPoint.position).normalized;
        }

        GameObject projectileObj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();

        if (projectile != null)
        {
            projectile.Initialize(direction, projectileSpeed, projectileDamage);
        }
        else
        {
            Debug.LogWarning("EnemyShooter: EnemyProjectile 컴포넌트 없음");
        }
    }

    public void SetFireInterval(float newInterval)
    {
        fireInterval = Mathf.Max(0.2f, newInterval);
    }

    public void SetProjectileSpeed(float newSpeed)
    {
        projectileSpeed = Mathf.Max(0.5f, newSpeed);
    }

    public void SetProjectileDamage(int newDamage)
    {
        projectileDamage = Mathf.Max(1, newDamage);
    }
}
