using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("=== ДВИЖЕНИЕ ===")]
    public float moveSpeed = 8f;
    public float acceleration = 15f;
    public float deceleration = 20f;
    public float crouchMoveSpeed = 4f;

    private MovingPlatform currentPlatform = null;
    private bool isOnMovingPlatform = false;

    [Header("=== ПРЫЖКИ ===")]
    public float jumpForce = 12f;
    public float jumpCooldown = 0.2f;
    [SerializeField] private int _maxJumps = 1;
    public LayerMask groundLayer = 1;
    public float groundCheckRadius = 0.2f;
    public Transform groundCheckPoint;

    public int maxJumps => _maxJumps;

    [Header("=== ПРИСЕДАНИЕ ===")]
    public bool canCrouch = true;
    public float crouchHeight = 0.8f; 
    private float originalHeight;
    public float crouchTransitionSpeed = 10f;
    public bool canStandUp = true;
    public float headCheckRadius = 0.3f;
    public Transform headCheckPoint;
    [Range(0f, 1f)] public float currentCrouchPercent = 0f;

    [Header("=== СПОСОБНОСТИ ===")]
    public int maxHackCharges = 3;
    public int maxShieldCharges = 2;
    public float hackCooldown = 7f;
    public float shieldDuration = 3f;
    public float hackDuration = 5.5f;

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
    public bool isCrouching = false;
    public bool wantsToCrouch = false;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public GameObject shieldEffect;
    public GameObject hackEffect;

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
    private float currentColliderHeight = 0f;
    private Vector2[] originalPoints;
    private bool alreadyDied = false;

    [Header("=== ГРАНИЦЫ УРОВНЯ ===")]
    public float deathBoundaryY = -20f;

    [Header("=== ВЗАИМОДЕЙСТВИЕ ===")]
    public TextMeshProUGUI interactionText;
    public GameObject interactionHint;
    public Vector3 hintOffset = new Vector3(0, 1.5f, 0);

    private ComputerTerminal currentComputer = null;
    private bool canInteract = false;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();

        if (playerCollider is PolygonCollider2D polyCollider)
        {
            originalPoints = new Vector2[polyCollider.points.Length];
            for (int i = 0; i < polyCollider.points.Length; i++)
            {
                originalPoints[i] = polyCollider.points[i];
            }
            originalHeight = GetPolygonHeight(polyCollider);
            currentColliderHeight = originalHeight;
        }
        else if (playerCollider is BoxCollider2D boxCollider)
        {
            originalHeight = boxCollider.size.y;
            currentColliderHeight = originalHeight;
        }
        else if (playerCollider is CapsuleCollider2D capsuleCollider)
        {
            originalHeight = capsuleCollider.size.y;
            currentColliderHeight = originalHeight;
        }
        else
        {
            Debug.LogWarning("Используется неподдерживаемый тип коллайдера для приседания");
            canCrouch = false;
        }

        if (headCheckPoint == null && canCrouch)
        {
            GameObject headCheckObj = new GameObject("HeadCheck");
            headCheckObj.transform.SetParent(transform);
            headCheckObj.transform.localPosition = Vector3.zero;
            headCheckPoint = headCheckObj.transform;
            UpdateHeadCheckPosition();
        }

        if (groundCheckPoint == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            groundCheckPoint = groundCheckObj.transform;
        }

        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }

        if (interactionHint != null)
        {
            interactionHint.SetActive(false);
        }

        _maxJumps = 1;
        currentJumps = 0;
        currentHackCharges = maxHackCharges;
        currentShieldCharges = maxShieldCharges;
        originalColor = spriteRenderer.color;

        Debug.Log($"🎯 PlayerController инициализирован. Исходная высота: {originalHeight}, Crouch: {crouchHeight}");
    }

    void Update()
    {
        if (IsGamePaused()) return;

        HandleInput();
        UpdateCooldowns();
        UpdateVisuals();
        CheckGrounded();
        CheckIfCanStandUp();
        UpdateCrouchPercentage();
        CheckDeathBoundaries();
        UpdateInteractionUI();
    }

    void UpdateInteractionUI()
    {
        bool showHint = false;
        string hintText = "";

        if (currentComputer != null)
        {
            // Проверяем состояние компьютера
            if (!currentComputer.used) // Если компьютер еще не использован
            {
                showHint = true;
                hintText = "Нажми [E] для активации";
            }
            else if (currentComputer.canBeReused) // Если можно использовать повторно
            {
                showHint = true;
                hintText = "Нажми [E] для активации";
            }
            else // Если использован и нельзя повторно
            {
                showHint = true;
                hintText = "Уже использован";
            }
        }

        // Обновляем TextMeshPro текст
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(showHint);
            if (showHint)
            {
                interactionText.text = hintText;

                // Позиционируем текст над игроком (в мировых координатах)
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + hintOffset);
                interactionText.transform.position = screenPos;
            }
        }

        // Обновляем GameObject-подсказку
        if (interactionHint != null)
        {
            interactionHint.SetActive(showHint);
            if (showHint)
            {
                // Позиционируем над игроком в мировых координатах
                interactionHint.transform.position = transform.position + hintOffset;

                // Обновляем текст если есть TextMeshPro внутри
                TextMeshPro hintTMP = interactionHint.GetComponentInChildren<TextMeshPro>();
                if (hintTMP != null)
                {
                    hintTMP.text = hintText;
                }
            }
        }

        canInteract = showHint && currentComputer != null && !currentComputer.used;
    }

    void FixedUpdate()
    {
        if (canMove && !IsGamePaused())
        {
            HandleMovement();
            HandleJump();
            HandleCrouch();
        }
    }

    float GetPolygonHeight(PolygonCollider2D poly)
    {
        if (poly.points.Length == 0) return 0;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        foreach (Vector2 point in poly.points)
        {
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }
        return maxY - minY;
    }

    void UpdateCrouchPercentage()
    {
        if (!canCrouch || originalHeight == 0) return;
        float heightDifference = originalHeight - crouchHeight;
        if (heightDifference > 0)
        {
            float currentDifference = originalHeight - currentColliderHeight;
            currentCrouchPercent = Mathf.Clamp01(currentDifference / heightDifference);
        }
        else
        {
            currentCrouchPercent = 0f;
        }
    }

    bool IsGamePaused()
    {
        return GameManager.Instance != null && (GameManager.Instance.isPaused || !GameManager.Instance.isGameActive);
    }

    void HandleInput()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement = movement.normalized;

        if (canCrouch)
        {
            bool sKeyPressed = Input.GetKey(KeyCode.S);
            bool downArrowPressed = Input.GetKey(KeyCode.DownArrow);
            float verticalInput = Input.GetAxisRaw("Vertical");
            bool downInput = verticalInput < -0.5f;

            bool shouldCrouch = sKeyPressed || downArrowPressed || downInput;

            if (shouldCrouch && !wantsToCrouch)
            {
                Debug.Log("✅ Начало приседания");
            }
            else if (!shouldCrouch && wantsToCrouch)
            {
                Debug.Log("✅ Завершение приседания");
            }

            wantsToCrouch = shouldCrouch;
        }

        // Прыжок
        jumpKeyHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow);
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)) && CanJump())
        {
            if (isCrouching && canStandUp)
            {
                isCrouching = false;
                Debug.Log("✅ Встали для прыжка");
            }
            jumpRequested = true;
        }

        // Способности
        if (Input.GetKeyDown(KeyCode.P) && CanUseHack())
        {
            UseHack();
        }

        if (Input.GetKeyDown(KeyCode.O) && CanUseShield())
        {
            UseShield();
        }

        // Отладка
        if (Input.GetKeyDown(KeyCode.F1)) Debug.Log($"Crouch: {isCrouching}, Wants: {wantsToCrouch}, Height: {currentColliderHeight:F2}");
        if (Input.GetKeyDown(KeyCode.T)) TakeDamage(10);
    }

    void HandleMovement()
    {
        float currentMoveSpeed = isCrouching ? crouchMoveSpeed : moveSpeed;
        float horizontalInput = movement.x;

        // Основная желаемая скорость от игрока
        float playerDesiredVelocityX = horizontalInput * currentMoveSpeed;

        // Добавляем скорость платформы, если стоим на ней
        if (currentPlatform != null)
        {
            playerDesiredVelocityX += currentPlatform.GetPlatformVelocity().x;
        }

        Vector2 targetVelocity = new Vector2(playerDesiredVelocityX, rb.linearVelocity.y);

        // Если игрок стоит на платформе — не тормозим, просто следуем за её скоростью
        if (currentPlatform != null && Mathf.Abs(horizontalInput) < 0.1f)
        {
            rb.linearVelocity = new Vector2(currentPlatform.GetPlatformVelocity().x, rb.linearVelocity.y);
        }
        else if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, deceleration * Time.fixedDeltaTime);
        }
    }

    void CheckDeathBoundaries()
    {
        if (transform.position.y < deathBoundaryY)
        {
            // Мгновенная смерть без урона
            if (!alreadyDied)
            {
                Die();
            }
        }
    }

    void HandleCrouch()
    {
        if (!canCrouch) return;

        // Простая логика: хочет присесть → приседает, не хочет → встаёт
        if (wantsToCrouch && !isCrouching)
        {
            isCrouching = true;
            Debug.Log("✅ Начато приседание");
        }
        else if (!wantsToCrouch && isCrouching)
        {
            if (canStandUp)
            {
                isCrouching = false;
                Debug.Log("✅ Вставание");
            }
            else
            {
                Debug.Log("❌ Нельзя встать — над головой препятствие");
            }
        }

        UpdateColliderHeight();
    }

    void CheckIfCanStandUp()
    {
        if (!canCrouch || !isCrouching || headCheckPoint == null) return;

        Collider2D[] overhead = Physics2D.OverlapCircleAll(headCheckPoint.position, headCheckRadius, groundLayer);
        canStandUp = true;
        foreach (Collider2D col in overhead)
        {
            if (col != null && col != playerCollider)
            {
                canStandUp = false;
                break;
            }
        }
    }

    void UpdateHeadCheckPosition()
    {
        if (headCheckPoint != null) headCheckPoint.localPosition = new Vector3(0f, currentColliderHeight * 0.5f, 0f);
    }

    void UpdateColliderHeight()
    {
        if (!canCrouch) return;
        float targetHeight = isCrouching ? crouchHeight : originalHeight;

        if (playerCollider is PolygonCollider2D polyCollider && originalPoints != null)
        {
            float originalMinY = float.MaxValue;
            foreach (Vector2 p in originalPoints) originalMinY = Mathf.Min(originalMinY, p.y);
            float scaleY = originalHeight > 0 ? targetHeight / originalHeight : 1f;
            scaleY = Mathf.Clamp01(scaleY);

            Vector2[] newPoints = new Vector2[originalPoints.Length];
            for (int i = 0; i < originalPoints.Length; i++)
            {
                Vector2 p = originalPoints[i];
                float newY = originalMinY + (p.y - originalMinY) * scaleY;
                newPoints[i] = new Vector2(p.x, newY);
            }
            polyCollider.points = newPoints;
            currentColliderHeight = GetPolygonHeight(polyCollider);
            UpdateHeadCheckPosition();
        }
        else if (playerCollider is BoxCollider2D box)
        {
            Vector2 size = box.size;
            float newH = Mathf.Lerp(size.y, targetHeight, crouchTransitionSpeed * Time.fixedDeltaTime);
            newH = Mathf.Clamp(newH, Mathf.Min(crouchHeight, originalHeight), Mathf.Max(crouchHeight, originalHeight));
            float bottom = transform.position.y + box.offset.y - size.y * 0.5f;
            box.size = new Vector2(size.x, newH);
            box.offset = new Vector2(box.offset.x, bottom - transform.position.y + newH * 0.5f);
            currentColliderHeight = newH;
            UpdateHeadCheckPosition();
        }
        else if (playerCollider is CapsuleCollider2D cap)
        {
            Vector2 size = cap.size;
            float newH = Mathf.Lerp(size.y, targetHeight, crouchTransitionSpeed * Time.fixedDeltaTime);
            newH = Mathf.Clamp(newH, Mathf.Min(crouchHeight, originalHeight), Mathf.Max(crouchHeight, originalHeight));
            float bottom = transform.position.y + cap.offset.y - size.y * 0.5f;
            cap.size = new Vector2(size.x, newH);
            cap.offset = new Vector2(cap.offset.x, bottom - transform.position.y + newH * 0.5f);
            currentColliderHeight = newH;
            UpdateHeadCheckPosition();
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
            if (animator != null) animator.SetTrigger("Jump");
        }
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckPoint.position, groundCheckRadius, groundLayer);
        isGrounded = false;
        foreach (Collider2D col in colliders)
        {
            if (col != playerCollider)
            {
                float colliderTop = col.bounds.max.y;
                float playerBottom = playerCollider.bounds.min.y;
                if (playerBottom <= colliderTop + 0.1f && rb.linearVelocity.y <= 0.1f)
                {
                    isGrounded = true;
                    break;
                }
            }
        }
        if (isGrounded) lastTimeGrounded = Time.time;
        if (!wasGrounded && isGrounded) OnLand();
    }

    void OnLand()
    {
        currentJumps = 0;
        jumpCooldownTimer = 0f;
        if (animator != null) animator.SetTrigger("Land");
        if (jumpKeyHeld && CanJump()) jumpRequested = true;
    }

    bool CanJump()
    {
        if (currentJumps >= _maxJumps || jumpCooldownTimer > 0f || IsGamePaused()) return false;
        return currentJumps < _maxJumps && (isGrounded || (Time.time - lastTimeGrounded) <= groundRememberTime);
    }

    public void RestoreAbilityCharges(int amount)
    {
        currentHackCharges = Mathf.Min(currentHackCharges + amount, maxHackCharges);
        currentShieldCharges = Mathf.Min(currentShieldCharges + amount, maxShieldCharges);

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);

        Debug.Log($"🔋 Восстановлены заряды способностей: +{amount}");
    }

    public void TakeDamage(int damage)
    {
        if (IsGamePaused() || alreadyDied) return;

        bool isLaserAttack = (damage == 1000 || damage == 35);

        if (!isLaserAttack && isShieldActive)
        {
            DeactivateShield();
            Debug.Log("🛡 Щит поглотил урон!");
            return;
        }

        health -= damage;

        // Защита от отрицательного здоровья
        if (health < 0) health = 0;

        StartCoroutine(DamageFlash());

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealthUI(health);

        Debug.Log($"💔 Получен урон: {damage}. Здоровье: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    bool CanUseHack() => currentHackCharges > 0 && currentHackCooldown <= 0 && !IsGamePaused();
    bool CanUseShield() => currentShieldCharges > 0 && !isShieldActive && !IsGamePaused();

    void UseHack()
    {
        if (IsGamePaused()) return;

        // Проверяем, можно ли взломать босса
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 15f);
        foreach (Collider2D hit in hits)
        {
            CoreBoss boss = hit.GetComponent<CoreBoss>();
            if (boss != null && boss.CanBeHacked())
            {
                boss.HackBoss();
                Debug.Log("⚡ Взлом босса (без расхода заряда)");
                return; // Не тратим заряд!
            }
        }

        if (currentHackCharges > 0)
        {
            currentHackCharges--;

            // Запускаем взлом
            if (AbilityManager.Instance != null)
                AbilityManager.Instance.ActivateHack();

            // Обновляем UI
            UIManager.Instance?.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);

            // Устанавливаем кулдаун
            currentHackCooldown = hackCooldown;

            Debug.Log("Активирован Взлом!");
        }

        if (animator != null)
        {
            animator.SetTrigger("UseHack");
        }
    }


    void DeactivateShield()
    {
        isShieldActive = false;
        if (shieldEffect != null) shieldEffect.SetActive(false);
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
        if (currentHackCooldown > 0) currentHackCooldown -= Time.deltaTime;
        if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;
    }

    void UpdateVisuals()
    {
        if (animator == null) return;

        float speed = 0f;

        // Если НЕ на платформе — используем реальную скорость
        if (!isOnMovingPlatform)
        {
            speed = Mathf.Abs(rb.linearVelocity.x);
        }

        animator.SetFloat("Speed", speed);

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);

        if (movement.x > 0.1f) spriteRenderer.flipX = false;
        else if (movement.x < -0.1f) spriteRenderer.flipX = true;

        // === ОБНОВЛЕНИЕ UI СПОСОБНОСТЕЙ ===
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(health);
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);
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
        if (alreadyDied) return;
        alreadyDied = true;

        Debug.Log("💀 PlayerController: Игрок погиб!");

        // Останавливаем игрока
        canMove = false;

        // Останавливаем физику
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // Отключаем физику
        }

        // Визуальный эффект
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }

        // Вызываем GameManager
        if (GameManager.Instance != null)
        {
            // Небольшая задержка перед показом экрана смерти
            Invoke("CallGameManagerDeath", 0.5f);
        }
    }

    void CallGameManagerDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
        }
    }

    void UseShield()
    {
        if (IsGamePaused()) return;

        // Тратим заряд
        currentShieldCharges--;

        // Активируем щит
        isShieldActive = true;

        // Визуальный эффект (если есть)
        if (shieldEffect != null)
            shieldEffect.SetActive(true);

        spriteRenderer.color = new Color(0.3f, 0.8f, 1f, 0.8f);

        // Обновляем UI
        UIManager.Instance?.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);

        // Деактивация через 3 секунды
        Invoke(nameof(DeactivateShield), shieldDuration);

        if (animator != null)
        {
            animator.SetTrigger("UseShield");
        }

        Debug.Log("🛡 Активирован Щит!");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Computer"))
        {
            currentComputer = other.GetComponent<ComputerTerminal>();
        }

        if (other.CompareTag("DataPacket"))
        {
            CollectDataPacket(other.gameObject);
        }

        if (other.CompareTag("Exit"))
        {
            GameManager.Instance?.CompleteLevel();
        }
        else if (other.CompareTag("TutorialExit"))
        {
            GameManager.Instance?.CompleteTutorial();
        }

        if (other.CompareTag("Coin"))
        {
            GameManager.Instance?.CollectCoin(1);
        }

    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Computer"))
        {
            if (currentComputer != null && currentComputer.gameObject == other.gameObject)
            {
                Debug.Log($"💻 Вышел из зоны компьютера: {currentComputer.gameObject.name}");
                currentComputer = null;
                canInteract = false;
            }
        }
    }

    void CollectDataPacket(GameObject dataPacket)
    {
        GameManager.Instance?.CollectDataPacket(1);
        Destroy(dataPacket);
    }


    void OnCollisionStay2D(Collision2D collision) => CheckGroundOnCollision(collision);

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("MovingPlatform"))
        {
            currentPlatform = col.gameObject.GetComponent<MovingPlatform>();
            isOnMovingPlatform = true;
        }
        CheckGroundOnCollision(col);
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("MovingPlatform"))
        {
            currentPlatform = null;
            isOnMovingPlatform = false;
        }
        CheckGroundOnCollision(col);
    }

    void CheckGroundOnCollision(Collision2D col)
    {
        if (((1 << col.gameObject.layer) & groundLayer) != 0)
        {
            foreach (ContactPoint2D contact in col.contacts)
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

    public Vector2 GetPosition() => transform.position;
    public bool IsAlive() => health > 0;
    public float GetHackCooldownProgress() => 1f - (currentHackCooldown / hackCooldown);

    public void ResetPlayer()
    {
        if (gameObject == null) return;

        alreadyDied = false;
        health = 100;
        currentHackCharges = maxHackCharges;
        currentShieldCharges = maxShieldCharges;
        currentHackCooldown = 0f;
        isShieldActive = false;
        canMove = true;
        _maxJumps = 1;
        currentJumps = 0;
        isGrounded = false;
        isCrouching = false;
        wantsToCrouch = false;
        currentCrouchPercent = 0f;
        jumpRequested = false;
        jumpCooldownTimer = 0f;
        jumpKeyHeld = false;
        wasGrounded = false;
        lastTimeGrounded = 0f;

        if (canCrouch)
        {
            if (playerCollider is PolygonCollider2D poly && originalPoints != null)
            {
                poly.points = originalPoints;
                currentColliderHeight = originalHeight;
                UpdateHeadCheckPosition();
            }
            else if (playerCollider is BoxCollider2D box)
            {
                box.size = new Vector2(box.size.x, originalHeight);
                currentColliderHeight = originalHeight;
                UpdateHeadCheckPosition();
            }
            else if (playerCollider is CapsuleCollider2D cap)
            {
                cap.size = new Vector2(cap.size.x, originalHeight);
                currentColliderHeight = originalHeight;
                UpdateHeadCheckPosition();
            }
        }

        if (shieldEffect != null) shieldEffect.SetActive(false);
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(health);
            UIManager.Instance.UpdateAbilitiesUI(currentHackCharges, currentShieldCharges);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
        if (headCheckPoint != null && canCrouch)
        {
            Gizmos.color = canStandUp ? Color.blue : Color.yellow;
            Gizmos.DrawWireSphere(headCheckPoint.position, headCheckRadius);
        }
    }
}
