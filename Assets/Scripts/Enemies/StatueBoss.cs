using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatueBoss : BossBase
{
    [Header("Shared Pattern Timing")]
    [SerializeField] private float firstPatternDelay = 1.2f;
    [SerializeField] private float patternCooldown = 2.0f;

    [Header("Phase 1 / Dual Column Slam")]
    [SerializeField] private float columnWarningDuration = 1.2f;
    [SerializeField] private float columnHitFlashDuration = 0.25f;
    [SerializeField] private float intervalBetweenColumns = 0.2f;
    [SerializeField] private int columnCount = 6;
    [SerializeField] private int slamDamage = 1;
    [SerializeField] private int dualColumnAttackCount = 6;
    [SerializeField] private int gapBetweenHitColumns = 2;
    [SerializeField] private bool avoidSamePairInARow = true;

    [Header("Phase 2 / Rock Throw")]
    [SerializeField] private int rockThrowCount = 4;
    [SerializeField] private float rockWarningDuration = 1.0f;
    [SerializeField] private float rockHitFlashDuration = 0.25f;
    [SerializeField] private float intervalBetweenRocks = 0.2f;
    [SerializeField] private int rockDamage = 1;
    [SerializeField] private Vector2 rockImpactSize = new Vector2(1.4f, 1.4f);
    [SerializeField] private float rockSpawnHeightOffset = 2.5f;
    [SerializeField] private float rockFallTravelTime = 0.35f;
    [SerializeField] private GameObject rockVisualPrefab;

    [Header("Battle Area")]
    [SerializeField] private float sidePadding = 0.15f;
    [SerializeField] private float bottomPadding = 0.15f;
    [SerializeField] private float gapBelowBoss = 0.2f;
    [SerializeField] private float minAttackHeight = 1.0f;

    [Header("Telegraph Visual")]
    [SerializeField] private GameObject telegraphPrefab;
    [SerializeField] private Color warningColor = new Color(1f, 0f, 0f, 0.75f);
    [SerializeField] private Color hitColor = new Color(1f, 1f, 0f, 0.9f);
    [SerializeField] private string telegraphObjectLayerName = "Default";
    [SerializeField] private int telegraphSortingOrder = 1000;

    [Header("Animation / Debug")]
    [SerializeField] private Animator animator;
    [SerializeField] private string slamTriggerName = "Slam";
    [SerializeField] private string throwRockTriggerName = "ThrowRock";
    [SerializeField] private bool enableDebugLog = true;

    private const string TELEGRAPH_SORTING_LAYER = "Enemy";

    private Collider2D bodyCollider;
    private Renderer bodyRenderer;

    private readonly List<GameObject> activeOverlays = new List<GameObject>();
    private int currentBossPhase = 1;

    private struct ColumnArea
    {
        public Vector2 center;
        public Vector2 size;
        public int index;

        public ColumnArea(Vector2 center, Vector2 size, int index)
        {
            this.center = center;
            this.size = size;
            this.index = index;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        bodyCollider = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();
        bodyRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
    }

    protected override void OnBattleStarted()
    {
        currentBossPhase = 1;
        Log("Battle started");
    }

    protected override void OnEnteredBattleArea()
    {
        Log("Entered battle area");
    }

    protected override void OnPhaseChanged(int newPhase)
    {
        base.OnPhaseChanged(newPhase);
        currentBossPhase = newPhase;
        Log($"Phase changed -> {newPhase}");
    }

    protected override void OnFinalDeath()
    {
        ClearAllOverlays();
        base.OnFinalDeath();
        Log("Final death");
    }

    protected override IEnumerator RunBossPattern()
    {
        if (firstPatternDelay > 0f)
            yield return new WaitForSeconds(firstPatternDelay);

        while (!FinalDeathHandled)
        {
            // 1페이즈: 기믹 1만
            yield return StartCoroutine(RunColumnSlamPattern());

            if (FinalDeathHandled)
                yield break;

            // 2페이즈: 기믹 1 + 기믹 2
            if (currentBossPhase >= 2)
            {
                yield return StartCoroutine(RunRockThrowPattern());

                if (FinalDeathHandled)
                    yield break;
            }

            if (patternCooldown > 0f)
                yield return new WaitForSeconds(patternCooldown);
        }
    }

    private IEnumerator RunColumnSlamPattern()
    {
        if (!TryBuildColumnAreas(out List<ColumnArea> columnAreas))
        {
            yield return new WaitForSeconds(1f);
            yield break;
        }

        int pairOffset = gapBetweenHitColumns + 1;

        if (columnAreas.Count < pairOffset + 1)
        {
            Log($"Column slam pattern failed / columnCount={columnAreas.Count}, pairOffset={pairOffset}");
            yield break;
        }

        int previousStartIndex = -1;
        Log("Dual column slam pattern start");

        for (int attackIndex = 0; attackIndex < dualColumnAttackCount; attackIndex++)
        {
            if (FinalDeathHandled)
                yield break;

            int firstIndex = GetRandomDualColumnStartIndex(columnAreas.Count, pairOffset, previousStartIndex);
            int secondIndex = firstIndex + pairOffset;

            previousStartIndex = firstIndex;

            yield return StartCoroutine(DoDualColumnSlam(
                columnAreas[firstIndex],
                columnAreas[secondIndex],
                attackIndex + 1
            ));

            if (intervalBetweenColumns > 0f && attackIndex < dualColumnAttackCount - 1)
                yield return new WaitForSeconds(intervalBetweenColumns);
        }
    }

    private IEnumerator RunRockThrowPattern()
    {
        Log("Rock throw pattern start");

        for (int i = 0; i < rockThrowCount; i++)
        {
            if (FinalDeathHandled)
                yield break;

            if (!TryGetBattleBounds(out float minX, out float maxX, out float minY, out float maxY))
            {
                yield return new WaitForSeconds(1f);
                yield break;
            }

            Vector2 targetCenter = GetRandomRockTargetCenter(minX, maxX, minY, maxY);
            yield return StartCoroutine(DoSingleRockThrow(targetCenter, i + 1));

            if (intervalBetweenRocks > 0f && i < rockThrowCount - 1)
                yield return new WaitForSeconds(intervalBetweenRocks);
        }
    }

    private IEnumerator DoDualColumnSlam(ColumnArea firstArea, ColumnArea secondArea, int attackIndex)
    {
        PlayAnimationTrigger(slamTriggerName);

        GameObject warningA = SpawnOverlay(firstArea.center, firstArea.size, warningColor);
        GameObject warningB = SpawnOverlay(secondArea.center, secondArea.size, warningColor);

        Log($"Dual column warning / attack={attackIndex} / pair=({firstArea.index}, {secondArea.index})");

        if (columnWarningDuration > 0f)
            yield return new WaitForSeconds(columnWarningDuration);

        if (warningA != null)
        {
            activeOverlays.Remove(warningA);
            Destroy(warningA);
        }

        if (warningB != null)
        {
            activeOverlays.Remove(warningB);
            Destroy(warningB);
        }

        ExecuteBoxDamage(firstArea.center, firstArea.size, slamDamage,
            $"Player hit by dual column slam / column={firstArea.index}");

        ExecuteBoxDamage(secondArea.center, secondArea.size, slamDamage,
            $"Player hit by dual column slam / column={secondArea.index}");

        GameObject hitA = SpawnOverlay(firstArea.center, firstArea.size, hitColor);
        GameObject hitB = SpawnOverlay(secondArea.center, secondArea.size, hitColor);

        Log($"Dual column hit / attack={attackIndex} / pair=({firstArea.index}, {secondArea.index})");

        if (columnHitFlashDuration > 0f)
            yield return new WaitForSeconds(columnHitFlashDuration);

        if (hitA != null)
        {
            activeOverlays.Remove(hitA);
            Destroy(hitA);
        }

        if (hitB != null)
        {
            activeOverlays.Remove(hitB);
            Destroy(hitB);
        }
    }

    private IEnumerator DoSingleRockThrow(Vector2 targetCenter, int throwIndex)
    {
        PlayAnimationTrigger(throwRockTriggerName);

        GameObject warning = SpawnOverlay(targetCenter, rockImpactSize, warningColor);
        Log($"Rock warning / index={throwIndex} / target={targetCenter}");

        float preFallDelay = Mathf.Max(0f, rockWarningDuration - rockFallTravelTime);
        if (preFallDelay > 0f)
            yield return new WaitForSeconds(preFallDelay);

        if (rockVisualPrefab != null && rockFallTravelTime > 0f)
        {
            yield return StartCoroutine(PlayRockFallVisual(targetCenter));
        }
        else
        {
            float remain = rockWarningDuration - preFallDelay;
            if (remain > 0f)
                yield return new WaitForSeconds(remain);
        }

        if (warning != null)
        {
            activeOverlays.Remove(warning);
            Destroy(warning);
        }

        ExecuteBoxDamage(targetCenter, rockImpactSize, rockDamage, $"Player hit by rock / index={throwIndex}");

        GameObject hitFlash = SpawnOverlay(targetCenter, rockImpactSize, hitColor);
        Log($"Rock hit / index={throwIndex}");

        if (rockHitFlashDuration > 0f)
            yield return new WaitForSeconds(rockHitFlashDuration);

        if (hitFlash != null)
        {
            activeOverlays.Remove(hitFlash);
            Destroy(hitFlash);
        }
    }

    private IEnumerator PlayRockFallVisual(Vector2 targetCenter)
    {
        if (rockVisualPrefab == null)
            yield break;

        Vector3 startPos = new Vector3(targetCenter.x, targetCenter.y + rockSpawnHeightOffset, 0f);
        Vector3 endPos = new Vector3(targetCenter.x, targetCenter.y, 0f);

        GameObject rock = Instantiate(rockVisualPrefab, startPos, Quaternion.identity);

        SpriteRenderer[] renderers = rock.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = TELEGRAPH_SORTING_LAYER;
            renderers[i].sortingOrder = telegraphSortingOrder + 1;
        }

        float timer = 0f;
        while (timer < rockFallTravelTime)
        {
            if (rock == null)
                yield break;

            float t = timer / Mathf.Max(rockFallTravelTime, 0.0001f);
            rock.transform.position = Vector3.Lerp(startPos, endPos, t);

            timer += Time.deltaTime;
            yield return null;
        }

        if (rock != null)
        {
            rock.transform.position = endPos;
            Destroy(rock);
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

    private bool TryBuildColumnAreas(out List<ColumnArea> columnAreas)
    {
        columnAreas = new List<ColumnArea>();

        if (!TryGetBattleBounds(out float minX, out float maxX, out float minY, out float maxY))
            return false;

        if (columnCount <= 0)
        {
            Log("BuildColumnAreas failed / columnCount <= 0");
            return false;
        }

        float totalWidth = maxX - minX;
        float totalHeight = maxY - minY;

        if (totalWidth <= 0f)
        {
            Log($"BuildColumnAreas failed / invalid width = {totalWidth:F2}");
            return false;
        }

        if (totalHeight < minAttackHeight)
        {
            Log(
                $"BuildColumnAreas failed / minY={minY:F2}, maxY={maxY:F2}, totalHeight={totalHeight:F2}, " +
                $"bossBottom={GetBossBottomY():F2}, minRequired={minAttackHeight:F2}"
            );
            return false;
        }

        float columnWidth = totalWidth / columnCount;
        float centerY = minY + totalHeight * 0.5f;

        for (int i = 0; i < columnCount; i++)
        {
            float centerX = minX + columnWidth * i + columnWidth * 0.5f;

            columnAreas.Add(new ColumnArea(
                new Vector2(centerX, centerY),
                new Vector2(columnWidth, totalHeight),
                i
            ));
        }

        return true;
    }

    private bool TryGetBattleBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = 0f;
        maxX = 0f;
        minY = 0f;
        maxY = 0f;

        Camera cam = battleCamera != null ? battleCamera : Camera.main;
        if (cam == null)
        {
            Log("BuildBounds failed / camera is null");
            return false;
        }

        float spawnZ = transform.position.z;
        float distanceFromCamera = Mathf.Abs(spawnZ - cam.transform.position.z);

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, distanceFromCamera));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distanceFromCamera));

        minX = bottomLeft.x + sidePadding;
        maxX = topRight.x - sidePadding;
        minY = bottomLeft.y + bottomPadding;
        maxY = GetBossBottomY() - gapBelowBoss;

        if (maxX <= minX || maxY <= minY)
        {
            Log($"BuildBounds failed / minX={minX:F2}, maxX={maxX:F2}, minY={minY:F2}, maxY={maxY:F2}");
            return false;
        }

        return true;
    }

    private Vector2 GetRandomRockTargetCenter(float minX, float maxX, float minY, float maxY)
    {
        float halfW = rockImpactSize.x * 0.5f;
        float halfH = rockImpactSize.y * 0.5f;

        float targetX = Random.Range(minX + halfW, maxX - halfW);
        float targetY = Random.Range(minY + halfH, maxY - halfH);

        return new Vector2(targetX, targetY);
    }

    private int GetRandomDualColumnStartIndex(int totalColumnCount, int pairOffset, int previousStartIndex)
    {
        List<int> candidates = new List<int>();

        for (int i = 0; i + pairOffset < totalColumnCount; i++)
            candidates.Add(i);

        if (avoidSamePairInARow && previousStartIndex >= 0 && candidates.Count > 1)
            candidates.Remove(previousStartIndex);

        int picked = candidates[Random.Range(0, candidates.Count)];
        return picked;
    }

    private float GetBossBottomY()
    {
        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();

        if (bodyRenderer == null)
            bodyRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();

        if (bodyCollider != null)
            return bodyCollider.bounds.min.y;

        if (bodyRenderer != null)
            return bodyRenderer.bounds.min.y;

        return transform.position.y;
    }

    private GameObject SpawnOverlay(Vector2 center, Vector2 size, Color color)
    {
        if (telegraphPrefab == null)
        {
            Log("telegraphPrefab is NULL");
            return null;
        }

        Vector3 spawnPos = new Vector3(center.x, center.y, 0f);
        GameObject overlay = Instantiate(telegraphPrefab, spawnPos, Quaternion.identity);

        int objectLayer = LayerMask.NameToLayer(telegraphObjectLayerName);
        if (objectLayer != -1)
            SetLayerRecursively(overlay, objectLayer);

        SpriteRenderer[] renderers = overlay.GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            Log("telegraphPrefab has no SpriteRenderer");
            overlay.transform.localScale = new Vector3(size.x, size.y, 1f);
            activeOverlays.Add(overlay);
            return overlay;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            sr.enabled = true;
            sr.color = color;
            sr.sortingLayerName = TELEGRAPH_SORTING_LAYER;
            sr.sortingOrder = telegraphSortingOrder;
        }

        SpriteRenderer rootSr = renderers[0];

        if (rootSr.drawMode == SpriteDrawMode.Simple)
        {
            float spriteWidth = rootSr.sprite != null ? rootSr.sprite.bounds.size.x : 1f;
            float spriteHeight = rootSr.sprite != null ? rootSr.sprite.bounds.size.y : 1f;

            overlay.transform.localScale = new Vector3(
                size.x / Mathf.Max(spriteWidth, 0.0001f),
                size.y / Mathf.Max(spriteHeight, 0.0001f),
                1f
            );
        }
        else
        {
            overlay.transform.localScale = Vector3.one;
            rootSr.size = size;
        }

        activeOverlays.Add(overlay);
        return overlay;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;

        for (int i = 0; i < target.transform.childCount; i++)
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
    }

    private void ClearAllOverlays()
    {
        for (int i = activeOverlays.Count - 1; i >= 0; i--)
        {
            if (activeOverlays[i] != null)
                Destroy(activeOverlays[i]);
        }

        activeOverlays.Clear();
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

        Debug.Log("[StatueBoss] " + message);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Camera cam = battleCamera != null ? battleCamera : Camera.main;
        if (cam == null)
            return;

        if (TryBuildColumnAreas(out List<ColumnArea> columnAreas))
        {
            for (int i = 0; i < columnAreas.Count; i++)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
                Gizmos.DrawCube(columnAreas[i].center, columnAreas[i].size);
            }
        }

        if (TryGetBattleBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.15f);
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif
}
