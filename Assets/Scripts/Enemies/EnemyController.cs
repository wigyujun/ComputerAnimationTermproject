using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private int contactDamage = 1;

    [Header("Cleanup")]
    [SerializeField] private float destroyY = -7f;

    [Header("Reward")]
    [SerializeField] private int coinReward = 1;

    private EnemySpawner spawner;
    private PlayerWallet playerWallet;
    private Health health;

    private bool hasNotifiedRemove = false;
    private bool rewardGranted = false;

    private void Start()
    {
        playerWallet = FindFirstObjectByType<PlayerWallet>();
        health = GetComponent<Health>();
    }

    private void Update()
    {
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;

        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }

    public void SetSpawner(EnemySpawner newSpawner)
    {
        spawner = newSpawner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponentInParent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(contactDamage);
            }

            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
            return;

        if (!hasNotifiedRemove && spawner != null)
        {
            spawner.NotifyEnemyRemoved();
            hasNotifiedRemove = true;
        }

        if (health != null && health.CurrentHP <= 0 && !rewardGranted)
        {
            if (playerWallet != null)
            {
                playerWallet.AddCoin(coinReward);
            }

            rewardGranted = true;
        }
    }
}
