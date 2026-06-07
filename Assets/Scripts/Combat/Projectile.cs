using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private int damage = 1;

    [Header("Pierce")]
    [SerializeField] private int pierceCount = 0;
    private int hitCount = 0;

    [Header("Despawn")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float despawnMargin = 0.5f;

    private Vector2 moveDirection = Vector2.up;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }
        else
        {
            transform.position += (Vector3)(moveDirection * speed * Time.fixedDeltaTime);
        }
    }

    private void Update()
    {
        CheckOutsideScreenAndDestroy();
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetPierceCount(int newPierceCount)
    {
        pierceCount = Mathf.Max(0, newPierceCount);
    }

    public void SetCamera(Camera cam)
    {
        targetCamera = cam;
    }

    private void CheckOutsideScreenAndDestroy()
    {
        if (targetCamera == null)
            return;

        float objectZ = transform.position.z;
        float cameraZ = targetCamera.transform.position.z;
        float distanceFromCamera = Mathf.Abs(objectZ - cameraZ);

        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distanceFromCamera)
        );

        Vector3 topRight = targetCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, distanceFromCamera)
        );

        float minX = bottomLeft.x - despawnMargin;
        float maxX = topRight.x + despawnMargin;
        float minY = bottomLeft.y - despawnMargin;
        float maxY = topRight.y + despawnMargin;

        Vector3 pos = transform.position;

        if (pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Projectile hit: {other.name}, tag: {other.tag}, layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        if (!other.CompareTag("Enemy"))
            return;

        Health enemyHealth = other.GetComponentInParent<Health>();
        Debug.Log("enemyHealth found? " + (enemyHealth != null));

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }

        if (pierceCount <= 0)
        {
            Destroy(gameObject);
            return;
        }

        hitCount++;

        if (hitCount > pierceCount)
        {
            Destroy(gameObject);
        }
    }
}
