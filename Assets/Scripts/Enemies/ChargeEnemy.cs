using UnityEngine;

public class ChargeEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTarget;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private int contactDamage = 1;

    [Header("Optional Lifetime Despawn")]
    [SerializeField] private bool useDashLifeTime = false;
    [SerializeField] private float dashLifeTime = 3f;

    [Header("Despawn")]
    [SerializeField] private Camera battleCamera;
    [SerializeField] private float bottomDespawnOffset = 1.5f;

    [Header("Rotation")]
    [SerializeField] private float spriteAngleOffset = 160f;

    private Rigidbody2D rb;
    private EnemySpawner spawner;
    private Renderer cachedRenderer;
    private Collider2D cachedCollider;

    private Vector2 dashDirection = Vector2.down;
    private float dashTimer = 0f;
    private bool removeHandled = false;
    private bool dashStarted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInChildren<Rigidbody2D>();

        cachedRenderer = GetComponentInChildren<Renderer>();
        cachedCollider = GetComponentInChildren<Collider2D>();

        if (battleCamera == null)
            battleCamera = Camera.main;
    }

    private void OnEnable()
    {
        removeHandled = false;
        dashStarted = false;
        dashTimer = 0f;
        dashDirection = Vector2.down;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (battleCamera == null)
            battleCamera = Camera.main;

        ResolvePlayerTarget();
        TryStartDash();
    }

    private void Update()
    {
        if (removeHandled)
            return;

        if (!dashStarted)
        {
            ResolvePlayerTarget();
            TryStartDash();
            return;
        }

        if (useDashLifeTime)
        {
            dashTimer += Time.deltaTime;

            if (dashTimer >= dashLifeTime)
            {
                RemoveSelf();
                return;
            }
        }

        ApplyVelocity(dashDirection * dashSpeed);
        RotateToDashDirection();
        CheckBelowScreenAndRemove();
    }

    private void TryStartDash()
    {
        if (dashStarted)
            return;

        if (playerTarget == null)
            return;

        Vector2 toPlayer = (Vector2)playerTarget.position - (Vector2)transform.position;

        if (toPlayer.sqrMagnitude <= 0.0001f)
            dashDirection = Vector2.down;
        else
            dashDirection = toPlayer.normalized;

        dashStarted = true;
        dashTimer = 0f;

        ApplyVelocity(dashDirection * dashSpeed);
        RotateToDashDirection();
    }

    private void ApplyVelocity(Vector2 velocity)
    {
        if (rb != null)
            rb.linearVelocity = velocity;
        else
            transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void RotateToDashDirection()
    {
        if (dashDirection.sqrMagnitude <= 0.0001f)
            return;

        float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
    }

    private void CheckBelowScreenAndRemove()
    {
        if (battleCamera == null)
            return;

        float objectZ = transform.position.z;
        float cameraZ = battleCamera.transform.position.z;
        float distanceFromCamera = Mathf.Abs(objectZ - cameraZ);

        Vector3 bottomCenter = battleCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0f, distanceFromCamera)
        );

        float despawnY = bottomCenter.y - bottomDespawnOffset;
        float objectBottomY = GetObjectBottomY();

        if (objectBottomY < despawnY)
        {
            RemoveSelf();
        }
    }

    private float GetObjectBottomY()
    {
        if (cachedRenderer != null)
            return cachedRenderer.bounds.min.y;

        if (cachedCollider != null)
            return cachedCollider.bounds.min.y;

        return transform.position.y;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!dashStarted || removeHandled)
            return;

        if (!other.CompareTag("Player"))
            return;

        Health playerHealth = other.GetComponentInParent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(contactDamage);
        }

        RemoveSelf();
    }

    private void RemoveSelf()
    {
        if (removeHandled)
            return;

        removeHandled = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        EnemyController enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
            enemyController = GetComponentInParent<EnemyController>();

        if (enemyController != null)
        {
            enemyController.HandleDespawn();
        }
        else
        {
            if (spawner != null)
                spawner.NotifyEnemyRemoved();

            Destroy(gameObject);
        }
    }

    private void ResolvePlayerTarget()
    {
        if (playerTarget != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTarget = player.transform;
    }

    public void SetSpawner(EnemySpawner newSpawner)
    {
        spawner = newSpawner;
    }

    public void SetBattleCamera(Camera newCamera)
    {
        battleCamera = newCamera;
    }

    public void SetDashSpeed(float newSpeed)
    {
        dashSpeed = Mathf.Max(0.5f, newSpeed);
    }

    public void SetPlayerTarget(Transform newTarget)
    {
        playerTarget = newTarget;

        if (!dashStarted && playerTarget != null)
        {
            TryStartDash();
        }
    }
}
