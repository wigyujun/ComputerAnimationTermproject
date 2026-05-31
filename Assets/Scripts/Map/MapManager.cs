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

    [Header("Reward Only")]
    [SerializeField] private RewardNodeController rewardNodeController;

    private List<MapNodeData> nodes = new List<MapNodeData>();
    private readonly Dictionary<string, MapNodeUI> nodeUIMap = new Dictionary<string, MapNodeUI>();

    private string pendingSpecialNodeId;

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

    private void BuildMap()
    {
        ClearChildren(nodesRoot);
        ClearChildren(linesRoot);
        nodeUIMap.Clear();

        if (nodes == null || nodes.Count == 0)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            CreateNodeUI(nodes[i]);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            CreateLinesForNode(nodes[i]);
        }
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

        if (rt == null) return;

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

    public void OnNodeClicked(string nodeId)
    {
        MapNodeData clickedNode = FindNode(nodeId);
        if (clickedNode == null) return;
        if (clickedNode.nodeState != NodeState.Selectable) return;

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
                Debug.Log("Shop 노드는 다음 단계에서 구현 예정");

                // 아직 미구현이므로 선택 복구
                clickedNode.nodeState = NodeState.Selectable;
                RunContext.SetMapNodes(nodes);
                RefreshAll();
                break;
        }
    }

    private void EnterBattle(MapNodeData node)
    {
        if (node == null) return;

        // 네 현재 RunContext 시그니처 기준 유지
        RunContext.SetBattleEntry(node);
        RunContext.SetMapNodes(nodes);

        SceneManager.LoadScene(battleSceneName);
    }

    private void OpenRewardNode(MapNodeData node)
    {
        if (node == null) return;

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

    private void CompleteSpecialNode(string nodeId)
    {
        MapNodeData node = FindNode(nodeId);
        if (node == null)
        {
            pendingSpecialNodeId = null;
            return;
        }

        node.nodeState = NodeState.Cleared;
        UnlockNextNodes(node);

        pendingSpecialNodeId = null;

        RunContext.SetMapNodes(nodes);
        RefreshAll();
        FocusToFirstSelectable();

        Debug.Log($"MapManager: 특수 노드 완료 처리 - {node.nodeId}");
    }

    // 보상 패널을 닫기만 하고 선택 안 했을 때 복구용
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

        Debug.Log("MapManager: 특수 노드 선택 취소");
    }

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

    private void LockOtherSelectableNodes(string exceptNodeId)
    {
        if (nodes == null) return;

        for (int i = 0; i < nodes.Count; i++)
        {
            MapNodeData node = nodes[i];
            if (node == null) continue;

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
        if (node == null || node.nextNodeIds == null) return;

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
            if (pair.Value == null) continue;

            MapNodeData data = FindNode(pair.Key);
            if (data != null)
            {
                pair.Value.Refresh(data);
            }
        }
    }

    private void FocusToCurrentOrFirstSelectable()
    {
        if (nodes == null || nodes.Count == 0) return;

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
        if (nodes == null || nodes.Count == 0) return;

        MapNodeData selectableNode = nodes.Find(n => n.nodeState == NodeState.Selectable);
        if (selectableNode != null)
        {
            FocusToNode(selectableNode);
        }
    }

    private void FocusToNode(MapNodeData node)
    {
        if (node == null) return;
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

        scrollRect.horizontalNormalizedPosition = clampedX / maxX;
    }

    private void ClearChildren(Transform root)
    {
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }
}
