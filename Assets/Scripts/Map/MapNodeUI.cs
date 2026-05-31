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
    [SerializeField] private Image iconImage;   // 있으면 사용, 없어도 됨
    [SerializeField] private Color lockedTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color normalTextColor = Color.white;

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
        RefreshColor();
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

    private void RefreshColor()
    {
        if (bgImage == null) return;

        bgImage.color = GetNodeColor(data.nodeType, data.nodeState);
    }

    private void RefreshInteractable()
    {
        if (button == null) return;

        button.interactable = (data.nodeState == NodeState.Selectable);
    }

    private void RefreshIcon()
    {
        if (iconImage == null) return;

        // 아이콘 이미지를 별도로 안 쓸 거면 그냥 색만 맞춰줌
        iconImage.color = GetIconColor(data.nodeType, data.nodeState);
    }

    private void OnClick()
    {
        if (mapManager == null || data == null) return;
        if (data.nodeState != NodeState.Selectable) return;

        mapManager.OnNodeClicked(data.nodeId);
    }

    private string GetNodeLabel(MapNodeData node)
    {
        if (node == null) return "노드";

        switch (node.nodeType)
        {
            case NodeType.NormalBattle:
                return "전투";
            case NodeType.HardBattle:
                return "강적";
            case NodeType.Reward:
                return "보상";
            case NodeType.Shop:
                return "상점";
            case NodeType.Boss:
                return "보스";
            default:
                return "노드";
        }
    }

    private Color GetNodeColor(NodeType nodeType, NodeState nodeState)
    {
        // 상태 우선
        switch (nodeState)
        {
            case NodeState.Locked:
                return new Color(0.28f, 0.28f, 0.28f, 1f);

            case NodeState.Current:
                return new Color(1.00f, 0.85f, 0.20f, 1f);

            case NodeState.Cleared:
                return new Color(0.55f, 0.55f, 0.55f, 1f);

            case NodeState.Selectable:
                return GetSelectableTypeColor(nodeType);
        }

        return Color.white;
    }

    private Color GetSelectableTypeColor(NodeType nodeType)
    {
        switch (nodeType)
        {
            case NodeType.NormalBattle:
                return new Color(0.20f, 0.80f, 1.00f, 1f);   // 파랑
            case NodeType.HardBattle:
                return new Color(1.00f, 0.45f, 0.25f, 1f);   // 주황/빨강
            case NodeType.Reward:
                return new Color(0.35f, 0.95f, 0.45f, 1f);   // 초록
            case NodeType.Shop:
                return new Color(0.75f, 0.45f, 1.00f, 1f);   // 보라
            case NodeType.Boss:
                return new Color(0.95f, 0.15f, 0.20f, 1f);   // 진한 빨강
            default:
                return Color.white;
        }
    }

    private Color GetIconColor(NodeType nodeType, NodeState nodeState)
    {
        if (nodeState == NodeState.Locked)
            return new Color(0.6f, 0.6f, 0.6f, 1f);

        switch (nodeType)
        {
            case NodeType.NormalBattle:
                return new Color(0.85f, 0.95f, 1f, 1f);
            case NodeType.HardBattle:
                return new Color(1f, 0.9f, 0.8f, 1f);
            case NodeType.Reward:
                return new Color(0.9f, 1f, 0.9f, 1f);
            case NodeType.Shop:
                return new Color(0.95f, 0.9f, 1f, 1f);
            case NodeType.Boss:
                return new Color(1f, 0.85f, 0.85f, 1f);
            default:
                return Color.white;
        }
    }
}
