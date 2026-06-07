using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBattleController : MonoBehaviour
{
    [System.Serializable]
    public class BossEntry
    {
        public int floorIndex;
        public ThemeType themeType;
        public GameObject bossPrefab;
    }

    [Header("Battle References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private BackgroundLooper backgroundLooper;
    [SerializeField] private Transform enemyRoot;
    [SerializeField] private Camera battleCamera;

    [Header("Boss Database")]
    [SerializeField] private List<BossEntry> bossEntries = new List<BossEntry>();

    [Header("Boss Spawn")]
    [SerializeField] private float preSpawnDelay = 1.0f;
    [SerializeField] private float topSpawnOffset = 1.5f;
    [SerializeField] private Vector3 fallbackSpawnPosition = new Vector3(0f, 6f, 0f);

    [Header("Boss HP Bar")]
    [SerializeField] private BossHpBarUI bossHpBarPrefab;
    [SerializeField] private RectTransform bossHpBarRoot;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    private bool isBossBattle = false;
    private bool bossSequenceStarted = false;
    private bool bossSpawned = false;
    private bool bossDefeated = false;

    private GameObject currentBossInstance;
    private BossHpBarUI currentBossHpBar;

    private void Awake()
    {
        if (battleCamera == null)
            battleCamera = Camera.main;

        if (enemyRoot == null)
        {
            GameObject rootObj = GameObject.Find("EnemyRoot");
            if (rootObj != null)
                enemyRoot = rootObj.transform;
        }
    }

    private void Start()
    {
        isBossBattle = RunContext.NextNodeType == NodeType.Boss;

        Log($"Start / isBossBattle={isBossBattle}, floor={RunContext.CurrentFloor}, theme={RunContext.NextBattleTheme}");

        if (!isBossBattle)
            return;

        if (enemySpawner == null)
        {
            Debug.LogError("[BossBattleController] EnemySpawner reference is missing.");
        }
    }

    // 일반 웨이브 종료 여부를 감시하고, 조건이 되면 보스 등장 시퀀스를 시작한다.
    private void Update()
    {
        if (!isBossBattle)
            return;

        if (bossDefeated)
            return;

        if (!bossSequenceStarted)
        {
            if (IsRegularWaveFinished())
            {
                StartCoroutine(StartBossSequence());
            }

            return;
        }

        // 보스가 이미 스폰된 뒤 사라졌다면 처치로 간주
        if (bossSpawned && !bossDefeated && currentBossInstance == null)
        {
            Log("Current boss instance became null -> treat as defeated");
            NotifyBossDefeated();
        }
    }

    private bool IsRegularWaveFinished()
    {
        if (enemySpawner == null)
            return false;

        return enemySpawner.IsSpawnFinished && enemySpawner.GetAliveEnemyCount() <= 0;
    }

    // 일반 적 처치 후 배경 정지와 대기 연출을 거쳐 보스를 등장시키는 코루틴이다.
    private IEnumerator StartBossSequence()
    {
        if (bossSequenceStarted)
            yield break;

        bossSequenceStarted = true;

        Log("Regular wave finished -> start boss sequence");

        if (backgroundLooper != null)
        {
            backgroundLooper.SetScrolling(false);
            Log("Background scrolling stopped");
        }

        if (enemySpawner != null)
        {
            enemySpawner.StopSpawn();
        }

        yield return new WaitForSeconds(preSpawnDelay);

        SpawnBoss();
    }

    // 현재 층/테마에 맞는 보스를 생성하고 밸런스, 카메라, HP바를 연결한다.
    private void SpawnBoss()
    {
        if (bossSpawned)
            return;

        GameObject bossPrefab = FindBossPrefab(RunContext.CurrentFloor, RunContext.NextBattleTheme);

        if (bossPrefab == null)
        {
            Debug.LogError($"[BossBattleController] Boss prefab not found. floor={RunContext.CurrentFloor}, theme={RunContext.NextBattleTheme}");
            return;
        }

        Vector3 spawnPosition = GetBossSpawnPosition();
        Transform parent = enemyRoot != null ? enemyRoot : transform;

        currentBossInstance = Instantiate(bossPrefab, spawnPosition, Quaternion.identity, parent);
        currentBossInstance.name = bossPrefab.name;

        FloorBalanceUtility.ApplyBossBalance(currentBossInstance, RunContext.CurrentFloor);

        bossSpawned = true;

        currentBossInstance.SendMessage("SetBattleController", this, SendMessageOptions.DontRequireReceiver);
        currentBossInstance.SendMessage("SetBattleCamera", battleCamera, SendMessageOptions.DontRequireReceiver);
        currentBossInstance.SendMessage("BeginBossBattle", SendMessageOptions.DontRequireReceiver);

        CreateBossHpBar();

        Log($"Boss spawned -> {bossPrefab.name}, pos={spawnPosition}, hp={FloorBalanceUtility.GetBossMaxHp(RunContext.CurrentFloor)}");
    }

    private void CreateBossHpBar()
    {
        if (bossHpBarPrefab == null || bossHpBarRoot == null || currentBossInstance == null)
            return;

        Health bossHealth = currentBossInstance.GetComponent<Health>();
        if (bossHealth == null)
            bossHealth = currentBossInstance.GetComponentInChildren<Health>();

        if (bossHealth == null)
        {
            Debug.LogWarning("[BossBattleController] Boss Health component not found. HP bar will not be created.");
            return;
        }

        if (currentBossHpBar != null)
            Destroy(currentBossHpBar.gameObject);

        currentBossHpBar = Instantiate(bossHpBarPrefab, bossHpBarRoot);
        currentBossHpBar.Bind(bossHealth, currentBossInstance.name);
    }

    private GameObject FindBossPrefab(int floorIndex, ThemeType themeType)
    {
        // 1차: floor + theme 일치
        for (int i = 0; i < bossEntries.Count; i++)
        {
            BossEntry entry = bossEntries[i];
            if (entry == null || entry.bossPrefab == null)
                continue;

            if (entry.floorIndex == floorIndex && entry.themeType == themeType)
                return entry.bossPrefab;
        }

        // 2차: 같은 floor의 첫 번째 보스 fallback
        for (int i = 0; i < bossEntries.Count; i++)
        {
            BossEntry entry = bossEntries[i];
            if (entry == null || entry.bossPrefab == null)
                continue;

            if (entry.floorIndex == floorIndex)
                return entry.bossPrefab;
        }

        return null;
    }

    private Vector3 GetBossSpawnPosition()
    {
        if (battleCamera == null)
            battleCamera = Camera.main;

        if (battleCamera == null)
            return fallbackSpawnPosition;

        float spawnZ = enemyRoot != null ? enemyRoot.position.z : 0f;
        float distanceFromCamera = Mathf.Abs(spawnZ - battleCamera.transform.position.z);

        Vector3 topCenter = battleCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, distanceFromCamera));
        return new Vector3(topCenter.x, topCenter.y + topSpawnOffset, spawnZ);
    }

    // 보스 처치 보상 지급과 승리 상태 기록을 담당한다.
    public void NotifyBossDefeated()
    {
        if (bossDefeated)
            return;

        bossDefeated = true;

        if (currentBossHpBar != null)
            Destroy(currentBossHpBar.gameObject);

        int clearReward = FloorBalanceUtility.GetBossClearReward(RunContext.CurrentFloor);
        if (clearReward > 0)
        {
            RunContext.AddCoin(clearReward);
            Log($"Boss clear reward added -> +{clearReward} coin");
        }

        Log("Boss defeated -> BattleResult.Win");

        RunContext.SetBattleResult(BattleResult.Win);
    }

    public bool IsBossBattle()
    {
        return isBossBattle;
    }

    public bool HasBossSpawned()
    {
        return bossSpawned;
    }

    public bool IsBossSequenceStarted()
    {
        return bossSequenceStarted;
    }

    public bool IsBossDefeated()
    {
        return bossDefeated;
    }

    public GameObject GetCurrentBossInstance()
    {
        return currentBossInstance;
    }

    private void Log(string message)
    {
        if (!enableDebugLog)
            return;

        Debug.Log("[BossBattleController] " + message);
    }
}
