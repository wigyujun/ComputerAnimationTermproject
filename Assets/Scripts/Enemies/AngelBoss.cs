using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngelBoss : BossBase
{
    [Header("Boss Sorting")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private string bossSortingLayerName = "Enemy";
    [SerializeField] private int bossSortingOrder = 100;

    [Header("Shared Pattern Timing")]
    [SerializeField] private float firstPatternDelay = 1.2f;
    [SerializeField] private float patternCooldown = 2.0f;

    [Header("Beam Attack (Phase 1+)")]
    [SerializeField] private float beamWarningDuration = 1.2f;
    [SerializeField] private float beamHitFlashDuration = 0.25f;
    [SerializeField] private float intervalBetweenBeams = 0.2f;
    [SerializeField] private int beamColumnCount = 6;
    [SerializeField] private int beamDamage = 1;
    [SerializeField] private int dualBeamAttackCount = 6;
    [SerializeField] private bool avoidSameBeamPairInARow = false;
    [SerializeField] private GameObject beamTelegraphPrefab;

    [Header("Staff Slam (Phase 2+)")]
    [SerializeField] private int staffSlamCount = 6;
    [SerializeField] private float staffWarningDuration = 0.9f;
    [SerializeField] private float staffHitFlashDuration = 0.25f;
    [SerializeField] private float intervalBetweenStaffSlams = 0.15f;
    [SerializeField] private int staffDamage = 1;
    [SerializeField] private float staffImpactRadius = 1.9f;
    [SerializeField] private bool targetPlayerForStaffSlam = true;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private GameObject circleTelegraphPrefab;

    [Header("Battle Area")]
    [SerializeField] private float sidePadding = 0.15f;
    [SerializeField] private float bottomPadding = 0.15f;
    [SerializeField] private float gapBelowBoss = 0.2f;
    [SerializeField] private float minAttackHeight = 1.0f;

    [Header("Telegraph Visual")]
    [SerializeField] private Color warningColor = new Color(1f, 0f, 0f, 0.75f);
    [SerializeField] private Color hitColor = new Color(1f, 1f, 0f, 0.9f);
    [SerializeField] private string telegraphObjectLayerName = "Default";
    [SerializeField] private int telegraphSortingOrder = 1000;

    [Header("Animation / Debug")]
    [SerializeField] private Animator animator;
    [SerializeField] private string beamTriggerName = "Beam";
    [SerializeField] private string staffSlamTriggerName = "StaffSlam";
    [SerializeField] private bool enableDebugLog = true;

    private const string TELEGRAPH_SORTING_LAYER = "Enemy";

    private readonly List<GameObject> activeOverlays = new List<GameObject>();

    private bool firstPatternPlayed = false;
    private int currentBossPhase = 1;
    private int patternRunToken = 0;
    private Collider2D bodyCollider;

    private struct RectAttackArea
    {
        public Vector2 center;
        public Vector2 size;
        public int index;

        public RectAttackArea(Vector2 center, Vector2 size, int index)
        {
            this.center = center;
            this.size = size;
            this.index = index;
        }
    }

    private struct BattleBounds
    {
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;

        public float Width => maxX - minX;
        public float Height => maxY - minY;
        public Vector2 Center => new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
        ApplyBossSorting();
    }

    private void OnDisable()
    {
        CancelPatternRunAndClearOverlays();
    }

    private void OnDestroy()
    {
        CancelPatternRunAndClearOverlays();
    }

    protected override void OnBattleStarted()
    {
        ResolveReferences();
        ApplyBossSorting();
        firstPatternPlayed = false;
        currentBossPhase = 1;
        CancelPatternRunAndClearOverlays();
        Log("Battle started");
    }

    protected override void OnEnteredBattleArea()
    {
        ResolveReferences();
        ApplyBossSorting();
        Log("Entered battle area");
    }

    protected override void OnPhaseChanged(int newPhase)
    {
        base.OnPhaseChanged(newPhase);
        currentBossPhase = newPhase;

        // phase 전환 시 남은 telegraph만 정리
        ClearAllOverlays();

        Log($"Phase changed -> {currentBossPhase}");
    }

    protected override void OnFinalDeath()
    {
        CancelPatternRunAndClearOverlays();
        Log("Final death / cleared all overlays");
    }

    protected override IEnumerator RunBossPattern()
    {
        int runToken = BeginPatternRun();

        if (!firstPatternPlayed)
        {
            firstPatternPlayed = true;

            if (firstPatternDelay > 0f)
                yield return new WaitForSeconds(firstPatternDelay);
        }

        if (!IsPatternRunValid(runToken))
            yield break;

        while (IsPatternRunValid(runToken))
        {
            yield return RunBeamPattern(runToken);

            if (!IsPatternRunValid(runToken))
                yield break;

            if (currentBossPhase >= 2)
            {
                yield return RunStaffSlamPattern(runToken);

                if (!IsPatternRunValid(runToken))
                    yield break;
            }

            if (patternCooldown > 0f)
                yield return new WaitForSeconds(patternCooldown);

            if (!IsPatternRunValid(runToken))
                yield break;
        }
    }

    private int BeginPatternRun()
    {
        patternRunToken++;
        ClearAllOverlays();
        CleanupNullOverlays();
        return patternRunToken;
    }

    private void CancelPatternRunAndClearOverlays()
    {
        patternRunToken++;
        ClearAllOverlays();
    }

    private bool IsPatternRunValid(int token)
    {
        return token == patternRunToken && !FinalDeathHandled && isActiveAndEnabled;
    }

    private IEnumerator RunBeamPattern(int runToken)
    {
        if (!TryBuildBeamAreas(out List<RectAttackArea> beamAreas))
        {
            Log("RunBeamPattern skipped / failed to build beam areas");
            yield break;
        }

        if (beamAreas.Count < 2)
        {
            Log("RunBeamPattern skipped / beam area count < 2");
            yield break;
        }

        int prevA = -1;
        int prevB = -1;

        for (int attackIndex = 0; attackIndex < dualBeamAttackCount; attackIndex++)
        {
            if (!IsPatternRunValid(runToken))
                yield break;

            GetRandomBeamPair(beamAreas.Count, prevA, prevB, out int firstIndex, out int secondIndex);

            prevA = firstIndex;
            prevB = secondIndex;

            PlayAnimationTrigger(beamTriggerName);

            yield return DoDualBeamAttack(
                beamAreas[firstIndex],
                beamAreas[secondIndex],
                attackIndex + 1,
                runToken
            );

            if (!IsPatternRunValid(runToken))
                yield break;

            if (attackIndex < dualBeamAttackCount - 1 && intervalBetweenBeams > 0f)
                yield return new WaitForSeconds(intervalBetweenBeams);
        }
    }

    private IEnumerator RunStaffSlamPattern(int runToken)
    {
        if (!TryGetBattleBounds(out BattleBounds bounds))
        {
            Log("RunStaffSlamPattern skipped / failed to get battle bounds");
            yield break;
        }

        if (circleTelegraphPrefab == null)
        {
            Debug.LogError("[AngelBoss] circleTelegraphPrefab is NULL. 원형 telegraph를 생성할 수 없습니다.");
            yield break;
        }

        for (int i = 0; i < staffSlamCount; i++)
        {
            if (!IsPatternRunValid(runToken))
                yield break;

            Vector2 targetPoint = GetStaffTargetPoint(bounds);

            PlayAnimationTrigger(staffSlamTriggerName);

            yield return DoCircleAttack(
                targetPoint,
                staffImpactRadius,
                staffWarningDuration,
                staffHitFlashDuration,
                staffDamage,
                $"StaffSlam {i + 1}/{staffSlamCount}",
                runToken
            );

            if (!IsPatternRunValid(runToken))
                yield break;

            if (i < staffSlamCount - 1 && intervalBetweenStaffSlams > 0f)
                yield return new WaitForSeconds(intervalBetweenStaffSlams);
        }
    }

    private IEnumerator DoDualBeamAttack(RectAttackArea firstArea, RectAttackArea secondArea, int attackIndex, int runToken)
    {
        GameObject warningA = null;
        GameObject warningB = null;
        GameObject hitA = null;
        GameObject hitB = null;

        try
        {
            if (!IsPatternRunValid(runToken))
                yield break;

            warningA = SpawnRectOverlay(firstArea, warningColor, $"BeamWarning_{attackIndex}_{firstArea.index}");
            warningB = SpawnRectOverlay(secondArea, warningColor, $"BeamWarning_{attackIndex}_{secondArea.index}");

            Log($"Dual beam warning / attack={attackIndex} / pair=({firstArea.index}, {secondArea.index})");

            if (beamWarningDuration > 0f)
                yield return new WaitForSeconds(beamWarningDuration);

            if (!IsPatternRunValid(runToken))
                yield break;

            ReleaseOverlay(ref warningA);
            ReleaseOverlay(ref warningB);

            ExecuteBoxDamage(firstArea.center, firstArea.size, beamDamage, $"Player hit by beam / column={firstArea.index}");
            ExecuteBoxDamage(secondArea.center, secondArea.size, beamDamage, $"Player hit by beam / column={secondArea.index}");

            hitA = SpawnRectOverlay(firstArea, hitColor, $"BeamHit_{attackIndex}_{firstArea.index}");
            hitB = SpawnRectOverlay(secondArea, hitColor, $"BeamHit_{attackIndex}_{secondArea.index}");

            Log($"Dual beam hit / attack={attackIndex} / pair=({firstArea.index}, {secondArea.index})");

            if (beamHitFlashDuration > 0f)
                yield return new WaitForSeconds(beamHitFlashDuration);
        }
        finally
        {
            ReleaseOverlay(ref warningA);
            ReleaseOverlay(ref warningB);
            ReleaseOverlay(ref hitA);
            ReleaseOverlay(ref hitB);
        }
    }

    private IEnumerator DoCircleAttack(
        Vector2 center,
        float radius,
        float warningDuration,
        float hitFlashDuration,
        int damage,
        string debugName,
        int runToken)
    {
        GameObject warning = null;
        GameObject hit = null;

        try
        {
            if (!IsPatternRunValid(runToken))
                yield break;

            if (circleTelegraphPrefab == null)
            {
                Debug.LogError("[AngelBoss] circleTelegraphPrefab is NULL. 원형 telegraph를 생성할 수 없음.");
                yield break;
            }

            warning = SpawnCircleOverlay(center, radius, warningColor, $"CircleWarning_{Time.frameCount}");
            Log($"{debugName} warning / center={center} radius={radius}");

            if (warningDuration > 0f)
                yield return new WaitForSeconds(warningDuration);

            if (!IsPatternRunValid(runToken))
                yield break;

            ReleaseOverlay(ref warning);

            ExecuteCircleDamage(center, radius, damage, $"{debugName} damage");

            hit = SpawnCircleOverlay(center, radius, hitColor, $"CircleHit_{Time.frameCount}");
            Log($"{debugName} hit / center={center} radius={radius}");

            if (hitFlashDuration > 0f)
                yield return new WaitForSeconds(hitFlashDuration);
        }
        finally
        {
            ReleaseOverlay(ref warning);
            ReleaseOverlay(ref hit);
        }
    }

    private bool TryBuildBeamAreas(out List<RectAttackArea> areas)
    {
        areas = new List<RectAttackArea>();

        if (!TryGetBattleBounds(out BattleBounds bounds))
        {
            Log("TryBuildBeamAreas failed / no battle bounds");
            return false;
        }

        if (beamColumnCount <= 0)
        {
            Log("TryBuildBeamAreas failed / beamColumnCount <= 0");
            return false;
        }

        float columnWidth = bounds.Width / beamColumnCount;
        if (columnWidth <= 0f)
        {
            Log("TryBuildBeamAreas failed / columnWidth <= 0");
            return false;
        }

        for (int i = 0; i < beamColumnCount; i++)
        {
            float minX = bounds.minX + columnWidth * i;
            float maxX = minX + columnWidth;

            Vector2 center = new Vector2((minX + maxX) * 0.5f, bounds.Center.y);
            Vector2 size = new Vector2(columnWidth, bounds.Height);

            areas.Add(new RectAttackArea(center, size, i));
        }

        return true;
    }

    private bool TryGetBattleBounds(out BattleBounds bounds)
    {
        bounds = default;

        if (battleCamera == null)
            battleCamera = Camera.main;

        if (battleCamera == null)
        {
            Log("TryGetBattleBounds failed / battleCamera is NULL");
            return false;
        }

        float depth = Mathf.Abs(transform.position.z - battleCamera.transform.position.z);

        Vector3 bottomLeft = battleCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 topRight = battleCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));

        float minX = bottomLeft.x + sidePadding;
        float maxX = topRight.x - sidePadding;
        float minY = bottomLeft.y + bottomPadding;
        float maxY = GetBossBottomY() - gapBelowBoss;

        float totalHeight = maxY - minY;

        if (maxX <= minX || maxY <= minY)
        {
            Log($"TryGetBattleBounds failed / minX={minX:F2}, maxX={maxX:F2}, minY={minY:F2}, maxY={maxY:F2}");
            return false;
        }

        if (totalHeight < minAttackHeight)
        {
            Log($"TryGetBattleBounds failed / minY={minY:F2}, maxY={maxY:F2}, totalHeight={totalHeight:F2}, bossBottom={GetBossBottomY():F2}");
            return false;
        }

        bounds.minX = minX;
        bounds.maxX = maxX;
        bounds.minY = minY;
        bounds.maxY = maxY;
        return true;
    }

    private Vector2 GetStaffTargetPoint(BattleBounds bounds)
    {
        if (targetPlayerForStaffSlam && playerTarget != null)
        {
            Vector3 playerPos = playerTarget.position;
            float clampedX = Mathf.Clamp(playerPos.x, bounds.minX, bounds.maxX);
            float clampedY = Mathf.Clamp(playerPos.y, bounds.minY, bounds.maxY);
            return new Vector2(clampedX, clampedY);
        }

        float randomX = Random.Range(bounds.minX, bounds.maxX);
        float randomY = Random.Range(bounds.minY, bounds.maxY);
        return new Vector2(randomX, randomY);
    }

    private void GetRandomBeamPair(int totalCount, int prevA, int prevB, out int firstIndex, out int secondIndex)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int i = 0; i < totalCount; i++)
        {
            for (int j = i + 1; j < totalCount; j++)
            {
                if (avoidSameBeamPairInARow)
                {
                    bool sameAsPrevious =
                        (i == prevA && j == prevB) ||
                        (i == prevB && j == prevA);

                    if (sameAsPrevious)
                        continue;
                }

                candidates.Add(new Vector2Int(i, j));
            }
        }

        if (candidates.Count == 0)
        {
            firstIndex = 0;
            secondIndex = Mathf.Min(1, totalCount - 1);
            return;
        }

        Vector2Int picked = candidates[Random.Range(0, candidates.Count)];
        firstIndex = picked.x;
        secondIndex = picked.y;
    }

    private float GetBossBottomY()
    {
        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyCollider != null)
            return bodyCollider.bounds.min.y;

        if (bodyRenderer != null)
            return bodyRenderer.bounds.min.y;

        return transform.position.y;
    }

    private GameObject SpawnRectOverlay(RectAttackArea area, Color color, string objectName)
    {
        if (beamTelegraphPrefab == null)
        {
            Debug.LogError("[AngelBoss] SpawnRectOverlay failed / beamTelegraphPrefab is NULL");
            return null;
        }

        return SpawnSizedVisual(
            beamTelegraphPrefab,
            area.center,
            area.size,
            color,
            objectName,
            telegraphSortingOrder
        );
    }

    private GameObject SpawnCircleOverlay(Vector2 center, float radius, Color color, string objectName)
    {
        if (circleTelegraphPrefab == null)
        {
            Debug.LogError("[AngelBoss] SpawnCircleOverlay failed / circleTelegraphPrefab is NULL");
            return null;
        }

        float diameter = radius * 2f;
        Vector2 size = new Vector2(diameter, diameter);

        GameObject overlay = SpawnSizedVisual(
            circleTelegraphPrefab,
            center,
            size,
            color,
            objectName,
            telegraphSortingOrder
        );

        if (overlay == null)
            return null;

        SpriteRenderer rootSr = overlay.GetComponent<SpriteRenderer>();
        if (rootSr == null)
            rootSr = overlay.GetComponentInChildren<SpriteRenderer>(true);

        if (rootSr != null)
            rootSr.drawMode = SpriteDrawMode.Simple;

        return overlay;
    }

    private GameObject SpawnSizedVisual(GameObject prefab, Vector2 center, Vector2 size, Color color, string objectName, int sortingOrder)
    {
        if (prefab == null)
        {
            Log($"SpawnSizedVisual failed / prefab is NULL / object={objectName}");
            return null;
        }

        Vector3 spawnPos = new Vector3(center.x, center.y, 0f);
        GameObject visual = Instantiate(prefab, spawnPos, Quaternion.identity);
        visual.name = objectName;

        int objectLayer = LayerMask.NameToLayer(telegraphObjectLayerName);
        if (objectLayer >= 0)
            SetLayerRecursively(visual, objectLayer);

        visual.transform.rotation = Quaternion.identity;

        SpriteRenderer rootSr = visual.GetComponent<SpriteRenderer>();
        if (rootSr == null)
            rootSr = visual.GetComponentInChildren<SpriteRenderer>(true);

        if (rootSr != null)
        {
            if (rootSr.drawMode == SpriteDrawMode.Simple)
            {
                float spriteWidth = rootSr.sprite != null ? rootSr.sprite.bounds.size.x : 1f;
                float spriteHeight = rootSr.sprite != null ? rootSr.sprite.bounds.size.y : 1f;

                visual.transform.localScale = new Vector3(
                    size.x / Mathf.Max(spriteWidth, 0.0001f),
                    size.y / Mathf.Max(spriteHeight, 0.0001f),
                    1f
                );
            }
            else
            {
                visual.transform.localScale = Vector3.one;
                rootSr.size = size;
            }
        }
        else
        {
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        ApplyVisualRenderSettings(visual, color, sortingOrder);
        RegisterOverlay(visual);

        return visual;
    }

    private void ApplyVisualRenderSettings(GameObject visual, Color color, int sortingOrder)
    {
        if (visual == null)
            return;

        SpriteRenderer[] renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogError("[AngelBoss] ApplyVisualRenderSettings / no SpriteRenderer found");
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            sr.enabled = true;
            sr.color = color;
            sr.sortingLayerName = TELEGRAPH_SORTING_LAYER;
            sr.sortingOrder = sortingOrder;
        }
    }

    private void ExecuteBoxDamage(Vector2 center, Vector2 size, int damage, string logMessage)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        if (hits == null || hits.Length == 0)
            return;

        HashSet<Health> damagedTargets = new HashSet<Health>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            if (!hit.CompareTag("Player") && !hit.transform.root.CompareTag("Player"))
                continue;

            Health playerHp = hit.GetComponent<Health>();
            if (playerHp == null)
                playerHp = hit.GetComponentInParent<Health>();

            if (playerHp == null)
                continue;

            if (damagedTargets.Contains(playerHp))
                continue;

            damagedTargets.Add(playerHp);
            playerHp.TakeDamage(damage);
            Log($"{logMessage} / damage={damage}");
        }
    }

    private void ExecuteCircleDamage(Vector2 center, float radius, int damage, string logMessage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        if (hits == null || hits.Length == 0)
            return;

        HashSet<Health> damagedTargets = new HashSet<Health>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            if (!hit.CompareTag("Player") && !hit.transform.root.CompareTag("Player"))
                continue;

            Health playerHp = hit.GetComponent<Health>();
            if (playerHp == null)
                playerHp = hit.GetComponentInParent<Health>();

            if (playerHp == null)
                continue;

            if (damagedTargets.Contains(playerHp))
                continue;

            damagedTargets.Add(playerHp);
            playerHp.TakeDamage(damage);
            Log($"{logMessage} / damage={damage}");
        }
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();

        if (battleCamera == null)
            battleCamera = Camera.main;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }
    }

    private void ApplyBossSorting()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyRenderer == null)
        {
            Log("ApplyBossSorting skipped / bodyRenderer is NULL");
            return;
        }

        bodyRenderer.sortingLayerName = bossSortingLayerName;
        bodyRenderer.sortingOrder = bossSortingOrder;
    }

    private void RegisterOverlay(GameObject overlay)
    {
        if (overlay == null)
            return;

        CleanupNullOverlays();

        if (!activeOverlays.Contains(overlay))
            activeOverlays.Add(overlay);
    }

    private void RemoveOverlay(GameObject overlay)
    {
        if (overlay == null)
            return;

        activeOverlays.Remove(overlay);

        if (overlay != null)
        {
            overlay.SetActive(false);
            Destroy(overlay);
        }
    }

    private void ReleaseOverlay(ref GameObject overlay)
    {
        if (overlay == null)
            return;

        activeOverlays.Remove(overlay);

        if (overlay != null)
        {
            overlay.SetActive(false);
            Destroy(overlay);
        }

        overlay = null;
    }

    private void ClearAllOverlays()
    {
        for (int i = activeOverlays.Count - 1; i >= 0; i--)
        {
            if (activeOverlays[i] != null)
            {
                activeOverlays[i].SetActive(false);
                Destroy(activeOverlays[i]);
            }
        }

        activeOverlays.Clear();
    }

    private void CleanupNullOverlays()
    {
        for (int i = activeOverlays.Count - 1; i >= 0; i--)
        {
            if (activeOverlays[i] == null)
                activeOverlays.RemoveAt(i);
        }
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;

        for (int i = 0; i < target.transform.childCount; i++)
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
    }

    private void PlayAnimationTrigger(string triggerName)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(triggerName))
            return;

        animator.SetTrigger(triggerName);
    }

    private void Log(string message)
    {
        if (!enableDebugLog)
            return;

        Debug.Log("[AngelBoss] " + message);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Camera cam = battleCamera != null ? battleCamera : Camera.main;
        if (cam == null)
            return;

        if (TryBuildBeamAreas(out List<RectAttackArea> beamAreas))
        {
            for (int i = 0; i < beamAreas.Count; i++)
            {
                Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.2f);
                Gizmos.DrawCube(beamAreas[i].center, beamAreas[i].size);
            }
        }

        if (TryGetBattleBounds(out BattleBounds bounds))
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.2f);
            Gizmos.DrawWireCube(
                new Vector3(bounds.Center.x, bounds.Center.y, 0f),
                new Vector3(bounds.Width, bounds.Height, 1f)
            );
        }
    }
#endif
}
