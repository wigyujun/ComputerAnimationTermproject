using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapNodeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image bgImage;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button button;

    [Header("Optional")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Color lockedTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color normalTextColor = Color.white;

    [Header("Node Sprites")]
    [SerializeField] private Sprite normalBattleSprite;
    [SerializeField] private Sprite hardBattleSprite;
    [SerializeField] private Sprite rewardSprite;
    [SerializeField] private Sprite shopSprite;
    [SerializeField] private Sprite bossSprite;

    [Header("Locked Visual")]
    [SerializeField] private Color lockedNodeTint = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color normalNodeTint = Color.white;

    private MapNodeData data;
    private MapManager mapManager;

    public string NodeId => data != null ? data.nodeId : string.Empty;

    public void Setup(MapNodeData newData, MapManager manager)
    {
        data = newData;
        mapManager = manager;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (data == null) return;

        RefreshLabel();
        RefreshBackgroundSprite();
        RefreshInteractable();
        RefreshIcon();
    }

    public void Refresh(MapNodeData newData)
    {
        data = newData;
        Refresh();
    }

    private void RefreshLabel()
    {
        if (label == null) return;

        label.text = GetNodeLabel(data);

        if (data.nodeState == NodeState.Locked)
            label.color = lockedTextColor;
        else
            label.color = normalTextColor;
    }

    private void RefreshBackgroundSprite()
    {
        if (bgImage == null || data == null)
            return;

        bgImage.sprite = GetNodeSprite(data.nodeType);
        bgImage.type = Image.Type.Simple;
        bgImage.preserveAspect = true;

        if (data.nodeState == NodeState.Locked)
            bgImage.color = lockedNodeTint;
        else
            bgImage.color = normalNodeTint;
    }

    private void RefreshInteractable()
    {
        if (button == null) return;

        button.interactable = (data.nodeState == NodeState.Selectable);
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        // 아이콘을 안 쓸 거면 숨김
        iconImage.gameObject.SetActive(false);
    }

    private void OnClick()
    {
        if (mapManager == null || data == null) return;
        if (data.nodeState != NodeState.Selectable) return;

        mapManager.OnNodeClicked(data.nodeId);
    }

    private Sprite GetNodeSprite(NodeType nodeType)
    {
        switch (nodeType)
        {
            case NodeType.NormalBattle:
                return normalBattleSprite;

            case NodeType.HardBattle:
                return hardBattleSprite;

            case NodeType.Reward:
                return rewardSprite;

            case NodeType.Shop:
                return shopSprite;

            case NodeType.Boss:
                return bossSprite;

            default:
                return normalBattleSprite;
        }
    }

    private string GetNodeLabel(MapNodeData node)
    {
        if (node == null)
            return "노드";

        switch (node.nodeType)
        {
            case NodeType.NormalBattle:
                return $"전투 · {GetThemeLabel(node.themeType)}";

            case NodeType.HardBattle:
                return $"긴급전투 · {GetThemeLabel(node.themeType)}";

            case NodeType.Boss:
                return $"보스 · {GetThemeLabel(node.themeType)}";

            case NodeType.Reward:
                return "보상";

            case NodeType.Shop:
                return "상점";

            default:
                return "노드";
        }
    }

    private string GetThemeLabel(ThemeType themeType)
    {
        switch (themeType)
        {
            case ThemeType.Forest:
                return "숲";
            case ThemeType.Sky:
                return "하늘";
            case ThemeType.Sea:
                return "바다";
            default:
                return "없음";
        }
    }
}
