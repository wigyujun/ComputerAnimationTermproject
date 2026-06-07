using UnityEngine;

public class BattleThemeController : MonoBehaviour
{
    [Header("Current Theme")]
    public ThemeType currentTheme = ThemeType.Forest;

    [Header("Background")]
    [SerializeField] private SpriteRenderer bgA;
    [SerializeField] private SpriteRenderer bgB;

    [SerializeField] private Sprite forestBackground;
    [SerializeField] private Sprite skyBackground;
    [SerializeField] private Sprite seaBackground;

    [Header("Spawner")]
    [SerializeField] private EnemySpawner enemySpawner;

    [SerializeField] private GameObject forestEnemyPrefab;
    [SerializeField] private GameObject skyEnemyPrefab;
    [SerializeField] private GameObject seaEnemyPrefab;

    private void Start()
    {
        ApplyTheme(currentTheme);
    }

    // 테마에 맞는 배경과 적 프리팹을 한 번에 바꿔 전투 연출을 맞춘다.
    public void ApplyTheme(ThemeType theme)
    {
        currentTheme = theme;

        ApplyBackground(theme);
        ApplyEnemy(theme);
    }

    private void ApplyBackground(ThemeType theme)
    {
        Sprite selected = null;

        switch (theme)
        {
            case ThemeType.Forest:
                selected = forestBackground;
                break;
            case ThemeType.Sky:
                selected = skyBackground;
                break;
            case ThemeType.Sea:
                selected = seaBackground;
                break;
        }

        if (selected != null)
        {
            bgA.sprite = selected;
            bgB.sprite = selected;
        }
    }

    // 스포너가 이후 생성할 적 프리팹을 현재 테마 기준으로 교체한다.
    private void ApplyEnemy(ThemeType theme)
    {
        if (enemySpawner == null)
            return;

        switch (theme)
        {
            case ThemeType.Forest:
                enemySpawner.SetEnemyPrefab(forestEnemyPrefab);
                break;
            case ThemeType.Sky:
                enemySpawner.SetEnemyPrefab(skyEnemyPrefab);
                break;
            case ThemeType.Sea:
                enemySpawner.SetEnemyPrefab(seaEnemyPrefab);
                break;
        }
    }
}
