using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateLevelUI(levelIndex + 1);
    }

    void ConfigureLevel(LevelConfig config)
    {
        ActivateEnemyTypes(config);
        SetupLevelEnvironment(config);
    }

    void ActivateEnemyTypes(LevelConfig config)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in enemies)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null)
            {
                if (enemy is Scanner && !config.hasScanners)
                {
                    enemyObj.SetActive(false);
                }
                else if (enemy is Guardian && !config.hasGuardians)
                {
                    enemyObj.SetActive(false);
                }
                else if (enemy is Turret && !config.hasTurrets)
                {
                    enemyObj.SetActive(false);
                }
                else
                {
                    enemyObj.SetActive(true);
                    enemy.isActive = true;
                }
            }
        }
    }

    void SetupLevelEnvironment(LevelConfig config)
    {
        SetupLighting(config);
        SetupBackground(config);
    }

    void SetupLighting(LevelConfig config)
    {
        // ИСПРАВЛЕННЫЙ КОД - замена устаревшего метода
        Light mainLight = FindFirstObjectByType<Light>(); // ЗАМЕНА FindObjectOfType

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

    void SetupBackground(LevelConfig config)
    {
        // Настройка фона
    }

    public void CompleteLevel()
    {
        if (isLevelCompleted) return;

        isLevelCompleted = true;
        LevelConfig config = levels[currentLevelIndex];

        Debug.Log($"🎉 Уровень '{config.levelName}' пройден!");

        float levelTime = Time.timeSinceLevelLoad;
        Debug.Log($"⏱️ Время прохождения: {levelTime:F1} сек");

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