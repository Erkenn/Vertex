using UnityEngine;

public class Guardian : Enemy
{
    [Header("=== НАСТРОЙКИ СТРАЖА ===")]
    public float detectionDistance = 15f;
    public float attackRate = 1f;
    public int attackDamage = 30;
    public float groundCheckDistance = 0.2f;
    [Tooltip("Слой земли для проверки")]
    public LayerMask groundLayer;
    [Header("Преследование")]
    public float chaseSpeed = 6f;// Скорость погони
    public float attackRange = 1.5f; // Дистанция атаки

    private bool isChasing = false;
    private bool canAttack = true;
    private float attackTimer = 0f;
    private Collider2D guardianCollider;
    private Vector2 moveDirection;

    protected override void Start()
    {
        base.Start();
        isChasing = false;
        canAttack = true;

        // Получаем коллайдер
        guardianCollider = GetComponent<Collider2D>();

        FixSpriteColliderAlignment();

        // НАСТРАИВАЕМ Rigidbody2D
        ConfigureRigidbody();

        // ФИКС: Исправляем позицию при старте
        Invoke(nameof(FixSpawnPosition), 0.05f);
    }

    private void ConfigureRigidbody()
    {
        if (rb != null)
        {
            // ГРАВИТАЦИЯ И ФИЗИКА
            rb.gravityScale = 1f;
            rb.mass = 3f;
            rb.linearDamping = 2f; // ПРАВИЛЬНО: drag, а не linearDamping
            rb.angularDamping = 0.05f; // ПРАВИЛЬНО: angularDrag
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            Debug.Log($"Guardian Rigidbody настроен");
        }
    }

    private void FixSpriteColliderAlignment()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Collider2D col = GetComponent<Collider2D>();

        if (sr == null || col == null) return;

        // 1. Найти разницу между центрами
        float spriteCenterY = sr.bounds.center.y;
        float colliderCenterY = col.bounds.center.y;
        float differenceY = spriteCenterY - colliderCenterY;

        Debug.Log($"Разница позиций спрайт-коллайдер: {differenceY}");

        // 2. Исправить позицию спрайта
        if (Mathf.Abs(differenceY) > 0.01f)
        {
            transform.position += Vector3.up * differenceY;
            Debug.Log($"Исправлено: сдвинуто на {differenceY}");
        }

        // 3. Проверить границы
        float spriteBottom = sr.bounds.min.y;
        float colliderBottom = col.bounds.min.y;

        if (spriteBottom < colliderBottom)
        {
            float fixAmount = colliderBottom - spriteBottom + 0.05f;
            Debug.Log($"Спрайт ниже коллайдера. Поднимаем на: {fixAmount}");
            transform.position += Vector3.up * fixAmount;
        }

        // 4. Дополнительно: сделать Z = 0
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    }

    void FixSpawnPosition()
    {
        if (guardianCollider == null || rb == null) return;

        // Временно делаем kinematic для точной установки позиции
        RigidbodyType2D originalType = rb.bodyType;
        rb.bodyType = RigidbodyType2D.Kinematic;

        float rayLength = 5f;
        Vector2 rayStart = transform.position + Vector3.up * 2f;

        RaycastHit2D hit = Physics2D.Raycast(
            rayStart,
            Vector2.down,
            rayLength,
            groundLayer
        );

        if (hit.collider != null)
        {
            float halfHeight = guardianCollider.bounds.extents.y;
            Vector2 targetPosition = new Vector2(
                transform.position.x,
                hit.point.y + halfHeight + 0.1f
            );

            // Используем MovePosition для плавного перемещения
            rb.MovePosition(targetPosition);
            Debug.Log($"Guardian установлен на высоту: {targetPosition.y}");
        }

        // Возвращаем оригинальный тип
        rb.bodyType = originalType;
    }

    protected override void CustomBehavior()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerDetected = distanceToPlayer <= detectionDistance;

        if (playerDetected && !isChasing)
        {
            isChasing = true;
            Debug.Log($"🛡️ Страж {name} начал преследование!");
        }
        else if (!playerDetected && isChasing)
        {
            isChasing = false;
            moveDirection = Vector2.zero;
            Debug.Log($"🛡️ Страж {name} потерял игрока");
        }

        if (isChasing)
        {
            // Вычисляем направление к игроку
            float directionX = Mathf.Sign(player.position.x - transform.position.x);
            moveDirection = new Vector2(directionX, 0);

            // Поворот спрайта
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = directionX < 0;
            }

            // Если игрок близко - атакуем
            if (distanceToPlayer <= attackRange)
            {
                TryAttack();
            }
        }
    }

    private void FixedUpdate()
    {
        // ВСЁ ДВИЖЕНИЕ ТОЛЬКО В FixedUpdate!
        if (rb != null && IsGrounded())
        {
            if (isChasing && moveDirection != Vector2.zero)
            {
                // Используем velocity (не linearVelocity!)
                float currentChaseSpeed = chaseSpeed;
                Vector2 targetVelocity = moveDirection * currentChaseSpeed;

                // Плавное изменение скорости
                rb.linearVelocity = new Vector2(
                    Mathf.Lerp(rb.linearVelocity.x, targetVelocity.x, Time.fixedDeltaTime * 10f),
                    rb.linearVelocity.y
                );
            }
            else if (!isChasing)
            {
                // Плавная остановка
                rb.linearVelocity = new Vector2(
                    Mathf.Lerp(rb.linearVelocity.x, 0, Time.fixedDeltaTime * 5f),
                    rb.linearVelocity.y
                );
            }
        }
    }

    private bool IsGrounded()
    {
        if (guardianCollider == null) return false;

        float checkDistance = groundCheckDistance;
        Vector2 rayOrigin = new Vector2(
            transform.position.x,
            transform.position.y - guardianCollider.bounds.extents.y + 0.05f
        );

        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            checkDistance,
            groundLayer
        );

        Debug.DrawRay(rayOrigin, Vector2.down * checkDistance, Color.green);

        return hit.collider != null;
    }

    private void TryAttack()
    {
        if (!canAttack || player == null) return;

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsAlive())
        {
            if (playerController.isShieldActive)
            {
                Die();
                Debug.Log("🛡️ Страж уничтожен щитом!");
                return;
            }

            playerController.TakeDamage(attackDamage);
            canAttack = false;
            attackTimer = 1f / attackRate;
            Debug.Log($"🛡️ Страж ударил игрока ({attackDamage} урона)");

            // Отскок после атаки
            if (rb != null)
            {
                float directionFromPlayer = Mathf.Sign(transform.position.x - player.position.x);
                rb.linearVelocity = new Vector2(directionFromPlayer * 3f, rb.linearVelocity.y);
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        // Таймер атаки в Update
        if (!canAttack)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                canAttack = true;
                attackTimer = 0;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TryAttack();
        }
    }

    protected override void Die()
    {
        GameManager.Instance?.RegisterEnemyDestroyed();
        Destroy(gameObject);
    }
}