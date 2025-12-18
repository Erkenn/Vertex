/*
using UnityEngine;

public class DefenseSystem : Enemy
{
    [Header("=== НАСТРОЙКИ СИСТЕМЫ ЗАЩИТЫ ===")]
    public CoreBoss coreBoss;
    public GameObject destructionEffect;

    protected override void Start()
    {
        base.Start();

        // Находим ядро если не назначено
        if (coreBoss == null)
        {
            coreBoss = FindFirstObjectByType<CoreBoss>();
        }
    }

    protected override void PatrolBehavior()
    {
        // Системы защиты не патрулируют
    }

    protected override void ChaseBehavior(float distanceToPlayer)
    {
        // Системы защиты не преследуют
    }

    public override void TakeDamage(int damageAmount)
    {
        base.TakeDamage(damageAmount);

        // Визуальная обратная связь
        StartCoroutine(DamageFlash());
    }

    protected override void Die()
    {
        isActive = false;

        // Эффект разрушения
        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        }

        // Уведомляем ядро о разрушении системы
        if (coreBoss != null)
        {
            coreBoss.OnDefenseSystemDestroyed();
        }

        Debug.Log("💥 Система защиты уничтожена!");

        // Уничтожение объекта
        Destroy(gameObject);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Визуализация связи с ядром
        if (coreBoss != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, coreBoss.transform.position);
        }
    }
}
*/