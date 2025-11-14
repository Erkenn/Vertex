using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("=== UI ПАНЕЛИ ===")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject loadGamePanel;

    [Header("=== UI ЭЛЕМЕНТЫ ===")]
    public TextMeshProUGUI versionText;
    public TextMeshProUGUI bestTimeText;
    public TextMeshProUGUI dataPacketsText;

    [Header("=== АНИМАЦИИ ===")]
    public Animator cameraAnimator;
    public Animator uiAnimator;

    [Header("=== НЕОНОВЫЕ ЭФФЕКТЫ ===")]
    public ParticleSystem backgroundParticles;
    public Light menuLight;

    private string currentPanel = "Main";

    void Start()
    {
        InitializeMainMenu();
        PlayMenuMusic();
        SetupVisualEffects();
    }

    void InitializeMainMenu()
    {
        // Показать главное меню, скрыть остальное
        ShowPanel("Main");

        // Обновление статистики
        UpdateStatistics();

        // Версия игры
        if (versionText != null)
        {
            versionText.text = $"Версия {Application.version}";
        }

        Debug.Log("🏠 Главное меню инициализировано");
    }

    void PlayMenuMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMainMenuMusic();
        }
    }

    void SetupVisualEffects()
    {
        // Настройка неоновых эффектов
        if (backgroundParticles != null)
        {
            backgroundParticles.Play();
        }

        if (menuLight != null)
        {
            menuLight.color = new Color(0.2f, 0.8f, 1f, 1f);
        }
    }

    void UpdateStatistics()
    {
        if (bestTimeText != null)
        {
            float bestTime = PlayerPrefs.GetFloat("BestCompletionTime", 0f);
            if (bestTime > 0)
            {
                bestTimeText.text = $"Лучшее время: {FormatTime(bestTime)}";
            }
            else
            {
                bestTimeText.text = "Лучшее время: --:--";
            }
        }

        if (dataPacketsText != null)
        {
            int totalPackets = PlayerPrefs.GetInt("TotalDataPackets", 0);
            dataPacketsText.text = $"Всего данных: {totalPackets}";
        }
    }

    // === НАВИГАЦИЯ ПО МЕНЮ ===

    public void ShowPanel(string panelName)
    {
        // Скрыть все панели
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (loadGamePanel != null) loadGamePanel.SetActive(false);

        // Показать выбранную панель
        switch (panelName)
        {
            case "Main":
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;
            case "Settings":
                if (settingsPanel != null) settingsPanel.SetActive(true);
                break;
            case "Credits":
                if (creditsPanel != null) creditsPanel.SetActive(true);
                break;
            case "Load":
                if (loadGamePanel != null) loadGamePanel.SetActive(true);
                break;
        }

        currentPanel = panelName;
        PlayNavigationSound();
    }

    // === ОСНОВНЫЕ КНОПКИ ===

    public void StartNewGame()
    {
        PlayUIClick();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        else
        {
            // Fallback - прямая загрузка сцены
            SceneManager.LoadScene("Level_1");
        }

        Debug.Log("🚀 Начата новая игра");
    }

    public void ContinueGame()
    {
        PlayUIClick();

        if (GameManager.Instance != null)
        {
            int savedLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            GameManager.Instance.LoadLevel(savedLevel);
        }

        Debug.Log("↩️ Продолжена сохраненная игра");
    }

    public void OpenSettings()
    {
        ShowPanel("Settings");
        PlayUIClick();

        // Показываем панель настроек через SettingsManager
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ShowSettings();
        }
    }

    public void OpenCredits()
    {
        ShowPanel("Credits");
        PlayUIClick();
    }

    public void QuitGame()
    {
        PlayUIClick();

        Debug.Log("👋 Выход из игры");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // === НАСТРОЙКИ ===

    public void CloseSettings()
    {
        ShowPanel("Main");
        PlayUIClick();

        // Сохранение настроек - ИСПРАВЛЕННАЯ СТРОКА
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.CloseSettings(); // Используем CloseSettings вместо SaveSettings
        }
    }

    public void ResetStatistics()
    {
        PlayUIClick();

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        UpdateStatistics();

        Debug.Log("📊 Статистика сброшена");
    }

    // === СИСТЕМА ЗВУКОВ ===

    void PlayNavigationSound()
    {
        AudioManager.Instance?.PlayUIHover();
    }

    void PlayUIClick()
    {
        AudioManager.Instance?.PlayUIClick();
    }

    // === УТИЛИТЫ ===

    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    // === ОБРАБОТКА ВВОДА ===

    void Update()
    {
        // Клавиша Escape для возврата в главное меню
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentPanel != "Main")
            {
                ShowPanel("Main");

                // Также закрываем панель настроек через SettingsManager
                if (SettingsManager.Instance != null && currentPanel == "Settings")
                {
                    SettingsManager.Instance.CloseSettings();
                }
            }
        }
    }

    public void OnPointerEnterUI()
    {
        PlayNavigationSound();
    }
}