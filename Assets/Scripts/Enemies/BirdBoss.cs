using System.Collections;
using UnityEngine;

public class BirdBoss : BossBase
{
    [Header("Target")]
    [SerializeField] private Transform playerTarget;

    [Header("Bird Strike")]
    [SerializeField] private int strikeCountPerPattern = 3;
    [SerializeField] private float firstPatternDelay = 1.5f;
    [SerializeField] private float preDashDelay = 0.35f;
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float maxDashDuration = 2.5f;
    [SerializeField] private float strikeInterval = 0.35f;
    [SerializeField] private float patternCooldown = 2.5f;

    [Header("Strike Start Positions")]
    [SerializeField] private float topSpawnOffset = 1.0f;
    [SerializeField] private float leftViewportX = 0.2f;
    [SerializeField] private float centerViewportX = 0.5f;
    [SerializeField] private float rightViewportX = 0.8f;

    [Header("Offscreen Exit")]
    [SerializeField] private float offscreenExitMargin = 1.2f;
    [SerializeField] private float reappearDelay = 0.15f;

    [Header("Combat")]
    [SerializeField] private int contactDamage = 1;

    [Header("Rotation")]
    [SerializeField] private bool rotateToDashDirection = false; // 사용 안 함(호환용 유지)
    [SerializeField] private float spriteAngleOffset = 180f;     // 사용 안 함(호환용 유지)

    [Header("Animation / Debug")]
    [SerializeField] private Animator animator;
    [SerializeField] private string strikeTriggerName = "Strike";
    [SerializeField] private bool enableDebugLog = true;

    private Vector3 idleBattlePosition;
    private Quaternion idleBattleRotation;
    private bool idlePositionCached = false;
    private bool isDashing = false;
    private Vector2 currentDashDirection = Vector2.down;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    protected override void OnBattleStarted()
    {
        ResolveReferences();
        Log("Battle started");
    }

    protected override void OnEnteredBattleArea()
    {
        ResolveReferences();

        idleBattlePosition = transform.position;
        idleBattleRotation = transform.rotation;
        idlePositionCached = true;

        Log($"Entered battle area / idlePos={idleBattlePosition}");
    }

    protected override void OnPhaseChanged(int newPhase)
    {
        base.OnPhaseChanged(newPhase);
        Log($"Phase changed -> {newPhase}");
    }

    protected override void OnFinalDeath()
    {
        base.OnFinalDeath();
        isDashing = false;
        Log("Final death");
    }

    protected override IEnumerator RunBossPattern()
    {
        if (firstPatternDelay > 0f)
            yield return new WaitForSeconds(firstPatternDelay);

        while (!FinalDeathHandled)
        {
            for (int i = 0; i < strikeCountPerPattern; i++)
            {
                if (FinalDeathHandled)
                    yield break;

                yield return StartCoroutine(DoSingleBirdStrike(i + 1));

                if (strikeInterval > 0f && i < strikeCountPerPattern - 1)
                    yield return new WaitForSeconds(strikeInterval);
            }

            ReturnToIdlePosition();

            if (patternCooldown > 0f)
                yield return new WaitForSeconds(patternCooldown);
        }
    }

    private IEnumerator DoSingleBirdStrike(int strikeIndex)
    {
        ResolveReferences();

        Vector3 strikeStartPos = GetRandomStrikeStartPosition();
        transform.position = strikeStartPos;

        // 처음 전투 위치에 들어왔을 때의 회전값 유지
        if (idlePositionCached)
            transform.rotation = idleBattleRotation;

        currentDashDirection = GetDashDirectionTowardPlayer();

        // 회전은 하지 않고, 방향 계산만 해서 이동에만 사용
        PlayStrikeAnimation();
        Log($"Bird strike #{strikeIndex} / startPos={strikeStartPos} / dir={currentDashDirection}");

        if (preDashDelay > 0f)
            yield return new WaitForSeconds(preDashDelay);

        yield return StartCoroutine(DashUntilOffscreen());

        if (reappearDelay > 0f)
            yield return new WaitForSeconds(reappearDelay);
    }

    private IEnumerator DashUntilOffscreen()
    {
        isDashing = true;

        float timer = 0f;

        while (!FinalDeathHandled)
        {
            // 회전 없이 이동만
            transform.position += (Vector3)(currentDashDirection * dashSpeed * Time.deltaTime);

            timer += Time.deltaTime;

            if (IsOutsideBattleView(offscreenExitMargin))
                break;

            if (timer >= maxDashDuration)
                break;

            yield return null;
        }

        isDashing = false;
    }

    private Vector3 GetRandomStrikeStartPosition()
    {
        Camera cam = battleCamera != null ? battleCamera : Camera.main;
        if (cam == null)
            return transform.position + new Vector3(0f, 3f, 0f);

        float[] viewportXs = new float[3]
        {
            leftViewportX,
            centerViewportX,
            rightViewportX
        };

        float selectedX = viewportXs[Random.Range(0, viewportXs.Length)];

        float spawnZ = transform.position.z;
        float distanceFromCamera = Mathf.Abs(spawnZ - cam.transform.position.z);

        Vector3 topPoint = cam.ViewportToWorldPoint(new Vector3(selectedX, 1f, distanceFromCamera));
        return new Vector3(topPoint.x, topPoint.y + topSpawnOffset, spawnZ);
    }

    private Vector2 GetDashDirectionTowardPlayer()
    {
        if (playerTarget == null)
            return Vector2.down;

        Vector2 dir = (Vector2)playerTarget.position - (Vector2)transform.position;
        if (dir.sqrMagnitude <= 0.0001f)
            return Vector2.down;

        return dir.normalized;
    }

    private void ReturnToIdlePosition()
    {
        if (!idlePositionCached)
            return;

        isDashing = false;
        transform.position = idleBattlePosition;
        transform.rotation = idleBattleRotation;
    }

    private bool IsOutsideBattleView(float margin)
    {
        Camera cam = battleCamera != null ? battleCamera : Camera.main;
        if (cam == null)
            return false;

        float objectZ = transform.position.z;
        float cameraZ = cam.transform.position.z;
        float distanceFromCamera = Mathf.Abs(objectZ - cameraZ);

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, distanceFromCamera));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distanceFromCamera));

        float minX = bottomLeft.x - margin;
        float maxX = topRight.x + margin;
        float minY = bottomLeft.y - margin;
        float maxY = topRight.y + margin;

        Vector3 pos = transform.position;

        return pos.x < minX || pos.x > maxX || pos.y < minY || pos.y > maxY;
    }

    private void PlayStrikeAnimation()
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(strikeTriggerName))
            return;

        animator.SetTrigger(strikeTriggerName);
    }

    private void ResolveReferences()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTarget = playerObj.transform;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isDashing)
            return;

        if (!other.CompareTag("Player"))
            return;

        Health playerHp = other.GetComponent<Health>();
        if (playerHp == null)
            playerHp = other.GetComponentInParent<Health>();

        if (playerHp != null)
        {
            playerHp.TakeDamage(contactDamage);
            Log($"Hit player / damage={contactDamage}");
        }
    }

    private void Log(string message)
    {
        if (!enableDebugLog)
            return;

        Debug.Log("[BirdBoss] " + message);
    }
}
