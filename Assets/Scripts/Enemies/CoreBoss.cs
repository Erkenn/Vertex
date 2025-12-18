/*
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CoreBoss : Enemy
{
    [Header("=== НАСТРОЙКИ ЯДРА ===")]
    public int totalHealth = 1000;
    public int phaseCount = 3;
    public float vulnerabilityDuration = 10f;
    public float attackCooldown = 3f;
    public GameObject[] defenseSystems;

    [Header("=== АТАКИ ЯДРА ===")]
    public GameObject projectilePrefab;
    public GameObject laserPrefab;
    public Transform[] attackPoints;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public Material vulnerableMaterial;
    public Material invulnerableMaterial;
    public GameObject shieldEffect;
    public GameObject destructionEffect;

    private int currentPhase = 1;
    private bool isVulnerable = false;
    private bool defenseSystemsActive = true;
    private float vulnerabilityTimer = 0f;
    private float attackTimer = 0f;
    private SpriteRenderer coreRenderer;
    private List<GameObject> activeDefenses = new List<GameObject>();

    protected override void Start()
    {
        base.Start();

        health = totalHealth;
        coreRenderer = GetComponent<SpriteRenderer>();

        // Активация систем защиты
        ActivateDefenseSystems();

        Debug.Log("👁️ Центральное Ядро активировано!");
    }

    protected override void Update()
    {
        if (!isActive || isStunned) return;

        UpdateVulnerability();
        UpdateAttack();
        UpdatePhase();
    }

    protected override void PatrolBehavior()
    {
        // Босс не патрулирует
    }

    protected override void ChaseBehavior(float distanceToPlayer)
    {
        // Босс не преследует
    }

    void UpdateVulnerability()
    {
        if (isVulnerable)
        {
            vulnerabilityTimer -= Time.deltaTime;

            if (vulnerabilityTimer <= 0)
            {
                SetVulnerable(false);
            }
        }
    }

    void UpdateAttack()
    {
        if (!isVulnerable || player == null) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            PerformAttack();
            attackTimer = attackCooldown;
        }
    }

    void UpdatePhase()
    {
        int targetPhase = Mathf.CeilToInt((float)health / (totalHealth / phaseCount));
        targetPhase = Mathf.Clamp(targetPhase, 1, phaseCount);

        if (targetPhase != currentPhase)
        {
            ChangePhase(targetPhase);
        }
    }

    void PerformAttack()
    {
        if (player == null) return;

        // Случайный выбор атаки в зависимости от фазы
        int attackType = Random.Range(0, currentPhase + 1);

        switch (attackType)
        {
            case 0:
                SingleProjectileAttack();
                break;
            case 1:
                SpreadAttack();
                break;
            case 2:
                LaserAttack();
                break;
            case 3:
                CircleAttack();
                break;
        }
    }

    void SingleProjectileAttack()
    {
        if (projectilePrefab == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage = damage * currentPhase;
            proj.isEnemyProjectile = true;
        }

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * 8f;
        }

        Debug.Log("💥 Ядро: Одиночная атака!");
    }

    void SpreadAttack()
    {
        if (projectilePrefab == null) return;

        for (int i = 0; i < 5; i++)
        {
            float angle = (i - 2) * 15f;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * (player.position - transform.position).normalized;

            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

            Projectile proj = projectile.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.damage = damage;
                proj.isEnemyProjectile = true;
            }

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direction * 6f;
            }
        }

        Debug.Log("💥 Ядро: Веерная атака!");
    }

    void LaserAttack()
    {
        if (laserPrefab == null) return;

        foreach (Transform point in attackPoints)
        {
            GameObject laser = Instantiate(laserPrefab, point.position, point.rotation);
            Destroy(laser, 2f);
        }

        Debug.Log("🔫 Ядро: Лазерная атака!");
    }

    void CircleAttack()
    {
        if (projectilePrefab == null) return;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;

            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

            Projectile proj = projectile.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.damage = damage;
                proj.isEnemyProjectile = true;
            }

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direction * 5f;
            }
        }

        Debug.Log("💥 Ядро: Круговая атака!");
    }

    void ChangePhase(int newPhase)
    {
        currentPhase = newPhase;

        // Увеличиваем сложность атак
        attackCooldown = Mathf.Max(1f, attackCooldown * 0.7f);
        damage = Mathf.RoundToInt(damage * 1.5f);

        Debug.Log($"🔄 Ядро перешло в фазу {currentPhase}! Сложность увеличена.");

        // Визуальные изменения
        if (coreRenderer != null)
        {
            coreRenderer.color = Color.Lerp(Color.white, Color.red, (float)(currentPhase - 1) / (phaseCount - 1));
        }
    }

    void ActivateDefenseSystems()
    {
        defenseSystemsActive = true;

        foreach (GameObject system in defenseSystems)
        {
            if (system != null)
            {
                system.SetActive(true);
                activeDefenses.Add(system);
            }
        }

        SetVulnerable(false);

        Debug.Log("🛡️ Системы защиты ядра активированы!");
    }

    void DeactivateDefenseSystems()
    {
        defenseSystemsActive = false;

        foreach (GameObject system in activeDefenses)
        {
            if (system != null)
            {
                system.SetActive(false);
            }
        }

        SetVulnerable(true);

        Debug.Log("🛡️ Системы защиты ядра деактивированы!");
    }

    void SetVulnerable(bool vulnerable)
    {
        isVulnerable = vulnerable;

        if (coreRenderer != null)
        {
            coreRenderer.material = vulnerable ? vulnerableMaterial : invulnerableMaterial;
        }

        if (shieldEffect != null)
        {
            shieldEffect.SetActive(!vulnerable);
        }

        if (vulnerable)
        {
            vulnerabilityTimer = vulnerabilityDuration;
            Debug.Log("⚠️ Ядро уязвимо! Атакуйте!");
        }
        else
        {
            Debug.Log("🛡️ Ядро защищено. Уничтожьте системы защиты.");
        }
    }

    public void OnDefenseSystemDestroyed()
    {
        // Проверяем остались ли активные системы защиты
        bool anyDefenseActive = false;
        foreach (GameObject system in activeDefenses)
        {
            if (system != null && system.activeInHierarchy)
            {
                anyDefenseActive = true;
                break;
            }
        }

        if (!anyDefenseActive)
        {
            DeactivateDefenseSystems();
        }
    }

    public override void TakeDamage(int damageAmount)
    {
        if (!isVulnerable || !isActive) return;

        base.TakeDamage(damageAmount);

        // Визуальная обратная связь при получении урона
        StartCoroutine(DamageFlash());

        Debug.Log($"💢 Ядро получило урон: {damageAmount}. Здоровье: {health}/{totalHealth}");
    }

    protected override void Die()
    {
        isActive = false;

        // Эффект разрушения
        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        }

        // Победа в игре
        if (GameManager.Instance != null)
        {
            GameManager.Instance.WinGame();
        }

        Debug.Log("🎉 Центральное Ядро уничтожено! Победа!");

        // Уничтожение объекта
        Destroy(gameObject, 2f);
    }

    public override void GetStunned(float duration)
    {
        // Босс не может быть оглушен обычным взломом
        // Нужна специальная механика для оглушения босса
        Debug.Log("🛡️ Взлом не эффективен против Центрального Ядра!");
    }

    // Метод для активации специальной уязвимости (через взлом)
    public void ActivateVulnerability(float duration)
    {
        if (defenseSystemsActive) return;

        SetVulnerable(true);
        vulnerabilityTimer = duration;

        Debug.Log($"⚡ Специальный взлом! Ядро уязвимо на {duration} секунд!");
    }
}
*/