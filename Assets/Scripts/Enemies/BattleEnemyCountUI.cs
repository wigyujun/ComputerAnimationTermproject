using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleEnemyCountUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";

    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private BossBattleController bossBattleController;

    [Header("UI")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private TMP_Text enemyCountText;

    [Header("Text")]
    [SerializeField] private string normalFormat = "남은 적 : {0}";
    [SerializeField] private string clearText = "전투 클리어!";
    [SerializeField] private string bossWaitingText = "보스 출현 대기중...";
    [SerializeField] private string bossFightText = "보스전";

    private void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = FindAnyObjectByType<EnemySpawner>();

        if (bossBattleController == null)
            bossBattleController = FindAnyObjectByType<BossBattleController>();

        if (uiRoot == null && enemyCountText != null)
            uiRoot = enemyCountText.gameObject;
    }

    private void Update()
    {
        if (!IsBattleScene())
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (enemyCountText == null)
            return;

        bool isBossNode = RunContext.NextNodeType == NodeType.Boss;

        if (isBossNode)
        {
            UpdateBossText();
            return;
        }

        UpdateNormalBattleText();
    }

    private bool IsBattleScene()
    {
        return SceneManager.GetActiveScene().name == battleSceneName;
    }

    private void SetVisible(bool visible)
    {
        if (uiRoot != null && uiRoot.activeSelf != visible)
            uiRoot.SetActive(visible);
    }

    private void UpdateNormalBattleText()
    {
        if (enemySpawner == null)
        {
            enemyCountText.text = "";
            return;
        }

        int remaining = GetRemainingEnemyCount();

        if (enemySpawner.IsBattleClear())
        {
            enemyCountText.text = clearText;
            return;
        }

        enemyCountText.text = string.Format(normalFormat, remaining);
    }

    private void UpdateBossText()
    {
        if (bossBattleController == null)
        {
            enemyCountText.text = bossFightText;
            return;
        }

        if (bossBattleController.IsBossDefeated())
        {
            enemyCountText.text = clearText;
            return;
        }

        if (!bossBattleController.HasBossSpawned())
        {
            if (enemySpawner != null)
            {
                int remaining = GetRemainingEnemyCount();
                enemyCountText.text = string.Format(normalFormat, remaining);
            }
            else
            {
                enemyCountText.text = bossWaitingText;
            }

            return;
        }

        enemyCountText.text = bossFightText;
    }

    private int GetRemainingEnemyCount()
    {
        if (enemySpawner == null)
            return 0;

        int notSpawnedYet = Mathf.Max(0, enemySpawner.TotalEnemiesToSpawn - enemySpawner.SpawnedCount);
        int aliveNow = Mathf.Max(0, enemySpawner.AliveCount);

        return notSpawnedYet + aliveNow;
    }
}
