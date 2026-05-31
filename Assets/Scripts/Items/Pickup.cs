using UnityEngine;

public class Pickup : MonoBehaviour
{
    public enum PickupType { Heal, Score }

    [SerializeField] private PickupType pickupType = PickupType.Heal;
    [SerializeField] private int value = 1;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float lifeTime = 8f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (pickupType == PickupType.Heal)
        {
            Health health = other.GetComponent<Health>();
            if (health != null) health.Heal(value);
        }

        Destroy(gameObject);
    }
}
