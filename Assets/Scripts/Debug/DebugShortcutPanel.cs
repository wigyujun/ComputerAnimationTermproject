using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugShortcutPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Transform floatingUiRoot;
    [SerializeField] private KeyCode toggleKey = KeyCode.N;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool keepFloatingUiOnTop = true;

    [Header("Scene Names")]
    [SerializeField] private string mapSceneName = "SampleScene";
    [SerializeField] private string battleSceneName = "BattleScene";

    [Header("Optional Scene Controllers")]
    [SerializeField] private RewardNodeController rewardNodeController;
    [SerializeField] private ShopNodeController shopNodeController;

    [Header("Debug Loadout")]
    [SerializeField] private bool resetRunBeforeJump = false;
    [SerializeField] private bool applyTestLoadout = false;
    [SerializeField] private int testCoin = 999;
    [SerializeField] private int testMaxHP = 20;
    [SerializeField] private bool fullHeal = true;
    [SerializeField][Range(0, 4)] private int testWeaponLevel = 4;
    [SerializeField][Range(0, 2)] private int testCompanionCount = 2;

    [Header("Optional")]
    [SerializeField] private bool forceTimeScaleToOne = true;

    private enum PendingShortcutAction
    {
        None,
        OpenReward,
        OpenShop
    }

    private static PendingShortcutAction pendingShortcutAction = PendingShortcutAction.None;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (debugPanel != null && hideOnStart)
            debugPanel.SetActive(false);

        TryResolveSceneControllers();
        TryExecutePendingShortcut();
        BringFloatingUiToFront();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log("[DebugShortcutPanel] Toggle key pressed: " + toggleKey);

            if (debugPanel != null)
            {
                bool next = !debugPanel.activeSelf;
                debugPanel.SetActive(next);
                Debug.Log("[DebugShortcutPanel] DebugPanel active = " + next);
            }
            else
            {
                Debug.LogWarning("[DebugShortcutPanel] debugPanel is NULL");
            }
        }
    }

    private void LateUpdate()
    {
        if (keepFloatingUiOnTop)
            BringFloatingUiToFront();
    }

    public void ToggleDebugPanel()
    {
        if (debugPanel == null)
        {
            Debug.LogWarning("[DebugShortcutPanel] debugPanel is not assigned.");
            return;
        }

        debugPanel.SetActive(!debugPanel.activeSelf);
        BringFloatingUiToFront();
    }

    public void OpenDebugPanel()
    {
        if (debugPanel != null)
            debugPanel.SetActive(true);

        BringFloatingUiToFront();
    }

    public void CloseDebugPanel()
    {
        if (debugPanel != null)
            debugPanel.SetActive(false);
    }

    // 디버그용으로 3층 맵 상태를 즉시 만들어 이동한다.
    public void GoToFloor3Map()
    {
        PrepareBeforeJump();

        RunContext.PrepareForFloorChange(3);
        SceneManager.LoadScene(mapSceneName);
    }

    // 디버그용으로 5층 맵 상태를 즉시 만들어 이동한다.
    public void GoToFloor5Map()
    {
        PrepareBeforeJump();

        RunContext.PrepareForFloorChange(5);
        SceneManager.LoadScene(mapSceneName);
    }

    public void Start3FMonkeyBoss()
    {
        StartBossBattle(3, ThemeType.Forest, "debug_3f_monkey_boss");
    }

    public void Start3FBirdBoss()
    {
        StartBossBattle(3, ThemeType.Sky, "debug_3f_bird_boss");
    }

    public void Start3FSharkBoss()
    {
        StartBossBattle(3, ThemeType.Sea, "debug_3f_shark_boss");
    }

    public void Start5FStatueBoss()
    {
        StartBossBattle(5, ThemeType.Forest, "debug_5f_statue_boss");
    }

    public void Start5FAngelBoss()
    {
        StartBossBattle(5, ThemeType.Sky, "debug_5f_angel_boss");
    }

    public void Start5FDeepSeaBoss()
    {
        StartBossBattle(5, ThemeType.Sea, "debug_5f_deepsea_boss");
    }

    public void OpenRewardShortcut()
    {
        int targetFloor = Mathf.Max(1, RunContext.CurrentFloor);

        PrepareBeforeJump();

        if (!IsCurrentSceneMap())
        {
            pendingShortcutAction = PendingShortcutAction.OpenReward;
            RunContext.PrepareForFloorChange(targetFloor);
            SceneManager.LoadScene(mapSceneName);
            return;
        }

        OpenRewardShortcutInternal();
    }

    public void OpenShopShortcut()
    {
        int targetFloor = Mathf.Max(1, RunContext.CurrentFloor);

        PrepareBeforeJump();

        if (!IsCurrentSceneMap())
        {
            pendingShortcutAction = PendingShortcutAction.OpenShop;
            RunContext.PrepareForFloorChange(targetFloor);
            SceneManager.LoadScene(mapSceneName);
            return;
        }

        OpenShopShortcutInternal();
    }

    // 지정한 층/테마 보스전을 바로 재현하기 위한 디버그 전투 진입 함수다.
    private void StartBossBattle(int floor, ThemeType theme, string nodeId)
    {
        PrepareBeforeJump();

        RunContext.PrepareForFloorChange(floor);

        MapNodeData debugNode = new MapNodeData
        {
            nodeId = nodeId,
            floorIndex = floor,
            columnIndex = 0,
            laneIndex = 0,
            nodeType = NodeType.Boss,
            themeType = theme,
            nodeState = NodeState.Current,
            uiPosition = Vector2.zero,
            nextNodeIds = new List<string>()
        };

        RunContext.SetBattleEntry(debugNode);
        RunContext.SetBattleResult(BattleResult.None);

        Debug.Log($"[DebugShortcutPanel] Start Boss Battle -> floor={floor}, theme={theme}, nodeId={nodeId}");

        SceneManager.LoadScene(battleSceneName);
    }

    // 디버그 점프 전에 시간 배율, 런 초기화, 테스트 장비 세팅을 정리한다.
    private void PrepareBeforeJump()
    {
        if (forceTimeScaleToOne)
            Time.timeScale = 1f;

        if (resetRunBeforeJump)
            RunContext.ResetForNewRun();

        if (applyTestLoadout)
            ApplyTestLoadout();
    }

    // 코인/체력/무기/동료를 테스트용 상태로 맞춰 빠른 밸런스 체크를 돕는다.
    private void ApplyTestLoadout()
    {
        RunContext.SetCoin(testCoin);
        RunContext.SetMaxHP(testMaxHP, fullHeal);

        while (RunContext.CanUpgradeWeapon() && RunContext.WeaponUpgradeLevel < testWeaponLevel)
            RunContext.UpgradeWeapon();

        while (RunContext.CanRecruitCompanion() && RunContext.CompanionCount < testCompanionCount)
            RunContext.RecruitCompanion();

        Debug.Log(
            $"[DebugShortcutPanel] Test Loadout Applied -> " +
            $"Coin={RunContext.Coin}, HP={RunContext.CurrentHP}/{RunContext.MaxHP}, " +
            $"WeaponLevel={RunContext.WeaponUpgradeLevel}, Companions={RunContext.CompanionCount}"
        );
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != mapSceneName)
            return;

        TryResolveSceneControllers();
        TryExecutePendingShortcut();
        BringFloatingUiToFront();
    }

    private void TryResolveSceneControllers()
    {
        if (rewardNodeController == null)
            rewardNodeController = FindAnyObjectByType<RewardNodeController>();

        if (shopNodeController == null)
            shopNodeController = FindAnyObjectByType<ShopNodeController>();
    }

    // 씬이 바뀐 뒤에도 예약된 보상/상점 단축 동작을 이어서 실행한다.
    private void TryExecutePendingShortcut()
    {
        if (pendingShortcutAction == PendingShortcutAction.None)
            return;

        switch (pendingShortcutAction)
        {
            case PendingShortcutAction.OpenReward:
                OpenRewardShortcutInternal();
                break;

            case PendingShortcutAction.OpenShop:
                OpenShopShortcutInternal();
                break;
        }

        pendingShortcutAction = PendingShortcutAction.None;
    }

    private bool IsCurrentSceneMap()
    {
        return SceneManager.GetActiveScene().name == mapSceneName;
    }

    // 맵 씬에서 보상 패널을 직접 열어 보상 플로우를 테스트한다.
    private void OpenRewardShortcutInternal()
    {
        TryResolveSceneControllers();

        if (rewardNodeController == null)
        {
            Debug.LogError("[DebugShortcutPanel] RewardNodeController를 찾을 수 없습니다.");
            return;
        }

        if (debugPanel != null)
            debugPanel.SetActive(false);

        rewardNodeController.OpenRewardPanel(null);
        BringFloatingUiToFront();
        Debug.Log("[DebugShortcutPanel] Reward shortcut opened");
    }

    // 맵 씬에서 상점 패널을 직접 열어 상점 플로우를 테스트한다.
    private void OpenShopShortcutInternal()
    {
        TryResolveSceneControllers();

        if (shopNodeController == null)
        {
            Debug.LogError("[DebugShortcutPanel] ShopNodeController를 찾을 수 없습니다.");
            return;
        }

        if (debugPanel != null)
            debugPanel.SetActive(false);

        shopNodeController.OpenShopPanel(null);
        BringFloatingUiToFront();
        Debug.Log("[DebugShortcutPanel] Shop shortcut opened");
    }

    private void BringFloatingUiToFront()
    {
        Transform target = floatingUiRoot != null ? floatingUiRoot : transform;

        if (target != null && target.parent != null)
            target.SetAsLastSibling();
    }
}
