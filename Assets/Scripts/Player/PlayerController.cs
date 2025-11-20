﻿using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("=== ДВИЖЕНИЕ ===")]
    public float moveSpeed = 8f;
    public float acceleration = 15f;
    public float deceleration = 20f;

    [Header("=== СПОСОБНОСТИ ===")]
    public int maxHackCharges = 3;
    public int maxShieldCharges = 2;
    public float hackCooldown = 5f;
    public float shieldDuration = 3f;
    public float hackDuration = 3f;

    [Header("=== ТЕКУЩЕЕ СОСТОЯНИЕ ===")]
    public int currentHackCharges;
    public int currentShieldCharges;
    public int health = 100;
    public bool isShieldActive = false;
    public bool canMove = true;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public GameObject shieldEffect;
    public GameObject hackEffect;

    // Приватные переменные
    private Rigidbody2D rb;
    private Vector2 movement;
    private float currentHackCooldown = 0f;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Color originalColor;

    void Start()
    {
        // Инициализация компонентов
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Начальные значения
        currentHackCharges = maxHackCharges;
        currentShieldCharges = maxShieldCharges;
        originalColor = spriteRenderer.color;

        Debug.Log("🎯 PlayerController инициализирован");
    }

    void Update()
    {
        // ПРОВЕРКА ПАУЗЫ И АКТИВНОСТИ ИГРЫ
        if (IsGamePaused()) return;

        HandleInput();
        UpdateCooldowns();
        UpdateVisuals();
    }

    void FixedUpdate()
    {
        if (canMove && !IsGamePaused())
            HandleMovement();
    }

    // === ПРОВЕРКА СОСТОЯНИЯ ИГРЫ ===
    bool IsGamePaused()
    {
        return GameManager.Instance != null && (GameManager.Instance.isPaused || !GameManager.Instance.isGameActive);
    }

    void HandleInput()
    {
        // Движение (требование: интуитивное управление)
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        // Способности (требование: быстрый доступ)
        if (Input.GetKeyDown(KeyCode.Space) && CanUseHack())
        {
            UseHack();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && CanUseShield())
        {
            UseShield();
        }

        // Тестовые команды (только если игра активна)
        if (Input.GetKeyDown(KeyCode.T) && !IsGamePaused())
        {
            TakeDamage(10);
        }
    }

    void HandleMovement()
    {
        if (movement.magnitude > 0.1f)
        {
            // Плавное ускорение
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, movement * moveSpeed, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            // Плавное замедление
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
        }
    }

    // === СИСТЕМА СПОСОБНОСТЕЙ ===
    bool CanUseHack()
    {
        return currentHackCharges > 0 && currentHackCooldown <= 0 && !IsGamePaused();
    }

    bool CanUseShield()
    {
        return currentShieldCharges > 0 && !isShieldActive && !IsGamePaused();
    }

    void UseHack()
    {
        if (IsGamePaused()) return;

        currentHackCharges--;
        currentHackCooldown = hackCooldown;

        // Активация взлома через AbilityManager
        if (AbilityManager.Instance != null)
            AbilityManager.Instance.ActivateHack(hackDuration);

        // Визуальные эффекты
        StartCoroutine(HackVisualEffect());

        // Обновление UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);

        Debug.Log("⚡ Активирован Взлом!");
    }

    void UseShield()
    {
        if (IsGamePaused()) return;

        currentShieldCharges--;
        isShieldActive = true;

        // Визуальный эффект щита
        if (shieldEffect != null)
            shieldEffect.SetActive(true);

        spriteRenderer.color = new Color(0.3f, 0.8f, 1f, 0.8f);

        // Обновление UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);

        Invoke(nameof(DeactivateShield), shieldDuration);
        Debug.Log("🛡️ Активирован Щит!");
    }

    void DeactivateShield()
    {
        isShieldActive = false;
        if (shieldEffect != null)
            shieldEffect.SetActive(false);
        spriteRenderer.color = originalColor;
    }

    IEnumerator HackVisualEffect()
    {
        // Неоновые импульсы при взломе
        if (hackEffect != null)
        {
            hackEffect.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            hackEffect.SetActive(false);
        }
    }

    void UpdateCooldowns()
    {
        if (currentHackCooldown > 0)
        {
            currentHackCooldown -= Time.deltaTime;
        }
    }

    void UpdateVisuals()
    {
        // Анимация движения
        if (animator != null)
        {
            animator.SetFloat("Speed", rb.linearVelocity.magnitude);
            animator.SetFloat("Horizontal", movement.x);
            animator.SetFloat("Vertical", movement.y);
        }
    }

    // === СИСТЕМА ЗДОРОВЬЯ И УРОНА ===
    public void TakeDamage(int damage)
    {
        if (IsGamePaused()) return;

        if (isShieldActive)
        {
            // Щит поглощает урон (требование: ровно один удар)
            DeactivateShield();
            Debug.Log("🛡️ Щит поглотил урон!");
            return;
        }

        health -= damage;

        // Визуальная обратная связь
        StartCoroutine(DamageFlash());

        // Обновление UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealthUI(health);

        Debug.Log($"💔 Получен урон: {damage}. Здоровье: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    void Die()
    {
        Debug.Log("💀 Игрок погиб!");
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerDied();
    }

    // === СИСТЕМА СБОРА ДАННЫХ ===
    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsGamePaused()) return;

        if (other.CompareTag("DataPacket"))
        {
            CollectDataPacket(other.gameObject);
        }

        if (other.CompareTag("Exit"))
        {
            CompleteLevel();
        }
    }

    void CollectDataPacket(GameObject dataPacket)
    {
        // Сбор пакета данных
        if (GameManager.Instance != null)
            GameManager.Instance.CollectDataPacket(1);

        // Уничтожение объекта
        Destroy(dataPacket);

        Debug.Log("💾 Собран пакет данных!");
    }

    void CompleteLevel()
    {
        Debug.Log($"🎉 Уровень {GameManager.Instance.currentLevel} пройден!");
        if (GameManager.Instance != null)
            GameManager.Instance.CompleteLevel();
    }

    // === ВНЕШНИЙ ДОСТУП К СПОСОБНОСТЯМ ===
    public void RestoreAbilityCharges(int amount)
    {
        currentHackCharges = Mathf.Min(currentHackCharges + amount, maxHackCharges);
        currentShieldCharges = Mathf.Min(currentShieldCharges + amount, maxShieldCharges);

        // Обновление UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);

        Debug.Log($"🔋 Восстановлены заряды способностей: +{amount}");
    }

    // === МЕТОДЫ ДЛЯ ВНЕШНЕГО ДОСТУПА ===
    public Vector2 GetPosition()
    {
        return transform.position;
    }

    public bool IsAlive()
    {
        return health > 0;
    }

    public float GetHackCooldownProgress()
    {
        return 1f - (currentHackCooldown / hackCooldown);
    }

    // === ОБРАБОТКА ПЕРЕЗАПУСКА ===
    public void ResetPlayer()
    {
        health = 100;
        currentHackCharges = maxHackCharges;
        currentShieldCharges = maxShieldCharges;
        currentHackCooldown = 0f;
        isShieldActive = false;
        canMove = true;

        // Сброс визуальных эффектов
        if (shieldEffect != null)
            shieldEffect.SetActive(false);
        spriteRenderer.color = originalColor;

        // Сброс физики
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Обновление UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(health);
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);
        }
    }

    // === ОБРАБОТКА УНИЧТОЖЕНИЯ ===
    void OnDestroy()
    {
        // Отмена всех вызовов Invoke
        CancelInvoke();

        // Остановка всех корутин
        StopAllCoroutines();
    }
}