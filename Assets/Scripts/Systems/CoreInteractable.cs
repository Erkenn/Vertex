using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class CoreInteractable : MonoBehaviour
{
    [Header("Настройки")]
    public VideoClip coreHackCutscene; // Видео взлома ядра
    public float interactionDistance = 3f;
    public string interactionPrompt = "Нажмите E, чтобы взломать ядро";

    private bool isPlayerInRange;
    private bool isHacking;
    private PlayerController player;
    private TextMeshProUGUI promptText;
    private Coroutine interactionRoutine;

    void Start()
    {
        // Ядро изначально неактивно — будет активировано после победы над боссом
        gameObject.SetActive(false);
        CreateInteractionPrompt();
    }

    void Update()
    {
        if (isHacking || !isPlayerInRange || player == null) return;

        // Показываем подсказку
        if (Vector3.Distance(player.transform.position, transform.position) <= interactionDistance)
        {
            promptText.gameObject.SetActive(true);
            UpdatePromptPosition();
        }
        else
        {
            promptText.gameObject.SetActive(false);
        }

        // Обработка взаимодействия
        if (Input.GetKeyDown(KeyCode.E) && Vector3.Distance(player.transform.position, transform.position) <= interactionDistance)
        {
            StartCoreHack();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Core] OnTrigger: {other.name}, tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerController>();
            isPlayerInRange = true;
            Debug.Log("✅ Игрок вошёл в зону ядра");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            promptText?.gameObject.SetActive(false);
            Debug.Log("🚪 Игрок покинул зону ядра");
        }
    }

    public void ActivateCore()
    {
        gameObject.SetActive(true);
        Debug.Log("✅ Ядро активировано после победы над боссом!");
    }

    void StartCoreHack()
    {
        if (isHacking) return;
        isHacking = true;

        // Блокируем управление
        player.SetControlsEnabled(false);
        Time.timeScale = 0f;

        // Запускаем кат-сцену
        StartCoroutine(HackCoreSequence());
    }

    IEnumerator HackCoreSequence()
    {
        Debug.Log("🎬 Попытка запуска кат-сцены...");

        if (coreHackCutscene == null)
        {
            Debug.LogError("❌ Видео взлома ядра не назначено в инспекторе!");
            yield break;
        }

        if (CutsceneManager.Instance == null)
        {
            Debug.LogError("❌ CutsceneManager.Instance == null! Убедитесь, что на сцене есть объект с компонентом CutsceneManager.");
            yield break;
        }

        if (CutsceneManager.Instance.videoPlayer == null)
        {
            Debug.LogError("❌ CutsceneManager: videoPlayer не назначен в инспекторе!");
            yield break;
        }

        // Запускаем кат-сцену
        yield return StartCoroutine(CutsceneManager.Instance.PlayCutscene(coreHackCutscene, OnCutsceneComplete));
    }

    void OnCutsceneComplete()
    {
        Debug.Log("✅ Кат-сцена завершена. Переходим к статистике");

        // Разблокируем управление
        player.SetControlsEnabled(true);
        Time.timeScale = 1f;

        // Завершаем уровень
        GameManager.Instance.CompleteFinalCore();
    }

    void CreateInteractionPrompt()
    {
        GameObject canvasObj = new GameObject("CoreInteractionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject promptObj = new GameObject("HackPrompt");
        promptObj.transform.SetParent(canvasObj.transform);

        promptText = promptObj.AddComponent<TextMeshProUGUI>();
        promptText.fontSize = 24;
        promptText.color = Color.green;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.text = interactionPrompt;
        promptText.gameObject.SetActive(false);

        RectTransform rect = promptObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400, 50);
    }

    void UpdatePromptPosition()
    {
        if (promptText == null || player == null) return;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        promptText.transform.position = screenPos;
    }
}