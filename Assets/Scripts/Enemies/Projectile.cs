using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ СНАРЯДА ===")]
    public int damage = 1;
    public float lifetime = 3f;
    public bool isEnemyProjectile = false;
    public GameObject impactEffect;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public Color playerProjectileColor = new Color(0.2f, 0.8f, 1f, 1f);
    public Color enemyProjectileColor = new Color(1f, 0.3f, 0.3f, 1f);

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Установка цвета в зависимости от типа снаряда
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isEnemyProjectile ? enemyProjectileColor : playerProjectileColor;
        }

        // Автоматическое уничтожение через время
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Игнорируем триггеры и столкновения с создателем
        if (other.isTrigger) return;

        // Обработка попадания по игроку (для вражеских снарядов)
        if (isEnemyProjectile && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                CreateImpactEffect();
                Destroy(gameObject);
            }
        }
        // Обработка попадания по врагам (для снарядов игрока)
        else if (!isEnemyProjectile && other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                CreateImpactEffect();
                Destroy(gameObject);
            }
        }
        // Столкновение со стенами
        else if (other.CompareTag("Wall"))
        {
            CreateImpactEffect();
            Destroy(gameObject);
        }
    }

    private void CreateImpactEffect()
    {
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }
    }
}