using UnityEngine;

public class BattleSceneEntry : MonoBehaviour
{
    [SerializeField] private BattleThemeController battleThemeController;
    [SerializeField] private GameManager gameManager;

    private void Start()
    {
        Debug.Log("BattleSceneEntry Start 실행");

        if (battleThemeController == null)
            battleThemeController = FindAnyObjectByType<BattleThemeController>();

        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        if (battleThemeController == null)
        {
            Debug.LogError("BattleThemeController를 찾지 못했습니다.");
            return;
        }

        if (gameManager == null)
        {
            Debug.LogError("GameManager를 찾지 못했습니다. BattleScene 안의 GameManager 오브젝트에 GameManager 스크립트가 붙어 있는지 확인하세요.");
            return;
        }

        ThemeType themeToApply = RunContext.NextBattleTheme == ThemeType.None
            ? ThemeType.Forest
            : RunContext.NextBattleTheme;

        battleThemeController.ApplyTheme(themeToApply);
        gameManager.BeginBattle();

        Debug.Log($"BattleSceneEntry 적용 완료 - Theme: {themeToApply}, NodeType: {RunContext.NextNodeType}");
    }
}
