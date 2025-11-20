using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("=== ДВИЖЕНИЕ ===")]
    public float moveSpeed = 8f;
    public float acceleration = 15f;
    public float deceleration = 20f;

    [Header("=== ПРЫЖКИ ===")]
    public float jumpForce = 12f;
    public float gravityScale = 4f;
    public int maxAirJumps = 0; // 0 = только с земли, 1 = один доп. прыжок в воздухе
    public float coyoteTime = 0.1f; // Можно прыгнуть после схода с платформы
    public float jumpBufferTime = 0.1f; // Можно нажать прыжок до приземления

    [Header("=== ПРОВЕРКА ЗЕМЛИ ===")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer = 1;

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
    public ParticleSystem jumpParticles;
    public ParticleSystem landParticles;

    // Приватные переменные
    private Rigidbody2D rb;
    private Vector2 movement;
    private float currentHackCooldown = 0f;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Color originalColor;

    // Переменные для прыжков
    private bool isGrounded;
    private int airJumpsRemaining;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool wasGrounded;
    private bool isJumping;

    void Start()
    {
        // Инициализация компонентов
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Настройка физики
        if (rb != null)
        {
            rb.gravityScale = gravityScale;
        }

        // Начальные значения
        currentHackCharges = maxHackCharges;
        currentShieldCharges = maxShieldCharges;
        originalColor = spriteRenderer.color;
        airJumpsRemaining = maxAirJumps;

        // Создание groundCheck если не назначен
        if (groundCheck == null)
        {
            CreateGroundCheck();
        }

        Debug.Log("🎯 PlayerController инициализирован с системой прыжков");
    }


    void Update()
    {
        // ПРОВЕРКА ПАУЗЫ И АКТИВНОСТИ ИГРЫ
        if (IsGamePaused()) return;

        HandleInput();
        HandleJump();
        UpdateCooldowns();
        UpdateVisuals();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (canMove && !IsGamePaused())
        {
            HandleMovement();
            CheckGrounded();
            ApplyBetterGravity();
        }
    }

    // === СОЗДАНИЕ GROUND CHECK ЕСЛИ ОТСУТСТВУЕТ ===
    void CreateGroundCheck()
    {
        GameObject checkObject = new GameObject("GroundCheck");
        checkObject.transform.SetParent(transform);
        checkObject.transform.localPosition = new Vector3(0, -0.5f, 0);
        groundCheck = checkObject.transform;

        Debug.Log("📍 Автоматически создан GroundCheck");
    }

    // === СИСТЕМА ДВИЖЕНИЯ ===

    bool IsGamePaused()
    {
        return GameManager.Instance != null && (GameManager.Instance.isPaused || !GameManager.Instance.isGameActive);
    }

    void HandleInput()
    {
        // Движение
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = 0; // Вертикальное движение теперь через прыжки

        // Нормализация только для горизонтального движения
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        // Способности
        if (Input.GetKeyDown(KeyCode.Space) && CanUseHack())
        {
            UseHack();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && CanUseShield())
        {
            UseShield();
        }
    }

    void HandleMovement()
    {
        if (movement.magnitude > 0.1f)
        {
            // Плавное ускорение (только по X)
            Vector2 targetVelocity = new Vector2(movement.x * moveSpeed, rb.linearVelocity.y);
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            // Плавное замедление (только по X)
            Vector2 targetVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, deceleration * Time.fixedDeltaTime);
        }
    }

    // === СИСТЕМА ПРЫЖКОВ ===

    void HandleJump()
    {
        // Обновление койот-таймера и буфера прыжка
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            airJumpsRemaining = maxAirJumps;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Буфер прыжка (можно нажать прыжок до приземления)
        if (Input.GetKeyDown(KeyCode.W)) // w для прыжка
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Проверка возможности прыжка
        if (jumpBufferCounter > 0)
        {
            if (coyoteTimeCounter > 0)
            {
                // Прыжок с земли
                PerformJump();
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
            }
            else if (airJumpsRemaining > 0)
            {
                // Дополнительный прыжок в воздухе
                PerformJump();
                airJumpsRemaining--;
                jumpBufferCounter = 0;
            }
        }

        // Отпускание кнопки прыжка для контроля высоты
        if (Input.GetKeyUp(KeyCode.J) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    void PerformJump()
    {
        // Сброс вертикальной скорости для consistency
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

        // Применение силы прыжка
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // Визуальные эффекты
        if (jumpParticles != null)
        {
            jumpParticles.Play();
        }

        // Звук прыжка
        if (AudioManager.Instance != null)
        {

            AudioManager.Instance.PlaySFX("PlayerJump");
        }

        isJumping = true;
        Debug.Log($"🦘 Прыжок! Доп. прыжков: {airJumpsRemaining}");
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;
        isGrounded = false;

        if (groundCheck != null)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject != gameObject)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        // Эффект приземления
        if (!wasGrounded && isGrounded)
        {
            OnLand();
        }
    }

    void OnLand()
    {
        // Эффекты приземления
        if (landParticles != null)
        {
            landParticles.Play();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("PlayerLand");
        }

        isJumping = false;
    }

    void ApplyBetterGravity()
    {
        // Усиленная гравитация при падении
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (gravityScale - 1) * Time.fixedDeltaTime;
        }
        // Ослабленная гравитация при подъеме (если держать кнопку прыжка)
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.J))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (gravityScale - 1) * 0.5f * Time.fixedDeltaTime;
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

        Debug.Log("⚡️ Активирован Взлом!");
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
        Debug.Log("🛡 Активирован Щит!");
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

    // === ВИЗУАЛЫ И АНИМАЦИИ ===

    void UpdateVisuals()
    {
        // Поворот спрайта в направлении движения
        if (movement.x != 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = movement.x < 0;
        }
    }

    void UpdateAnimations()
    {

        if (animator != null)
        {
            // Анимация движения
            animator.SetFloat("Speed", Mathf.Abs(movement.x));

            // Анимация прыжка/падения
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);

            // Анимация способностей
            animator.SetBool("IsShieldActive", isShieldActive);
        }
    }

    // === СИСТЕМА ЗДОРОВЬЯ И УРОНА ===

    public void TakeDamage(int damage)
    {
        if (IsGamePaused()) return;

        if (isShieldActive)
        {
            // Щит поглощает урон
            DeactivateShield();
            Debug.Log("🛡 Щит поглотил урон!");
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

    // === ВИЗУАЛИЗАЦИЯ В РЕДАКТОРЕ ===

    void OnDrawGizmosSelected()
    {
        // Визуализация ground check
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
