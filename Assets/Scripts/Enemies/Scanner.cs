using UnityEngine;

public class Scanner : Enemy
{
    [Header("=== ПАТРУЛИРОВАНИЕ ===")]
    public Transform[] patrolPoints;
    public float waitTime = 1f;

    [Header("=== СКАНИРОВАНИЕ ===")]
    public float scanDistance = 10f;
    public LayerMask playerLayer;

    private AudioSource scannerAudioSource;
    public AudioClip scanLoopSound;

    [Range(0, 90)]
    public float scanAngle = 45f; // по умолчанию 45°

    private int currentPoint = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool hasKilledPlayer = false;
    public GameObject scannerLaserPrefab;
    private GameObject activeLaser;
    private AudioSource scanAudioSource;
    public AudioClip scanLoopClip;

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

        scanAudioSource = gameObject.AddComponent<AudioSource>();
        scanAudioSource.clip = scanLoopClip;
        scanAudioSource.loop = true;
        scanAudioSource.playOnAwake = false;
        scanAudioSource.volume = 0.3f;

        if (scanLoopClip != null && !isStunned)
        {
            scanAudioSource.Play();
        }

        UpdateSFXVolume();

        if (scanLoopClip != null && !isStunned)
        {
            scanAudioSource.Play();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.OnSFXVolumeChanged += UpdateSFXVolume;
        }
    }

    private void UpdateSFXVolume()
    {
        if (scanAudioSource == null || AudioManager.Instance == null) return;

        float sfxVol = AudioManager.Instance.sfxVolume;
        float masterVol = AudioManager.Instance.masterVolume;
        scanAudioSource.volume = Mathf.Clamp01(sfxVol * masterVol * 0.5f); // 0.5f — ваш множитель
    }

    // === ПЕРЕОПРЕДЕЛЯЕМ RecoverFromStun ===
    protected override void RecoverFromStun()
    {
        base.RecoverFromStun();

        hasKilledPlayer = false;
        HideLaser();

        if (scanLoopClip != null && scanAudioSource != null && !scanAudioSource.isPlaying)
        {
            scanAudioSource.Play();
        }
    }

    // === ПЕРЕОПРЕДЕЛЯЕМ GetStunned, чтобы остановить звук ===
    public override void GetStunned(float duration)
    {
        // Сначала останавливаем звук
        if (scanAudioSource != null && scanAudioSource.isPlaying)
        {
            scanAudioSource.Stop();
        }

        // Затем вызываем базовую логику оглушения
        base.GetStunned(duration);
    }

    // === ОСТАНОВКА ЗВУКА ПРИ УНИЧТОЖЕНИИ ===
    protected override void Die()
    {
        if (scanAudioSource != null)
        {
            scanAudioSource.Stop();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.OnSFXVolumeChanged -= UpdateSFXVolume;
        }
    
        base.Die();
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
        if (hasKilledPlayer || player == null)
        {
            HideLaser();
            return;
        }

        Vector2 lookDirection = spriteRenderer.flipX ? Vector2.right : Vector2.left;
        float angleRad = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDirection = new Vector2(
            lookDirection.x,
            -Mathf.Tan(angleRad) * Mathf.Abs(lookDirection.x)
        ).normalized;

        Vector2 scanOrigin = (Vector2)transform.position + lookDirection * 0.5f + Vector2.down * 0.3f;

        // Показываем лазер
        ShowLaser(scanOrigin, scanDirection * scanDistance);

        RaycastHit2D hit = Physics2D.Raycast(scanOrigin, scanDirection, scanDistance, playerLayer);
        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            KillPlayer();
            hasKilledPlayer = true;
        }
    }

    private void ShowLaser(Vector2 origin, Vector2 endOffset)
    {
        if (scannerLaserPrefab == null) return;

        // Создаём лазер, если его нет
        if (activeLaser == null)
        {
            activeLaser = Instantiate(scannerLaserPrefab, transform.position, Quaternion.identity);
            activeLaser.transform.SetParent(transform); // Опционально
        }

        LineRenderer line = activeLaser.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.SetPosition(0, origin);
            line.SetPosition(1, origin + endOffset);
            line.enabled = true;
        }
    }

    private void HideLaser()
    {
        if (activeLaser != null)
        {
            LineRenderer line = activeLaser.GetComponent<LineRenderer>();
            if (line != null) line.enabled = false;
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
            Vector2 lookDirection = spriteRenderer.flipX ? Vector2.right : Vector2.left;
            float angleRad = scanAngle * Mathf.Deg2Rad;
            Vector2 scanDirection = new Vector2(
                lookDirection.x,
                -Mathf.Tan(angleRad) * Mathf.Abs(lookDirection.x)
            ).normalized;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + scanDirection * scanDistance);
        }
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.OnSFXVolumeChanged -= UpdateSFXVolume;
        }
    }

}