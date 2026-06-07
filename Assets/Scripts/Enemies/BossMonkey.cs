using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeyBoss : BossBase
{
    [Header("Monkey Summon")]
    [SerializeField] private GameObject monkeyEnemyPrefab;
    [SerializeField] private Transform summonRoot;
    [SerializeField] private List<Transform> summonPoints = new List<Transform>();

    [Header("Pattern Timing")]
    [SerializeField] private float firstPatternDelay = 1.5f;
    [SerializeField] private float roarDuration = 1.0f;
    [SerializeField] private float summonInterval = 0.25f;
    [SerializeField] private float patternCooldown = 3.0f;

    [Header("Summon Count")]
    [SerializeField] private int summonCountPerPattern = 3;
    [SerializeField] private int maxAliveSummons = 6;

    [Header("Fallback Top Spawn")]
    [SerializeField] private float horizontalPadding = 0.6f;
    [SerializeField] private float topSpawnOffset = 1.0f;
    [SerializeField] private float visibleScreenPadding = 0.15f;

    [Header("Boss Hit Control")]
    [SerializeField] private bool blockBossHitWhileSummonsAlive = true;

    [Header("Animation / Debug")]
    [SerializeField] private Animator animator;
    [SerializeField] private string roarTriggerName = "Roar";
    [SerializeField] private bool enableDebugLog = true;

    private readonly List<GameObject> aliveSummons = new List<GameObject>();
    private Transform playerTarget;
    private Collider2D[] bossColliders;
    private bool bossHitEnabled = true;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        bossColliders = GetComponentsInChildren<Collider2D>(true);
    }

    protected override void OnBattleStarted()
    {
        ResolveReferences();
        UpdateBossHitState();
        Log("Battle started");
    }

    protected override void OnEnteredBattleArea()
    {
        ResolveReferences();
        UpdateBossHitState();
        Log("Entered battle area");
    }

    protected override void OnPhaseChanged(int newPhase)
    {
        base.OnPhaseChanged(newPhase);
        UpdateBossHitState();
        Log("Phase changed");
    }

    protected override void OnFinalDeath()
    {
        base.OnFinalDeath();

        SetBossHitEnabled(true);
        DestroyAllAliveSummons();

        Log("Boss final death -> all summoned monkeys removed");
    }

    protected override void OnBossUpdate()
    {
        CleanupDeadSummons();
        UpdateBossHitState();
    }

    protected override IEnumerator RunBossPattern()
    {
        if (firstPatternDelay > 0f)
            yield return new WaitForSeconds(firstPatternDelay);

        while (!FinalDeathHandled)
        {
            CleanupDeadSummons();
            UpdateBossHitState();

            int currentAlive = GetAliveSummonCount();
            if (currentAlive >= maxAliveSummons)
            {
                Log($"Summon skipped: aliveSummons={currentAlive}/{maxAliveSummons}");
                yield return new WaitForSeconds(1.0f);
                continue;
            }

            yield return StartCoroutine(DoRoarAndSummon());

            if (patternCooldown > 0f)
                yield return new WaitForSeconds(patternCooldown);
        }
    }

    private IEnumerator DoRoarAndSummon()
    {
        PlayRoarAnimation();
        Log("Roar");

        if (roarDuration > 0f)
            yield return new WaitForSeconds(roarDuration);

        CleanupDeadSummons();

        int remainCapacity = Mathf.Max(0, maxAliveSummons - GetAliveSummonCount());
        int spawnCount = Mathf.Min(summonCountPerPattern, remainCapacity);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnOneMonkey();
            UpdateBossHitState();

            if (summonInterval > 0f && i < spawnCount - 1)
                yield return new WaitForSeconds(summonInterval);
        }
    }

    private void SpawnOneMonkey()
    {
        if (monkeyEnemyPrefab == null)
        {
            Debug.LogError("[MonkeyBoss] monkeyEnemyPrefab is null.");
            return;
        }

        ResolveReferences();

        Vector3 spawnPosition = GetSummonPosition();
        Transform parent = summonRoot != null ? summonRoot : (transform.parent != null ? transform.parent : null);

        GameObject spawned = Instantiate(monkeyEnemyPrefab, spawnPosition, Quaternion.identity, parent);
        spawned.name = monkeyEnemyPrefab.name;

        aliveSummons.Add(spawned);

        InitializeSummonedMonkey(spawned);

        Log($"Spawn monkey -> {spawned.name}, pos={spawnPosition}");
    }

    private void InitializeSummonedMonkey(GameObject spawned)
    {
        if (spawned == null)
            return;

        Camera cam = battleCamera != null ? battleCamera : Camera.main;

        spawned.SendMessage("SetBattleCamera", cam, SendMessageOptions.DontRequireReceiver);
        spawned.SendMessage("SetCamera", cam, SendMessageOptions.DontRequireReceiver);

        if (playerTarget != null)
            spawned.SendMessage("SetPlayerTarget", playerTarget, SendMessageOptions.DontRequireReceiver);

        FloorBalanceUtility.ApplyEnemyBalance(spawned, RunContext.CurrentFloor, NodeType.Boss);

        Rigidbody2D summonedRb = spawned.GetComponent<Rigidbody2D>() ?? spawned.GetComponentInChildren<Rigidbody2D>();
        if (summonedRb != null)
        {
            summonedRb.linearVelocity = Vector2.zero;
            summonedRb.angularVelocity = 0f;
        }

        ClampSummonedMonkeyInsideCamera(spawned, cam);
    }

    private void ClampSummonedMonkeyInsideCamera(GameObject spawned, Camera cam)
    {
        if (spawned == null || cam == null)
            return;

        Renderer targetRenderer = spawned.GetComponentInChildren<Renderer>();
        Collider2D targetCollider = spawned.GetComponentInChildren<Collider2D>();

        Bounds bounds;
        if (targetRenderer != null)
            bounds = targetRenderer.bounds;
        else if (targetCollider != null)
            bounds = targetCollider.bounds;
        else
            return;

        float objectZ = spawned.transform.position.z;
        float cameraZ = cam.transform.position.z;
        float distanceFromCamera = Mathf.Abs(objectZ - cameraZ);

        Vector3 topLeft = cam.ViewportToWorldPoint(new Vector3(0f, 1f, distanceFromCamera));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distanceFromCamera));
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, distanceFromCamera));
        Vector3 bottomRight = cam.ViewportToWorldPoint(new Vector3(1f, 0f, distanceFromCamera));

        float minVisibleX = Mathf.Min(topLeft.x, bottomLeft.x) + visibleScreenPadding;
        float maxVisibleX = Mathf.Max(topRight.x, bottomRight.x) - visibleScreenPadding;
        float maxVisibleY = Mathf.Max(topLeft.y, topRight.y) - visibleScreenPadding;

        Vector3 offset = Vector3.zero;

        if (bounds.min.x < minVisibleX)
            offset.x += minVisibleX - bounds.min.x;

        if (bounds.max.x > maxVisibleX)
            offset.x -= bounds.max.x - maxVisibleX;

        if (bounds.max.y > maxVisibleY)
            offset.y -= bounds.max.y - maxVisibleY;

        if (offset != Vector3.zero)
            spawned.transform.position += offset;
    }

    private Vector3 GetSummonPosition()
    {
        if (summonPoints != null && summonPoints.Count > 0)
        {
            List<Transform> validPoints = new List<Transform>();

            for (int i = 0; i < summonPoints.Count; i++)
            {
                if (summonPoints[i] != null)
                    validPoints.Add(summonPoints[i]);
            }

            if (validPoints.Count > 0)
            {
                Transform point = validPoints[Random.Range(0, validPoints.Count)];
                return point.position;
            }
        }

        return GetFallbackTopSpawnPosition();
    }

    private Vector3 GetFallbackTopSpawnPosition()
    {
        Camera cam = battleCamera != null ? battleCamera : Camera.main;

        if (cam == null)
            return transform.position + new Vector3(Random.Range(-2.5f, 2.5f), 2f, 0f);

        float spawnZ = transform.position.z;
        float distanceFromCamera = Mathf.Abs(spawnZ - cam.transform.position.z);

        Vector3 topLeft = cam.ViewportToWorldPoint(new Vector3(0f, 1f, distanceFromCamera));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distanceFromCamera));

        float minX = Mathf.Min(topLeft.x, topRight.x) + horizontalPadding;
        float maxX = Mathf.Max(topLeft.x, topRight.x) - horizontalPadding;

        if (maxX < minX)
        {
            float centerX = (topLeft.x + topRight.x) * 0.5f;
            minX = centerX;
            maxX = centerX;
        }

        float randomX = Random.Range(minX, maxX);
        float spawnY = Mathf.Max(topLeft.y, topRight.y) + topSpawnOffset;

        return new Vector3(randomX, spawnY, spawnZ);
    }

    private void ResolveReferences()
    {
        if (summonRoot == null && transform.parent != null)
            summonRoot = transform.parent;

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTarget = playerObj.transform;
        }
    }

    private void PlayRoarAnimation()
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(roarTriggerName))
            return;

        animator.SetTrigger(roarTriggerName);
    }

    private int GetAliveSummonCount()
    {
        CleanupDeadSummons();
        return aliveSummons.Count;
    }

    private void CleanupDeadSummons()
    {
        for (int i = aliveSummons.Count - 1; i >= 0; i--)
        {
            if (aliveSummons[i] == null)
                aliveSummons.RemoveAt(i);
        }
    }

    private void DestroyAllAliveSummons()
    {
        for (int i = aliveSummons.Count - 1; i >= 0; i--)
        {
            if (aliveSummons[i] != null)
                Destroy(aliveSummons[i]);
        }

        aliveSummons.Clear();
    }

    private void UpdateBossHitState()
    {
        if (!blockBossHitWhileSummonsAlive)
            return;

        bool shouldEnableHit = !HasAliveSummons();
        SetBossHitEnabled(shouldEnableHit);
    }

    private void SetBossHitEnabled(bool enabled)
    {
        if (bossHitEnabled == enabled)
            return;

        bossHitEnabled = enabled;

        if (bossColliders == null || bossColliders.Length == 0)
            bossColliders = GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < bossColliders.Length; i++)
        {
            if (bossColliders[i] == null)
                continue;

            bossColliders[i].enabled = enabled;
        }

        Log(enabled ? "Boss hit ENABLED" : "Boss hit DISABLED");
    }

    public bool HasAliveSummons()
    {
        CleanupDeadSummons();
        return aliveSummons.Count > 0;
    }

    public bool CanTakePlayerProjectileDamage()
    {
        return !HasAliveSummons();
    }

    private void Log(string message)
    {
        if (!enableDebugLog)
            return;

        Debug.Log("[MonkeyBoss] " + message);
    }
}
