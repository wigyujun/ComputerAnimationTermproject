using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerWallet playerWallet;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI coinText;

    private void Update()
    {
        if (playerHealth != null && hpText != null)
        {
            hpText.text = $"HP : {playerHealth.CurrentHP} / {playerHealth.MaxHP}";
        }

        if (playerWallet != null && coinText != null)
        {
            coinText.text = $"Coin : {playerWallet.Coin}";
        }
    }
}
