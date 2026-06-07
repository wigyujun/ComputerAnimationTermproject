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

    // 핵심 입력 처리: 방향키와 WASD를 함께 받아 현재 이동 방향과 조준 방향을 갱신한다.
    private void Update()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            x -= 1f;

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            x += 1f;

        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            y -= 1f;

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            y += 1f;

        if (Mathf.Approximately(x, 0f))
            x = Input.GetAxisRaw("Horizontal");

        if (Mathf.Approximately(y, 0f))
            y = Input.GetAxisRaw("Vertical");

        input = new Vector2(x, y).normalized;

        if (input.sqrMagnitude > 0.01f)
        {
            AimDirection = input;
        }
    }

    // 핵심 이동 처리: 물리 프레임마다 플레이어를 이동시키고 카메라 화면 안으로 제한한다.
    private void FixedUpdate()
    {
        Vector2 nextPosition = rb.position + input * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(ClampToCamera(nextPosition));
    }

    // 화면 밖으로 나가지 않도록 플레이어 좌표를 카메라 경계 안으로 고정한다.
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
