using UnityEngine;
using System.Collections;

public class CoreBoss : MonoBehaviour
{
    [Header("=== СОСТОЯНИЯ БОССА (5 УДАРОВ) ===")]
    public int totalHitsRequired = 5;
    public int currentHits = 0;

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
    public GameObject laserVisual; // Префаб лазера (линия из 2 точек)
    public Transform laserOrigin;  // Точка старта лазера (глаз босса)
    public float laserDuration = 1.5f;
    public float laserWidth = 0.3f;

    [Header("=== ДВИЖЕНИЕ ПРИ АТАКЕ ===")]
    public float chargeSpeed = 8f; // Скорость приближения к игроку
    public float maxChargeDistance = 4f; // Максимальное расстояние атаки

    [Header("=== ВХОДЫ/ВЫХОДЫ ===")]
    public GameObject arenaEntrance;  // Дверь/стена, закрывающая вход на арену
    public GameObject victoryExit;    // Выход после победы

    [Header("=== КАМЕРА ===")]
    public float zoomSize = 8f;

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

    // Состояния босса
    public enum Phase { Full, HornsBroken, Final }
    public Phase currentPhase = Phase.Full;

    void Start()
    {
        // Получаем компоненты
        player = FindObjectOfType<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        // Сохраняем оригинальную позицию и настройки камеры
        originalPosition = transform.position;
        if (mainCamera != null && mainCamera.orthographic)
        {
            originalCameraSize = mainCamera.orthographicSize;
        }

        // Инициализируем параметры
        ResetAttackTimers();
        currentHits = 0;
        isActive = false;

        Debug.Log("👹 CoreBoss инициализирован (5-ударная система)");
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

        // Закрываем вход на арену
        if (arenaEntrance != null)
        {
            arenaEntrance.SetActive(false);
            Debug.Log("🚪 Вход на арену закрыт");
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

    /// <summary>
    /// Подготовка к атаке: показ предупреждения
    /// </summary>
    IEnumerator PrepareAttack()
    {
        isAttacking = true;

        // Случайный выбор атаки (50/50)
        bool isPhysicalAttack = Random.value > 0.5f;

        if (isPhysicalAttack)
        {
            UIManager.Instance?.ShowShieldWarning();
            Debug.Log($"⚠️ ФИЗИЧЕСКАЯ АТАКА! (У вас {currentWarningTime:F1} сек на реакцию)");
        }
        else
        {
            UIManager.Instance?.ShowHackWarning();
            isLaserAttackQueued = true;
            Debug.Log($"⚠️ ЛАЗЕРНАЯ АТАКА! (У вас {currentWarningTime:F1} сек на реакцию)");
        }

        yield return new WaitForSeconds(currentWarningTime);

        // Запускаем атаку
        if (isPhysicalAttack)
            StartCoroutine(PhysicalAttack());
        else
            StartCoroutine(LaserAttack());

        isAttacking = false;
        attackTimer = isPhysicalAttack ? currentPhysicalCooldown : currentLaserCooldown;
    }

    /// <summary>
    /// Физическая атака: босс приближается к игроку
    /// </summary>
    IEnumerator PhysicalAttack()
    {
        Debug.Log("💥 ФИЗИЧЕСКАЯ АТАКА! Босс бежит к игроку...");

        // Анимация заряда (если есть)
        // if (animator != null) animator.SetTrigger("Charge");

        // Босс движется к игроку
        Vector2 targetPosition = player.transform.position;
        float distanceToPlayer = Vector2.Distance(transform.position, targetPosition);

        // Ограничиваем дистанцию атаки
        if (distanceToPlayer > maxChargeDistance)
        {
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            targetPosition = (Vector2)transform.position + direction * maxChargeDistance;
        }

        // Плавное движение к цели
        float startTime = Time.time;
        while (Time.time - startTime < 0.5f)
        {
            transform.position = Vector2.Lerp(transform.position, targetPosition, (Time.time - startTime) * chargeSpeed);
            yield return null;
        }

        // Проверяем щит
        if (player != null && player.isShieldActive)
        {
            // Отражение атаки
            Debug.Log("🛡️ АТАКА ОТРАЖЕНА! Босс получает удар");
            currentHits++;

            // Обновляем состояние босса
            UpdateBossStateAfterHit();

            // Визуальный эффект
            if (spriteRenderer != null)
            {
                StartCoroutine(FlashColor(Color.red, 0.3f));
            }
        }
        else
        {
            // Игрок получает урон
            if (player != null) player.TakeDamage(25);
        }

        // Возвращаемся на исходную позицию
        startTime = Time.time;
        while (Time.time - startTime < 0.4f)
        {
            transform.position = Vector2.Lerp(transform.position, originalPosition, (Time.time - startTime) * chargeSpeed);
            yield return null;
        }

        // Проверяем победу
        CheckVictoryCondition();
    }

    /// <summary>
    /// Лазерная атака: видимый луч от глаза к игроку
    /// </summary>
    IEnumerator LaserAttack()
    {
        Debug.Log("🔴 ЛАЗЕРНАЯ АТАКА! Босс целится в игрока...");

        // Анимация подготовки (если есть)
        // if (animator != null) animator.SetTrigger("LaserCharge");

        yield return new WaitForSeconds(0.8f);

        if (isLaserAttackQueued && player != null)
        {
            Debug.Log("🔥 ЛАЗЕР ЗАПУЩЕН!");

            // Создаём луч ТОЛЬКО если есть LineRenderer
            if (laserVisual != null)
            {
                GameObject laser = Instantiate(laserVisual, transform.position, Quaternion.identity);
                LineRenderer line = laser.GetComponent<LineRenderer>();

                if (line != null)
                {
                    // Начало луча — позиция laserOrigin (глаз)
                    Vector3 startPos = laserOrigin != null ? laserOrigin.position : transform.position;
                    Vector3 endPos = player.transform.position;

                    line.SetPosition(0, startPos);
                    line.SetPosition(1, endPos);
                }

                Destroy(laser, laserDuration);
            }

            // Урон игроку (если нет щита)
            if (player != null)
            {
                player.TakeDamage(35);
            }
        }
        else
        {
            Debug.Log("✅ ЛАЗЕРНАЯ АТАКА УСПЕШНО ОТМЕНЕНА ВЗЛОМОМ!");
        }

        isLaserAttackQueued = false;
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// Обновление состояния босса после удара
    /// </summary>
    void UpdateBossStateAfterHit()
    {
        // Обновляем фазу в зависимости от количества ударов
        if (currentHits == 2 && !leftHornBroken && !rightHornBroken)
        {
            // Ломаем рога
            leftHornBroken = true;
            rightHornBroken = true;
            currentPhase = Phase.HornsBroken;
            Debug.Log("🦏 РОГА СЛОМАНЫ! Босс становится быстрее");

            // Ускоряем атаки
            currentWarningTime = warningTimePhase2;
            currentPhysicalCooldown *= 0.7f;
            currentLaserCooldown *= 0.7f;
        }
        else if (currentHits == 4 && !eyeBroken && !coreExposed)
        {
            // Ломаем глаз и открываем ядро
            eyeBroken = true;
            coreExposed = true;
            currentPhase = Phase.Final;
            Debug.Log("👁️ ГЛАЗ СЛОМАН! ЯДРО ОТКРЫТО! Босс в финальной фазе");

            // Ещё сильнее ускоряем
            currentWarningTime = warningTimePhase3;
            currentPhysicalCooldown *= 0.6f;
            currentLaserCooldown *= 0.6f;
        }
    }

    /// <summary>
    /// Проверка условия победы
    /// </summary>
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

        Debug.Log("⚡ ВЗЛОМ УСПЕШЕН! Лазер отменён");
        isLaserAttackQueued = false;

        // Визуальный эффект
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashColor(Color.cyan, 0.5f));
        }
    }

    void Die()
    {
        Debug.Log("💀 БОСС ПОБЕЖДЁН! ВСЕ 5 УДАРОВ НАНЕСЕНЫ");

        // Открываем выход
        if (victoryExit != null)
        {
            victoryExit.SetActive(true);
            Debug.Log("🎉 ВЫХОД ОТКРЫТ! Можете покинуть арену");
        }

        // Отключаем босса
        isActive = false;
        enabled = false;

        // Событие для GameManager
        GameManager.Instance?.BossDefeated();
    }

    // Для отладки в редакторе
    void OnDrawGizmosSelected()
    {
        if (laserOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(laserOrigin.position, 0.2f);
        }
    }
}