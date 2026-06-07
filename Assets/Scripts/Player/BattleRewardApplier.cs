using UnityEngine;

public class BattleRewardApplier : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerCombatStats playerCombatStats;

    [Header("Companion")]
    [SerializeField] private GameObject companionPrefab;
    [SerializeField] private Transform companionRoot;
    [SerializeField] private Vector3 leftOffset = new Vector3(-1.2f, 0f, 0f);
    [SerializeField] private Vector3 rightOffset = new Vector3(1.2f, 0f, 0f);

    [Header("Weapon")]
    [SerializeField] private AutoBowShooter autoBowShooter;

    private void Start()
    {
        ResolveReferences();
        ApplyPersistentStatsToRuntime();
        ApplyCompanionReward();
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (playerHealth == null && player != null)
            playerHealth = player.GetComponentInParent<Health>();

        if (playerCombatStats == null && player != null)
            playerCombatStats = player.GetComponentInParent<PlayerCombatStats>();

        if (autoBowShooter == null && player != null)
            autoBowShooter = player.GetComponentInChildren<AutoBowShooter>();

        if (companionRoot == null)
            companionRoot = transform;
    }

    private void ApplyPersistentStatsToRuntime()
    {
        if (playerHealth != null)
        {
            playerHealth.SetHP(RunContext.CurrentHP, RunContext.MaxHP);
            Debug.Log($"[BattleRewardApplier] HP applied = {RunContext.CurrentHP}/{RunContext.MaxHP}");
        }

        if (playerCombatStats != null)
        {
            playerCombatStats.ApplyRunContextStats();
            Debug.Log($"[BattleRewardApplier] Combat stats applied / weapon={playerCombatStats.WeaponLevel}, aspd={playerCombatStats.AttackSpeedPercent}, apow={playerCombatStats.AttackPowerPercent}");
        }

        if (autoBowShooter != null)
        {
            autoBowShooter.SetWeaponLevel(RunContext.WeaponUpgradeLevel);
            autoBowShooter.RefreshCombatStats();
            Debug.Log($"[BattleRewardApplier] Shooter applied / weapon={RunContext.WeaponUpgradeLevel}");
        }
    }

    private void ApplyCompanionReward()
    {
        if (player == null || companionPrefab == null)
            return;

        int count = RunContext.CompanionCount;

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Vector3.zero;

            if (i == 0)
                offset = leftOffset;
            else if (i == 1)
                offset = rightOffset;
            else
                offset = new Vector3((i - 1) * 1.1f, 0f, 0f);

            Instantiate(companionPrefab, player.position + offset, Quaternion.identity, companionRoot);
        }

        Debug.Log($"[BattleRewardApplier] Companion spawned count = {count}");
    }
}
