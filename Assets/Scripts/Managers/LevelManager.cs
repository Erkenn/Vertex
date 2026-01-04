using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [System.Serializable]
    public class LevelConfig
    {
        public string levelName;
        public int enemyCount;
        public int dataPackets;
        public bool hasTurrets;
        public bool hasGuardians;
        public bool hasScanners;
        public float levelTimeEstimate;
    }

    [Header("=== КОНФИГУРАЦИЯ УРОВНЕЙ ===")]
    public LevelConfig[] levels = new LevelConfig[5];

    private int currentLevelIndex = 0;
    private bool isLevelCompleted = false;
    private float levelStartTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("🗺️ LevelManager инициализирован");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (levels.Length == 0)
        {
            InitializeDefaultLevels();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void InitializeDefaultLevels()
    {
        levels = new LevelConfig[5];

        // Уровень 1: Внешний Периметр
        levels[0] = new LevelConfig
        {
            levelName = "Внешний Периметр",
            enemyCount = 3,
            dataPackets = 5,
            hasScanners = true,
            hasGuardians = false,
            hasTurrets = false,
            levelTimeEstimate = 300f
        };

        // Уровень 2: Архив
        levels[1] = new LevelConfig
        {
            levelName = "Архив",
            enemyCount = 6,
            dataPackets = 8,
            hasScanners = true,
            hasGuardians = true,
            hasTurrets = false,
            levelTimeEstimate = 360f // 6 минут
        };

        // Уровень 3: Серверная
        levels[2] = new LevelConfig
        {
            levelName = "Серверная",
            enemyCount = 8,
            dataPackets = 10,
            hasScanners = true,
            hasGuardians = true,
            hasTurrets = true,
            levelTimeEstimate = 420f // 7 минут
        };

        // Уровень 4: Путь к Ядру
        levels[3] = new LevelConfig
        {
            levelName = "Путь к Ядру",
            enemyCount = 10,
            dataPackets = 12,
            hasScanners = true,
            hasGuardians = true,
            hasTurrets = true,
            levelTimeEstimate = 480f // 8 минут
        };

        // Уровень 5: Финальный босс
        levels[4] = new LevelConfig
        {
            levelName = "Центральное Ядро",
            enemyCount = 15,
            dataPackets = 15,
            hasScanners = true,
            hasGuardians = true,
            hasTurrets = true,
            levelTimeEstimate = 600f // 10 минут
        };
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Level_"))
        {
            levelStartTime = Time.time;

            string levelNumberStr = scene.name.Replace("Level_", "");
            if (int.TryParse(levelNumberStr, out int levelNum))
            {
                currentLevelIndex = levelNum - 1; // Преобразуем в 0-based индекс
            }

            StartLevel(currentLevelIndex);
        }
    }

    public void StartLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogError($"Неверный индекс уровня: {levelIndex}");
            return;
        }

        currentLevelIndex = levelIndex;
        isLevelCompleted = false;

        LevelConfig config = levels[levelIndex];

        Debug.Log($"🚀 Запуск уровня: {config.levelName}");

        ConfigureLevel(config);

        // ОБНОВЛЯЕМ UI С ЗАЩИТОЙ
        StartCoroutine(UpdateLevelUIDelayed(levelIndex + 1));
    }

    private IEnumerator UpdateLevelUIDelayed(int levelNumber)
    {
        // Ждем пока UIManager инициализируется
        yield return null;
        yield return null; // Два кадра для надежности

        if (UIManager.Instance != null)
        {
            // Пробуем оба метода
            if (UIManager.Instance.HasMethod("UpdateLevelUI"))
            {
                UIManager.Instance.UpdateLevelUI(levelNumber);
            }
            else if (UIManager.Instance.HasMethod("SetLevelUI"))
            {
                UIManager.Instance.SetLevelUI(levelNumber);
            }
            else
            {
                // Прямое обновление через поиск текстового поля
                UpdateLevelTextDirectly(levelNumber);
            }
        }
        else
        {
            Debug.LogWarning("UIManager.Instance is null, не могу обновить UI уровня");
        }
    }

    void ConfigureLevel(LevelConfig config)
    {
        ActivateEnemyTypes(config);
        SetupLevelEnvironment(config);

        // Настройка спавнера
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.ConfigureSpawner(config);
        }
    }

    private void UpdateLevelTextDirectly(int levelNumber)
    {
        // Ищем TextMeshProUGUI с уровнем напрямую
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text.name.Contains("Level") || text.text.Contains("УРОВЕНЬ"))
            {
                text.text = $"УРОВЕНЬ: {levelNumber}/5";
                break;
            }
        }
    }

    void ActivateEnemyTypes(LevelConfig config)
    {
        // Если нет спавнера, активируем врагов вручную
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in enemies)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null)
            {
                bool shouldBeActive = true;

                if (enemy is Scanner && !config.hasScanners)
                {
                    shouldBeActive = false;
                }
                // else if (enemy.GetType().Name.Contains("Guardian") && !config.hasGuardians)
                // {
                //     shouldBeActive = false;
                // }
                // else if (enemy.GetType().Name.Contains("Turret") && !config.hasTurrets)
                // {
                //     shouldBeActive = false;
                // }

                enemyObj.SetActive(shouldBeActive);
                if (enemy != null)
                {
                    enemy.isActive = shouldBeActive;
                }
            }
        }
    }

    void SetupLevelEnvironment(LevelConfig config)
    {
        SetupLighting(config);
        // SetupBackground(config); // Закомментировано, так как метод пустой
    }

    void SetupLighting(LevelConfig config)
    {
        Light mainLight = FindAnyObjectByType<Light>(); // Современный метод поиска

        if (mainLight != null)
        {
            float intensity = 1f - (currentLevelIndex * 0.15f);
            mainLight.intensity = Mathf.Clamp(intensity, 0.4f, 1f);

            if (currentLevelIndex >= 2)
            {
                mainLight.color = Color.Lerp(Color.white, new Color(0.6f, 0.8f, 1f, 1f), 0.3f);
            }
        }
    }

    public void CompleteLevel()
    {
        if (isLevelCompleted) return;

        isLevelCompleted = true;
        LevelConfig config = levels[currentLevelIndex];
        float levelTime = Time.time - levelStartTime;

        Debug.Log($"🎉 Уровень '{config.levelName}' пройден!");
        Debug.Log($"⏱️ Время прохождения: {levelTime:F1} сек");

        // Останавливаем спавнер
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.StopSpawning();
        }

        if (currentLevelIndex < levels.Length - 1)
        {
            StartCoroutine(LoadNextLevelWithDelay(3f));
        }
        else
        {
            if (GameManager.Instance != null)
                GameManager.Instance.WinGame();
        }
    }

    IEnumerator LoadNextLevelWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        LoadLevel(currentLevelIndex + 1);
    }

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Length)
        {
            currentLevelIndex = levelIndex;
            SceneManager.LoadScene($"Level_{levelIndex + 1}");
        }
    }

    public LevelConfig GetCurrentLevelConfig()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Length)
        {
            return levels[currentLevelIndex];
        }
        return null;
    }

    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }

    public string GetCurrentLevelName()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Length)
        {
            return levels[currentLevelIndex].levelName;
        }
        return "Unknown Level";
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}