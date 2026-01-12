using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ComputerTerminal : MonoBehaviour
{
    [Header("Настройки компьютера")]
    public KeyCode activationKey = KeyCode.E;
    public float activationRadius = 1.5f;

    [Header("Состояние")]
    public bool isActive = false;
    public bool canBeReused = false; // Можно использовать повторно? (по умолчанию нет)
    public bool used = false; // Уже использован

    [Header("Цвета индикации")]
    public Color readyColor = new Color(0f, 0.8f, 0f, 1f); // Зеленый (готов)
    public Color activeColor = new Color(0f, 1f, 0.2f, 1f); // Ярко-зеленый (активен)
    public Color usedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Серый (использован)

    [Header("Эффекты")]
    public ParticleSystem activationParticles;
    public AudioClip activationSound;

    [Header("События")]
    public UnityEvent onComputerActivated;

    private SpriteRenderer spriteRenderer;
    private bool playerInRange = false;
    private AudioSource audioSource;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        UpdateVisual();
    }

    void Update()
    {
        // Проверяем можно ли нажимать
        bool canActivate = playerInRange &&
                          Input.GetKeyDown(activationKey) &&
                          !used;

        if (canActivate)
        {
            ActivateComputer();
        }
    }

    void ActivateComputer()
    {
        isActive = true;

        // Отмечаем как использованный, если нельзя повторно
        if (!canBeReused)
        {
            used = true;
        }

        // Воспроизводим звук
        if (activationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activationSound);
        }

        // Запускаем частицы
        if (activationParticles != null)
        {
            activationParticles.Play();
        }

        // Вызываем событие
        onComputerActivated?.Invoke();

        // Обновляем визуал
        UpdateVisual();

        Debug.Log($"🖥 Компьютер {gameObject.name} активирован. Повторное использование: {(canBeReused ? "ДА" : "НЕТ")}");
    }

    void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            if (used && !canBeReused)
            {
                // Серый - использован и нельзя повторно
                spriteRenderer.color = usedColor;
            }
            else if (isActive)
            {
                // Зеленый - активен (можно нажать если canBeReused = true)
                spriteRenderer.color = activeColor;
            }
            else
            {
                // Готов к использованию
                spriteRenderer.color = readyColor;
            }
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    // Методы для внешнего управления
    public void ResetComputer()
    {
        used = false;
        isActive = false;
        UpdateVisual();
        Debug.Log($"🖥 Компьютер {gameObject.name} сброшен");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = used ? usedColor : readyColor;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}