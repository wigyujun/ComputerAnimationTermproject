using UnityEngine;

public static class FloorBalanceUtility
{
    public struct EnemyFloorProfile
    {
        public int hp;
        public float moveSpeed;
        public float dashSpeed;
        public float fireInterval;
        public float projectileSpeed;
        public int coinReward;
    }

    // 층수와 노드 난이도에 따라 일반 적의 체력/속도/공격값 프로필을 반환한다.
    public static EnemyFloorProfile GetEnemyProfile(int floor, NodeType nodeType)
    {
        int clampedFloor = Mathf.Clamp(floor, 1, 5);
        bool useHardProfile = nodeType == NodeType.HardBattle || nodeType == NodeType.Boss;

        if (useHardProfile)
        {
            switch (clampedFloor)
            {
                case 1:
                    return new EnemyFloorProfile
                    {
                        hp = 8,
                        moveSpeed = 2.7f,
                        dashSpeed = 9.0f,
                        fireInterval = 2.1f,
                        projectileSpeed = 5.8f,
                        coinReward = 2
                    };

                case 2:
                    return new EnemyFloorProfile
                    {
                        hp = 10,
                        moveSpeed = 2.9f,
                        dashSpeed = 10.0f,
                        fireInterval = 1.95f,
                        projectileSpeed = 6.2f,
                        coinReward = 2
                    };

                case 3:
                    return new EnemyFloorProfile
                    {
                        hp = 14,
                        moveSpeed = 3.15f,
                        dashSpeed = 11.2f,
                        fireInterval = 1.8f,
                        projectileSpeed = 6.8f,
                        coinReward = 2
                    };

                case 4:
                    return new EnemyFloorProfile
                    {
                        hp = 24,
                        moveSpeed = 3.5f,
                        dashSpeed = 13.2f,
                        fireInterval = 1.55f,
                        projectileSpeed = 7.5f,
                        coinReward = 2
                    };

                default: // floor 5
                    return new EnemyFloorProfile
                    {
                        hp = 32,
                        moveSpeed = 3.8f,
                        dashSpeed = 14.5f,
                        fireInterval = 1.35f,
                        projectileSpeed = 8.0f,
                        coinReward = 2
                    };
            }
        }

        switch (clampedFloor)
        {
            case 1:
                return new EnemyFloorProfile
                {
                    hp = 5,
                    moveSpeed = 2.4f,
                    dashSpeed = 8.3f,
                    fireInterval = 2.3f,
                    projectileSpeed = 5.5f,
                    coinReward = 1
                };

            case 2:
                return new EnemyFloorProfile
                {
                    hp = 6,
                    moveSpeed = 2.6f,
                    dashSpeed = 9.3f,
                    fireInterval = 2.1f,
                    projectileSpeed = 6.0f,
                    coinReward = 1
                };

            case 3:
                return new EnemyFloorProfile
                {
                    hp = 9,
                    moveSpeed = 2.85f,
                    dashSpeed = 10.5f,
                    fireInterval = 1.9f,
                    projectileSpeed = 6.5f,
                    coinReward = 1
                };

            case 4:
                return new EnemyFloorProfile
                {
                    hp = 16,
                    moveSpeed = 3.15f,
                    dashSpeed = 12.0f,
                    fireInterval = 1.65f,
                    projectileSpeed = 7.0f,
                    coinReward = 1
                };

            default: // floor 5
                return new EnemyFloorProfile
                {
                    hp = 22,
                    moveSpeed = 3.4f,
                    dashSpeed = 13.2f,
                    fireInterval = 1.45f,
                    projectileSpeed = 7.6f,
                    coinReward = 1
                };
        }
    }

    // 생성된 적 오브젝트에 체력, 이동속도, 대시속도, 발사속도, 보상을 일괄 적용한다.
    public static void ApplyEnemyBalance(GameObject enemyObject, int floor, NodeType nodeType)
    {
        if (enemyObject == null)
            return;

        EnemyFloorProfile profile = GetEnemyProfile(floor, nodeType);

        Health health = enemyObject.GetComponent<Health>();
        if (health == null)
            health = enemyObject.GetComponentInChildren<Health>();
        if (health != null)
            health.SetHP(profile.hp, profile.hp);

        EnemyVerticalMover verticalMover = enemyObject.GetComponent<EnemyVerticalMover>();
        if (verticalMover == null)
            verticalMover = enemyObject.GetComponentInChildren<EnemyVerticalMover>();
        if (verticalMover != null)
            verticalMover.SetMoveSpeed(profile.moveSpeed);

        ChargeEnemy chargeEnemy = enemyObject.GetComponent<ChargeEnemy>();
        if (chargeEnemy == null)
            chargeEnemy = enemyObject.GetComponentInChildren<ChargeEnemy>();
        if (chargeEnemy != null)
            chargeEnemy.SetDashSpeed(profile.dashSpeed);

        EnemyShooter enemyShooter = enemyObject.GetComponent<EnemyShooter>();
        if (enemyShooter == null)
            enemyShooter = enemyObject.GetComponentInChildren<EnemyShooter>();
        if (enemyShooter != null)
        {
            enemyShooter.SetFireInterval(profile.fireInterval);
            enemyShooter.SetProjectileSpeed(profile.projectileSpeed);
        }

        EnemyController enemyController = enemyObject.GetComponent<EnemyController>();
        if (enemyController == null)
            enemyController = enemyObject.GetComponentInChildren<EnemyController>();
        if (enemyController != null)
            enemyController.SetCoinReward(GetEnemyCoinReward(nodeType));
    }

    public static int GetEnemyCoinReward(NodeType nodeType)
    {
        return nodeType == NodeType.HardBattle ? 2 : 1;
    }

    // 층수별 보스 체력 기준값을 반환해 보스 난이도 상향에 사용한다.
    public static int GetBossMaxHp(int floor)
    {
        switch (Mathf.Clamp(floor, 1, 99))
        {
            case 3:
                return 1500;

            case 4:
                return 900;

            case 5:
                return 2500;

            default:
                return Mathf.RoundToInt(260f + floor * 70f);
        }
    }

    public static int GetBossClearReward(int floor)
    {
        switch (Mathf.Clamp(floor, 1, 99))
        {
            case 3: return 30;
            case 5: return 50;
            default: return 0;
        }
    }

    // 보스 오브젝트의 Health에 층수별 최대 체력을 반영한다.
    public static void ApplyBossBalance(GameObject bossObject, int floor)
    {
        if (bossObject == null)
            return;

        int maxHp = GetBossMaxHp(floor);

        Health health = bossObject.GetComponent<Health>();
        if (health == null)
            health = bossObject.GetComponentInChildren<Health>();

        if (health != null)
            health.SetHP(maxHp, maxHp);
    }
}
