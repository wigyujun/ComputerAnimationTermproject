using System;
using UnityEngine;
using TMPro;

public class ShopNodeController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Shop Settings")]
    [SerializeField] private bool pauseTimeWhenOpen = true;

    [Header("Prices")]
    [SerializeField] private int heal3Cost = 50;
    [SerializeField] private int attackSpeed20Cost = 100;
    [SerializeField] private int attackPower10Cost = 100;
    [SerializeField] private int weaponUpgradeCost = 150;
    [SerializeField] private int maxHpAndFullHealCost = 150;

    private bool isOpen = false;
    private Action onShopCompleted;

    private void Awake()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    public void OpenShopPanel(Action onCompleted)
    {
        onShopCompleted = onCompleted;
        isOpen = true;

        if (shopPanel != null)
            shopPanel.SetActive(true);

        if (pauseTimeWhenOpen)
            Time.timeScale = 0f;

        SetInfo("상점이 열렸습니다.");
    }

    public void ConfirmShop()
    {
        CloseInternal(true);
    }

    public void CancelShop()
    {
        CloseInternal(false);
    }

    private void CloseInternal(bool invokeCompletion)
    {
        isOpen = false;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (pauseTimeWhenOpen)
            Time.timeScale = 1f;

        SetInfo("");

        if (invokeCompletion)
            onShopCompleted?.Invoke();

        onShopCompleted = null;
    }

    public void BuyHeal3()
    {
        if (!RunContext.TrySpendCoin(heal3Cost))
        {
            SetInfo($"코인이 부족합니다. 필요 코인: {heal3Cost}");
            return;
        }

        RunContext.HealPlayer(3);
        SetInfo("HP +3 회복");
    }

    public void BuyAttackSpeed20()
    {
        if (!RunContext.TrySpendCoin(attackSpeed20Cost))
        {
            SetInfo($"코인이 부족합니다. 필요 코인: {attackSpeed20Cost}");
            return;
        }

        RunContext.AddAttackSpeedPercent(0.20f);
        SetInfo("공격속도 +20%");
    }

    public void BuyAttackPower10()
    {
        if (!RunContext.TrySpendCoin(attackPower10Cost))
        {
            SetInfo($"코인이 부족합니다. 필요 코인: {attackPower10Cost}");
            return;
        }

        RunContext.AddAttackPowerPercent(0.10f);
        SetInfo("공격력 +10%");
    }

    public void BuyWeaponUpgrade()
    {
        if (!RunContext.CanUpgradeWeapon())
        {
            SetInfo("이미 최고 무기 단계입니다.");
            return;
        }

        if (!RunContext.TrySpendCoin(weaponUpgradeCost))
        {
            SetInfo($"코인이 부족합니다. 필요 코인: {weaponUpgradeCost}");
            return;
        }

        RunContext.UpgradeWeapon();
        SetInfo("무기 업그레이드 완료");
    }

    public void BuyMaxHpAndFullHeal()
    {
        if (!RunContext.TrySpendCoin(maxHpAndFullHealCost))
        {
            SetInfo($"코인이 부족합니다. 필요 코인: {maxHpAndFullHealCost}");
            return;
        }

        RunContext.IncreaseMaxHP(5, true);
        SetInfo("최대 HP +5 / 풀회복");
    }

    private void SetInfo(string message)
    {
        if (infoText != null)
            infoText.text = message;

        if (!string.IsNullOrEmpty(message))
            Debug.Log("[ShopNodeController] " + message);
    }
}
