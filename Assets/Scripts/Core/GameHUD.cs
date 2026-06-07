using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameHUD : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";

    [Header("References")]
    [SerializeField] private Health playerHealth;

    [Header("UI Root")]
    [SerializeField] private GameObject hudRoot;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI coinText;

    private void Awake()
    {
        if (hudRoot == null)
            hudRoot = gameObject;
    }

    private void Update()
    {
        bool isBattleScene = SceneManager.GetActiveScene().name == battleSceneName;

        if (hudRoot != null && hudRoot.activeSelf != isBattleScene)
            hudRoot.SetActive(isBattleScene);

        if (!isBattleScene)
            return;

        if (playerHealth != null && hpText != null)
            hpText.text = $"HP : {playerHealth.CurrentHP} / {playerHealth.MaxHP}";

        if (coinText != null)
            coinText.text = $"Coin : {RunContext.Coin}";
    }
}
