using UnityEngine;

public class ChargeEnemy : MonoBehaviour
{
    private enum State
    {
        Approach,
        Aim,
        Dash
    }

    [Header("Target")]
    [SerializeField] private Transform playerTarget;

    [Header("Approach")]
    [SerializeField] private float descendSpeed = 2.0f;
    [SerializeField] private float aimStartY = 2.5f;

    [Header("Aim")]
    [SerializeField] private float aimDuration = 0.45f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 10.0f;
    [SerializeField] private float dashLifeTime = 3.0f;
    [SerializeField] private int contactDamage = 1;

    [Header("Cleanup")]
    [SerializeField] private float despawnY = -7.5f;

    private Rigidbody2D rb;
    private State currentState = State.Approach;
    private float stateTimer = 0f;
    private Vector2 dashDirection;
    private bool hasDashed = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }
    }

    private void OnEnable()
    {
        currentState = State.Approach;
        stateTimer = 0f;
        hasDashed = false;
    }

    private void Update()
    {
        if (playerTarget == null)
            return;

        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case State.Approach:
                UpdateApproach();
                break;

            case State.Aim:
                UpdateAim();
                break;

            case State.Dash:
                UpdateDash();
                break;
        }

        RotateToVelocityOrTarget();

        if (transform.position.y < despawnY)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateApproach()
    {
        rb.linearVelocity = Vector2.down * descendSpeed;

        if (transform.position.y <= aimStartY)
        {
            currentState = State.Aim;
            stateTimer = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void UpdateAim()
    {
        rb.linearVelocity = Vector2.zero;

        Vector2 toPlayer = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        dashDirection = toPlayer;

        if (stateTimer >= aimDuration)
        {
            currentState = State.Dash;
            stateTimer = 0f;
            hasDashed = true;
            rb.linearVelocity = dashDirection * dashSpeed;
        }
    }

    private void UpdateDash()
    {
        rb.linearVelocity = dashDirection * dashSpeed;

        if (stateTimer >= dashLifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void RotateToVelocityOrTarget()
    {
        Vector2 dir;

        if (currentState == State.Dash && hasDashed)
        {
            dir = rb.linearVelocity.normalized;
        }
        else if (playerTarget != null)
        {
            dir = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        }
        else
        {
            return;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 160f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasDashed)
            return;

        var player = other.GetComponentInParent<Health>(); 
        if (player != null)
        {
            player.TakeDamage(contactDamage);
            Destroy(gameObject);
        }
    }
}
