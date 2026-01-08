using UnityEngine;
using System.Collections; // ← добавьте это для IEnumerator

public abstract class Enemy : MonoBehaviour
{
    [Header("=== ОСНОВНЫЕ ПАРАМЕТРЫ ===")]
    public int health = 1;
    public float moveSpeed = 3f;
    public int scoreValue = 100;

    [Header("=== СОСТОЯНИЯ ===")]
    private bool _isActive = true;
    public virtual bool isActive
    {
        get => _isActive;
        set => _isActive = value;
    }
    public bool isStunned = false;

    public System.Action OnEnemyDestroyed;

    // Компоненты
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;

    // Для оглушения
    protected float stunTimer = 0f;

    private bool alreadyDied = false;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        isActive = true;
    }

    protected virtual void Update()
    {
        if (!isActive) return;

        if (isStunned)
        {
            HandleStun();
            return;
        }

        CustomBehavior();
    }

    protected abstract void CustomBehavior();

    // === НОВЫЙ МЕТОД: ВЗЯТИЕ УРОНА ===
    public virtual void TakeDamage(int damageAmount)
    {
        if (!isActive) return;

        health -= damageAmount;
        StartCoroutine(DamageFlash());

        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
        }
    }

    protected virtual void Die()
    {
        if (alreadyDied) return;
        alreadyDied = true;

        // Уведомляем GameManager или других слушателей
        OnEnemyDestroyed?.Invoke();

        // Уведомление GameManager (опционально, но у вас есть такой вызов)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemyDestroyed();
        }

        // Уничтожаем объект
        Destroy(gameObject);
    }

    // === ОСТАЛЬНЫЕ МЕТОДЫ ===
    public virtual void GetStunned(float duration)
    {
        if (!isActive) return;

        isStunned = true;
        stunTimer = duration;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.7f, 0.7f, 1f, 0.8f);

        Debug.Log($"{gameObject.name} оглушен на {duration} сек");
    }

    protected virtual void HandleStun()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0)
        {
            RecoverFromStun();
        }
    }

    protected virtual void RecoverFromStun()
    {
        isStunned = false;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    public void StopEnemy()
    {
        enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (animator != null)
        {
            animator.enabled = false;
        }
        isActive = false;
    }

}