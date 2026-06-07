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
    // =========================
    // 기본 진행 상태
    // =========================
    public static int CurrentFloor { get; set; } = 1;

    // 전투 진입 전 저장되는 노드 정보
    public static string PendingNodeId { get; private set; }
    public static ThemeType NextBattleTheme { get; private set; } = ThemeType.None;
    public static NodeType NextNodeType { get; private set; } = NodeType.NormalBattle;

    // 전투 종료 후 맵 복귀 처리용
    public static BattleResult PendingBattleResult { get; private set; } = BattleResult.None;

    // 맵 노드 저장본
    private static List<MapNodeData> savedNodes = null;

    // =========================
    // 플레이어 런 데이터
    // =========================
    public static int Coin { get; private set; } = 0;

    public static int MaxHP { get; private set; } = 10;
    public static int CurrentHP { get; private set; } = 10;

    // 누적 퍼센트 보너스
    // 예: 0.20f = 20%
    public static float AttackSpeedBonusPercent { get; private set; } = 0f;
    public static float AttackPowerBonusPercent { get; private set; } = 0f;

    // =========================
    // 무기 업그레이드
    // 0 = 목재 활
    // 1 = 권총
    // 2 = 라이플
    // 3 = 산탄총
    // 4 = 레이저
    // =========================
    public static int WeaponUpgradeLevel { get; private set; } = 0;
    public static int MaxWeaponUpgradeLevel => 4;

    // 무기 단계당 추가 데미지 보너스
    public static float WeaponFlatDamageBonus => WeaponUpgradeLevel * 0.5f;

    public static string WeaponName => GetWeaponName(WeaponUpgradeLevel);

    // =========================
    // 동료
    // =========================
    public static int CompanionCount { get; private set; } = 0;
    public static int MaxCompanionCount => 1;

    // =========================
    // 테마 방문 카운트
    // =========================
    public static int ForestVisitCount { get; private set; } = 0;
    public static int SkyVisitCount { get; private set; } = 0;
    public static int SeaVisitCount { get; private set; } = 0;

    // =========================
    // 새 게임 시작 초기화
    // =========================
    // 새 게임 시작 시 층, 체력, 코인, 무기, 동료 등 전체 런 상태를 초기화한다.
    public static void ResetForNewRun()
    {
        CurrentFloor = 1;

        PendingNodeId = null;
        NextBattleTheme = ThemeType.None;
        NextNodeType = NodeType.NormalBattle;
        PendingBattleResult = BattleResult.None;

        savedNodes = null;

        Coin = 0;

        MaxHP = 10;
        CurrentHP = 10;

        AttackSpeedBonusPercent = 0f;
        AttackPowerBonusPercent = 0f;

        WeaponUpgradeLevel = 0;
        CompanionCount = 0;

        ForestVisitCount = 0;
        SkyVisitCount = 0;
        SeaVisitCount = 0;
    }

    // =========================
    // 전투 진입 / 종료 플래그
    // =========================
    // 맵에서 선택한 노드 정보를 저장해 다음 전투 씬이 어떤 전투인지 알 수 있게 한다.
    public static void SetBattleEntry(MapNodeData node)
    {
        if (node == null)
            return;

        CurrentFloor = Mathf.Max(1, node.floorIndex);
        PendingNodeId = node.nodeId;
        NextBattleTheme = node.themeType;
        NextNodeType = node.nodeType;

        RecordThemeVisit(node.themeType);
    }

    public static void SetBattleResult(BattleResult result)
    {
        PendingBattleResult = result;
    }

    public static void ClearBattleFlags()
    {
        PendingNodeId = null;
        NextBattleTheme = ThemeType.None;
        NextNodeType = NodeType.NormalBattle;
        PendingBattleResult = BattleResult.None;
    }

    // 층 전환 직전에 전투 관련 플래그와 저장된 맵 데이터를 초기화한다.
    public static void PrepareForFloorChange(int nextFloor)
    {
        CurrentFloor = Mathf.Clamp(nextFloor, 1, 999);

        PendingNodeId = null;
        NextBattleTheme = ThemeType.None;
        NextNodeType = NodeType.NormalBattle;
        PendingBattleResult = BattleResult.None;

        SetMapNodes(null);
    }

    // =========================
    // 맵 노드 저장 / 복원
    // =========================
    // 현재 맵 상태를 깊은 복사로 저장해 씬 전환 후에도 진행도를 유지한다.
    public static void SetMapNodes(List<MapNodeData> nodes)
    {
        if (nodes == null)
        {
            savedNodes = null;
            return;
        }

        savedNodes = CloneNodeList(nodes);
    }

    public static List<MapNodeData> GetMapNodes()
    {
        if (savedNodes == null)
            return null;

        return CloneNodeList(savedNodes);
    }

    private static List<MapNodeData> CloneNodeList(List<MapNodeData> source)
    {
        if (source == null)
            return null;

        List<MapNodeData> clone = new List<MapNodeData>(source.Count);

        for (int i = 0; i < source.Count; i++)
        {
            MapNodeData src = source[i];
            if (src == null)
            {
                clone.Add(null);
                continue;
            }

            MapNodeData copy = new MapNodeData
            {
                nodeId = src.nodeId,
                floorIndex = src.floorIndex,
                columnIndex = src.columnIndex,
                laneIndex = src.laneIndex,
                nodeType = src.nodeType,
                themeType = src.themeType,
                nodeState = src.nodeState,
                uiPosition = src.uiPosition,
                nextNodeIds = src.nextNodeIds != null
                    ? new List<string>(src.nextNodeIds)
                    : new List<string>()
            };

            clone.Add(copy);
        }

        return clone;
    }

    // =========================
    // 코인
    // =========================
    // 전투/보상 결과로 획득한 코인을 런 전체 상태에 누적한다.
    public static void AddCoin(int amount)
    {
        if (amount <= 0)
            return;

        Coin += amount;
    }

    public static bool TrySpendCoin(int amount)
    {
        if (amount <= 0)
            return true;

        if (Coin < amount)
            return false;

        Coin -= amount;
        return true;
    }

    public static void SetCoin(int value)
    {
        Coin = Mathf.Max(0, value);
    }

    // =========================
    // HP / 생존 데이터
    // =========================
    public static void SetCurrentHP(int value)
    {
        CurrentHP = Mathf.Clamp(value, 0, MaxHP);
    }

    public static void SetMaxHP(int value, bool fullHeal = false)
    {
        MaxHP = Mathf.Max(1, value);

        if (fullHeal)
            CurrentHP = MaxHP;
        else
            CurrentHP = Mathf.Clamp(CurrentHP, 0, MaxHP);
    }

    public static void HealPlayer(int amount)
    {
        if (amount <= 0)
            return;

        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, MaxHP);
    }

    public static void DamagePlayer(int amount)
    {
        if (amount <= 0)
            return;

        CurrentHP = Mathf.Clamp(CurrentHP - amount, 0, MaxHP);
    }

    public static void IncreaseMaxHP(int amount, bool fullHeal)
    {
        if (amount <= 0)
            return;

        MaxHP = Mathf.Max(1, MaxHP + amount);

        if (fullHeal)
            CurrentHP = MaxHP;
        else
            CurrentHP = Mathf.Clamp(CurrentHP, 0, MaxHP);
    }

    // =========================
    // 공격속도 / 공격력 영구 보너스
    // =========================
    public static void AddAttackSpeedPercent(float value)
    {
        if (value <= 0f)
            return;

        AttackSpeedBonusPercent += value;
    }

    public static void AddAttackPowerPercent(float value)
    {
        if (value <= 0f)
            return;

        AttackPowerBonusPercent += value;
    }

    public static float GetAttackSpeedMultiplier()
    {
        return 1f + AttackSpeedBonusPercent;
    }

    public static float GetAttackPowerMultiplier()
    {
        return 1f + AttackPowerBonusPercent;
    }

    // =========================
    // 무기 업그레이드
    // =========================
    public static bool CanUpgradeWeapon()
    {
        return WeaponUpgradeLevel < MaxWeaponUpgradeLevel;
    }

    // 무기 업그레이드 단계가 남아 있으면 한 단계 상승시킨다.
    public static bool UpgradeWeapon()
    {
        if (!CanUpgradeWeapon())
            return false;

        WeaponUpgradeLevel++;
        return true;
    }

    // 보상 노드에서 호출하기 쉬운 별칭
    public static bool GrantBlacksmithReward()
    {
        return UpgradeWeapon();
    }

    public static void SetWeaponUpgradeLevel(int level)
    {
        WeaponUpgradeLevel = Mathf.Clamp(level, 0, MaxWeaponUpgradeLevel);
    }

    public static bool HasWeaponUpgrade()
    {
        return WeaponUpgradeLevel > 0;
    }

    public static string GetWeaponName()
    {
        return GetWeaponName(WeaponUpgradeLevel);
    }

    public static string GetWeaponName(int level)
    {
        switch (Mathf.Clamp(level, 0, MaxWeaponUpgradeLevel))
        {
            case 0: return "목재 활";
            case 1: return "권총";
            case 2: return "라이플";
            case 3: return "산탄총";
            case 4: return "레이저";
            default: return "목재 활";
        }
    }

    // =========================
    // 동료
    // =========================
    public static bool CanRecruitCompanion()
    {
        return CompanionCount < MaxCompanionCount;
    }

    // 동료 최대 수 제한 안에서 새 동료 보상을 적용한다.
    public static bool RecruitCompanion()
    {
        if (!CanRecruitCompanion())
            return false;

        CompanionCount++;
        return true;
    }

    // 보상 노드에서 호출하기 쉬운 별칭
    public static bool GrantGuildReward()
    {
        return RecruitCompanion();
    }

    public static void SetCompanionCount(int count)
    {
        CompanionCount = Mathf.Clamp(count, 0, MaxCompanionCount);
    }

    public static bool HasCompanion()
    {
        return CompanionCount > 0;
    }

    // =========================
    // 테마 방문 카운트
    // =========================
    public static void RecordThemeVisit(ThemeType theme)
    {
        switch (theme)
        {
            case ThemeType.Forest:
                ForestVisitCount++;
                break;

            case ThemeType.Sky:
                SkyVisitCount++;
                break;

            case ThemeType.Sea:
                SeaVisitCount++;
                break;
        }
    }

    public static int GetThemeVisitCount(ThemeType theme)
    {
        switch (theme)
        {
            case ThemeType.Forest: return ForestVisitCount;
            case ThemeType.Sky: return SkyVisitCount;
            case ThemeType.Sea: return SeaVisitCount;
            default: return 0;
        }
    }

    // UI 표시용: 최다 진입 테마들을 모두 반환
    public static List<ThemeType> GetMostVisitedThemes()
    {
        List<ThemeType> result = new List<ThemeType>();

        int maxCount = Mathf.Max(ForestVisitCount, SkyVisitCount, SeaVisitCount);

        if (maxCount <= 0)
            return result;

        if (ForestVisitCount == maxCount)
            result.Add(ThemeType.Forest);

        if (SkyVisitCount == maxCount)
            result.Add(ThemeType.Sky);

        if (SeaVisitCount == maxCount)
            result.Add(ThemeType.Sea);

        return result;
    }

    // 보스 선택용: 동률이면 그중 하나 랜덤 선택
    public static ThemeType GetMostVisitedTheme()
    {
        List<ThemeType> tiedThemes = GetMostVisitedThemes();

        if (tiedThemes.Count == 0)
            return ThemeType.None;

        if (tiedThemes.Count == 1)
            return tiedThemes[0];

        int randomIndex = Random.Range(0, tiedThemes.Count);
        return tiedThemes[randomIndex];
    }

    // UI 표시용 문자열
    public static string GetMostVisitedThemesLabel()
    {
        List<ThemeType> tiedThemes = GetMostVisitedThemes();

        if (tiedThemes.Count == 0)
            return "없음";

        List<string> names = new List<string>();

        for (int i = 0; i < tiedThemes.Count; i++)
        {
            names.Add(GetThemeLabel(tiedThemes[i]));
        }

        return string.Join(", ", names);
    }

    public static string GetThemeLabel(ThemeType theme)
    {
        switch (theme)
        {
            case ThemeType.Forest: return "숲";
            case ThemeType.Sky: return "하늘";
            case ThemeType.Sea: return "바다";
            default: return "없음";
        }
    }
}
