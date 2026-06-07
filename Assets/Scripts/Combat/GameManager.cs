using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Battle Refs")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private BattleNodeController battleNodeController;

    private bool battleStarted;

    private void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = FindAnyObjectByType<EnemySpawner>();

        if (battleNodeController == null)
            battleNodeController = FindAnyObjectByType<BattleNodeController>();
    }

    // 전투 시작 요청을 한 번만 처리하고 EnemySpawner의 스폰 루프를 연다.
    public void BeginBattle()
    {
        Debug.Log("6) GameManager.BeginBattle 실행");

        if (battleStarted)
        {
            Debug.LogWarning("GameManager: 이미 전투 시작됨");
            return;
        }

        battleStarted = true;

        if (enemySpawner == null)
            enemySpawner = FindAnyObjectByType<EnemySpawner>();

        Debug.Log("7) enemySpawner 찾음? " + (enemySpawner != null));

        if (enemySpawner != null)
        {
            enemySpawner.BeginSpawn();
            Debug.Log("8) enemySpawner.BeginSpawn 호출");
        }
        else
        {
            Debug.LogError("GameManager: enemySpawner를 찾지 못함");
        }
    }

    // 플레이어 사망 시 다른 스크립트에서 이 메서드를 호출해도
    // BattleNodeController가 최종 패배 처리만 담당하도록 연결
    // 플레이어 사망 시 최종 패배 처리를 BattleNodeController로 위임한다.
    public void OnPlayerDefeat()
    {
        Debug.Log("GameManager: OnPlayerDefeat 호출");

        if (battleNodeController == null)
            battleNodeController = FindAnyObjectByType<BattleNodeController>();

        if (battleNodeController != null)
        {
            battleNodeController.EndBattle(false);
        }
        else
        {
            Debug.LogError("GameManager: BattleNodeController를 찾지 못함");
        }
    }
}
