using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private int damage = 1;

    [Header("Despawn")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float despawnMargin = 0.5f;

    private Vector2 moveDirection = Vector2.down;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
        CheckOutsideScreenAndDestroy();
    }

    public void Initialize(Vector2 direction, float projectileSpeed, int projectileDamage)
    {
        moveDirection = direction.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
        if (!other.CompareTag("Player"))
            return;

        Health playerHealth = other.GetComponentInParent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
