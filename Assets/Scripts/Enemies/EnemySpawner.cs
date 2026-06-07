using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemyRoot;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointsRoot;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private int totalSpawnCount = 5;
    [SerializeField] private bool autoStartOnStart = false;

    [Header("Camera Clamp")]
    [SerializeField] private Camera battleCamera;
    [SerializeField] private bool clampNonChargeEnemiesInsideCamera = true;
    [SerializeField] private float visibleScreenPadding = 0.15f;

    [Header("Offscreen Spawn For Sky / Sea Enemies")]
    [SerializeField] private bool spawnAerialEnemiesAboveScreen = true;
    [SerializeField] private float topSpawnOffset = 0.8f;
    [SerializeField] private float offscreenHorizontalPadding = 0.35f;

    [Header("Forest Monkey Entry")]
    [SerializeField] private bool forceForestEnemiesSpawnFromTop = true;
    [SerializeField] private bool addVerticalMoverToForestEnemies = true;
    [SerializeField] private bool addBottomDespawnToForestEnemies = true;

    public bool IsSpawnFinished { get; private set; }
    public int SpawnedCount { get; private set; }
    public int TotalEnemiesToSpawn => totalSpawnCount;
    public int AliveCount { get; private set; }

    private Coroutine spawnRoutine;
    private bool isSpawning;

    private void Awake()
    {
        if (enemyRoot == null)
        {
            GameObject rootObj = GameObject.Find("EnemyRoot");
            if (rootObj != null)
                enemyRoot = rootObj.transform;
        }

        if (battleCamera == null)
            battleCamera = Camera.main;

        if (spawnPointsRoot != null && spawnPoints.Count == 0)
            CacheSpawnPointsFromRoot();
    }

    private void Start()
    {
        if (autoStartOnStart)
            BeginSpawn();
    }

    private void CacheSpawnPointsFromRoot()
    {
        spawnPoints.Clear();

        if (spawnPointsRoot == null)
            return;

        for (int i = 0; i < spawnPointsRoot.childCount; i++)
            spawnPoints.Add(spawnPointsRoot.GetChild(i));
    }

    public void SetEnemyPrefab(GameObject newEnemyPrefab)
    {
        enemyPrefab = newEnemyPrefab;
    }

    public GameObject GetEnemyPrefab()
    {
        return enemyPrefab;
    }

    public void SetSpawnPointsRoot(Transform newRoot)
    {
        spawnPointsRoot = newRoot;
        CacheSpawnPointsFromRoot();
    }

    public void SetSpawnInterval(float newInterval)
    {
        spawnInterval = Mathf.Max(0.05f, newInterval);
    }

    public void SetTotalSpawnCount(int newCount)
    {
        totalSpawnCount = Mathf.Max(0, newCount);
    }

    // 새 전투 시작 시 기존 적을 정리하고 스폰 카운터를 초기화한 뒤 코루틴을 시작한다.
    public void BeginSpawn()
    {
        Debug.Log("9) EnemySpawner.BeginSpawn 진입");

        if (isSpawning)
        {
            Debug.LogWarning("EnemySpawner: 이미 스폰 중");
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: enemyPrefab이 비어 있음");
            return;
        }

        Debug.Log("10) enemyPrefab = " + enemyPrefab.name);

        if ((spawnPoints == null || spawnPoints.Count == 0) && spawnPointsRoot != null)
            CacheSpawnPointsFromRoot();

        Debug.Log("11) spawnPoints 개수 = " + (spawnPoints == null ? 0 : spawnPoints.Count));

        if (enemyRoot == null)
        {
            Debug.LogWarning("EnemySpawner: enemyRoot가 비어 있어 transform으로 대체");
            enemyRoot = transform;
        }

        ClearAllEnemies();

        SpawnedCount = 0;
        AliveCount = 0;
        IsSpawnFinished = false;
        isSpawning = true;

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawn()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        isSpawning = false;
        IsSpawnFinished = true;
    }

    // 설정된 횟수만큼 일정 간격으로 적을 생성하는 메인 스폰 루프다.
    private IEnumerator SpawnRoutine()
    {
        for (int i = 0; i < totalSpawnCount; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
        IsSpawnFinished = true;

        Debug.Log("EnemySpawner: 모든 적 스폰 완료");
        DebugBattleState();
    }

    // 적 1마리를 실제 생성하고, 테마별 진입 방식/카메라 참조/밸런스 값을 연결한다.
    private void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("EnemySpawner: SpawnEnemy 실패 - enemyPrefab 또는 spawnPoints 문제");
            return;
        }

        if (battleCamera == null)
            battleCamera = Camera.main;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Vector3 spawnPosition = point.position;

        GameObject spawned = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemyRoot);
        spawned.name = enemyPrefab.name;

        bool hasChargeEnemy = HasChargeEnemy(spawned);
        bool hasVerticalMover = HasVerticalMover(spawned);
        bool hasBottomDespawn = HasBottomDespawn(spawned);

        bool forceForestTopSpawn = ShouldForceForestTopSpawn();

        if (forceForestTopSpawn && addVerticalMoverToForestEnemies && !hasChargeEnemy && !hasVerticalMover)
        {
            EnsureVerticalMover(spawned);
            hasVerticalMover = true;
        }

        if (forceForestTopSpawn && addBottomDespawnToForestEnemies && !hasBottomDespawn)
        {
            EnsureBottomDespawn(spawned);
            hasBottomDespawn = true;
        }

        bool shouldSpawnAboveScreen = forceForestTopSpawn || ShouldSpawnAboveScreen(hasChargeEnemy, hasVerticalMover);

        if (shouldSpawnAboveScreen)
        {
            PlaceSpawnedEnemyAboveScreen(spawned, point);
        }
        else if (clampNonChargeEnemiesInsideCamera)
        {
            ClampSpawnedEnemyInsideCamera(spawned);
        }

        WireCameraReferences(spawned);
        WireSpawnerReferences(spawned);
        FloorBalanceUtility.ApplyEnemyBalance(spawned, RunContext.CurrentFloor, RunContext.NextNodeType);

        SpawnedCount++;
        AliveCount++;

        EnemyController enemyController = spawned.GetComponent<EnemyController>();
        if (enemyController == null)
            enemyController = spawned.GetComponentInChildren<EnemyController>(true);

        if (enemyController != null)
        {
            enemyController.SetSpawner(this);
        }
        else
        {
            Debug.LogWarning($"EnemySpawner: {spawned.name} 에 EnemyController가 없음");
        }

        Debug.Log(
            $"[EnemySpawner] Spawned={SpawnedCount}, Alive={AliveCount}, " +
            $"Theme={RunContext.NextBattleTheme}, ForceForestTop={forceForestTopSpawn}, " +
            $"OffscreenTop={shouldSpawnAboveScreen}, Pos={spawned.transform.position}"
        );
    }

    public void NotifyEnemyRemoved()
    {
        AliveCount = Mathf.Max(0, AliveCount - 1);
        Debug.Log($"[EnemySpawner] Enemy removed -> AliveCount={AliveCount}");
    }

    private bool HasChargeEnemy(GameObject obj)
    {
        return obj.GetComponent<ChargeEnemy>() != null || obj.GetComponentInChildren<ChargeEnemy>(true) != null;
    }

    private bool HasVerticalMover(GameObject obj)
    {
        return obj.GetComponent<EnemyVerticalMover>() != null || obj.GetComponentInChildren<EnemyVerticalMover>(true) != null;
    }

    private bool HasBottomDespawn(GameObject obj)
    {
        return obj.GetComponent<EnemyBottomDespawn>() != null || obj.GetComponentInChildren<EnemyBottomDespawn>(true) != null;
    }

    private bool ShouldForceForestTopSpawn()
    {
        if (!forceForestEnemiesSpawnFromTop)
            return false;

        return RunContext.NextBattleTheme == ThemeType.Forest
            && RunContext.NextNodeType != NodeType.Boss;
    }

    private bool ShouldSpawnAboveScreen(bool hasChargeEnemy, bool hasVerticalMover)
    {
        if (!spawnAerialEnemiesAboveScreen)
            return false;

        return hasChargeEnemy || hasVerticalMover;
    }

    private void EnsureVerticalMover(GameObject obj)
    {
        if (obj == null)
            return;

        EnemyVerticalMover mover = obj.GetComponent<EnemyVerticalMover>();
        if (mover == null && obj.GetComponentInChildren<EnemyVerticalMover>(true) == null)
        {
            obj.AddComponent<EnemyVerticalMover>();
            Debug.Log($"[EnemySpawner] EnemyVerticalMover 자동 추가: {obj.name}");
        }
    }

    private void EnsureBottomDespawn(GameObject obj)
    {
        if (obj == null)
            return;

        EnemyBottomDespawn despawn = obj.GetComponent<EnemyBottomDespawn>();
        if (despawn == null && obj.GetComponentInChildren<EnemyBottomDespawn>(true) == null)
        {
            obj.AddComponent<EnemyBottomDespawn>();
            Debug.Log($"[EnemySpawner] EnemyBottomDespawn 자동 추가: {obj.name}");
        }
    }

    // 하늘/바다 적이나 숲 원숭이를 화면 위 바깥에서 시작시키기 위한 위치 보정 함수다.
    private void PlaceSpawnedEnemyAboveScreen(GameObject spawned, Transform point)
    {
        if (spawned == null)
            return;

        if (battleCamera == null)
            battleCamera = Camera.main;

        if (battleCamera == null)
            return;

        float objectZ = spawned.transform.position.z;
        float cameraZ = battleCamera.transform.position.z;
        float distanceFromCamera = Mathf.Abs(objectZ - cameraZ);

        Vector3 topLeft = battleCamera.ViewportToWorldPoint(new Vector3(0f, 1f, distanceFromCamera));
        Vector3 topRight = battleCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distanceFromCamera));

        float minX = Mathf.Min(topLeft.x, topRight.x) + offscreenHorizontalPadding;
        float maxX = Mathf.Max(topLeft.x, topRight.x) - offscreenHorizontalPadding;

        float spawnX = point != null ? point.position.x : spawned.transform.position.x;
        if (spawnX < minX || spawnX > maxX)
            spawnX = Random.Range(minX, maxX);

        float extraHeight = 0f;
        if (TryGetWorldBounds(spawned, out Bounds bounds))
            extraHeight = bounds.extents.y;

        float spawnY = topLeft.y + topSpawnOffset + extraHeight;
        spawned.transform.position = new Vector3(spawnX, spawnY, spawned.transform.position.z);
    }

    // 생성된 적이 카메라 기준 이동/소멸 판정을 할 수 있도록 참조를 전달한다.
    private void WireCameraReferences(GameObject spawned)
    {
        if (spawned == null)
            return;

        if (battleCamera == null)
            battleCamera = Camera.main;

        if (battleCamera == null)
            return;

        ChargeEnemy chargeEnemy = spawned.GetComponent<ChargeEnemy>();
        if (chargeEnemy == null)
            chargeEnemy = spawned.GetComponentInChildren<ChargeEnemy>(true);
        if (chargeEnemy != null)
            chargeEnemy.SetBattleCamera(battleCamera);

        EnemyVerticalMover verticalMover = spawned.GetComponent<EnemyVerticalMover>();
        if (verticalMover == null)
            verticalMover = spawned.GetComponentInChildren<EnemyVerticalMover>(true);
        if (verticalMover != null)
            verticalMover.SetBattleCamera(battleCamera);

        EnemyBottomDespawn bottomDespawn = spawned.GetComponent<EnemyBottomDespawn>();
        if (bottomDespawn == null)
            bottomDespawn = spawned.GetComponentInChildren<EnemyBottomDespawn>(true);
        if (bottomDespawn != null)
            bottomDespawn.SetCamera(battleCamera);
    }

    private void WireSpawnerReferences(GameObject spawned)
    {
        if (spawned == null)
            return;

        ChargeEnemy chargeEnemy = spawned.GetComponent<ChargeEnemy>();
        if (chargeEnemy == null)
            chargeEnemy = spawned.GetComponentInChildren<ChargeEnemy>(true);
        if (chargeEnemy != null)
            chargeEnemy.SetSpawner(this);

        EnemyController enemyController = spawned.GetComponent<EnemyController>();
        if (enemyController == null)
            enemyController = spawned.GetComponentInChildren<EnemyController>(true);
        if (enemyController != null)
            enemyController.SetSpawner(this);
    }

    // 화면 안에서 바로 보여야 하는 적은 카메라 바깥으로 잘리지 않게 위치를 보정한다.
    private void ClampSpawnedEnemyInsideCamera(GameObject spawned)
    {
        if (spawned == null)
            return;

        if (battleCamera == null)
            battleCamera = Camera.main;

        if (battleCamera == null)
            return;

        if (!TryGetWorldBounds(spawned, out Bounds bounds))
            return;

        float objectZ = spawned.transform.position.z;
        float cameraZ = battleCamera.transform.position.z;
        float distanceFromCamera = Mathf.Abs(objectZ - cameraZ);

        Vector3 min = battleCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distanceFromCamera));
        Vector3 max = battleCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distanceFromCamera));

        float targetMinX = min.x + visibleScreenPadding;
        float targetMaxX = max.x - visibleScreenPadding;
        float targetMinY = min.y + visibleScreenPadding;
        float targetMaxY = max.y - visibleScreenPadding;

        Vector3 offset = Vector3.zero;

        if (bounds.min.x < targetMinX)
            offset.x += targetMinX - bounds.min.x;
        else if (bounds.max.x > targetMaxX)
            offset.x -= bounds.max.x - targetMaxX;

        if (bounds.min.y < targetMinY)
            offset.y += targetMinY - bounds.min.y;
        else if (bounds.max.y > targetMaxY)
            offset.y -= bounds.max.y - targetMaxY;

        if (offset.sqrMagnitude > 0f)
            spawned.transform.position += offset;
    }

    private bool TryGetWorldBounds(GameObject obj, out Bounds bounds)
    {
        Renderer renderer = obj.GetComponentInChildren<Renderer>(true);
        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        Collider2D collider2D = obj.GetComponentInChildren<Collider2D>(true);
        if (collider2D != null)
        {
            bounds = collider2D.bounds;
            return true;
        }

        bounds = new Bounds(obj.transform.position, Vector3.zero);
        return false;
    }

    // 스폰 완료 + 생존 적 0 조건을 만족하는지 검사해 일반전 클리어 판정에 사용한다.
    public bool IsBattleClear()
    {
        return IsSpawnFinished && AliveCount <= 0 && SpawnedCount >= totalSpawnCount;
    }

    public int GetAliveEnemyCount()
    {
        return AliveCount;
    }

    public void ClearAllEnemies()
    {
        if (enemyRoot == null)
            return;

        for (int i = enemyRoot.childCount - 1; i >= 0; i--)
            Destroy(enemyRoot.GetChild(i).gameObject);

        AliveCount = 0;
    }

    public void DebugBattleState()
    {
        Debug.Log($"[EnemySpawner] Spawned={SpawnedCount}, Total={totalSpawnCount}, Alive={AliveCount}, IsSpawnFinished={IsSpawnFinished}, Clear={IsBattleClear()}");
    }
}
