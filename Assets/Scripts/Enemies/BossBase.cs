using System.Collections;
using UnityEngine;

public abstract class BossBase : MonoBehaviour
{
    [Header("Boss Common")]
    [SerializeField] private int totalPhases = 1;          // 3층 = 1, 5층 = 2
    [SerializeField] private float phase2TriggerHpRatio = 0.7f;

    [Header("Entry Move")]
    [SerializeField] private float enterSpeed = 2.5f;
    [SerializeField] private float stopY = 5.0f;

    [Header("Entry Invincibility")]
    [SerializeField] private bool invincibleDuringEntry = true;

    [Header("Optional")]
    [SerializeField] private bool destroyOnFinalDeath = true;

    protected BossBattleController battleController;
    protected Camera battleCamera;
    protected Health health;
    protected Rigidbody2D rb;

    protected int currentPhase = 1;
    protected bool battleStarted = false;
    protected bool enteredBattleArea = false;
    protected bool phaseTransitionHandled = false;
    protected bool finalDeathHandled = false;

    private Coroutine patternRoutine;

    public int CurrentPhase => currentPhase;
    public int TotalPhases => totalPhases;
    public bool BattleStarted => battleStarted;
    public bool EnteredBattleArea => enteredBattleArea;
    public bool FinalDeathHandled => finalDeathHandled;

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
            health = GetComponentInChildren<Health>();

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInChildren<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        if (!battleStarted)
            return;

        if (finalDeathHandled)
            return;

        HandleEntryMove();

        if (!enteredBattleArea)
            return;

        CheckPhaseOrDeath();
        OnBossUpdate();
    }

    // BossBattleController가 SendMessage로 호출
    public void SetBattleController(BossBattleController controller)
    {
        battleController = controller;
    }

    // BossBattleController가 SendMessage로 호출
    public void SetBattleCamera(Camera cam)
    {
        battleCamera = cam;
    }

    // BossBattleController가 SendMessage로 호출
    public void BeginBossBattle()
    {
        if (battleStarted)
            return;

        battleStarted = true;
        currentPhase = 1;
        phaseTransitionHandled = false;
        finalDeathHandled = false;
        phase2TriggerHpRatio = 0.7f;
        enteredBattleArea = false;

        SetEntryInvincible(invincibleDuringEntry);

        OnBattleStarted();
    }

    private void HandleEntryMove()
    {
        if (enteredBattleArea)
            return;

        Vector3 move = Vector3.down * enterSpeed * Time.deltaTime;
        transform.position += move;

        if (transform.position.y <= stopY)
        {
            Vector3 fixedPos = transform.position;
            fixedPos.y = stopY;
            transform.position = fixedPos;

            enteredBattleArea = true;

            // 등장 완료 후 무적 해제
            SetEntryInvincible(false);

            OnEnteredBattleArea();
            RestartPatternLoop();
        }
    }

    private void CheckPhaseOrDeath()
    {
        if (health == null)
            return;

        // 2페이즈 보스면 HP 70% 이하에서 페이즈 전환
        if (totalPhases >= 2 && currentPhase == 1 && !phaseTransitionHandled)
        {
            float hpRatio = GetCurrentHpRatio();
            if (hpRatio <= phase2TriggerHpRatio)
            {
                EnterNextPhase();
                return;
            }
        }

        // 최종 사망
        if (health.CurrentHP <= 0)
        {
            HandleFinalDeath();
        }
    }

    private float GetCurrentHpRatio()
    {
        if (health == null)
            return 0f;

        if (health.MaxHP <= 0)
            return 0f;

        return (float)health.CurrentHP / health.MaxHP;
    }

    protected void SetEntryInvincible(bool value)
    {
        if (health != null)
            health.SetInvincible(value);
    }

    protected virtual void EnterNextPhase()
    {
        phaseTransitionHandled = true;
        currentPhase++;

        StopPatternLoop();
        OnPhaseChanged(currentPhase);
        RestartPatternLoop();
    }

    protected virtual void HandleFinalDeath()
    {
        if (finalDeathHandled)
            return;

        finalDeathHandled = true;

        StopPatternLoop();
        SetEntryInvincible(false);
        OnFinalDeath();

        battleController?.NotifyBossDefeated();

        if (destroyOnFinalDeath)
        {
            Destroy(gameObject);
        }
    }

    protected void RestartPatternLoop()
    {
        if (!battleStarted)
            return;

        if (!enteredBattleArea)
            return;

        if (finalDeathHandled)
            return;

        StopPatternLoop();
        patternRoutine = StartCoroutine(RunBossPattern());
    }

    protected void StopPatternLoop()
    {
        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }
    }

    protected virtual void OnBattleStarted()
    {
    }

    protected virtual void OnEnteredBattleArea()
    {
    }

    protected virtual void OnPhaseChanged(int newPhase)
    {
        Debug.Log($"[{name}] Phase Changed -> {newPhase}");
    }

    protected virtual void OnFinalDeath()
    {
        Debug.Log($"[{name}] Final Death");
    }

    protected virtual void OnBossUpdate()
    {
    }

    // 각 보스가 반드시 구현
    protected abstract IEnumerator RunBossPattern();
}
