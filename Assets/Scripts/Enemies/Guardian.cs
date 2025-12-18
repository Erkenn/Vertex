/*
using UnityEngine;

public class Guardian : Enemy
{
    [Header("=== НАСТРОЙКИ СТРАЖА ===")]
    public float chaseSpeed = 4f;
    public float acceleration = 8f;
    public float rotationSpeed = 5f;

    protected override void PatrolBehavior()
    {
        // Стражи не патрулируют - они стоят на месте
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    protected override void ChaseBehavior(float distanceToPlayer)
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;

        // Плавное преследование с ускорением
        if (rb != null)
        {
            Vector2 targetVelocity = direction * chaseSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.deltaTime);
        }

        // Плавный поворот к игроку
        if (direction.x != 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x > 0;
        }

        // Анимация преследования
        if (animator != null)
        {
            animator.SetFloat("Speed", rb.linearVelocity.magnitude);
        }
    }

    protected override void StartChasing()
    {
        base.StartChasing();
        Debug.Log($"👁️ Страж {gameObject.name} начал преследование!");

        // Увеличиваем скорость при обнаружении
        moveSpeed = chaseSpeed;
    }

    protected override void StopChasing()
    {
        base.StopChasing();

        // Возвращаем обычную скорость
        moveSpeed = chaseSpeed * 0.7f;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    protected override void AttackPlayer(PlayerController player)
    {
        base.AttackPlayer(player);

        // Дополнительный эффект при атаке стража
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
}
*/