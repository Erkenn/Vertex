/*
using UnityEngine;
using System.Collections;

public class Turret : Enemy
{
    [Header("=== НАСТРОЙКИ ТУРЕЛИ ===")]
    public float rotationSpeed = 2f;
    public float shootCooldown = 2f;
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float projectileSpeed = 8f;

    private float shootTimer = 0f;
    private bool canShoot = true;

    protected override void Start()
    {
        base.Start();

        // Турели не двигаются - ИСПРАВЛЕННЫЙ КОД
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // ЗАМЕНА isKinematic
            rb.linearVelocity = Vector2.zero;
        }

        shootTimer = shootCooldown;
    }

    protected override void PatrolBehavior()
    {
        // Турели не патрулируют
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    protected override void ChaseBehavior(float distanceToPlayer)
    {
        if (player == null || !canShoot) return;

        // Поворот к игроку
        Vector2 direction = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Плавный поворот
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Стрельба
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0 && distanceToPlayer <= detectionRange)
        {
            Shoot();
            shootTimer = shootCooldown;
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || shootPoint == null) return;

        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();

        if (projectileRb != null)
        {
            Vector2 shootDirection = (player.position - shootPoint.position).normalized;
            projectileRb.linearVelocity = shootDirection * projectileSpeed;
        }

        // Настройка снаряда
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.damage = damage;
            projectileScript.isEnemyProjectile = true;
        }

        Debug.Log($"🔫 Турель {gameObject.name} выстрелила");
    }

    public override void GetStunned(float duration)
    {
        base.GetStunned(duration);
        canShoot = false;

        // Останавливаем стрельбу на время оглушения
        StartCoroutine(RecoverShooting(duration));
    }

    private IEnumerator RecoverShooting(float delay)
    {
        yield return new WaitForSeconds(delay);
        canShoot = true;
        shootTimer = shootCooldown;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (shootPoint != null)
        {
            // Линия выстрела
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(shootPoint.position, shootPoint.position + shootPoint.right * 3f);
        }
    }
}
*/