using System.Collections.Generic;
using UnityEngine;

public static class MapFloorGenerator
{
    // 열 위치
    private const float COL1_X = 300f;   // A열
    private const float COL2_X = 800f;   // B열
    private const float COL3_X = 1300f;  // C열
    private const float COL4_X = 1800f;  // Reward / Boss

    // 행 위치
    private const float TOP_Y = 180f;
    private const float MID_Y = 0f;
    private const float BOT_Y = -180f;

    public static List<MapNodeData> GenerateFloor(int floorIndex)
    {
        List<MapNodeData> nodes = new List<MapNodeData>();

        // ===== A열 : 전투 2개 =====
        MapNodeData a1 = CreateNormalBattleNode(floorIndex, 1, 0, "A1", COL1_X, TOP_Y, true);
        MapNodeData a2 = CreateNormalBattleNode(floorIndex, 1, 1, "A2", COL1_X, BOT_Y, true);

        nodes.Add(a1);
        nodes.Add(a2);

        // ===== B열 =====
        MapNodeData b1;
        MapNodeData b2;
        MapNodeData b3;

        if (IsRewardFloor(floorIndex))
        {
            // 1,2,4층: B열 3개, 하드 랜덤 등장
            b1 = CreateRandomBattleNode(floorIndex, 2, 0, "B1", COL2_X, TOP_Y);
            b2 = CreateRandomBattleNode(floorIndex, 2, 1, "B2", COL2_X, MID_Y);
            b3 = CreateRandomBattleNode(floorIndex, 2, 2, "B3", COL2_X, BOT_Y);
        }
        else
        {
            // 3,5층: B열 3개, 하드 랜덤 등장
            b1 = CreateRandomBattleNode(floorIndex, 2, 0, "B1", COL2_X, TOP_Y);
            b2 = CreateRandomBattleNode(floorIndex, 2, 1, "B2", COL2_X, MID_Y);
            b3 = CreateRandomBattleNode(floorIndex, 2, 2, "B3", COL2_X, BOT_Y);
        }

        nodes.Add(b1);
        nodes.Add(b2);
        nodes.Add(b3);

        // 연결: A열 → B열
        ConnectNodes(a1, b1, b2, b3);
        ConnectNodes(a2, b1, b2, b3);

        // ===== 층별 분기 =====
        if (IsRewardFloor(floorIndex))
        {
            // ===== C열 : 전투 2개 =====
            MapNodeData c1 = CreateNormalBattleNode(floorIndex, 3, 0, "C1", COL3_X, TOP_Y, false);
            MapNodeData c2 = CreateNormalBattleNode(floorIndex, 3, 1, "C2", COL3_X, BOT_Y, false);

            nodes.Add(c1);
            nodes.Add(c2);

            // 연결: B열 → C열
            ConnectNodes(b1, c1, c2);
            ConnectNodes(b2, c1, c2);
            ConnectNodes(b3, c1, c2);

            // Reward
            MapNodeData reward = CreateNode(
                floorIndex: floorIndex,
                columnIndex: 4,
                laneIndex: 0,
                nodeId: "R1",
                nodeType: NodeType.Reward,
                themeType: ThemeType.None,
                nodeState: NodeState.Locked,
                uiPosition: new Vector2(COL4_X, MID_Y)
            );

            nodes.Add(reward);

            // 연결: C열 → Reward
            ConnectNodes(c1, reward);
            ConnectNodes(c2, reward);
        }
        else if (IsBossFloor(floorIndex))
        {
            // ===== C열 : 전투 3개, 하드 랜덤 등장 =====
            MapNodeData c1 = CreateRandomBattleNode(floorIndex, 3, 0, "C1", COL3_X, TOP_Y);
            MapNodeData c2 = CreateRandomBattleNode(floorIndex, 3, 1, "C2", COL3_X, MID_Y);
            MapNodeData c3 = CreateRandomBattleNode(floorIndex, 3, 2, "C3", COL3_X, BOT_Y);

            nodes.Add(c1);
            nodes.Add(c2);
            nodes.Add(c3);

            // 연결: B열 → C열
            ConnectNodes(b1, c1, c2, c3);
            ConnectNodes(b2, c1, c2, c3);
            ConnectNodes(b3, c1, c2, c3);

            // Boss
            MapNodeData boss = CreateNode(
                floorIndex: floorIndex,
                columnIndex: 4,
                laneIndex: 0,
                nodeId: "Boss",
                nodeType: NodeType.Boss,
                themeType: GetRandomTheme(),
                nodeState: NodeState.Locked,
                uiPosition: new Vector2(COL4_X, MID_Y)
            );

            nodes.Add(boss);

            // 연결: C열 → Boss
            ConnectNodes(c1, boss);
            ConnectNodes(c2, boss);
            ConnectNodes(c3, boss);
        }
        else
        {
            // 예외층: Reward 구조로 처리
            MapNodeData c1 = CreateNormalBattleNode(floorIndex, 3, 0, "C1", COL3_X, TOP_Y, false);
            MapNodeData c2 = CreateNormalBattleNode(floorIndex, 3, 1, "C2", COL3_X, BOT_Y, false);

            nodes.Add(c1);
            nodes.Add(c2);

            ConnectNodes(b1, c1, c2);
            ConnectNodes(b2, c1, c2);
            ConnectNodes(b3, c1, c2);

            MapNodeData reward = CreateNode(
                floorIndex: floorIndex,
                columnIndex: 4,
                laneIndex: 0,
                nodeId: "R1",
                nodeType: NodeType.Reward,
                themeType: ThemeType.None,
                nodeState: NodeState.Locked,
                uiPosition: new Vector2(COL4_X, MID_Y)
            );

            nodes.Add(reward);

            ConnectNodes(c1, reward);
            ConnectNodes(c2, reward);
        }

        return nodes;
    }

    private static bool IsRewardFloor(int floorIndex)
    {
        return floorIndex == 1 || floorIndex == 2 || floorIndex == 4;
    }

    private static bool IsBossFloor(int floorIndex)
    {
        return floorIndex == 3 || floorIndex == 5;
    }

    private static MapNodeData CreateNormalBattleNode(
        int floorIndex,
        int columnIndex,
        int laneIndex,
        string nodeId,
        float x,
        float y,
        bool selectable)
    {
        return CreateNode(
            floorIndex: floorIndex,
            columnIndex: columnIndex,
            laneIndex: laneIndex,
            nodeId: nodeId,
            nodeType: NodeType.NormalBattle,
            themeType: GetRandomTheme(),
            nodeState: selectable ? NodeState.Selectable : NodeState.Locked,
            uiPosition: new Vector2(x, y)
        );
    }

    private static MapNodeData CreateRandomBattleNode(
        int floorIndex,
        int columnIndex,
        int laneIndex,
        string nodeId,
        float x,
        float y)
    {
        NodeType nodeType;

        // 일반 / 하드 랜덤
        if (floorIndex <= 2)
        {
            nodeType = Random.value < 0.75f
                ? NodeType.NormalBattle
                : NodeType.HardBattle;
        }
        else
        {
            nodeType = Random.value < 0.55f
                ? NodeType.NormalBattle
                : NodeType.HardBattle;
        }

        return CreateNode(
            floorIndex: floorIndex,
            columnIndex: columnIndex,
            laneIndex: laneIndex,
            nodeId: nodeId,
            nodeType: nodeType,
            themeType: GetRandomTheme(),
            nodeState: NodeState.Locked,
            uiPosition: new Vector2(x, y)
        );
    }

    private static MapNodeData CreateNode(
        int floorIndex,
        int columnIndex,
        int laneIndex,
        string nodeId,
        NodeType nodeType,
        ThemeType themeType,
        NodeState nodeState,
        Vector2 uiPosition)
    {
        MapNodeData node = new MapNodeData();
        node.nodeId = $"F{floorIndex}_{nodeId}";
        node.floorIndex = floorIndex;
        node.columnIndex = columnIndex;
        node.laneIndex = laneIndex;
        node.nodeType = nodeType;
        node.themeType = themeType;
        node.nodeState = nodeState;
        node.uiPosition = uiPosition;
        node.nextNodeIds = new List<string>();

        return node;
    }

    private static ThemeType GetRandomTheme()
    {
        int rand = Random.Range(0, 3);

        switch (rand)
        {
            case 0: return ThemeType.Forest;
            case 1: return ThemeType.Sky;
            case 2: return ThemeType.Sea;
            default: return ThemeType.Forest;
        }
    }

    private static void ConnectNodes(MapNodeData fromNode, params MapNodeData[] toNodes)
    {
        if (fromNode == null || toNodes == null)
            return;

        for (int i = 0; i < toNodes.Length; i++)
        {
            if (toNodes[i] == null)
                continue;

            if (!fromNode.nextNodeIds.Contains(toNodes[i].nodeId))
            {
                fromNode.nextNodeIds.Add(toNodes[i].nodeId);
            }
        }
    }

    public static MapNodeData Find(List<MapNodeData> nodes, string nodeId)
    {
        if (nodes == null || string.IsNullOrEmpty(nodeId))
            return null;

        return nodes.Find(n => n.nodeId == nodeId);
    }
}
