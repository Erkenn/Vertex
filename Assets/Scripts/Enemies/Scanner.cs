using UnityEngine;

public class Scanner : Enemy
{
    [Header("=== НАСТРОЙКИ СКАНЕРА ===")]
    public Transform[] patrolPoints;
    public float waitTimeAtPoints = 2f;

    private int currentPatrolIndex = 0;
    private bool isMoving = true;
    private float waitTimer = 0f;

    protected override void Start()
    {
        base.Start();

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning($"Scanner {gameObject.name} не имеет точек патрулирования!");
        }
    }

    protected override void PatrolBehavior()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (isMoving)
        {
            Transform targetPoint = patrolPoints[currentPatrolIndex];
            Vector2 direction = (targetPoint.position - transform.position).normalized;

            // Движение к точке
            if (rb != null)
                rb.linearVelocity = direction * moveSpeed;

            // Поворот спрайта
            if (spriteRenderer != null && direction.x != 0)
            {
                spriteRenderer.flipX = direction.x < 0;
            }

            // Проверка достижения точки
            if (Vector2.Distance(transform.position, targetPoint.position) < 0.1f)
            {
                isMoving = false;
                waitTimer = waitTimeAtPoints;

                if (rb != null)
                    rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            // Ожидание на точке
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isMoving = true;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
    }

    protected override void ChaseBehavior(float distanceToPlayer)
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;

        // Движение к игроку
        if (rb != null)
            rb.linearVelocity = direction * moveSpeed * 1.5f; // Быстрее при преследовании

        // Поворот спрайта
        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }

        // Атака при близком расстоянии
        if (distanceToPlayer <= attackRange)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    protected override void StartChasing()
    {
        base.StartChasing();
        Debug.Log($"🛰️ Сканер {gameObject.name} обнаружил игрока!");
    }
}