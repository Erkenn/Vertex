using UnityEngine;
using System.Collections;

public class CoreBoss : MonoBehaviour
{
    [Header("=== СОСТОЯНИЯ БОССА (5 УДАРОВ) ===")]
    public int totalHitsRequired = 5;
    private int currentHits = 0;

    // Состояния частей тела
    public bool leftHornBroken = false;
    public bool rightHornBroken = false;
    public bool eyeBroken = false;
    public bool coreExposed = false;

    [Header("=== НАСТРОЙКИ АТАК ===")]
    [Tooltip("Время реакции на 1-2 этапе (с рогами)")]
    public float warningTimePhase1 = 2.5f;
    [Tooltip("Время реакции на 3-4 этапе (без рог)")]
    public float warningTimePhase2 = 1.8f;
    [Tooltip("Время реакции на 5 этапе (глаз и ядро)")]
    public float warningTimePhase3 = 1.2f;

    public float physicalAttackCooldownBase = 5f;
    public float laserAttackCooldownBase = 7f;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public GameObject laserVisual;
    public Transform laserOrigin;
    public float laserDuration = 1.5f;

    [Header("=== ДВИЖЕНИЕ ПРИ АТАКЕ ===")]
    public float chargeSpeed = 8f;
    public float maxChargeDistance = 4f;

    [Header("=== ВХОДЫ/ВЫХОДЫ ===")]
    public GameObject arenaEntrance;  // Препятствие на входе в арену
    public GameObject victoryExit;    // Препятствие на выходе из арены

    [Header("=== КАМЕРА ===")]
    public float zoomSize = 8f;

    [Header("Ссылка на ядро")]
    public CoreInteractable coreInteractable;

    private PlayerController player;
    private SpriteRenderer spriteRenderer;
    private bool isActive = false;
    private bool isAttacking = false;
    private float attackTimer = 0f;
    private float currentWarningTime;
    private float currentPhysicalCooldown;
    private float currentLaserCooldown;
    private bool isLaserAttackQueued = false;
    private Vector3 originalPosition;
    private Camera mainCamera;
    private float originalCameraSize;

    public enum Phase { Full, HornsBroken, Final }
    private Phase currentPhase = Phase.Full;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        originalPosition = transform.position;

        if (mainCamera != null && mainCamera.orthographic)
        {
            originalCameraSize = mainCamera.orthographicSize;
        }

        ResetAttackTimers();
        currentHits = 0;
        isActive = false;

        // ✅ ИСПРАВЛЕНО: ОБА ПРЕПЯТСТВИЯ ИЗНАЧАЛЬНО НЕАКТИВНЫ
        if (arenaEntrance != null) arenaEntrance.SetActive(false);
        if (victoryExit != null) victoryExit.SetActive(false);

        Debug.Log("👹 CoreBoss инициализирован. Вход/выход скрыты.");
    }

    void Update()
    {
        if (!isActive || isAttacking || player == null) return;
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0)
        {
            StartCoroutine(PrepareAttack());
        }
    }

    void ResetAttackTimers()
    {
        currentWarningTime = warningTimePhase1;
        currentPhysicalCooldown = physicalAttackCooldownBase;
        currentLaserCooldown = laserAttackCooldownBase;
    }

    /// <summary>
    /// Активация босса (через триггер)
    /// </summary>
    public void ActivateBoss()
    {
        isActive = true;
        Debug.Log("👹 БОСС АКТИВИРОВАН!");

        // ✅ ИСПРАВЛЕНО: АКТИВИРУЕМ ОБА ПРЕПЯТСТВИЯ
        if (arenaEntrance != null)
        {
            arenaEntrance.SetActive(true); // Блокируем вход
            Debug.Log("🚪 Вход на арену заблокирован");
        }

        if (victoryExit != null)
        {
            victoryExit.SetActive(true); // Блокируем выход
            Debug.Log("🔒 Выход из арены заблокирован");
        }

        // Увеличиваем камеру
        if (mainCamera != null && mainCamera.orthographic)
        {
            StartCoroutine(ZoomCamera());
        }
    }

    IEnumerator ZoomCamera()
    {
        float startSize = mainCamera.orthographicSize;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            mainCamera.orthographicSize = Mathf.Lerp(startSize, zoomSize, t);
            yield return null;
        }
    }

    public void TakeDamage()
    {
        if (!isActive) return;

        currentHits++;
        Debug.Log($"💥 Удар #{currentHits}/{totalHitsRequired}");

        UpdateBossStateAfterHit();

        if (spriteRenderer != null)
        {
            StartCoroutine(FlashColor(Color.red, 0.3f));
        }

        CheckVictoryCondition();
    }

    void UpdateBossStateAfterHit()
    {
        if (currentHits == 2 && currentPhase == Phase.Full)
        {
            leftHornBroken = true;
            rightHornBroken = true;
            currentPhase = Phase.HornsBroken;

            currentWarningTime = warningTimePhase2;
            currentPhysicalCooldown *= 0.7f;
            currentLaserCooldown *= 0.7f;
            Debug.Log("🦏 РОГА СЛОМАНЫ! Скорость атак увеличена на 30%");
        }
        else if (currentHits == 4 && currentPhase == Phase.HornsBroken)
        {
            eyeBroken = true;
            coreExposed = true;
            currentPhase = Phase.Final;

            currentWarningTime = warningTimePhase3;
            currentPhysicalCooldown *= 0.6f;
            currentLaserCooldown *= 0.6f;
            Debug.Log("👁️ ГЛАЗ СЛОМАН! Скорость атак увеличена на 40%");
        }
    }

    IEnumerator PrepareAttack()
    {
        isAttacking = true;
        bool isPhysicalAttack = Random.value > 0.5f;

        if (isPhysicalAttack)
        {
            UIManager.Instance?.ShowShieldWarning();
            Debug.Log($"🛡️ ФИЗИЧЕСКАЯ АТАКА! (Предупреждение: {currentWarningTime:F1}с)");
        }
        else
        {
            UIManager.Instance?.ShowHackWarning();
            isLaserAttackQueued = true;
            Debug.Log($"💻 ЛАЗЕРНАЯ АТАКА! (Предупреждение: {currentWarningTime:F1}с)");
        }

        yield return new WaitForSeconds(currentWarningTime);

        if (isPhysicalAttack)
            StartCoroutine(PhysicalAttack());
        else
            StartCoroutine(LaserAttack());

        isAttacking = false;
        attackTimer = isPhysicalAttack ? currentPhysicalCooldown : currentLaserCooldown;
    }

    IEnumerator PhysicalAttack()
    {
        Debug.Log("💥 СТАРТ ФИЗИЧЕСКОЙ АТАКИ");

        Vector2 targetPosition = player.transform.position;
        float distance = Vector2.Distance(transform.position, targetPosition);

        if (distance > maxChargeDistance)
        {
            Vector2 dir = (targetPosition - (Vector2)transform.position).normalized;
            targetPosition = (Vector2)transform.position + dir * maxChargeDistance;
        }

        float startTime = Time.time;
        while (Time.time - startTime < 0.5f)
        {
            transform.position = Vector2.Lerp(transform.position, targetPosition, (Time.time - startTime) * chargeSpeed);
            yield return null;
        }

        if (player != null && player.isShieldActive)
        {
            Debug.Log("✅ АТАКА ОТРАЖЕНА ЩИТОМ!");
            TakeDamage();
        }
        else if (player != null)
        {
            Debug.Log("❌ ИГРОК ПОЛУЧИЛ УРОН ОТ ФИЗИЧЕСКОЙ АТАКИ");
            player.TakeDamage(25);
        }

        // Возврат на исходную позицию
        startTime = Time.time;
        while (Time.time - startTime < 0.4f)
        {
            transform.position = Vector2.Lerp(transform.position, originalPosition, (Time.time - startTime) * chargeSpeed);
            yield return null;
        }
    }

    IEnumerator LaserAttack()
    {
        Debug.Log("🔴 НАЧАЛО ЛАЗЕРНОЙ АТАКИ");
        yield return new WaitForSeconds(0.8f);

        if (isLaserAttackQueued && player != null)
        {
            Debug.Log("🔥 ЛАЗЕР АКТИВИРОВАН!");

            if (laserVisual != null && laserOrigin != null)
            {
                GameObject laser = Instantiate(laserVisual, laserOrigin.position, Quaternion.identity);
                LineRenderer line = laser.GetComponent<LineRenderer>();

                if (line != null)
                {
                    line.SetPosition(0, laserOrigin.position);
                    line.SetPosition(1, player.transform.position);
                }
                Destroy(laser, laserDuration);
            }

            player.TakeDamage(35);
        }
        else
        {
            Debug.Log("✅ ЛАЗЕР ОТМЕНЕН ВЗЛОМОМ");
        }

        isLaserAttackQueued = false;
        yield return new WaitForSeconds(0.5f);
    }

    void CheckVictoryCondition()
    {
        if (currentHits >= totalHitsRequired)
        {
            Die();
        }
    }

    IEnumerator FlashColor(Color color, float duration)
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        spriteRenderer.color = color;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = original;
    }

    public bool CanBeHacked() => isLaserAttackQueued && isActive;

    public void HackBoss()
    {
        if (!CanBeHacked()) return;
        isLaserAttackQueued = false;

        if (spriteRenderer != null)
        {
            StartCoroutine(FlashColor(Color.cyan, 0.5f));
        }
        Debug.Log("⚡ ВЗЛОМ УСПЕШЕН! Лазер деактивирован");
    }

    void Die()
    {
        Debug.Log("🎉 БОСС ПОБЕЖДЕН! Все 5 ударов нанесены");

        if (arenaEntrance != null)
        {
            arenaEntrance.SetActive(false);
            Debug.Log("🚪 Вход на арену открыт");
        }

        if (victoryExit != null)
        {
            victoryExit.SetActive(false);
            Debug.Log("✅ Выход из арены открыт");
        }

        gameObject.SetActive(false);

        GameManager.Instance?.BossDefeated();

        OnBossDefeated();
    }

    void OnDrawGizmosSelected()
    {
        if (laserOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(laserOrigin.position, 0.2f);
        }
    }

    public void OnBossDefeated()
    {
        Debug.Log("💥 Босс повержен! Активируем ядро...");

        GameManager.Instance.BossDefeated();

        if (coreInteractable != null)
        {
            coreInteractable.ActivateCore();
        }
        else
        {
            Debug.LogError("❌ CoreInteractable не назначен в инспекторе!");
        }
    }
}