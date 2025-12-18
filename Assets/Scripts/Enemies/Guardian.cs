using UnityEngine;

public class Guardian : Enemy
{
    [Header("=== НАСТРОЙКИ СТРАЖА ===")]
    public float detectionDistance = 8f;
    public float attackRate = 1f;
    public int attackDamage = 30;
    public float groundCheckDistance = 0.2f;
    [Tooltip("Слой земли для проверки")]
    public LayerMask groundLayer;

    private bool isChasing = false;
    private bool canAttack = true;
    private float attackTimer = 0f;
    private Collider2D guardianCollider;
    private float originalColliderHeight;

    protected override void Start()
    {
        base.Start();
        isChasing = false;
        canAttack = true;

        // Получаем коллайдер
        guardianCollider = GetComponent<Collider2D>();
        if (guardianCollider != null)
        {
            originalColliderHeight = guardianCollider.bounds.size.y;
        }

        // Настраиваем Rigidbody2D
        ConfigureRigidbody();
    }

    private void ConfigureRigidbody()
    {
        if (rb != null)
        {
            rb.gravityScale = 3f; // Сильная гравитация для лучшего прилипания к земле
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            // Важно: устанавливаем массу и линейное сопротивление
            rb.mass = 10f; // Тяжелый, чтобы не отталкивался
            rb.linearDamping = 2f; // Быстрее останавливается
            rb.angularDamping = 0.05f;
        }
    }

    protected override void CustomBehavior()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerDetected = distanceToPlayer <= detectionDistance;

        if (playerDetected && !isChasing)
        {
            isChasing = true;
            Debug.Log($"🛡️ Страж {name} обнаружил игрока!");
        }
        else if (!playerDetected && isChasing)
        {
            isChasing = false;
            StopMovement();
            Debug.Log($"🛡️ Страж {name} потерял игрока");
        }

        if (isChasing)
        {
            // Проверяем, можем ли мы двигаться
            if (IsGrounded() && CanMoveToPlayer())
            {
                MoveTowardsPlayer();
            }
            else
            {
                // Если не можем двигаться, останавливаемся
                StopMovement();
            }
        }
    }

    private void StopMovement()
    {
        if (rb != null && IsGrounded())
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private bool CanMoveToPlayer()
    {
        if (player == null) return false;

        // Проверяем, нет ли препятствия перед нами
        float directionX = Mathf.Sign(player.position.x - transform.position.x);
        float checkDistance = 0.5f;

        Vector2 rayOrigin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            new Vector2(directionX, 0),
            checkDistance
        );

        Debug.DrawRay(rayOrigin, new Vector2(directionX, 0) * checkDistance, Color.blue);

        // Если есть препятствие, но это не игрок - не можем двигаться
        if (hit.collider != null && !hit.collider.CompareTag("Player"))
        {
            return false;
        }

        return true;
    }

    private void MoveTowardsPlayer()
    {
        if (player == null || !IsGrounded()) return;

        // Направление по горизонтали
        float directionX = Mathf.Sign(player.position.x - transform.position.x);
        float chaseSpeed = moveSpeed * 1.5f;

        if (rb != null)
        {
            // Плавное движение
            float targetVelocityX = directionX * chaseSpeed;
            float currentVelocityX = rb.linearVelocity.x;
            float smoothVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, Time.deltaTime * 10f);

            rb.linearVelocity = new Vector2(smoothVelocityX, rb.linearVelocity.y);

            // Поворот спрайта
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = directionX < 0;
            }
        }

        // Атака при близком расстоянии
        float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(player.position.y - transform.position.y);

        if (horizontalDistance < 1f && verticalDistance < 1.5f)
        {
            TryAttack();
        }
    }

    private bool IsGrounded()
    {
        if (guardianCollider == null) return false;

        float checkDistance = groundCheckDistance;
        Vector2 rayOrigin = new Vector2(
            transform.position.x,
            transform.position.y - (guardianCollider.bounds.extents.y - 0.1f)
        );

        // Проверяем лучом вниз
        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            checkDistance,
            groundLayer
        );

        // Также можно проверить дополнительными лучами по бокам коллайдера
        RaycastHit2D hitLeft = Physics2D.Raycast(
            rayOrigin + Vector2.left * guardianCollider.bounds.extents.x * 0.5f,
            Vector2.down,
            checkDistance,
            groundLayer
        );

        RaycastHit2D hitRight = Physics2D.Raycast(
            rayOrigin + Vector2.right * guardianCollider.bounds.extents.x * 0.5f,
            Vector2.down,
            checkDistance,
            groundLayer
        );

        // Визуализация для отладки
        Debug.DrawRay(rayOrigin, Vector2.down * checkDistance, Color.green);
        Debug.DrawRay(rayOrigin + Vector2.left * guardianCollider.bounds.extents.x * 0.5f,
                     Vector2.down * checkDistance, Color.yellow);
        Debug.DrawRay(rayOrigin + Vector2.right * guardianCollider.bounds.extents.x * 0.5f,
                     Vector2.down * checkDistance, Color.yellow);

        return hit.collider != null || hitLeft.collider != null || hitRight.collider != null;
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

            // Легкий отскок при атаке
            if (rb != null)
            {
                float directionFromPlayer = Mathf.Sign(transform.position.x - player.position.x);
                rb.linearVelocity = new Vector2(directionFromPlayer * 2f, rb.linearVelocity.y);
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        // Таймер атаки
        if (!canAttack)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                canAttack = true;
                attackTimer = 0;
            }
        }

        // Отладочная информация
        Debug.Log($"Guardian: Grounded={IsGrounded()}, Velocity={rb.linearVelocity}");
    }

    private void FixedUpdate()
    {
        // Исправляем позицию, если провалился в землю
        FixPositionIfSunk();

        // В FixedUpdate работаем с физикой
        if (!isChasing && rb != null && IsGrounded())
        {
            // Плавная остановка
            float currentX = rb.linearVelocity.x;
            float newX = Mathf.Lerp(currentX, 0, Time.fixedDeltaTime * 15f);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }
    }

    private void FixPositionIfSunk()
    {
        if (guardianCollider == null) return;

        // Проверяем, не провалился ли страж в землю
        Vector2 checkOrigin = transform.position;
        float checkDepth = 0.5f;

        RaycastHit2D groundHit = Physics2D.Raycast(
            checkOrigin,
            Vector2.down,
            guardianCollider.bounds.extents.y + checkDepth,
            groundLayer
        );

        if (groundHit.collider != null)
        {
            // Если слишком глубоко в земле, приподнимаем
            float desiredY = groundHit.point.y + guardianCollider.bounds.extents.y + 0.05f;
            if (transform.position.y < desiredY - 0.1f)
            {
                Vector3 newPosition = transform.position;
                newPosition.y = Mathf.Lerp(newPosition.y, desiredY, Time.fixedDeltaTime * 10f);
                rb.MovePosition(newPosition);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Если столкнулись с игроком
        if (collision.gameObject.CompareTag("Player"))
        {
            // Не отталкиваем игрока сильно
            if (rb != null)
            {
                // Минимальное отталкивание
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.1f, rb.linearVelocity.y);
            }

            TryAttack();
        }
        // Если столкнулись с землей или стеной
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                 collision.gameObject.CompareTag("Ground") ||
                 collision.gameObject.CompareTag("Wall"))
        {
            // Останавливаем горизонтальное движение
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Если игрок стоит на страже
        if (collision.gameObject.CompareTag("Player"))
        {
            // Проверяем, находится ли игрок сверху
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) // Игрок сверху
                {
                    // Игрок может прыгнуть на стража
                    return;
                }
            }

            // Если игрок не сверху, а сбоку - атакуем
            TryAttack();
        }
    }

    protected override void Die()
    {
        GameManager.Instance?.RegisterEnemyDestroyed();
        Destroy(gameObject);
    }
}