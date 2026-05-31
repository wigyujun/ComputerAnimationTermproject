using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardNodeController : MonoBehaviour
{
    [Header("Main UI")]
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private TMP_Text infoText;

    [Header("Choice UI")]
    [SerializeField] private GameObject choiceRoot;
    [SerializeField] private GameObject guildButton;
    [SerializeField] private GameObject blacksmithButton;

    [Header("Result Popup UI")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;

    private bool rewardClaimed = false;
    private Action onRewardCompleted;

    private void Awake()
    {
        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (resultPopup != null)
            resultPopup.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
        }
    }

    public void OpenRewardPanel(Action onComplete)
    {
        onRewardCompleted = onComplete;
        rewardClaimed = false;

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        ShowChoiceUI();

        SetInfo(
            $"보상을 선택하세요.\n" +
            $"현재 동료: {RunContext.CompanionCount}/{RunContext.MaxCompanionCount}\n" +
            $"현재 무기: {RunContext.GetWeaponName()}"
        );

        Debug.Log("[RewardNodeController] OpenRewardPanel");
    }

    public void CloseRewardPanel()
    {
        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (resultPopup != null)
            resultPopup.SetActive(false);
    }

    public void ChooseGuild()
    {
        Debug.Log("[RewardNodeController] ChooseGuild clicked");

        if (rewardClaimed) return;

        if (!RunContext.CanRecruitCompanion())
        {
            SetInfo($"동료는 최대 {RunContext.MaxCompanionCount}명까지 모집할 수 있습니다.");
            return;
        }

        bool success = RunContext.RecruitCompanion();

        if (!success)
        {
            SetInfo("동료 모집에 실패했습니다.");
            return;
        }

        rewardClaimed = true;
        ShowResultPopup(
            $"길드 보상 획득!\n\n동료가 합류했습니다.\n현재 동료 수: {RunContext.CompanionCount}/{RunContext.MaxCompanionCount}"
        );
    }

    public void ChooseBlacksmith()
    {
        Debug.Log("[RewardNodeController] ChooseBlacksmith clicked");

        if (rewardClaimed) return;

        if (!RunContext.CanUpgradeWeapon())
        {
            SetInfo("무기가 이미 최대 강화 단계입니다.");
            return;
        }

        bool success = RunContext.UpgradeWeapon();

        if (!success)
        {
            SetInfo("무기 강화에 실패했습니다.");
            return;
        }

        rewardClaimed = true;
        ShowResultPopup(
            $"대장간 보상 획득!\n\n무기 업그레이드 완료.\n현재 무기: {RunContext.GetWeaponName()}"
        );
    }

    private void ShowChoiceUI()
    {
        if (choiceRoot != null)
            choiceRoot.SetActive(true);

        if (guildButton != null)
            guildButton.SetActive(true);

        if (blacksmithButton != null)
            blacksmithButton.SetActive(true);

        if (resultPopup != null)
            resultPopup.SetActive(false);
    }

    private void ShowResultPopup(string message)
    {
        if (choiceRoot != null)
            choiceRoot.SetActive(false);

        if (guildButton != null)
            guildButton.SetActive(false);

        if (blacksmithButton != null)
            blacksmithButton.SetActive(false);

        if (resultPopup != null)
            resultPopup.SetActive(true);

        if (resultText != null)
            resultText.text = message;

        Debug.Log("[RewardNodeController] Reward applied successfully");
        Debug.Log("[RewardNodeController] " + message);
    }

    public void OnClickConfirm()
    {
        Debug.Log("[RewardNodeController] Confirm clicked");

        CloseRewardPanel();

        onRewardCompleted?.Invoke();
        onRewardCompleted = null;
    }

    public void ResetRewardUI()
    {
        rewardClaimed = false;
        ShowChoiceUI();
        SetInfo("보상을 선택하세요.");
    }

    private void SetInfo(string message)
    {
        if (infoText != null)
            infoText.text = message;

        Debug.Log("[RewardNodeController] " + message);
    }
}
