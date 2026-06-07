using UnityEngine;

public class EnemyVerticalMover : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private bool useRigidbody2D = true;

    [Header("Despawn")]
    [SerializeField] private float bottomDespawnOffset = 1.5f;
    [SerializeField] private Camera battleCamera;

    private Rigidbody2D rb;
    private bool despawnHandled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (battleCamera == null)
            battleCamera = Camera.main;
    }

    private void OnEnable()
    {
        despawnHandled = false;

        if (battleCamera == null)
            battleCamera = Camera.main;
    }

    private void Update()
    {
        if (useRigidbody2D && rb != null)
            return;

        MoveDownByTransform();
        CheckBelowScreenAndDespawn();
    }

    private void FixedUpdate()
    {
        if (!useRigidbody2D || rb == null)
            return;

        rb.linearVelocity = Vector2.down * moveSpeed;
        CheckBelowScreenAndDespawn();
    }

    private void MoveDownByTransform()
    {
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
    }

    private void CheckBelowScreenAndDespawn()
    {
        if (despawnHandled) return;
        if (battleCamera == null) return;

        float objectZ = transform.position.z;
        float camZ = battleCamera.transform.position.z;
        float distanceFromCamera = Mathf.Abs(objectZ - camZ);

        Vector3 bottomCenter = battleCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0f, distanceFromCamera)
        );

        float despawnY = bottomCenter.y - bottomDespawnOffset;

        if (transform.position.y < despawnY)
        {
            HandleOffscreenDespawn();
        }
    }

    private void HandleOffscreenDespawn()
    {
        if (despawnHandled) return;
        despawnHandled = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.NotifyEnemyRemoved();
        }

        Debug.Log($"[EnemyVerticalMover] 화면 아래로 벗어나 제거: {gameObject.name}");
        Destroy(gameObject);
    }

    public void SetBattleCamera(Camera newCamera)
    {
        battleCamera = newCamera;
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0.1f, newSpeed);
    }
}
