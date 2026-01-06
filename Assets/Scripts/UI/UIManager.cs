using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("=== HUD ЭЛЕМЕНТЫ ===")]
    public Slider healthBar;
    public TextMeshProUGUI coinCountText;
    public TextMeshProUGUI hackCountText;
    public TextMeshProUGUI shieldCountText;

    [Header("=== ЭКРАН СМЕРТИ ===")]
    public GameObject deathScreen;
    public Button restartButtonInDeathScreen;
    public Button statsButtonInDeathScreen;
    public GameObject statsPanel;
    public TextMeshProUGUI statsText;

    [Header("=== МЕНЮ ПАУЗЫ ===")]
    public GameObject pauseMenuCanvas;
    public Button resumeButtonPause;
    public Button restartButtonPause;
    public Button mainMenuButtonPause;

    private bool uiInitialized = false;

    void Awake()
    {
        Debug.Log("UIManager Awake");

        // Простой синглтон для сцены
        if (Instance == null)
        {
            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("✅ UIManager создан");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Debug.Log("UIManager Start");

        // Инициализируем если в игровой сцене
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith("Level_") || sceneName == "Tutorial")
        {
            StartCoroutine(InitializeDelayed());
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"UIManager: загружена сцена {scene.name}");

        if (scene.name.StartsWith("Level_") || scene.name == "Tutorial")
        {
            uiInitialized = false;
            StartCoroutine(InitializeDelayed());
        }
    }

    IEnumerator InitializeDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        InitializeUI();
    }

    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ПОИСКА ЭЛЕМЕНТОВ
    GameObject FindChild(Transform parent, string childName)
    {
        if (parent == null) return null;

        // Прямой поиск
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child.gameObject;
        }

        // Глубокий поиск
        foreach (Transform child in parent)
        {
            GameObject found = FindChild(child, childName);
            if (found != null) return found;
        }

        return null;
    }

    T FindComponentInChildren<T>(Transform parent, string childName) where T : Component
    {
        GameObject obj = FindChild(parent, childName);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    // ФОРМАТИРОВАНИЕ ВРЕМЕНИ
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    void InitializeUI()
    {
        if (uiInitialized) return;

        Debug.Log("🔄 Инициализация UIManager...");

        // Находим HUD Canvas
        GameObject hudCanvas = GameObject.Find("HUD Canvas");
        if (hudCanvas == null)
        {
            Debug.LogError("❌ HUD Canvas не найден!");
            return;
        }

        Debug.Log($"✅ Найден HUD Canvas: {hudCanvas.name}");

        // === НАХОДИМ ВСЕ ЭЛЕМЕНТЫ ПРОСТЫМ СПОСОБОМ ===

        // HUD элементы
        healthBar = hudCanvas.transform.Find("HealthBar")?.GetComponent<Slider>();
        coinCountText = hudCanvas.transform.Find("CoinCount")?.GetComponent<TextMeshProUGUI>();
        hackCountText = hudCanvas.transform.Find("HackCount")?.GetComponent<TextMeshProUGUI>();
        shieldCountText = hudCanvas.transform.Find("ShieldCount")?.GetComponent<TextMeshProUGUI>();

        // DeathScreen
        deathScreen = hudCanvas.transform.Find("DeathScreen")?.gameObject;
        Debug.Log($"DeathScreen найден: {deathScreen != null}");

        if (deathScreen != null)
        {
            // Кнопки в DeathScreen
            restartButtonInDeathScreen = deathScreen.transform.Find("RestartButton")?.GetComponent<Button>();
            statsButtonInDeathScreen = deathScreen.transform.Find("StatsButton")?.GetComponent<Button>();

            // StatsPanel может быть внутри DeathScreen или рядом
            statsPanel = hudCanvas.transform.Find("StatsPanel")?.gameObject;
            if (statsPanel == null)
            {
                statsPanel = deathScreen.transform.Find("StatsPanel")?.gameObject;
            }

            Debug.Log($"StatsPanel найден: {statsPanel != null}");

            if (statsPanel != null)
            {
                // Ищем StatsText внутри StatsPanel
                statsText = statsPanel.transform.Find("StatsText")?.GetComponent<TextMeshProUGUI>();
                if (statsText == null)
                {
                    // Пробуем другие возможные имена
                    statsText = statsPanel.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                }

                Debug.Log($"StatsText найден: {statsText != null}");

                // Ищем кнопку "В главное меню" в StatsPanel - она называется "Continue"
                Button backToMenuButton = statsPanel.transform.Find("Continue")?.GetComponent<Button>();

                // Если не нашли "Continue", пробуем другие имена
                if (backToMenuButton == null)
                {
                    backToMenuButton = statsPanel.transform.Find("MainMenuButton")?.GetComponent<Button>();
                }
                if (backToMenuButton == null)
                {
                    backToMenuButton = statsPanel.transform.Find("BackButton")?.GetComponent<Button>();
                }
                if (backToMenuButton == null)
                {
                    backToMenuButton = statsPanel.transform.Find("MenuButton")?.GetComponent<Button>();
                }

                if (backToMenuButton != null)
                {
                    backToMenuButton.onClick.RemoveAllListeners();
                    backToMenuButton.onClick.AddListener(() => {
                        Debug.Log("🏠 Нажата кнопка Continue из StatsPanel");
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.ReturnToMainMenu();
                        }
                    });
                    Debug.Log($"✅ Кнопка '{backToMenuButton.name}' в StatsPanel настроена");
                }
                else
                {
                    Debug.LogWarning("⚠️ Кнопка Continue/MainMenu не найдена в StatsPanel!");

                    // Создаем кнопку вручную если не нашли
                    CreateContinueButtonInStatsPanel();
                }
            }
        }

        void CreateContinueButtonInStatsPanel()
        {
            if (statsPanel == null) return;

            Debug.Log("🛠️ Создаем кнопку Continue в StatsPanel...");

            // Создаем кнопку
            GameObject buttonObj = new GameObject("Continue");
            buttonObj.transform.SetParent(statsPanel.transform);

            // Добавляем компоненты
            Button button = buttonObj.AddComponent<Button>();
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.4f, 0.8f, 1f);

            // Текст кнопки
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "В ГЛАВНОЕ МЕНЮ";
            text.color = Color.white;
            text.fontSize = 20;
            text.alignment = TMPro.TextAlignmentOptions.Center;

            // Настраиваем RectTransform
            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -100);
            rt.sizeDelta = new Vector2(200, 50);

            // Настраиваем RectTransform текста
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = new Vector2(200, 50);

            // Настраиваем обработчик
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                Debug.Log("🏠 Нажата созданная кнопка Continue");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToMainMenu();
                }
            });

            Debug.Log("✅ Кнопка Continue создана");
        }

        // PauseMenuCanvas
        pauseMenuCanvas = GameObject.Find("PauseMenuCanvas");
        if (pauseMenuCanvas != null)
        {
            resumeButtonPause = pauseMenuCanvas.transform.Find("ResumeButton")?.GetComponent<Button>();
            restartButtonPause = pauseMenuCanvas.transform.Find("RestartButton")?.GetComponent<Button>();
            mainMenuButtonPause = pauseMenuCanvas.transform.Find("MainMenuButton")?.GetComponent<Button>();
        }

        // === НАСТРАИВАЕМ СОСТОЯНИЕ ===

        // Скрываем все меню
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
            Debug.Log("✅ DeathScreen скрыт");
        }

        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
            Debug.Log("✅ StatsPanel скрыт");
        }

        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
            Debug.Log("✅ PauseMenuCanvas скрыт");
        }

        // === НАСТРАИВАЕМ КНОПКИ ===
        SetupButtons();

        uiInitialized = true;
        Debug.Log("✅ UIManager инициализирован");
    }

    void SetupButtons()
    {
        Debug.Log("🔄 Настройка кнопок...");

        // === КНОПКИ ЭКРАНА СМЕРТИ ===
        if (restartButtonInDeathScreen != null)
        {
            restartButtonInDeathScreen.onClick.RemoveAllListeners();
            restartButtonInDeathScreen.onClick.AddListener(() => {
                Debug.Log("🔄 Нажата кнопка рестарта");
                HideDeathScreen();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestartCurrentLevel();
                }
            });
            Debug.Log("✅ Кнопка рестарта настроена");
        }

        if (statsButtonInDeathScreen != null)
        {
            statsButtonInDeathScreen.onClick.RemoveAllListeners();
            statsButtonInDeathScreen.onClick.AddListener(() => {
                Debug.Log("📊 Нажата кнопка статистики");
                OnShowStatsFromDeath();
            });
            Debug.Log("✅ Кнопка статистики настроена");
        }

        // === КНОПКИ МЕНЮ ПАУЗЫ ===
        if (resumeButtonPause != null)
        {
            resumeButtonPause.onClick.RemoveAllListeners();
            resumeButtonPause.onClick.AddListener(() => {
                Debug.Log("▶️ Нажата кнопка продолжить");
                // ВМЕСТО этого:
                // if (GameManager.Instance != null) GameManager.Instance.TogglePause();

                // ДЕЛАЕМ ЭТО:
                if (PauseManager.Instance != null)
                {
                    PauseManager.Instance.ResumeGame();
                }
                else
                {
                    // Резерв: просто снять паузу и скрыть канвас вручную
                    Time.timeScale = 1f;
                    if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
                    if (GameManager.Instance != null) GameManager.Instance.isPaused = false;
                }
            });
        }

        if (restartButtonPause != null)
        {
            restartButtonPause.onClick.RemoveAllListeners();
            restartButtonPause.onClick.AddListener(() => {
                Debug.Log("🔄 Нажата кнопка рестарта из паузы");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestartCurrentLevel();
                }
            });
        }

        if (mainMenuButtonPause != null)
        {
            mainMenuButtonPause.onClick.RemoveAllListeners();
            mainMenuButtonPause.onClick.AddListener(() => {
                Debug.Log("🏠 Нажата кнопка главного меню из паузы");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToMainMenu();
                }
            });
        }
    }

    public void OnShowStatsFromDeath()
    {
        Debug.Log("📊 Показываем панель статистики");

        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
            Debug.Log("✅ DeathScreen скрыт");
        }

        if (statsPanel != null)
        {
            statsPanel.SetActive(true);
            Debug.Log("✅ StatsPanel показан");

            // Обновляем текст статистики
            UpdateStatsText();
        }
        else
        {
            Debug.LogError("❌ StatsPanel не найден!");
        }
    }

    void UpdateStatsText()
    {
        if (statsText != null && GameManager.Instance != null)
        {
            string stats = $"<b>СТАТИСТИКА ИГРЫ</b>\n\n" +
                          $"Уровень: {GameManager.Instance.currentLevel}\n" +
                          $"Монеты: {GameManager.Instance.coinsCollected}\n" +
                          $"Данные: {GameManager.Instance.dataPacketsCollected}\n" +
                          $"Время: {FormatTime(GameManager.Instance.sessionTimer)}";

            statsText.text = stats;
            Debug.Log("✅ Статистика обновлена");
        }
    }

    public void ShowDeathScreen(float time, int coins, int dataPackets)
    {
        Debug.Log("💀 Показываем экран смерти");

        if (!uiInitialized)
        {
            InitializeUI();
        }

        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
            Time.timeScale = 0f;

            // Обновляем статистику в DeathScreen
            if (statsText != null)
            {
                string stats = $"<b>СТАТИСТИКА УРОВНЯ</b>\n\n" +
                              $"Время: {FormatTime(time)}\n" +
                              $"Монеты: {coins}\n" +
                              $"Данные: {dataPackets}";
                statsText.text = stats;
            }

            Debug.Log("✅ Экран смерти показан");
        }
        else
        {
            Debug.LogError("❌ DeathScreen не найден!");
        }
    }

    public void HideDeathScreen()
    {
        Debug.Log("❌ Скрываем экран смерти");

        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
            Debug.Log("✅ DeathScreen скрыт");
        }

        // Также скрываем StatsPanel если он открыт
        if (statsPanel != null && statsPanel.activeSelf)
        {
            statsPanel.SetActive(false);
            Debug.Log("✅ StatsPanel скрыт");
        }

        // Возобновляем время
        Time.timeScale = 1f;
    }

    public void ShowPauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);
        }
    }

    public void HidePauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
    }

    public void ShowVictoryScreen(float completionTime, int dataPackets, int enemiesDestroyed)
    {
        Debug.Log($"🎉 Победа!");
    }

    public void UpdateCoinsUI(int coins)
    {
        if (coinCountText != null)
        {
            coinCountText.text = coins.ToString();
        }
    }

    public void UpdateHealthUI(int health)
    {
        if (healthBar != null)
        {
            healthBar.value = health / 100f;
        }
    }

    public void UpdateAbilitiesUI(int hackCharges, int shieldCharges)
    {
        if (hackCountText != null)
        {
            hackCountText.text = hackCharges.ToString();
        }

        if (shieldCountText != null)
        {
            shieldCountText.text = shieldCharges.ToString();
        }
    }

    public void UpdateLevelUI(int level) { }
    public void SetLevelUI(int level) { }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}