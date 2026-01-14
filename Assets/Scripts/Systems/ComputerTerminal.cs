using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
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

    [Header("Подсказка для игрока")]
    public GameObject hintPanel; // Панель с текстом (Canvas -> Panel)
    public Text hintText; // Текстовый компонент
    public string hintMessage = "Нажмите E"; // Текст подсказки
    public string usedMessage = "Уже использовано"; // Текст когда использован
    public float hintOffsetY = 1.0f; // Смещение подсказки над объектом

    [Header("Эффекты")]
    public ParticleSystem activationParticles;
    public AudioClip activationSound;

    [Header("События")]
    public UnityEvent onComputerActivated;

    private SpriteRenderer spriteRenderer;
    private bool playerInRange = false;
    private AudioSource audioSource;
    private GameObject hintInstance; // Экземпляр подсказки

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        UpdateVisual();

        // Если панель подсказки не назначена, создаем простую по умолчанию
        if (hintPanel == null)
        {
            CreateDefaultHint();
        }
    }

    void Update()
    {
        // Обновляем позицию подсказки если она активна
        if (hintInstance != null && hintInstance.activeSelf)
        {
            UpdateHintPosition();
        }

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

        // Обновляем визуал и подсказку
        UpdateVisual();
        UpdateHintText();

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

    void UpdateHintText()
    {
        if (hintText != null)
        {
            hintText.text = used && !canBeReused ? usedMessage : hintMessage;
        }
    }

    void UpdateHintPosition()
    {
        if (hintInstance != null)
        {
            // Позиция над компьютером с учетом смещения
            Vector3 hintPosition = transform.position + Vector3.up * hintOffsetY;
            hintInstance.transform.position = Camera.main.WorldToScreenPoint(hintPosition);
        }
    }

    void ShowHint()
    {
        if (hintPanel == null) return;

        // Если подсказка еще не создана
        if (hintInstance == null)
        {
            // Создаем подсказку в Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Не найден Canvas в сцене! Создайте UI Canvas.");
                return;
            }

            hintInstance = Instantiate(hintPanel, canvas.transform);
            hintText = hintInstance.GetComponentInChildren<Text>();
        }

        // Обновляем текст и показываем
        UpdateHintText();
        hintInstance.SetActive(true);
        UpdateHintPosition();
    }

    void HideHint()
    {
        if (hintInstance != null)
        {
            hintInstance.SetActive(false);
        }
    }

    void CreateDefaultHint()
    {
        // Автоматически создаем простую подсказку если не настроена вручную
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("InteractionCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        hintPanel = new GameObject("HintPanel");
        hintPanel.transform.SetParent(canvas.transform);

        // Добавляем компоненты
        RectTransform rect = hintPanel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);

        Image image = hintPanel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.7f);

        // Создаем текст
        GameObject textObj = new GameObject("HintText");
        textObj.transform.SetParent(hintPanel.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        hintText = textObj.AddComponent<Text>();
        hintText.text = hintMessage;
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 20;
        hintText.color = Color.white;
        hintText.alignment = TextAnchor.MiddleCenter;

        hintPanel.SetActive(false);
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
        if (other.CompareTag("Player") && !used)
        {
            playerInRange = true;
            ShowHint();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            HideHint();
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

    void OnDestroy()
    {
        // Уничтожаем подсказку при удалении объекта
        if (hintInstance != null)
        {
            Destroy(hintInstance);
        }
    }
}