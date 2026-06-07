using TMPro;
using UnityEngine;

public class MapRunStatusPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text attackSpeedText;
    [SerializeField] private TMP_Text attackPowerText;
    [SerializeField] private TMP_Text weaponText;
    [SerializeField] private TMP_Text companionText;

    [Header("Options")]
    [SerializeField] private bool refreshEveryFrame = true;

    private void OnEnable()
    {
        RefreshUI();
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (refreshEveryFrame)
            RefreshUI();
    }

    public void RefreshUI()
    {
        if (coinText != null)
            coinText.text = $"Coin : {RunContext.Coin}";

        if (hpText != null)
            hpText.text = $"HP : {RunContext.CurrentHP} / {RunContext.MaxHP}";

        if (attackSpeedText != null)
            attackSpeedText.text = $"공격속도 : +{Mathf.RoundToInt(RunContext.AttackSpeedBonusPercent * 100f)}%";

        if (attackPowerText != null)
            attackPowerText.text = $"공격력 : +{Mathf.RoundToInt(RunContext.AttackPowerBonusPercent * 100f)}%";

        if (weaponText != null)
            weaponText.text = $"무기 : {RunContext.GetWeaponName()}";

        if (companionText != null)
            companionText.text = $"동료 : {RunContext.CompanionCount} / {RunContext.MaxCompanionCount}";
    }
}
