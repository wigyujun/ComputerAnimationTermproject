using System;
using UnityEngine;
using TMPro;

public class ShopSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private PlayerCombatStats playerStats;
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Debug")]
    [SerializeField] private bool allowDebugToggleKey = false;

    private bool isOpen = false;
    private Action onShopClosed;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!allowDebugToggleKey)
            return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleShop();
        }
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<Health>();

        if (playerWallet == null)
            playerWallet = FindAnyObjectByType<PlayerWallet>();

        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerCombatStats>();
    }

    public void OpenShopFromNode(Action onClose)
    {
        ResolveReferences();

        onShopClosed = onClose;
        isOpen = true;

        if (shopPanel != null)
            shopPanel.SetActive(true);

        // MapScene에서는 꼭 timeScale 0이 필요하지는 않지만,
        // 기존 동작 유지 원하면 남겨도 됨
        Time.timeScale = 0f;

        SetInfo("상점이 열렸습니다.");

        if (playerWallet == null)
            SetInfo("PlayerWallet 참조가 없습니다.");

        if (playerStats == null)
            SetInfo("PlayerCombatStats 참조가 없습니다.");
    }

    public void CloseShopFromNode()
    {
        isOpen = false;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        Time.timeScale = 1f;
        SetInfo("");

        onShopClosed?.Invoke();
        onShopClosed = null;
    }

    public void ToggleShop()
    {
        if (isOpen)
            CloseShopFromNode();
        else
            OpenShopFromNode(null);
    }

    public void BuyHeal3()
    {
        if (playerHealth == null)
        {
            SetInfo("Health 참조가 없습니다.");
            return;
        }

        TryPurchase(10, () =>
        {
            playerHealth.Heal(3);
            SetInfo("HP +3 회복");
        });
    }

    public void BuyAttackSpeed20()
    {
        if (playerStats == null)
        {
            SetInfo("PlayerCombatStats 참조가 없습니다.");
            return;
        }

        TryPurchase(50, () =>
        {
            playerStats.AddAttackSpeedPercent(0.20f);
            SetInfo("공격속도 +20%");
        });
    }

    public void BuyAttackPower10()
    {
        if (playerStats == null)
        {
            SetInfo("PlayerCombatStats 참조가 없습니다.");
            return;
        }

        TryPurchase(50, () =>
        {
            playerStats.AddAttackPowerPercent(0.10f);
            SetInfo("공격력 +10%");
        });
    }

    public void BuyWeaponUpgrade()
    {
        if (playerStats == null)
        {
            SetInfo("PlayerCombatStats 참조가 없습니다.");
            return;
        }

        if (!playerStats.CanUpgradeWeapon())
        {
            SetInfo("이미 최고 무기 단계입니다.");
            return;
        }

        TryPurchase(100, () =>
        {
            playerStats.UpgradeWeapon();
            SetInfo("무기 업그레이드 완료 " + playerStats.WeaponName);
        });
    }

    public void BuyMaxHpAndFullHeal()
    {
        if (playerHealth == null)
        {
            SetInfo("Health 참조가 없습니다.");
            return;
        }

        TryPurchase(100, () =>
        {
            playerHealth.IncreaseMaxHP(5, true);
            SetInfo("최대 HP +5 / 풀회복");
        });
    }

    private void TryPurchase(int cost, Action onSuccess)
    {
        if (playerWallet == null)
        {
            SetInfo("PlayerWallet 참조가 없습니다.");
            return;
        }

        if (!playerWallet.SpendCoin(cost))
        {
            SetInfo($"코인이 부족합니다. 필요 코인: {cost}");
            return;
        }

        onSuccess?.Invoke();
    }

    private void SetInfo(string message)
    {
        if (infoText != null)
            infoText.text = message;

        Debug.Log("[ShopSystem] " + message);
    }
}
