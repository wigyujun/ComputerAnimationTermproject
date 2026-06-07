using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeepSeaBoss : BossBase
{
    [Header("Boss Sorting")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private string bossSortingLayerName = "Enemy";
    [SerializeField] private int bossSortingOrder = 100;

    [Header("Shared Pattern Timing")]
    [SerializeField] private float firstPatternDelay = 1.2f;
    [SerializeField] private float patternCooldown = 2.0f;

    [Header("Phase 1 / Dual Tentacle Strike")]
    [SerializeField] private float tentacleWarningDuration = 1.2f;
    [SerializeField] private float tentacleHitFlashDuration = 0.25f;
    [SerializeField] private float intervalBetweenTentacleAttacks = 0.2f;
    [SerializeField] private int laneCount = 6;
    [SerializeField] private int tentacleDamage = 1;
    [SerializeField] private int dualTentacleAttackCount = 6;
    [SerializeField] private bool avoidSameTentaclePairInARow = false;

    [Header("Phase 2 / Moving Wave Sweep")]
    [SerializeField] private int waveAttackCount = 2;
    [SerializeField] private float waveWarningDuration = 1.0f;
    [SerializeField] private float waveMoveDuration = 0.9f;
    [SerializeField] private float waveHitFlashDuration = 0.15f;
    [SerializeField] private float intervalBetweenWaveAttacks = 0.25f;
    [SerializeField] private int waveDamage = 1;
    [SerializeField] private bool avoidSameWaveAreaInARow = true;
    [SerializeField] private bool avoidSameWaveDirectionInARow = false;
    [SerializeField] private float minWaveAreaHeight = 0.75f;
    [SerializeField] private float waveWidth = 1.8f;
    [SerializeField] private float waveSpawnExtraMargin = 0.75f;
    [SerializeField] private GameObject waveVisualPrefab;
    [SerializeField] private Color waveVisualColor = new Color(0.25f, 0.85f, 1f, 0.85f);
    [SerializeField] private bool allowTelegraphFallbackForWaveVisual = false;

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
    [SerializeField] private string tentacleTriggerName = "Tentacle";
    [SerializeField] private string waveTriggerName = "Wave";
    [SerializeField] private bool enableDebugLog = true;

    private const string TELEGRAPH_SORTING_LAYER = "Enemy";

    private readonly List<GameObject> activeOverlays = new List<GameObject>();

    private bool firstPatternPlayed = false;
    private int currentBossPhase = 1;
    private int patternRunToken = 0;

    private Collider2D bodyCollider;

    private struct LaneArea
    {
        public Vector2 center;
        public Vector2 size;
        public int index;

        public LaneArea(Vector2 center, Vector2 size, int index)
        {
            this.center = center;
            this.size = size;
            this.index = index;
        }
    }

    private struct SweepArea
    {
        public Vector2 center;
        public Vector2 size;
        public bool isUpper;

        public SweepArea(Vector2 center, Vector2 size, bool isUpper)
        {
            this.center = center;
            this.size = size;
            this.isUpper = isUpper;
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
        currentBossPhase = newPhase;
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
            yield return RunDualTentaclePattern(runToken);

            if (!IsPatternRunValid(runToken))
                yield break;

            if (currentBossPhase >= 2)
            {
                yield return RunWaveSweepPattern(runToken);

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

    private IEnumerator RunDualTentaclePattern(int runToken)
    {
        if (!TryBuildLaneAreas(out List<LaneArea> laneAreas))
        {
            Log("RunDualTentaclePattern skipped / failed to build lane areas");
            yield break;
        }

        if (laneAreas.Count < 2)
        {
            Log("RunDualTentaclePattern skipped / lane count < 2");
            yield break;
        }

        int previousA = -1;
        int previousB = -1;

        for (int attackIndex = 0; attackIndex < dualTentacleAttackCount; attackIndex++)
        {
            if (!IsPatternRunValid(runToken))
                yield break;

            GetRandomLanePair(
                laneAreas.Count,
                previousA,
                previousB,
                out int firstIndex,
                out int secondIndex
            );

            previousA = firstIndex;
            previousB = secondIndex;

            PlayAnimationTrigger(tentacleTriggerName);

            yield return DoDualTentacleAttack(
                laneAreas[firstIndex],
                laneAreas[secondIndex],
                attackIndex + 1,
                runToken
            );

            if (!IsPatternRunValid(runToken))
                yield break;

            if (attackIndex < dualTentacleAttackCount - 1 && intervalBetweenTentacleAttacks > 0f)
                yield return new WaitForSeconds(intervalBetweenTentacleAttacks);
        }
    }

    private IEnumerator RunWaveSweepPattern(int runToken)
    {
        if (!TryBuildWaveAreas(out SweepArea upperArea, out SweepArea lowerArea))
        {
            Log("RunWaveSweepPattern skipped / failed to build wave areas");
            yield break;
        }

        bool? previousWasUpper = null;
        bool? previousLeftToRight = null;

        for (int attackIndex = 0; attackIndex < waveAttackCount; attackIndex++)
        {
            if (!IsPatternRunValid(runToken))
                yield break;

            SweepArea selectedArea = GetRandomWaveArea(upperArea, lowerArea, previousWasUpper, out bool isUpper);
            bool leftToRight = GetRandomWaveDirection(previousLeftToRight);

            previousWasUpper = isUpper;
            previousLeftToRight = leftToRight;

            PlayAnimationTrigger(waveTriggerName);

            yield return DoMovingWaveAttack(selectedArea, leftToRight, attackIndex + 1, runToken);

            if (!IsPatternRunValid(runToken))
                yield break;

            if (attackIndex < waveAttackCount - 1 && intervalBetweenWaveAttacks > 0f)
                yield return new WaitForSeconds(intervalBetweenWaveAttacks);
        }
    }

    private IEnumerator DoDualTentacleAttack(LaneArea firstArea, LaneArea secondArea, int attackIndex, int runToken)
    {
        GameObject warningA = null;
        GameObject warningB = null;
        GameObject hitA = null;
        GameObject hitB = null;

        try
        {
            if (!IsPatternRunValid(runToken))
                yield break;

            warningA = SpawnOverlay(
                firstArea.center,
                firstArea.size,
                warningColor,
                $"TentacleWarning_{attackIndex}_{firstArea.index}"
            );

            warningB = SpawnOverlay(
                secondArea.center,
                secondArea.size,
                warningColor,
                $"TentacleWarning_{attackIndex}_{secondArea.index}"
            );

            Log($"Dual tentacle warning / attack={attackIndex} / pair=({firstArea.index}, {secondArea.index})");

            if (tentacleWarningDuration > 0f)
                yield return new WaitForSeconds(tentacleWarningDuration);

            if (!IsPatternRunValid(runToken))
                yield break;

            ReleaseOverlay(ref warningA);
            ReleaseOverlay(ref warningB);

            ExecuteBoxDamage(
                firstArea.center,
                firstArea.size,
                tentacleDamage,
                $"Player hit by tentacle / lane={firstArea.index}"
            );

            ExecuteBoxDamage(
                secondArea.center,
                secondArea.size,
                tentacleDamage,
                $"Player hit by tentacle / lane={secondArea.index}"
            );

            hitA = SpawnOverlay(
                firstArea.center,
                firstArea.size,
                hitColor,
                $"TentacleHit_{attackIndex}_{firstArea.index}"
            );

            hitB = SpawnOverlay(
                secondArea.center,
                secondArea.size,
                hitColor,
                $"TentacleHit_{attackIndex}_{secondArea.index}"
            );

            Log($"Dual tentacle hit / attack={attackIndex} / pair=({firstArea.index}, {secondArea.index})");

            if (tentacleHitFlashDuration > 0f)
                yield return new WaitForSeconds(tentacleHitFlashDuration);
        }
        finally
        {
            ReleaseOverlay(ref warningA);
            ReleaseOverlay(ref warningB);
            ReleaseOverlay(ref hitA);
            ReleaseOverlay(ref hitB);
        }
    }

    private IEnumerator DoMovingWaveAttack(SweepArea area, bool leftToRight, int attackIndex, int runToken)
    {
        GameObject warning = null;
        GameObject waveVisual = null;

        try
        {
            if (!IsPatternRunValid(runToken))
                yield break;

            string areaName = area.isUpper ? "Upper" : "Lower";
            string dirName = leftToRight ? "LtoR" : "RtoL";

            warning = SpawnOverlay(
                area.center,
                area.size,
                warningColor,
                $"WaveWarning_{attackIndex}_{areaName}_{dirName}"
            );

            Log($"Wave warning / attack={attackIndex} / area={areaName} / dir={dirName}");

            if (waveWarningDuration > 0f)
                yield return new WaitForSeconds(waveWarningDuration);

            if (!IsPatternRunValid(runToken))
                yield break;

            ReleaseOverlay(ref warning);

            Vector2 waveSize = new Vector2(Mathf.Max(0.1f, waveWidth), area.size.y);

            Vector3 startPos = GetWaveStartPosition(area, waveSize.x, leftToRight);
            Vector3 endPos = GetWaveEndPosition(area, waveSize.x, leftToRight);

            waveVisual = SpawnWaveVisual(
                startPos,
                waveSize,
                waveVisualColor,
                $"WaveBody_{attackIndex}_{areaName}_{dirName}"
            );

            HashSet<Health> damagedTargetsThisWave = new HashSet<Health>();

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.0001f, waveMoveDuration);

            while (elapsed < safeDuration)
            {
                if (!IsPatternRunValid(runToken))
                    yield break;

                float t = Mathf.Clamp01(elapsed / safeDuration);
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);

                if (waveVisual != null)
                    waveVisual.transform.position = currentPos;

                ExecuteBoxDamage(
                    currentPos,
                    waveSize,
                    waveDamage,
                    $"Player hit by moving wave / area={areaName} / dir={dirName}",
                    damagedTargetsThisWave
                );

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!IsPatternRunValid(runToken))
                yield break;

            if (waveVisual != null)
                waveVisual.transform.position = endPos;

            ExecuteBoxDamage(
                endPos,
                waveSize,
                waveDamage,
                $"Player hit by moving wave / area={areaName} / dir={dirName} / final",
                damagedTargetsThisWave
            );

            Log($"Wave sweep finished / attack={attackIndex} / area={areaName} / dir={dirName}");

            if (waveHitFlashDuration > 0f)
                yield return new WaitForSeconds(waveHitFlashDuration);
        }
        finally
        {
            ReleaseOverlay(ref warning);
            ReleaseOverlay(ref waveVisual);
        }
    }

    private bool TryBuildLaneAreas(out List<LaneArea> laneAreas)
    {
        laneAreas = new List<LaneArea>();

        if (!TryGetBattleBounds(out BattleBounds bounds))
        {
            Log("TryBuildLaneAreas failed / no battle bounds");
            return false;
        }

        if (laneCount <= 0)
        {
            Log("TryBuildLaneAreas failed / laneCount <= 0");
            return false;
        }

        float laneWidth = bounds.Width / laneCount;
        if (laneWidth <= 0f)
        {
            Log("TryBuildLaneAreas failed / laneWidth <= 0");
            return false;
        }

        for (int i = 0; i < laneCount; i++)
        {
            float minX = bounds.minX + laneWidth * i;
            float maxX = minX + laneWidth;

            Vector2 center = new Vector2((minX + maxX) * 0.5f, bounds.Center.y);
            Vector2 size = new Vector2(laneWidth, bounds.Height);

            laneAreas.Add(new LaneArea(center, size, i));
        }

        return true;
    }

    private bool TryBuildWaveAreas(out SweepArea upperArea, out SweepArea lowerArea)
    {
        upperArea = default;
        lowerArea = default;

        if (!TryGetBattleBounds(out BattleBounds bounds))
        {
            Log("TryBuildWaveAreas failed / no battle bounds");
            return false;
        }

        float totalHeight = bounds.Height;
        float halfHeight = totalHeight * 0.5f;

        if (halfHeight < minWaveAreaHeight)
        {
            Log($"TryBuildWaveAreas failed / halfHeight={halfHeight:F2}, minWaveAreaHeight={minWaveAreaHeight:F2}");
            return false;
        }

        float centerX = bounds.Center.x;
        float width = bounds.Width;

        Vector2 lowerCenter = new Vector2(centerX, bounds.minY + halfHeight * 0.5f);
        Vector2 upperCenter = new Vector2(centerX, bounds.minY + halfHeight + halfHeight * 0.5f);
        Vector2 size = new Vector2(width, halfHeight);

        lowerArea = new SweepArea(lowerCenter, size, false);
        upperArea = new SweepArea(upperCenter, size, true);

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

    private void GetRandomLanePair(int totalCount, int prevA, int prevB, out int firstIndex, out int secondIndex)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int i = 0; i < totalCount; i++)
        {
            for (int j = i + 1; j < totalCount; j++)
            {
                if (avoidSameTentaclePairInARow)
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

    private SweepArea GetRandomWaveArea(SweepArea upperArea, SweepArea lowerArea, bool? previousWasUpper, out bool isUpper)
    {
        if (!avoidSameWaveAreaInARow || previousWasUpper == null)
        {
            isUpper = Random.value < 0.5f;
            return isUpper ? upperArea : lowerArea;
        }

        isUpper = !previousWasUpper.Value;
        return isUpper ? upperArea : lowerArea;
    }

    private bool GetRandomWaveDirection(bool? previousLeftToRight)
    {
        if (!avoidSameWaveDirectionInARow || previousLeftToRight == null)
            return Random.value < 0.5f;

        return !previousLeftToRight.Value;
    }

    private Vector3 GetWaveStartPosition(SweepArea area, float currentWaveWidth, bool leftToRight)
    {
        float halfAreaWidth = area.size.x * 0.5f;
        float halfWaveWidth = currentWaveWidth * 0.5f;

        float x = leftToRight
            ? area.center.x - halfAreaWidth - halfWaveWidth - waveSpawnExtraMargin
            : area.center.x + halfAreaWidth + halfWaveWidth + waveSpawnExtraMargin;

        return new Vector3(x, area.center.y, 0f);
    }

    private Vector3 GetWaveEndPosition(SweepArea area, float currentWaveWidth, bool leftToRight)
    {
        float halfAreaWidth = area.size.x * 0.5f;
        float halfWaveWidth = currentWaveWidth * 0.5f;

        float x = leftToRight
            ? area.center.x + halfAreaWidth + halfWaveWidth + waveSpawnExtraMargin
            : area.center.x - halfAreaWidth - halfWaveWidth - waveSpawnExtraMargin;

        return new Vector3(x, area.center.y, 0f);
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

    private GameObject SpawnOverlay(Vector2 center, Vector2 size, Color color, string objectName)
    {
        return SpawnSizedVisual(
            telegraphPrefab,
            center,
            size,
            color,
            objectName,
            telegraphSortingOrder
        );
    }

    private GameObject SpawnWaveVisual(Vector2 center, Vector2 size, Color color, string objectName)
    {
        GameObject prefabToUse = waveVisualPrefab;

        if (prefabToUse == null)
        {
            if (!allowTelegraphFallbackForWaveVisual)
            {
                Log($"SpawnWaveVisual skipped / waveVisualPrefab is NULL / object={objectName}");
                return null;
            }

            prefabToUse = telegraphPrefab;
        }

        return SpawnSizedVisual(
            prefabToUse,
            center,
            size,
            color,
            objectName,
            telegraphSortingOrder + 1
        );
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
            Log("ApplyVisualRenderSettings / no SpriteRenderer found");
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

    private void ExecuteBoxDamage(
        Vector2 center,
        Vector2 size,
        int damage,
        string logMessage,
        HashSet<Health> alreadyDamagedTargets = null)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        if (hits == null || hits.Length == 0)
            return;

        HashSet<Health> localSet = alreadyDamagedTargets ?? new HashSet<Health>();

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

            if (localSet.Contains(playerHp))
                continue;

            localSet.Add(playerHp);
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

        Debug.Log("[DeepSeaBoss] " + message);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Camera cam = battleCamera != null ? battleCamera : Camera.main;
        if (cam == null)
            return;

        if (TryBuildLaneAreas(out List<LaneArea> laneAreas))
        {
            for (int i = 0; i < laneAreas.Count; i++)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
                Gizmos.DrawCube(laneAreas[i].center, laneAreas[i].size);
            }
        }

        if (TryBuildWaveAreas(out SweepArea upperArea, out SweepArea lowerArea))
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.15f);
            Gizmos.DrawCube(upperArea.center, upperArea.size);

            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.15f);
            Gizmos.DrawCube(lowerArea.center, lowerArea.size);
        }

        if (TryGetBattleBounds(out BattleBounds bounds))
        {
            Gizmos.color = new Color(0.1f, 0.5f, 1f, 0.25f);
            Gizmos.DrawWireCube(
                new Vector3(bounds.Center.x, bounds.Center.y, 0f),
                new Vector3(bounds.Width, bounds.Height, 1f)
            );
        }
    }
#endif
}
