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
    [SerializeField] private Button guildButton;
    [SerializeField] private Button blacksmithButton;

    [Header("Result Popup UI")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;

    private Action onRewardCompleted;
    private bool rewardClaimed = false;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (rewardPanel != null)
        {
            canvasGroup = rewardPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = rewardPanel.AddComponent<CanvasGroup>();

            rewardPanel.SetActive(false);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (guildButton != null)
            guildButton.onClick.AddListener(ChooseGuild);

        if (blacksmithButton != null)
            blacksmithButton.onClick.AddListener(ChooseBlacksmith);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClickConfirm);
    }

    public void OpenRewardPanel(Action onComplete)
    {
        onRewardCompleted = onComplete;
        rewardClaimed = false;

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            rewardPanel.transform.SetAsLastSibling();
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (choiceRoot != null)
            choiceRoot.SetActive(true);

        if (resultPopup != null)
            resultPopup.SetActive(false);

        SetInfo(
            $"보상을 선택하세요.\n" +
            $"현재 동료 수: {RunContext.CompanionCount}/{RunContext.MaxCompanionCount}\n" +
            $"현재 무기: {RunContext.GetWeaponName()}"
        );
    }

    public void ChooseGuild()
    {
        Debug.Log("[RewardNodeController] ChooseGuild clicked");

        if (rewardClaimed)
            return;

        if (!RunContext.CanRecruitCompanion())
        {
            ShowResult($"동료는 최대 {RunContext.MaxCompanionCount}명까지 모집 가능합니다.");
            return;
        }

        bool success = RunContext.RecruitCompanion();

        if (success)
        {
            rewardClaimed = true;
            ShowResult(
                $"길드 보상 적용 완료!\n" +
                $"동료 수: {RunContext.CompanionCount}/{RunContext.MaxCompanionCount}\n" +
                $"다음 전투 시작 시 동료가 생성됩니다."
            );
        }
        else
        {
            ShowResult("동료 모집 적용 실패");
        }
    }

    public void ChooseBlacksmith()
    {
        Debug.Log("[RewardNodeController] ChooseBlacksmith clicked");

        if (rewardClaimed)
            return;

        if (!RunContext.CanUpgradeWeapon())
        {
            ShowResult($"이미 최고 무기 단계입니다.\n현재 무기: {RunContext.GetWeaponName()}");
            return;
        }

        bool success = RunContext.UpgradeWeapon();

        if (success)
        {
            rewardClaimed = true;
            ShowResult(
                $"대장간 보상 적용 완료!\n" +
                $"현재 무기: {RunContext.GetWeaponName()}\n" +
                $"다음 전투 시작 시 새 총알로 적용됩니다."
            );
        }
        else
        {
            ShowResult("무기 업그레이드 적용 실패");
        }
    }

    public void OnClickConfirm()
    {
        ClosePanel();
        onRewardCompleted?.Invoke();
        onRewardCompleted = null;
    }

    public void CancelReward()
    {
        ClosePanel();
        onRewardCompleted = null;
        rewardClaimed = false;
    }

    private void ClosePanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (resultPopup != null)
            resultPopup.SetActive(false);
    }

    private void ShowResult(string msg)
    {
        if (choiceRoot != null)
            choiceRoot.SetActive(false);

        if (resultPopup != null)
            resultPopup.SetActive(true);

        if (resultText != null)
            resultText.text = msg;
    }

    private void SetInfo(string msg)
    {
        if (infoText != null)
            infoText.text = msg;
    }
}
