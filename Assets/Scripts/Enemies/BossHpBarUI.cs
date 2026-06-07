using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHpBarUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private CanvasGroup canvasGroup;

    private Health targetHealth;

    public void Bind(Health healthTarget, string bossName)
    {
        targetHealth = healthTarget;

        if (bossNameText != null)
            bossNameText.text = bossName;

        SetVisible(true);
        RefreshUI();
    }

    private void Update()
    {
        if (targetHealth == null)
        {
            Destroy(gameObject);
            return;
        }

        RefreshUI();

        if (targetHealth.CurrentHP <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void RefreshUI()
    {
        if (targetHealth == null)
            return;

        float ratio = targetHealth.MaxHP > 0
            ? (float)targetHealth.CurrentHP / targetHealth.MaxHP
            : 0f;

        ratio = Mathf.Clamp01(ratio);

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (hpText != null)
            hpText.text = $"{targetHealth.CurrentHP} / {targetHealth.MaxHP}";
    }

    private void SetVisible(bool value)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = value ? 1f : 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
