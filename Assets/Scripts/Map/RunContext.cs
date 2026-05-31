using System.Collections.Generic;
using UnityEngine;

public enum BattleResult
{
    None,
    Win,
    Lose
}

public static class RunContext
{
    public static int CurrentFloor = 1;

    public static string PendingNodeId;
    public static ThemeType NextBattleTheme = ThemeType.None;
    public static NodeType NextNodeType;

    public static BattleResult PendingBattleResult = BattleResult.None;

    private static List<MapNodeData> savedNodes;

    // ===== Reward data =====
    public static int CompanionCount = 0;
    public static int WeaponUpgradeLevel = 0;

    public static int MaxCompanionCount = 2;
    public static int MaxWeaponUpgradeLevel = 3;

    public static void SetBattleEntry(MapNodeData node)
    {
        if (node == null) return;

        PendingNodeId = node.nodeId;
        NextBattleTheme = node.themeType;
        NextNodeType = node.nodeType;
    }

    public static void SetBattleResult(BattleResult result)
    {
        PendingBattleResult = result;
    }

    public static void ClearBattleFlags()
    {
        PendingNodeId = null;
        NextBattleTheme = ThemeType.None;
        NextNodeType = default;
        PendingBattleResult = BattleResult.None;
    }

    public static void SetMapNodes(List<MapNodeData> nodes)
    {
        if (nodes == null)
        {
            savedNodes = null;
            return;
        }

        savedNodes = new List<MapNodeData>();
        foreach (var n in nodes)
        {
            savedNodes.Add(CloneNode(n));
        }
    }

    public static List<MapNodeData> GetMapNodes()
    {
        if (savedNodes == null) return null;

        var clone = new List<MapNodeData>();
        foreach (var n in savedNodes)
        {
            clone.Add(CloneNode(n));
        }
        return clone;
    }

    public static bool CanRecruitCompanion()
    {
        return CompanionCount < MaxCompanionCount;
    }

    public static bool RecruitCompanion()
    {
        if (!CanRecruitCompanion()) return false;
        CompanionCount++;
        return true;
    }

    public static bool CanUpgradeWeapon()
    {
        return WeaponUpgradeLevel < MaxWeaponUpgradeLevel;
    }

    public static bool UpgradeWeapon()
    {
        if (!CanUpgradeWeapon()) return false;
        WeaponUpgradeLevel++;
        return true;
    }

    public static string GetWeaponName()
    {
        switch (WeaponUpgradeLevel)
        {
            case 0: return "기본 활";
            case 1: return "강화 활";
            case 2: return "희귀 활";
            case 3: return "전설 활";
            default: return "기본 활";
        }
    }

    public static void ResetForNewRun()
    {
        CurrentFloor = 1;

        PendingNodeId = null;
        NextBattleTheme = ThemeType.None;
        NextNodeType = default;
        PendingBattleResult = BattleResult.None;

        CompanionCount = 0;
        WeaponUpgradeLevel = 0;

        SetMapNodes(null);
    }


    private static MapNodeData CloneNode(MapNodeData src)
    {
        if (src == null) return null;

        return new MapNodeData
        {
            nodeId = src.nodeId,
            floorIndex = src.floorIndex,
            columnIndex = src.columnIndex,
            laneIndex = src.laneIndex,
            nodeType = src.nodeType,
            themeType = src.themeType,
            nodeState = src.nodeState,
            uiPosition = src.uiPosition,
            nextNodeIds = src.nextNodeIds != null ? new List<string>(src.nextNodeIds) : new List<string>()
        };
    }
}
