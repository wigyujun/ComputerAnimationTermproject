using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float screenPadding = 0.45f;

    private Rigidbody2D rb;
    private Camera mainCam;
    private Vector2 input;

    public Vector2 MoveInput => input;
    public Vector2 AimDirection { get; private set; } = Vector2.up;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        input = new Vector2(x, y).normalized;

        if (input.sqrMagnitude > 0.01f)
        {
            AimDirection = input;
        }
    }

    private void FixedUpdate()
    {
        Vector2 nextPosition = rb.position + input * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(ClampToCamera(nextPosition));
    }

    private Vector2 ClampToCamera(Vector2 worldPos)
    {
        if (mainCam == null) return worldPos;

        Vector3 min = mainCam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 max = mainCam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        worldPos.x = Mathf.Clamp(worldPos.x, min.x + screenPadding, max.x - screenPadding);
        worldPos.y = Mathf.Clamp(worldPos.y, min.y + screenPadding, max.y - screenPadding);

        return worldPos;
    }
}
