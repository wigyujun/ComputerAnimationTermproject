using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleNodeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private BossBattleController bossBattleController;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private AutoBowShooter playerShooter;

    [Header("UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;

    [Header("Scene Names")]
    [SerializeField] private string mapSceneName = "SampleScene";
    [SerializeField] private string startSceneName = "StartScene";
    [SerializeField] private string clearSceneName = "ClearScene";

    private bool battleEnded = false;
    private bool battleClear = false;

    public bool BattleEnded => battleEnded;
    public bool BattleClear => battleClear;

    private void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = FindAnyObjectByType<EnemySpawner>();

        if (bossBattleController == null)
            bossBattleController = FindAnyObjectByType<BossBattleController>();

        ResolvePlayerReferences();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickResultConfirm);
        }
    }

    private void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    // 전투 중 승패 조건을 매 프레임 감시해 결과 패널 표시 시점을 결정한다.
    private void Update()
    {
        if (battleEnded)
            return;

        // 플레이어 사망
        if (playerHealth == null || playerHealth.CurrentHP <= 0)
        {
            EndBattle(false);
            return;
        }

        bool isBossNode = RunContext.NextNodeType == NodeType.Boss;

        // 보스전 승리 판정
        if (isBossNode)
        {
            if (bossBattleController != null && bossBattleController.IsBossDefeated())
            {
                EndBattle(true);
            }

            return;
        }

        // 일반전 승리 판정
        if (enemySpawner != null && enemySpawner.IsBattleClear())
        {
            EndBattle(true);
        }
    }

    private void ResolvePlayerReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return;

        if (playerHealth == null)
            playerHealth = playerObj.GetComponentInParent<Health>();

        if (playerController == null)
            playerController = playerObj.GetComponentInParent<PlayerController>();

        if (playerShooter == null)
            playerShooter = playerObj.GetComponentInChildren<AutoBowShooter>();
    }

    // 전투 종료 시 플레이어 상태 저장, 입력 차단, 결과 UI 표시를 한 번에 처리한다.
    public void EndBattle(bool clear)
    {
        if (battleEnded)
            return;

        battleEnded = true;
        battleClear = clear;

        if (playerHealth != null)
        {
            RunContext.SetMaxHP(playerHealth.MaxHP, false);
            RunContext.SetCurrentHP(playerHealth.CurrentHP);
        }

        if (playerController != null)
            playerController.enabled = false;

        if (playerShooter != null)
            playerShooter.enabled = false;

        if (enemySpawner != null)
            enemySpawner.StopSpawn();

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }

        if (resultText != null)
        {
            resultText.text = clear ? "전투 클리어!" : "패배...";
        }

        RunContext.SetBattleResult(clear ? BattleResult.Win : BattleResult.Lose);

        Debug.Log(clear ? "[BattleNodeController] Battle Clear" : "[BattleNodeController] Battle Lose");
    }


    // 결과 확인 버튼 이후의 씬 이동 흐름(시작화면/맵/클리어씬)을 담당한다.
    public void OnClickResultConfirm()
    {
        Debug.Log("[BattleNodeController] Confirm clicked");

        Time.timeScale = 1f;

        // 패배 -> 시작화면
        if (!battleClear)
        {
            RunContext.ResetForNewRun();
            SceneManager.LoadScene(startSceneName);
            return;
        }

        bool isBossNode = RunContext.NextNodeType == NodeType.Boss;

        // 보스 승리 처리
        if (isBossNode)
        {
            // 5층 최종보스 승리 -> 클리어 씬
            if (RunContext.CurrentFloor >= 5)
            {
                Debug.Log("[BattleNodeController] Final Boss Clear -> ClearScene");
                SceneManager.LoadScene(clearSceneName);
                return;
            }

            // 3층 보스 승리 -> 다음 층 맵
            int nextFloor = RunContext.CurrentFloor + 1;
            Debug.Log($"[BattleNodeController] Boss Clear -> {nextFloor}층 이동");

            RunContext.PrepareForFloorChange(nextFloor);
            SceneManager.LoadScene(mapSceneName);
            return;
        }

        // 일반/하드 전투 승리 -> 현재 층 맵으로 복귀
        SceneManager.LoadScene(mapSceneName);
    }
}
