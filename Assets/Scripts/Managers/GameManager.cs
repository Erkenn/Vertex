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
    public bool isPaused = false;

    [Header("=== СБОР ПРЕДМЕТОВ ===")]
    public int dataPacketsCollected = 0;
    public int coinsCollected = 0; // ← МОНЕТЫ ДОБАВЛЕНЫ
    public int enemiesDestroyed = 0;

    [Header("=== СТАТИСТИКА И РЕКОРДЫ ===")]
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
    public System.Action OnCoinCollected; // ← Опционально, для будущего

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

        // LoadAllPlayerData();

        currentLevel = 1;
        coinsCollected = 0;
        dataPacketsCollected = 0;

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
            UIManager.Instance?.ShowPauseMenu();
            AutoSaveProgress();
        }
        else
        {
            OnGameResume?.Invoke();
            UIManager.Instance?.HidePauseMenu();
            SettingsManager.Instance?.CloseSettings();
        }

        Debug.Log(isPaused ? "⏸ Игра на паузе" : "▶ Игра продолжена");
    }

    // === СИСТЕМА УРОВНЕЙ ===
    public void LoadLevel(int levelIndex)
    {
        Debug.Log($"🔍 LoadLevel вызван с levelIndex={levelIndex}, currentLevel до = {currentLevel}");

        // Сбрасываем данные уровня
        dataPacketsCollected = 0;
        coinsCollected = 0;
        enemiesDestroyed = 0;

        currentLevel = levelIndex; // Устанавливаем НОВЫЙ уровень

        Debug.Log($"🔄 Сброс данных: монеты = {coinsCollected}, currentLevel = {currentLevel}");

        SceneManager.LoadScene($"Level_{levelIndex}");
    }

    public void CompleteLevel()
    {
        Debug.Log($"✅ CompleteLevel вызван. Текущий уровень: {currentLevel}");

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

    // === СИСТЕМА МОНЕТ ===
    public void CollectCoin(int value = 1)
    {
        coinsCollected += value;
        Debug.Log($"💰 Монета подобрана! Всего: {coinsCollected}");
        OnCoinCollected?.Invoke();

        Debug.Log($"UIManager.Instance = {(UIManager.Instance != null ? "OK" : "NULL!")}");

        // Обновляем UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCoinsUI(coinsCollected);
        }

        if (autoSaveEnabled)
        {
            PlayerPrefs.SetInt("CoinsCollected", coinsCollected);
            PlayerPrefs.Save();
        }
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
        PlayerPrefs.SetInt("CoinsCollected", coinsCollected); // ← СОХРАНЯЕМ МОНЕТЫ
        PlayerPrefs.Save();
    }

    void LoadAllPlayerData()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        currentLives = PlayerPrefs.GetInt("CurrentLives", totalLives);
        sessionTimer = PlayerPrefs.GetFloat("SessionTimer", 0f);
        dataPacketsCollected = PlayerPrefs.GetInt("DataPackets", 0);
        coinsCollected = PlayerPrefs.GetInt("CoinsCollected", 0); // ← ЗАГРУЖАЕМ МОНЕТЫ
        bestCompletionTime = PlayerPrefs.GetFloat("BestCompletionTime", Mathf.Infinity);
    }

    void SaveSessionStats()
    {
        int totalPlayTime = PlayerPrefs.GetInt("TotalPlayTime", 0) + (int)sessionTimer;
        int totalDataPackets = PlayerPrefs.GetInt("TotalDataPackets", 0) + dataPacketsCollected;
        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0) + coinsCollected; // ← ОБЩИЕ МОНЕТЫ
        int totalEnemies = PlayerPrefs.GetInt("TotalEnemies", 0) + enemiesDestroyed;

        PlayerPrefs.SetInt("TotalPlayTime", totalPlayTime);
        PlayerPrefs.SetInt("TotalDataPackets", totalDataPackets);
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
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
        PlayerPrefs.DeleteKey("CurrentLevel");
        PlayerPrefs.DeleteKey("CoinsCollected");
        PlayerPrefs.DeleteKey("DataPackets");

        currentLevel = 1;
        currentLives = totalLives;
        coinsCollected = 0;
        dataPacketsCollected = 0;
        enemiesDestroyed = 0;
        sessionTimer = 0f;
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