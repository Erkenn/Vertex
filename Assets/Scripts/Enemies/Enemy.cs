using UnityEngine;
using System.Collections;

public abstract class Enemy : MonoBehaviour
{
    [Header("=== ������� ��������� ����� ===")]
    public int health = 1;
    public float moveSpeed = 3f;
    public int damage = 1;
    public float detectionRange = 5f;
    public float attackRange = 1f;
    public int scoreValue = 100;

    [Header("=== ��������� ����� ===")]
    public bool isActive = true;
    public bool isStunned = false;
    public bool isChasing = false;

    [Header("=== ���������� ������� ===")]
    public Material originalMaterial;
    public Color alertColor = new Color(1f, 0.3f, 0.3f, 1f);
    public GameObject deathEffect;

    // ����������
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;

    // ��������� ����������
    protected float stunTimeRemaining = 0f;
    protected Color originalColor;

    // �������
    public System.Action OnEnemyDestroyed;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
            originalColor = spriteRenderer.color;
        }

        Debug.Log($"{gameObject.name} ���������������");
    }

    protected virtual void Update()
    {
        if (!isActive || isStunned) return;

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                if (!isChasing)
                {
                    StartChasing();
                }
                ChaseBehavior(distanceToPlayer);
            }
            else
            {
                if (isChasing)
                {
                    StopChasing();
                }
                PatrolBehavior();
            }
        }
        else
        {
            PatrolBehavior();
        }

        UpdateStunStatus();
    }

    // === �������� ��������� ===
    protected abstract void PatrolBehavior();
    protected abstract void ChaseBehavior(float distanceToPlayer);

    protected virtual void StartChasing()
    {
        isChasing = true;
        if (spriteRenderer != null)
            spriteRenderer.color = alertColor;

        Debug.Log($"{gameObject.name} ����� �������������");
    }

    protected virtual void StopChasing()
    {
        isChasing = false;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    // === ������� ��������� ===
    public virtual void GetStunned(float duration)
    {
        if (!isActive) return;

        isStunned = true;
        stunTimeRemaining = duration;

        // ������������� ��������
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // ���������� ������ ���������
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.5f, 0.5f, 1f, 0.7f);
        }

        Debug.Log($"{gameObject.name} ������� �� {duration} ���");
    }

    protected virtual void UpdateStunStatus()
    {
        if (isStunned)
        {
            stunTimeRemaining -= Time.deltaTime;
            if (stunTimeRemaining <= 0)
            {
                RecoverFromStun();
            }
        }
    }

    protected virtual void RecoverFromStun()
    {
        isStunned = false;
        stunTimeRemaining = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = isChasing ? alertColor : originalColor;
        }

        Debug.Log($"{gameObject.name} ������������� ����� ���������");
    }

    // === ������� �������� � ����� ===
    public virtual void TakeDamage(int damageAmount)
    {
        if (!isActive) return;

        health -= damageAmount;

        // ���������� �������� �����
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
        isActive = false;

        // ���������� ������ ������
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // ������� ������
        GameManager.Instance?.RegisterEnemyDestroyed();

        // ������� �����������
        OnEnemyDestroyed?.Invoke();

        Debug.Log($"{gameObject.name} ���������");

        // ����������� �������
        Destroy(gameObject);
    }

    // === ������� ����� ===
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive || isStunned) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            AttackPlayer(player);
        }
    }

    protected virtual void AttackPlayer(PlayerController player)
    {
        player.TakeDamage(damage);
        Debug.Log($"{gameObject.name} �������� ������");
    }

    // === ������������ � EDITOR ===
    protected virtual void OnDrawGizmosSelected()
    {
        // ������ �����������
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // ������ �����
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}