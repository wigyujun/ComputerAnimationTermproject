using UnityEngine;

public class EnemyBottomDespawn : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Despawn")]
    [SerializeField] private float bottomDespawnOffset = 1.0f;

    private bool removeHandled = false;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        removeHandled = false;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        CheckBelowScreenAndRemove();
    }

    private void CheckBelowScreenAndRemove()
    {
        if (removeHandled || targetCamera == null)
            return;

        float objectZ = transform.position.z;
        float cameraZ = targetCamera.transform.position.z;
        float distanceFromCamera = Mathf.Abs(objectZ - cameraZ);

        Vector3 bottomCenter = targetCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0f, distanceFromCamera)
        );

        float despawnY = bottomCenter.y - bottomDespawnOffset;

        if (transform.position.y < despawnY)
        {
            RemoveSelf();
        }
    }

    private void RemoveSelf()
    {
        if (removeHandled)
            return;

        removeHandled = true;

        EnemyController enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
            enemyController = GetComponentInParent<EnemyController>();

        if (enemyController != null)
        {
            enemyController.HandleDespawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCamera(Camera cam)
    {
        targetCamera = cam;
    }
}
