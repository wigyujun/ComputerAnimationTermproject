using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    [Header("Floor")]
    [SerializeField] private int currentFloor = 1;

    [Header("Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";

    [Header("UI Roots")]
    [SerializeField] private RectTransform nodesRoot;
    [SerializeField] private RectTransform linesRoot;

    [Header("Prefabs")]
    [SerializeField] private MapNodeUI nodePrefab;
    [SerializeField] private Image linePrefab;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private float startFocusViewportRatio = 0.35f;
    [SerializeField] private float contentRightPadding = 500f;

    [Header("Special Node Controllers")]
    [SerializeField] private RewardNodeController rewardNodeController;
    [SerializeField] private ShopNodeController shopNodeController;

    private List<MapNodeData> nodes = new List<MapNodeData>();
    private readonly Dictionary<string, MapNodeUI> nodeUIMap = new Dictionary<string, MapNodeUI>();

    private string pendingSpecialNodeId;

    // 런에 저장된 맵 상태를 복원하거나 새 층 맵을 생성한 뒤 UI를 구성한다.
    private void Start()
    {
        if (RunContext.CurrentFloor > 0)
            currentFloor = RunContext.CurrentFloor;
        else
            RunContext.CurrentFloor = currentFloor;

        List<MapNodeData> savedNodes = RunContext.GetMapNodes();

        if (savedNodes != null && savedNodes.Count > 0)
        {
            nodes = savedNodes;
        }
        else
        {
            nodes = MapFloorGenerator.GenerateFloor(currentFloor);
            RunContext.SetMapNodes(nodes);
        }

        ApplyPendingBattleResult();
        BuildMap();
        RefreshAll();
        FocusToCurrentOrFirstSelectable();
    }

    public void SetFloor(int newFloor)
    {
        currentFloor = Mathf.Max(1, newFloor);
        RunContext.CurrentFloor = currentFloor;

        pendingSpecialNodeId = null;

        nodes = MapFloorGenerator.GenerateFloor(currentFloor);
        RunContext.SetMapNodes(nodes);

        BuildMap();
        RefreshAll();
        FocusToCurrentOrFirstSelectable();
    }

    // 저장된 노드 데이터로 노드 버튼과 연결선을 다시 그려 맵 화면을 만든다.
    private void BuildMap()
    {
        ClearChildren(nodesRoot);
        ClearChildren(linesRoot);
        nodeUIMap.Clear();

        if (nodes == null || nodes.Count == 0)
            return;

        AdjustContentWidth();

        for (int i = 0; i < nodes.Count; i++)
        {
            CreateNodeUI(nodes[i]);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            CreateLinesForNode(nodes[i]);
        }
    }

    private void AdjustContentWidth()
    {
        if (content == null || nodes == null || nodes.Count == 0)
            return;

        float maxX = 0f;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null)
                continue;

            if (nodes[i].uiPosition.x > maxX)
                maxX = nodes[i].uiPosition.x;
        }

        Vector2 size = content.sizeDelta;
        size.x = Mathf.Max(size.x, maxX + contentRightPadding);
        content.sizeDelta = size;
    }

    private void CreateNodeUI(MapNodeData data)
    {
        if (nodePrefab == null || nodesRoot == null || data == null)
            return;

        MapNodeUI ui = Instantiate(nodePrefab, nodesRoot);
        RectTransform rt = ui.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = data.uiPosition;
        }

        ui.Setup(data, this);
        nodeUIMap[data.nodeId] = ui;
    }

    private void CreateLinesForNode(MapNodeData fromNode)
    {
        if (linePrefab == null || linesRoot == null || fromNode == null || fromNode.nextNodeIds == null)
            return;

        foreach (string nextId in fromNode.nextNodeIds)
        {
            MapNodeData toNode = FindNode(nextId);
            if (toNode == null)
                continue;

            CreateLine(fromNode.uiPosition, toNode.uiPosition);
        }
    }

    private void CreateLine(Vector2 from, Vector2 to)
    {
        Image line = Instantiate(linePrefab, linesRoot);
        RectTransform rt = line.GetComponent<RectTransform>();

        if (rt == null)
            return;

        line.raycastTarget = false;

        Vector2 dir = to - from;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector2 mid = (from + to) * 0.5f;

        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = mid;
        rt.sizeDelta = new Vector2(length, 6f);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // 노드 클릭 시 전투/보상/상점 중 어떤 흐름으로 이어질지 결정하는 핵심 분기다.
    public void OnNodeClicked(string nodeId)
    {
        MapNodeData clickedNode = FindNode(nodeId);
        if (clickedNode == null)
            return;

        if (clickedNode.nodeState != NodeState.Selectable)
            return;

        LockOtherSelectableNodes(clickedNode.nodeId);
        clickedNode.nodeState = NodeState.Current;

        RunContext.SetMapNodes(nodes);
        RefreshAll();

        switch (clickedNode.nodeType)
        {
            case NodeType.NormalBattle:
            case NodeType.HardBattle:
            case NodeType.Boss:
                EnterBattle(clickedNode);
                break;

            case NodeType.Reward:
                OpenRewardNode(clickedNode);
                break;

            case NodeType.Shop:
                OpenShopNode(clickedNode);
                break;

            default:
                Debug.LogWarning($"MapManager: 처리되지 않은 노드 타입 - {clickedNode.nodeType}");
                clickedNode.nodeState = NodeState.Selectable;
                RunContext.SetMapNodes(nodes);
                RefreshAll();
                break;
        }
    }

    // 선택한 전투 노드 정보를 RunContext에 저장하고 전투 씬으로 이동한다.
    private void EnterBattle(MapNodeData node)
    {
        if (node == null)
            return;

        RunContext.SetBattleEntry(node);
        RunContext.SetMapNodes(nodes);

        SceneManager.LoadScene(battleSceneName);
    }

    private void OpenRewardNode(MapNodeData node)
    {
        if (node == null)
            return;

        string clickedNodeId = node.nodeId;
        pendingSpecialNodeId = clickedNodeId;

        if (rewardNodeController == null)
        {
            Debug.LogError("MapManager: RewardNodeController 참조가 없습니다.");

            node.nodeState = NodeState.Selectable;
            pendingSpecialNodeId = null;
            RunContext.SetMapNodes(nodes);
            RefreshAll();
            return;
        }

        rewardNodeController.OpenRewardPanel(() =>
        {
            CompleteSpecialNode(clickedNodeId);
        });
    }

    private void OpenShopNode(MapNodeData node)
    {
        if (node == null)
            return;

        string clickedNodeId = node.nodeId;
        pendingSpecialNodeId = clickedNodeId;

        if (shopNodeController == null)
        {
            Debug.LogError("MapManager: ShopNodeController 참조가 없습니다.");

            node.nodeState = NodeState.Selectable;
            pendingSpecialNodeId = null;
            RunContext.SetMapNodes(nodes);
            RefreshAll();
            return;
        }

        shopNodeController.OpenShopPanel(() =>
        {
            CompleteSpecialNode(clickedNodeId);
        });
    }

    // 보상/상점 처리 완료 후 다음 노드 해금 또는 다음 층 이동을 진행한다.
    private void CompleteSpecialNode(string nodeId)
    {
        MapNodeData node = FindNode(nodeId);
        if (node == null)
        {
            pendingSpecialNodeId = null;
            return;
        }

        node.nodeState = NodeState.Cleared;
        pendingSpecialNodeId = null;

        // 1,2,4층 보상 노드 완료 시 다음 층으로 이동
        if (IsFloorEndRewardNode(node))
        {
            int nextFloor = currentFloor + 1;

            if (nextFloor <= 5)
            {
                Debug.Log($"[MapManager] 보상 노드 완료 -> {nextFloor}층 이동");
                RunContext.PrepareForFloorChange(nextFloor);
                SetFloor(nextFloor);
                return;
            }
        }

        // Shop 포함 일반 특수 노드는 다음 노드 열기
        UnlockNextNodes(node);

        RunContext.SetMapNodes(nodes);
        RefreshAll();
        FocusToFirstSelectable();

        Debug.Log($"[MapManager] 특수 노드 완료 처리 - {node.nodeId}");
    }

    public void CancelPendingSpecialNode()
    {
        if (string.IsNullOrEmpty(pendingSpecialNodeId))
            return;

        MapNodeData node = FindNode(pendingSpecialNodeId);
        if (node != null && node.nodeState == NodeState.Current)
        {
            node.nodeState = NodeState.Selectable;
        }

        pendingSpecialNodeId = null;

        RunContext.SetMapNodes(nodes);
        RefreshAll();

        Debug.Log("[MapManager] 특수 노드 선택 취소");
    }

    // 전투 씬에서 돌아온 승패 결과를 현재 맵 노드 상태에 반영한다.
    private void ApplyPendingBattleResult()
    {
        if (RunContext.PendingBattleResult == BattleResult.None)
            return;

        if (string.IsNullOrEmpty(RunContext.PendingNodeId))
        {
            RunContext.ClearBattleFlags();
            return;
        }

        MapNodeData node = FindNode(RunContext.PendingNodeId);
        if (node == null)
        {
            RunContext.ClearBattleFlags();
            return;
        }

        if (RunContext.PendingBattleResult == BattleResult.Win)
        {
            node.nodeState = NodeState.Cleared;
            UnlockNextNodes(node);
        }
        else if (RunContext.PendingBattleResult == BattleResult.Lose)
        {
            node.nodeState = NodeState.Selectable;
        }

        RunContext.ClearBattleFlags();
        RunContext.SetMapNodes(nodes);
    }

    private bool IsFloorEndRewardNode(MapNodeData node)
    {
        if (node == null)
            return false;

        if (node.nodeType != NodeType.Reward)
            return false;

        return node.floorIndex == 1 || node.floorIndex == 2 || node.floorIndex == 4;
    }

    private void LockOtherSelectableNodes(string exceptNodeId)
    {
        if (nodes == null)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            MapNodeData node = nodes[i];
            if (node == null)
                continue;

            if (node.nodeId == exceptNodeId)
                continue;

            if (node.nodeState == NodeState.Selectable)
            {
                node.nodeState = NodeState.Locked;
            }
        }
    }

    private void UnlockNextNodes(MapNodeData node)
    {
        if (node == null || node.nextNodeIds == null)
            return;

        foreach (string nextId in node.nextNodeIds)
        {
            MapNodeData nextNode = FindNode(nextId);
            if (nextNode != null && nextNode.nodeState == NodeState.Locked)
            {
                nextNode.nodeState = NodeState.Selectable;
            }
        }
    }

    private MapNodeData FindNode(string nodeId)
    {
        if (nodes == null || string.IsNullOrEmpty(nodeId))
            return null;

        return nodes.Find(n => n.nodeId == nodeId);
    }

    public void RefreshAll()
    {
        foreach (var pair in nodeUIMap)
        {
            if (pair.Value == null)
                continue;

            MapNodeData data = FindNode(pair.Key);
            if (data != null)
            {
                pair.Value.Refresh(data);
            }
        }
    }

    private void FocusToCurrentOrFirstSelectable()
    {
        if (nodes == null || nodes.Count == 0)
            return;

        MapNodeData currentNode = nodes.Find(n => n.nodeState == NodeState.Current);
        if (currentNode != null)
        {
            FocusToNode(currentNode);
            return;
        }

        FocusToFirstSelectable();
    }

    private void FocusToFirstSelectable()
    {
        if (nodes == null || nodes.Count == 0)
            return;

        MapNodeData selectableNode = nodes.Find(n => n.nodeState == NodeState.Selectable);
        if (selectableNode != null)
        {
            FocusToNode(selectableNode);
        }
    }

    private void FocusToNode(MapNodeData node)
    {
        if (node == null)
            return;

        if (scrollRect == null || viewport == null || content == null)
            return;

        float contentWidth = content.rect.width;
        float viewportWidth = viewport.rect.width;

        if (contentWidth <= viewportWidth)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
            return;
        }

        float targetX = node.uiPosition.x - (viewportWidth * startFocusViewportRatio);
        float maxX = contentWidth - viewportWidth;
        float clampedX = Mathf.Clamp(targetX, 0f, maxX);

        scrollRect.horizontalNormalizedPosition = maxX <= 0f ? 0f : clampedX / maxX;
    }

    private void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }
}
