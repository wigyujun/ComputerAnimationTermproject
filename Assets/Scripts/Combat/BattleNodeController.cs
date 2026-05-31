using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleNodeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
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

    private bool battleEnded = false;
    private bool battleClear = false;

    public bool BattleEnded => battleEnded;
    public bool BattleClear => battleClear;

    private void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = FindAnyObjectByType<EnemySpawner>();

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<Health>();

        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        if (playerShooter == null)
            playerShooter = FindAnyObjectByType<AutoBowShooter>();

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

    private void Update()
    {
        if (battleEnded) return;

        // 플레이어 사망
        if (playerHealth == null || playerHealth.CurrentHP <= 0)
        {
            EndBattle(false);
            return;
        }

        // 전투 승리
        if (enemySpawner != null && enemySpawner.IsBattleClear())
        {
            EndBattle(true);
        }

        // if (Input.GetKeyDown(KeyCode.F8))
        // {
        //     Debug.Log("[BattleNodeController] F8 test");
        //     EndBattle(true);
        // }

    }

    public void EndBattle(bool clear)
    {
        if (battleEnded) return;

        battleEnded = true;
        battleClear = clear;

        if (playerController != null)
            playerController.enabled = false;

        if (playerShooter != null)
            playerShooter.enabled = false;

        if (enemySpawner != null)
            enemySpawner.StopSpawn();

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = clear ? "전투 클리어!" : "패배...";
        }

        if (clear)
        {
            RunContext.SetBattleResult(BattleResult.Win);
        }
        else
        {
            RunContext.SetBattleResult(BattleResult.Lose);
        }

        Debug.Log(clear ? "[BattleNodeController] Battle Clear" : "[BattleNodeController] Battle Lose");
    }

    public void OnClickResultConfirm()
    {
        Debug.Log("[BattleNodeController] Confirm clicked");

        Time.timeScale = 1f;

        if (battleClear)
        {
            // 승리 → 맵 복귀
            SceneManager.LoadScene(mapSceneName);
        }
        else
        {
            // 패배 → 새 게임 준비 후 시작화면
            RunContext.ResetForNewRun();
            SceneManager.LoadScene(startSceneName);
        }
    }
}
