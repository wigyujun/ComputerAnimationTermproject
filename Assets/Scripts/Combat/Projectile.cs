using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 1;

    [Header("Pierce")]
    [SerializeField] private int pierceCount = 0;
    private int hitCount = 0;

    private Vector2 moveDirection = Vector2.up;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }
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
