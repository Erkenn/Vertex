using UnityEngine;

public class Scanner : Enemy
{
    [Header("=== ПАТРУЛИРОВАНИЕ ===")]
    public Transform[] patrolPoints;
    public float waitTime = 1f;

    [Header("=== СКАНИРОВАНИЕ ===")]
    public float scanDistance = 10f;
    public LayerMask playerLayer;

    private int currentPoint = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool hasKilledPlayer = false;

    protected override void Start()
    {
        base.Start();

        // Проверка точек патрулирования
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError($"Scanner {name} не имеет точек патрулирования!");
            enabled = false;
            return;
        }

        // Ориентация в начальную точку
        transform.position = patrolPoints[0].position;
    }

    protected override void CustomBehavior()
    {
        PatrolMovement();
        ScanForPlayer();
    }

    private void PatrolMovement()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                currentPoint = (currentPoint + 1) % patrolPoints.Length;
            }
            return;
        }

        // Движение к текущей точке
        Vector2 targetPosition = patrolPoints[currentPoint].position;
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        // Передвижение
        if (rb != null)
            rb.linearVelocity = direction * moveSpeed;

        // Поворот спрайта в зависимости от направления
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x > 0; // Если движемся вправо - флипаем
        }

        // Проверка достижения точки
        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            isWaiting = true;
            waitTimer = waitTime;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    private void ScanForPlayer()
    {
        if (hasKilledPlayer || player == null) return;

        // Направление сканирования: влево или вправо в зависимости от ориентации
        Vector2 scanDirection = spriteRenderer.flipX ? Vector2.right : Vector2.left;

        // Луч под 45 градусов вниз
        Vector2 angledDirection = new Vector2(scanDirection.x, -1f).normalized;

        // Начальная точка луча (немного смещенная от центра)
        Vector2 scanOrigin = (Vector2)transform.position + new Vector2(scanDirection.x * 0.5f, -0.3f);

        // Отладочный луч
        Debug.DrawRay(scanOrigin, angledDirection * scanDistance, Color.cyan);

        // Проверка столкновения
        RaycastHit2D hit = Physics2D.Raycast(scanOrigin, angledDirection, scanDistance, playerLayer);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            KillPlayer();
            hasKilledPlayer = true;
        }
    }

    private void KillPlayer()
    {
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsAlive())
        {
            // Мгновенная смерть
            playerController.TakeDamage(1000);
            Debug.Log($"Scanner {name} убил игрока");
        }
    }

    protected override void RecoverFromStun()
    {
        base.RecoverFromStun();
        // Сбрасываем статус убийства, чтобы можно было убить снова после пробуждения
        hasKilledPlayer = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Рисуем точки патрулирования
        if (patrolPoints != null)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);

                // Соединяем точки линиями
                if (i < patrolPoints.Length - 1)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                }
                // Замыкаем цикл
                else if (patrolPoints.Length > 1)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                }
            }
        }

        // Рисуем зону сканирования
        if (Application.isPlaying && spriteRenderer != null)
        {
            Vector2 scanDirection = spriteRenderer.flipX ? Vector2.right : Vector2.left;
            Vector2 angledDirection = new Vector2(scanDirection.x, -1f).normalized;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + angledDirection * scanDistance);
        }
    }
}