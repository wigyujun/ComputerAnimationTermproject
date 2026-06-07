using UnityEngine;

public class ProjectileAutoDespawn : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Despawn Margin")]
    [SerializeField] private float extraMargin = 0.5f;

    private bool initialized = false;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized || targetCamera == null)
            return;

        if (IsOutsideCameraBounds())
        {
            Destroy(gameObject);
        }
    }

    private bool IsOutsideCameraBounds()
    {
        float objectZ = transform.position.z;
        float cameraZ = targetCamera.transform.position.z;
        float distanceFromCamera = Mathf.Abs(objectZ - cameraZ);

        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distanceFromCamera)
        );

        Vector3 topRight = targetCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, distanceFromCamera)
        );

        float minX = bottomLeft.x - extraMargin;
        float maxX = topRight.x + extraMargin;
        float minY = bottomLeft.y - extraMargin;
        float maxY = topRight.y + extraMargin;

        Vector3 pos = transform.position;

        return pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY;
    }

    public void SetCamera(Camera cam)
    {
        targetCamera = cam;
    }
}
