using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharkBoss : BossBase
{
    [Header("Pattern Timing")]
    [SerializeField] private float firstPatternDelay = 1.2f;
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float hitFlashDuration = 0.25f;
    [SerializeField] private float intervalBetweenUpperLower = 0.4f;
    [SerializeField] private float patternCooldown = 2.2f;

    [Header("Sweep Damage")]
    [SerializeField] private int sweepDamage = 1;

    [Header("Sweep Area")]
    [SerializeField] private float sidePadding = 0.2f;
    [SerializeField] private float bottomPadding = 0.2f;
    [SerializeField] private float gapBelowBoss = 0.2f;
    [SerializeField] private float minSweepHeight = 0.8f;

    [Header("Telegraph Visual")]
    [SerializeField] private GameObject telegraphPrefab;
    [SerializeField] private Color warningColor = new Color(1f, 0f, 0f, 0.75f);
    [SerializeField] private Color hitColor = new Color(1f, 1f, 0f, 0.9f);
    [SerializeField] private string telegraphObjectLayerName = "Default";
    [SerializeField] private int telegraphSortingOrder = 1000;

    [Header("Animation / Debug")]
    [SerializeField] private Animator animator;
    [SerializeField] private string upperSweepTriggerName = "SweepUpper";
    [SerializeField] private string lowerSweepTriggerName = "SweepLower";
    [SerializeField] private bool enableDebugLog = true;

    private const string TELEGRAPH_SORTING_LAYER = "Enemy";

    private Collider2D bodyCollider;
    private Renderer bodyRenderer;

    private readonly List<GameObject> activeOverlays = new List<GameObject>();

    private struct SweepArea
    {
        public Vector2 center;
        public Vector2 size;

        public SweepArea(Vector2 center, Vector2 size)
        {
            this.center = center;
            this.size = size;
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
        Log("Battle started");
    }

    protected override void OnEnteredBattleArea()
    {
        Log("Entered battle area");
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
            if (!TryBuildSweepAreas(out SweepArea upperArea, out SweepArea lowerArea))
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            bool upperFirst = Random.value < 0.5f;

            SweepArea firstArea = upperFirst ? upperArea : lowerArea;
            SweepArea secondArea = upperFirst ? lowerArea : upperArea;

            bool firstIsUpper = upperFirst;
            bool secondIsUpper = !upperFirst;

            Log(upperFirst ? "Attack order = Upper -> Lower" : "Attack order = Lower -> Upper");

            yield return StartCoroutine(DoSweepAttack(firstArea, firstIsUpper));

            if (intervalBetweenUpperLower > 0f)
                yield return new WaitForSeconds(intervalBetweenUpperLower);

            yield return StartCoroutine(DoSweepAttack(secondArea, secondIsUpper));

            if (patternCooldown > 0f)
                yield return new WaitForSeconds(patternCooldown);
        }
    }

    private IEnumerator DoSweepAttack(SweepArea area, bool isUpper)
    {
        PlaySweepAnimation(isUpper);

        GameObject warning = SpawnOverlay(area, warningColor);
        Log(isUpper ? "Upper sweep warning" : "Lower sweep warning");

        if (warningDuration > 0f)
            yield return new WaitForSeconds(warningDuration);

        if (warning != null)
        {
            activeOverlays.Remove(warning);
            Destroy(warning);
        }

        ExecuteSweepDamage(area);

        GameObject hitFlash = SpawnOverlay(area, hitColor);
        Log(isUpper ? "Upper sweep hit" : "Lower sweep hit");

        if (hitFlashDuration > 0f)
            yield return new WaitForSeconds(hitFlashDuration);

        if (hitFlash != null)
        {
            activeOverlays.Remove(hitFlash);
            Destroy(hitFlash);
        }
    }

    private void ExecuteSweepDamage(SweepArea area)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(area.center, area.size, 0f);
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
            playerHp.TakeDamage(sweepDamage);
            Log("Player hit by shark sweep / damage=" + sweepDamage);
        }
    }

    private bool TryBuildSweepAreas(out SweepArea upperArea, out SweepArea lowerArea)
    {
        upperArea = default;
        lowerArea = default;

        Camera cam = battleCamera != null ? battleCamera : Camera.main;
        if (cam == null)
        {
            Log("Sweep area build failed / camera is null");
            return false;
        }

        float spawnZ = transform.position.z;
        float distanceFromCamera = Mathf.Abs(spawnZ - cam.transform.position.z);

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, distanceFromCamera));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distanceFromCamera));

        float minX = bottomLeft.x + sidePadding;
        float maxX = topRight.x - sidePadding;
        float minY = bottomLeft.y + bottomPadding;
        float maxY = GetBossBottomY() - gapBelowBoss;

        float totalHeight = maxY - minY;
        if (totalHeight < minSweepHeight * 2f)
        {
            Log(
                $"Sweep area build failed / minY={minY:F2}, maxY={maxY:F2}, totalHeight={totalHeight:F2}, " +
                $"bossBottom={GetBossBottomY():F2}, minRequired={(minSweepHeight * 2f):F2}"
            );
            return false;
        }

        float halfHeight = totalHeight * 0.5f;
        float width = maxX - minX;
        float centerX = (minX + maxX) * 0.5f;

        lowerArea = new SweepArea(
            new Vector2(centerX, minY + halfHeight * 0.5f),
            new Vector2(width, halfHeight)
        );

        upperArea = new SweepArea(
            new Vector2(centerX, minY + halfHeight + halfHeight * 0.5f),
            new Vector2(width, halfHeight)
        );

        return true;
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

    private GameObject SpawnOverlay(SweepArea area, Color color)
    {
        if (telegraphPrefab == null)
        {
            Log("telegraphPrefab is NULL");
            return null;
        }

        Vector3 spawnPos = new Vector3(area.center.x, area.center.y, 0f);
        GameObject overlay = Instantiate(telegraphPrefab, spawnPos, Quaternion.identity);

        int objectLayer = LayerMask.NameToLayer(telegraphObjectLayerName);
        if (objectLayer != -1)
            SetLayerRecursively(overlay, objectLayer);

        SpriteRenderer[] renderers = overlay.GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            Log("telegraphPrefab has no SpriteRenderer");
            overlay.transform.localScale = new Vector3(area.size.x, area.size.y, 1f);
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
                area.size.x / Mathf.Max(spriteWidth, 0.0001f),
                area.size.y / Mathf.Max(spriteHeight, 0.0001f),
                1f
            );
        }
        else
        {
            overlay.transform.localScale = Vector3.one;
            rootSr.size = area.size;
        }

        Log($"SpawnOverlay / sortingLayer={TELEGRAPH_SORTING_LAYER}, order={telegraphSortingOrder}, pos={spawnPos}, size={area.size}");

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

    private void PlaySweepAnimation(bool isUpper)
    {
        if (animator == null)
            return;

        string triggerName = isUpper ? upperSweepTriggerName : lowerSweepTriggerName;
        if (string.IsNullOrEmpty(triggerName))
            return;

        animator.SetTrigger(triggerName);
    }

    private void Log(string message)
    {
        if (!enableDebugLog)
            return;

        Debug.Log("[SharkBoss] " + message);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Camera cam = battleCamera != null ? battleCamera : Camera.main;
        if (cam == null)
            return;

        if (!TryBuildSweepAreas(out SweepArea upperArea, out SweepArea lowerArea))
            return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawCube(upperArea.center, upperArea.size);

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawCube(lowerArea.center, lowerArea.size);
    }
#endif
}
