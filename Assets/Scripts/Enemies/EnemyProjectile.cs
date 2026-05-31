using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private int damage = 1;

    private Vector2 moveDirection = Vector2.down;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    public void Initialize(Vector2 direction, float projectileSpeed, int projectileDamage)
    {
        moveDirection = direction.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

        // 적 탄환 원본 스프라이트가 오른쪽 기준일 때
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Health playerHealth = other.GetComponentInParent<Health>();

        if (playerHealth != null && other.CompareTag("Player"))
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
