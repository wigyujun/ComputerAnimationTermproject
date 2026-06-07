using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Contact Damage")]
    [SerializeField] private bool useTouchDamage = false;
    [SerializeField] private int contactDamage = 1;

    [Header("Reward")]
    [SerializeField] private int coinReward = 1;

    private EnemySpawner spawner;
    private Health health;

    private bool hasNotifiedRemove = false;
    private bool rewardGranted = false;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
            health = GetComponentInChildren<Health>();
    }

    private void Start()
    {
        
    }

    public void SetSpawner(EnemySpawner newSpawner)
    {
        spawner = newSpawner;
    }

    public void HandleDespawn()
    {
        if (hasNotifiedRemove)
            return;

        NotifyRemoved();
        Destroy(gameObject);
    }

    public void HandleDeath()
    {
        if (!rewardGranted)
            GiveReward();

        if (!hasNotifiedRemove)
            NotifyRemoved();

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTouchDamage)
            return;

        if (!other.CompareTag("Player"))
            return;

        Health playerHealth = other.GetComponentInParent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(contactDamage);
        }

        HandleDespawn();
    }

    private void NotifyRemoved()
    {
        if (hasNotifiedRemove)
            return;

        hasNotifiedRemove = true;
        spawner?.NotifyEnemyRemoved();
    }

    private void GiveReward()
    {
        if (rewardGranted)
            return;

        RunContext.AddCoin(coinReward);
        rewardGranted = true;
    }

    public void SetCoinReward(int value)
    {
        coinReward = Mathf.Max(0, value);
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
            return;

        if (!hasNotifiedRemove)
        {
            NotifyRemoved();
        }

        if (!rewardGranted && health != null && health.CurrentHP <= 0)
        {
            GiveReward();
        }
    }
}
