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

        if (spawnPointsRoot != null && spawnPoints.Count == 0)
        {
            CacheSpawnPointsFromRoot();
        }
    }

    private void Start()
    {
        if (autoStartOnStart)
        {
            BeginSpawn();
        }
    }

    private void CacheSpawnPointsFromRoot()
    {
        spawnPoints.Clear();

        if (spawnPointsRoot == null)
            return;

        for (int i = 0; i < spawnPointsRoot.childCount; i++)
        {
            spawnPoints.Add(spawnPointsRoot.GetChild(i));
        }
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
        {
            CacheSpawnPointsFromRoot();
        }

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

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("EnemySpawner: SpawnEnemy 실패 - enemyPrefab 또는 spawnPoints 문제");
            return;
        }

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject spawned = Instantiate(enemyPrefab, point.position, Quaternion.identity, enemyRoot);
        spawned.name = enemyPrefab.name;

        SpawnedCount++;
        AliveCount++;

        EnemyController enemyController = spawned.GetComponent<EnemyController>();
        if (enemyController == null)
            enemyController = spawned.GetComponentInChildren<EnemyController>();

        if (enemyController != null)
        {
            enemyController.SetSpawner(this);
        }
        else
        {
            Debug.LogWarning($"EnemySpawner: {spawned.name} 에 EnemyController가 없음");
        }

        Debug.Log($"[EnemySpawner] Spawned={SpawnedCount}, Alive={AliveCount}");
    }

    public void NotifyEnemyRemoved()
    {
        AliveCount = Mathf.Max(0, AliveCount - 1);
        Debug.Log($"[EnemySpawner] Enemy removed -> AliveCount={AliveCount}");
    }

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
        {
            Destroy(enemyRoot.GetChild(i).gameObject);
        }

        AliveCount = 0;
    }

    public void DebugBattleState()
    {
        Debug.Log($"[EnemySpawner] Spawned={SpawnedCount}, Total={totalSpawnCount}, Alive={AliveCount}, IsSpawnFinished={IsSpawnFinished}, Clear={IsBattleClear()}");
    }
}
