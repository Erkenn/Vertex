using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Ищем существующий GameManager в сцене
                _instance = FindFirstObjectByType<GameManager>();

                // Если не нашли, создаем новый
                if (_instance == null)
                {
                    GameObject gm = new GameObject("GameManager");
                    _instance = gm.AddComponent<GameManager>();
                    DontDestroyOnLoad(gm);
                }
            }
            return _instance;
        }
    }

    [Header("=== ИГРОВЫЕ НАСТРОЙКИ ===")]
    public int totalLives = 1;
    public float estimatedPlaytime = 1800f;
    public bool autoSaveEnabled = true;

    [Header("=== ТЕКУЩЕЕ СОСТОЯНИЕ ===")]
    public int currentLives;
    public int currentLevel = 1;
    public float sessionTimer = 0f;
    public bool isGameActive = true;
    public bool isPaused = false;

    [Header("=== СБОР ПРЕДМЕТОВ ===")]
    public int dataPacketsCollected = 0;
    public int coinsCollected = 0;
    public int enemiesDestroyed = 0;

    [Header("=== СТАТИСТИКА И РЕКОРДЫ ===")]
    public float bestCompletionTime = Mathf.Infinity;
    public int totalSessionsPlayed = 0;

    private bool isInitialized = false;
    private Coroutine restartCoroutine;

    // === ВАЖНО: Ссылка на префаб UIManager ===
    public GameObject uiManagerPrefab; // Перетащите префаб сюда в инспекторе

    void Awake()
    {
        // Если уже есть Instance и это не мы
        if (_instance != null && _instance != this)
        {
            Debug.Log("⚠️ Уничтожен дубликат GameManager");
            Destroy(gameObject);
            return;
        }

        // Если Instance еще нет
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Инициализируем только один раз
            if (!isInitialized)
            {
                InitializeGameSystems();
                isInitialized = true;
            }

            // Подписываемся на события
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("✅ GameManager инициализирован как постоянный объект");
        }
    }

    void InitializeGameSystems()
    {
        currentLives = totalLives;
        sessionTimer = 0f;
        coinsCollected = 0;
        dataPacketsCollected = 0;
        enemiesDestroyed = 0;

        Debug.Log("🎮 Игровые системы инициализированы");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔄 GameManager: Загружена сцена '{scene.name}'");

        // При загрузке игровой сцены сбрасываем состояние
        if (scene.name.StartsWith("Level_") || scene.name == "Tutorial")
        {
            // Гарантируем что UIManager существует
            EnsureUIManagerExists();

            // Даем время на инициализацию
            StartCoroutine(ResetLevelAfterDelay());
        }
    }

    IEnumerator ResetLevelAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        ResetLevelState();
    }

    // === ВАЖНЫЙ МЕТОД: Гарантируем существование UIManager ===
    void EnsureUIManagerExists()
    {
        Debug.Log("🔍 Проверяем UIManager...");

        // Если UIManager уже существует
        if (UIManager.Instance != null)
        {
            Debug.Log("✅ UIManager уже существует");
            return;
        }

        Debug.Log("🔄 UIManager не найден, создаем...");

        // Способ 1: Используем префаб из инспектора
        if (uiManagerPrefab != null)
        {
            Instantiate(uiManagerPrefab);
            Debug.Log("✅ UIManager создан из префаба");
        }
        else
        {
            // Способ 2: Ищем в Resources
            GameObject prefab = Resources.Load<GameObject>("UIManager");
            if (prefab != null)
            {
                Instantiate(prefab);
                Debug.Log("✅ UIManager создан из Resources");
            }
            else
            {
                // Способ 3: Создаем вручную
                Debug.Log("⚠️ Создаем UIManager вручную");
                GameObject uiManagerObj = new GameObject("UIManager");
                uiManagerObj.AddComponent<UIManager>();
                DontDestroyOnLoad(uiManagerObj);
                Debug.Log("✅ UIManager создан вручную");
            }
        }

        // Ждем один кадр для инициализации
        StartCoroutine(WaitForUIManagerInitialization());
    }

    IEnumerator WaitForUIManagerInitialization()
    {
        yield return null; // Ждем один кадр

        if (UIManager.Instance != null)
        {
            Debug.Log("✅ UIManager успешно инициализирован");
        }
        else
        {
            Debug.LogError("❌ UIManager все еще не создан!");
        }
    }

    // Сброс состояния уровня при начале игры
    void ResetLevelState()
    {
        Debug.Log("🔄 Сброс состояния уровня...");

        // Останавливаем все корутины
        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
            restartCoroutine = null;
        }

        // Сбрасываем состояние
        isGameActive = true;
        isPaused = false;
        Time.timeScale = 1f;

        // Сбрасываем сбор предметов
        coinsCollected = 0;
        dataPacketsCollected = 0;
        enemiesDestroyed = 0;

        // Ждем один кадр чтобы все компоненты успели инициализироваться
        StartCoroutine(ResetPlayerDelayed());
    }

    private IEnumerator ResetPlayerDelayed()
    {
        // Ждем конец кадра
        yield return null;

        // Ищем и сбрасываем игрока
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.ResetPlayer();
        }

        // Обновляем UI (если он уже создан)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCoinsUI(coinsCollected);
            UIManager.Instance.UpdateHealthUI(100);
            Debug.Log("✅ UI обновлен при старте уровня");
        }
        else
        {
            Debug.LogWarning("⚠️ UIManager.Instance == null при сбросе уровня");
        }

        Debug.Log("✅ Состояние уровня сброшено");
    }

    void Update()
    {
        if (isGameActive && !isPaused)
        {
            UpdateSessionTimer();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    void UpdateSessionTimer()
    {
        sessionTimer += Time.deltaTime;
    }

    // === СИСТЕМА ПАУЗЫ ===
    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            OnGamePause?.Invoke();

            // Безопасный вызов ShowPauseMenu
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPauseMenu();
            }
            else
            {
                Debug.LogWarning("UIManager.Instance is null при паузе");
            }
        }
        else
        {
            Time.timeScale = 1f;
            OnGameResume?.Invoke();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.HidePauseMenu();
            }
        }

        Debug.Log(isPaused ? "⏸ Игра на паузе" : "▶ Игра продолжена");
    }

    // === СИСТЕМА УРОВНЕЙ ===
    public void LoadLevel(int levelIndex)
    {
        Debug.Log($"🔄 Загрузка уровня {levelIndex}");

        // Останавливаем все активные корутины
        StopAllCoroutines();

        // Сбрасываем состояние
        currentLevel = levelIndex;
        coinsCollected = 0;
        dataPacketsCollected = 0;
        enemiesDestroyed = 0;
        sessionTimer = 0f;
        isGameActive = true;
        isPaused = false;
        Time.timeScale = 1f;

        // Загружаем сцену
        SceneManager.LoadScene($"Level_{levelIndex}");
    }

    public void CompleteLevel()
    {
        Debug.Log($"✅ Уровень {currentLevel} завершен!");

        if (currentLevel < 5)
        {
            LoadLevel(currentLevel + 1);
        }
        else
        {
            WinGame();
        }
    }

    // === СИСТЕМА ЖИЗНЕЙ ===
    public void PlayerDied()
    {
        if (!isGameActive) return;

        Debug.Log("💀 GameManager: Игрок умер");
        isGameActive = false;

        Time.timeScale = 0f;

        StopAllEnemiesImmediately();

        ShowDeathScreenImmediate();
    }

    private void ShowDeathScreenImmediate()
    {
        Debug.Log($"📊 Статистика: время={sessionTimer}, монеты={coinsCollected}");

        // ГАРАНТИРУЕМ что UIManager существует
        EnsureUIManagerExists();

        if (UIManager.Instance != null)
        {
            Debug.Log("✅ UIManager.Instance найден, вызываем ShowDeathScreen");
            UIManager.Instance.ShowDeathScreen(sessionTimer, coinsCollected, dataPacketsCollected);
        }
        else
        {
            Debug.LogError("❌ UIManager.Instance все еще null!");

            // Создаем простой экран смерти
            Invoke("CreateSimpleDeathScreen", 0.5f);
        }
    }

    void CreateSimpleDeathScreen()
    {
        // Создаем простой Canvas
        GameObject canvas = new GameObject("SimpleDeathCanvas");
        Canvas canvasComp = canvas.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();

        // Панель
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvas.transform);
        UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0, 0, 0, 0.9f);

        // Текст
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panel.transform);
        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = $"ВЫ УМЕРЛИ\n\nНажмите любую кнопку";
        text.color = Color.red;
        text.fontSize = 32;
        text.alignment = TMPro.TextAlignmentOptions.Center;

        // Ждем нажатия
        StartCoroutine(WaitForRestart(canvas));
    }

    IEnumerator WaitForRestart(GameObject canvas)
    {
        yield return new WaitForSecondsRealtime(0.5f);

        while (true)
        {
            if (UnityEngine.Input.anyKeyDown)
            {
                Destroy(canvas);
                RestartCurrentLevel();
                yield break;
            }
            yield return null;
        }
    }

    private void StopAllEnemiesImmediately()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.StopEnemy();
            }
        }
        Debug.Log("❌ Все враги остановлены");
    }

    public void RestartCurrentLevel()
    {
        Debug.Log("🔄 Перезапуск текущего уровня...");

        // Останавливаем все корутины
        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
        }

        restartCoroutine = StartCoroutine(RestartLevelCoroutine());
    }

    private IEnumerator RestartLevelCoroutine()
    {
        // Небольшая задержка перед перезагрузкой
        yield return new WaitForSecondsRealtime(0.1f);

        Time.timeScale = 1f;

        // Сбрасываем сбор предметов
        coinsCollected = 0;
        dataPacketsCollected = 0;
        enemiesDestroyed = 0;

        // Загружаем сцену заново
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void WinGame()
    {
        Debug.Log("🎉 Победа! Все уровни пройдены!");
        isGameActive = false;

        if (sessionTimer < bestCompletionTime)
        {
            bestCompletionTime = sessionTimer;
            PlayerPrefs.SetFloat("BestCompletionTime", bestCompletionTime);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictoryScreen(sessionTimer, dataPacketsCollected, enemiesDestroyed);
        }
    }

    // === СИСТЕМА ДАННЫХ ===
    public void CollectDataPacket(int value = 1)
    {
        dataPacketsCollected += value;
        OnDataPacketCollected?.Invoke();
    }

    // === СИСТЕМА МОНЕТ ===
    public void CollectCoin(int value = 1)
    {
        coinsCollected += value;
        OnCoinCollected?.Invoke();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCoinsUI(coinsCollected);
        }
    }

    public void RegisterEnemyDestroyed()
    {
        enemiesDestroyed++;
        OnEnemyDestroyed?.Invoke();
    }

    // === ВОЗВРАТ В ГЛАВНОЕ МЕНЮ ===
    public void ReturnToMainMenu()
    {
        Debug.Log("🏠 Возврат в главное меню");

        // Останавливаем все корутины
        StopAllCoroutines();

        // Сбрасываем состояние
        isGameActive = false;
        isPaused = false;
        Time.timeScale = 1f;

        // Загружаем главное меню
        SceneManager.LoadScene("MainMenu");
    }

    public void CompleteTutorial()
    {
        Debug.Log("✅ Туториал завершен");

        Time.timeScale = 1f;
        PlayerPrefs.SetInt("HasCompletedTutorial", 1);
        PlayerPrefs.Save();

        // Начинаем с первого уровня
        LoadLevel(1);
    }

    // === СТАРТ НОВОЙ ИГРЫ ===
    public void StartNewGame()
    {
        Debug.Log("🚀 Начало новой игры");

        // Сбрасываем весь прогресс
        PlayerPrefs.DeleteKey("CurrentLevel");
        PlayerPrefs.DeleteKey("CoinsCollected");
        PlayerPrefs.DeleteKey("DataPackets");

        // Сбрасываем переменные
        currentLevel = 1;
        currentLives = totalLives;
        coinsCollected = 0;
        dataPacketsCollected = 0;
        enemiesDestroyed = 0;
        sessionTimer = 0f;
        isGameActive = true;
        isPaused = false;
        Time.timeScale = 1f;

        // Загружаем первый уровень
        LoadLevel(1);
    }

    // === УТИЛИТЫ ===
    public string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    // === ОЧИСТКА ПРИ УНИЧТОЖЕНИИ ===
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // === СОБЫТИЯ ===
    public System.Action OnGamePause;
    public System.Action OnGameResume;
    public System.Action OnGameOver;
    public System.Action OnLevelComplete;
    public System.Action OnDataPacketCollected;
    public System.Action OnEnemyDestroyed;
    public System.Action OnCoinCollected;
}