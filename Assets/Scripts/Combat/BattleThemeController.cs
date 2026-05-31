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
