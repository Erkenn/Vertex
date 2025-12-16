using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("=== ИГРОВЫЕ НАСТРОЙКИ ===")]
    public int totalLives = 3;
    public float estimatedPlaytime = 1800f;
    public bool autoSaveEnabled = true;

    [Header("=== ТЕКУЩЕЕ СОСТОЯНИЕ ===")]
    public int currentLives;
    public int currentLevel = 1;
    public float sessionTimer = 0f;
    public bool isGameActive = true;
    public bool isPaused = false; // ДОБАВЛЕНО ПОЛЕ

    [Header("=== СТАТИСТИКА И РЕКОРДЫ ===")]
    public int dataPacketsCollected = 0;
    public int enemiesDestroyed = 0;
    public float bestCompletionTime = Mathf.Infinity;
    public int totalSessionsPlayed = 0;

    [Header("=== СИСТЕМА ДОСТИЖЕНИЙ ===")]
    public List<Achievement> achievements = new List<Achievement>();

    // События
    public System.Action OnGamePause;
    public System.Action OnGameResume;
    public System.Action OnGameOver;
    public System.Action OnLevelComplete;
    public System.Action OnDataPacketCollected;
    public System.Action OnEnemyDestroyed;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameSystems();
            Debug.Log("🎮 GameManager инициализирован");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeGameSystems()
    {
        currentLives = totalLives;
        sessionTimer = 0f;
        LoadAllPlayerData();
        InitializeHighScores();
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
        Time.timeScale = isPaused ? 0 : 1;

        if (isPaused)
        {
            OnGamePause?.Invoke();
            UIManager.Instance?.ShowPauseMenu(); // Показываем меню паузы
            AutoSaveProgress();
        }
        else
        {
            OnGameResume?.Invoke();
            UIManager.Instance?.HidePauseMenu(); // Скрываем меню паузы

            // Также скрываем настройки если они открыты
            SettingsManager.Instance?.CloseSettings();
        }

        Debug.Log(isPaused ? "⏸ Игра на паузе" : "▶ Игра продолжена");
    }

    // === СИСТЕМА УРОВНЕЙ ===
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex >= 1 && levelIndex <= 5)
        {
            currentLevel = levelIndex;
            SceneManager.LoadScene($"Level_{levelIndex}");

            if (autoSaveEnabled)
                AutoSaveProgress();
        }
    }

    public void CompleteLevel()
    {
        OnLevelComplete?.Invoke();

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
        currentLives--;

        if (currentLives <= 0)
        {
            GameOver("Потеряны все жизни");
        }
        else
        {
            StartCoroutine(QuickRestartLevel());
        }
    }

    IEnumerator QuickRestartLevel()
    {
        UIManager.Instance?.ShowDeathScreen();
        yield return new WaitForSeconds(1.5f);
        UIManager.Instance?.HideDeathScreen();
        LoadLevel(currentLevel);
    }

    void GameOver(string reason)
    {
        isGameActive = false;
        Debug.Log($"💀 Game Over: {reason}");
        SaveSessionStats();
        UIManager.Instance?.ShowGameOverMenu(reason);
        OnGameOver?.Invoke();
    }

    public void WinGame()
    {
        isGameActive = false;

        if (sessionTimer < bestCompletionTime)
        {
            bestCompletionTime = sessionTimer;
            PlayerPrefs.SetFloat("BestCompletionTime", bestCompletionTime);
        }

        SaveSessionStats();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowVictoryScreen(sessionTimer, dataPacketsCollected, enemiesDestroyed);

        Debug.Log("🎉 Победа! Ядро уничтожено!");
    }

    // === СИСТЕМА ДАННЫХ ===
    public void CollectDataPacket(int value = 1)
    {
        dataPacketsCollected += value;
        OnDataPacketCollected?.Invoke();
    }

    public void RegisterEnemyDestroyed()
    {
        enemiesDestroyed++;
        OnEnemyDestroyed?.Invoke();
    }

    // === СИСТЕМА СОХРАНЕНИЙ ===
    void AutoSaveProgress()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("CurrentLives", currentLives);
        PlayerPrefs.SetFloat("SessionTimer", sessionTimer);
        PlayerPrefs.SetInt("DataPackets", dataPacketsCollected);
        PlayerPrefs.Save();
    }

    void LoadAllPlayerData()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        currentLives = PlayerPrefs.GetInt("CurrentLives", totalLives);
        sessionTimer = PlayerPrefs.GetFloat("SessionTimer", 0f);
        dataPacketsCollected = PlayerPrefs.GetInt("DataPackets", 0);
        bestCompletionTime = PlayerPrefs.GetFloat("BestCompletionTime", Mathf.Infinity);
    }

    void SaveSessionStats()
    {
        int totalPlayTime = PlayerPrefs.GetInt("TotalPlayTime", 0) + (int)sessionTimer;
        int totalDataPackets = PlayerPrefs.GetInt("TotalDataPackets", 0) + dataPacketsCollected;
        int totalEnemies = PlayerPrefs.GetInt("TotalEnemies", 0) + enemiesDestroyed;

        PlayerPrefs.SetInt("TotalPlayTime", totalPlayTime);
        PlayerPrefs.SetInt("TotalDataPackets", totalDataPackets);
        PlayerPrefs.SetInt("TotalEnemies", totalEnemies);
        PlayerPrefs.Save();
    }

    // === СИСТЕМА ДОСТИЖЕНИЙ ===

    void InitializeHighScores()
    {
        if (!PlayerPrefs.HasKey("HighScoresInitialized"))
        {
            PlayerPrefs.SetString("HighScores", "[]");
            PlayerPrefs.SetInt("HighScoresInitialized", 1);
            PlayerPrefs.Save();
        }
    }

    // === УТИЛИТЫ ===
    public string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public void StartNewGame()
    {
        currentLevel = 1;
        currentLives = totalLives;
        sessionTimer = 0f;
        dataPacketsCollected = 0;
        enemiesDestroyed = 0;
        isGameActive = true;
        isPaused = false;

        LoadLevel(1);
    }
}

[System.Serializable]
public class Achievement
{
    public string id;
    public string name;
    public string description;
    public bool unlocked;
    public System.Func<bool> condition;

    public Achievement(string id, string name, string description, System.Func<bool> condition = null)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.unlocked = false;
        this.condition = condition;
    }

    public bool CheckCondition()
    {
        return condition?.Invoke() ?? false;
    }
}