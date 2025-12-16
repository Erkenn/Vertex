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
    public float jumpCooldown = 0.2f;
    [SerializeField] private int _maxJumps = 1;
    public LayerMask groundLayer = 1;
    public float groundCheckRadius = 0.2f;
    public Transform groundCheckPoint;

    public int maxJumps => _maxJumps;

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
    public int currentJumps = 0;
    public bool isGrounded = false;
    public bool jumpRequested = false;
    public bool jumpKeyHeld = false;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public GameObject shieldEffect;
    public GameObject hackEffect;

    // Приватные переменные
    private Rigidbody2D rb;
    private Vector2 movement;
    private float currentHackCooldown = 0f;
    private float jumpCooldownTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Color originalColor;
    private Collider2D playerCollider;
    private bool wasGrounded = false;
    private float lastTimeGrounded = 0f;
    private float groundRememberTime = 0.15f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();

        _maxJumps = 1;
        currentJumps = 0;

        if (groundCheckPoint == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            groundCheckPoint = groundCheckObj.transform;
        }

        currentHackCharges = maxHackCharges;
        currentShieldCharges = maxShieldCharges;
        originalColor = spriteRenderer.color;

        Debug.Log($"🎯 PlayerController инициализирован. Макс прыжков: {_maxJumps}");
    }

    void Update()
    {
        if (IsGamePaused()) return;

        HandleInput();
        UpdateCooldowns();
        UpdateVisuals();
        CheckGrounded();
    }

    void FixedUpdate()
    {
        if (canMove && !IsGamePaused())
        {
            HandleMovement();
            HandleJump();
        }
    }

    bool IsGamePaused()
    {
        return GameManager.Instance != null && (GameManager.Instance.isPaused || !GameManager.Instance.isGameActive);
    }

    void HandleInput()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        Debug.Log($"Input X: {movement.x}, Normalized: {movement.x}");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        jumpKeyHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space);

        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) && CanJump())
        {
            jumpRequested = true;
        }

        // Способности
        if (Input.GetKeyDown(KeyCode.E) && CanUseHack())
        {
            UseHack();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && CanUseShield())
        {
            UseShield();
        }

        // Тестовые команды
        if (Input.GetKeyDown(KeyCode.T) && !IsGamePaused())
        {
            TakeDamage(10);
        }
    }

    void HandleMovement()
    {
        if (movement.magnitude > 0.1f)
        {
            Vector2 targetVelocity = new Vector2(movement.x * moveSpeed, rb.linearVelocity.y);
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            Vector2 targetVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, deceleration * Time.fixedDeltaTime);
        }
    }

    void HandleJump()
    {
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            currentJumps++;
            jumpCooldownTimer = jumpCooldown;
            jumpRequested = false;

            if (animator != null)
                animator.SetTrigger("Jump");

            Debug.Log($"Прыжок! Использовано прыжков: {currentJumps}/{_maxJumps}");
        }
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;

        Collider2D[] groundColliders = Physics2D.OverlapCircleAll(groundCheckPoint.position, groundCheckRadius, groundLayer);

        bool foundGround = false;
        foreach (Collider2D collider in groundColliders)
        {
            if (collider != null && collider != playerCollider)
            {
                float colliderTop = collider.bounds.max.y;
                float playerBottom = playerCollider.bounds.min.y;

                if (playerBottom <= colliderTop + 0.1f && rb.linearVelocity.y <= 0.1f)
                {
                    foundGround = true;
                    break;
                }
            }
        }

        isGrounded = foundGround;

        if (isGrounded)
        {
            lastTimeGrounded = Time.time;
        }

        if (!wasGrounded && isGrounded)
        {
            OnLand();
        }
    }

    void OnLand()
    {
        currentJumps = 0;
        jumpCooldownTimer = 0f;

        if (animator != null)
            animator.SetTrigger("Land");

        Debug.Log($"Приземление! Прыжки сброшены. CurrentJumps: {currentJumps}");

        if (jumpKeyHeld && CanJump())
        {
            jumpRequested = true;
            Debug.Log("Автоматический прыжок после приземления");
        }
    }

    bool CanJump()
    {
        if (currentJumps >= _maxJumps)
        {
            return false;
        }

        bool recentlyGrounded = (Time.time - lastTimeGrounded) <= groundRememberTime;
        bool hasJumpsLeft = currentJumps < _maxJumps;
        bool cooldownOver = jumpCooldownTimer <= 0f;
        bool canJump = hasJumpsLeft && cooldownOver && !IsGamePaused();

        return canJump;
    }

    // === МЕТОДЫ ДЛЯ ВНЕШНЕГО ДОСТУПА ===
    public void TakeDamage(int damage)
    {
        if (IsGamePaused()) return;

        if (isShieldActive)
        {
            DeactivateShield();
            Debug.Log("🛡 Щит поглотил урон!");
            return;
        }

        health -= damage;

        StartCoroutine(DamageFlash());

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealthUI(health);

        Debug.Log($"💔 Получен урон: {damage}. Здоровье: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    public void RestoreAbilityCharges(int amount)
    {
        currentHackCharges = Mathf.Min(currentHackCharges + amount, maxHackCharges);
        currentShieldCharges = Mathf.Min(currentShieldCharges + amount, maxShieldCharges);

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);

        Debug.Log($"🔋 Восстановлены заряды способностей: +{amount}");
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

        if (AbilityManager.Instance != null)
            AbilityManager.Instance.ActivateHack(hackDuration);

        StartCoroutine(HackVisualEffect());

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);

        Debug.Log("Активирован Взлом!");
    }

    void UseShield()
    {
        if (IsGamePaused()) return;

        currentShieldCharges--;
        isShieldActive = true;

        if (shieldEffect != null)
            shieldEffect.SetActive(true);

        spriteRenderer.color = new Color(0.3f, 0.8f, 1f, 0.8f);

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

        if (jumpCooldownTimer > 0)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }
    }

    void UpdateVisuals()
    {
        if (animator == null) return;

        float speed = Mathf.Abs(rb.linearVelocity.x);
        float velX = rb.linearVelocity.x;

        // Ключевая отладка - будет писать каждый кадр
        Debug.Log($"Frame: {Time.frameCount}, VelX={velX:F4}, Speed={speed:F4}, InputX={movement.x}");

        // Только ОСНОВНЫЕ параметры
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", isGrounded);

        // Поворот спрайта
        if (movement.x > 0.1f)
            spriteRenderer.flipX = false;
        else if (movement.x < -0.1f)
            spriteRenderer.flipX = true;
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
        if (GameManager.Instance != null)
            GameManager.Instance.CollectDataPacket(1);

        Destroy(dataPacket);
        Debug.Log("💾 Собран пакет данных!");
    }

    void CompleteLevel()
    {
        Debug.Log($"🎉 Уровень {GameManager.Instance.currentLevel} пройден!");
        if (GameManager.Instance != null)
            GameManager.Instance.CompleteLevel();
    }

    // === ОБРАБОТКА СТОЛКНОВЕНИЙ ===
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.7f)
                {
                    if (!isGrounded)
                    {
                        isGrounded = true;
                        lastTimeGrounded = Time.time;
                        OnLand();
                    }
                    break;
                }
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.7f)
                {
                    if (!isGrounded)
                    {
                        isGrounded = true;
                        lastTimeGrounded = Time.time;
                        OnLand();
                    }
                    break;
                }
            }
        }
    }

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

    public void ResetPlayer()
    {
        health = 100;
        currentHackCharges = maxHackCharges;
        currentShieldCharges = maxShieldCharges;
        currentHackCooldown = 0f;
        isShieldActive = false;
        canMove = true;

        _maxJumps = 1;
        currentJumps = 0;

        isGrounded = false;
        jumpRequested = false;
        jumpCooldownTimer = 0f;
        jumpKeyHeld = false;
        wasGrounded = false;
        lastTimeGrounded = 0f;

        if (shieldEffect != null)
            shieldEffect.SetActive(false);
        spriteRenderer.color = originalColor;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(health);
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);
        }

        Debug.Log($"🔄 Игрок сброшен. Макс прыжков: {_maxJumps}");
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }

    void OnDestroy()
    {
        CancelInvoke();
        StopAllCoroutines();
    }
}