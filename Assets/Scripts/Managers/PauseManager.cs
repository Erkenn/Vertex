using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("Настройки паузы")]
    public bool isPaused = false;

    [Header("UI элементы (ПЕРЕТАЩИ ВРУЧНУЮ из сцены!)")]
    public GameObject pauseMenuCanvas;    // Весь Canvas паузы
    public GameObject pausePanel;         // Панель с кнопками
    public Button pauseButton;           // Кнопка ⏸ в игре

    [Header("Кнопки в панели паузы")]
    public Button resumeButton;          // Продолжить
    public Button restartButton;         // Начать заново
    public Button menuButton;            // В меню
    public Button settingsButton;        // Настройки

    [Header("Панель настроек")]
    public GameObject settingsPanel;     // Панель настроек
    public Button settingsBackButton;    // Кнопка Назад в настройках
    public Button settingsApplyButton;   // Кнопка Применить

    [Header("Слайдеры громкости")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("✅ PauseManager создан");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Настраиваем все кнопки
        SetupAllButtons();

        // Настраиваем слайдеры громкости
        SetupVolumeSliders();

        // Скрываем все панели
        HideAllPanels();

        // Гарантируем что игра не на паузе
        Time.timeScale = 1f;
        isPaused = false;

        Debug.Log("🎮 PauseManager готов");
    }

    // === НАСТРОЙКА КНОПОК ===
    void SetupAllButtons()
    {
        Debug.Log("🔧 Настраиваю кнопки...");

        // 1. Кнопка паузы в игре
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(TogglePause);
            Debug.Log("✅ Кнопка паузы настроена");
        }
        else
        {
            Debug.LogError("❌ Кнопка паузы не назначена! Перетащи ее в инспектор");
        }

        // 2. Кнопки в панели паузы
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
            Debug.Log("✅ Кнопка Продолжить настроена");
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
            Debug.Log("✅ Кнопка Начать заново настроена");
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(GoToMainMenu);
            Debug.Log("✅ Кнопка Меню настроена");
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(ShowSettings);
            Debug.Log("✅ Кнопка Настройки настроена");
        }

        // 3. Кнопки в настройках
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveAllListeners();
            settingsBackButton.onClick.AddListener(HideSettings);
            Debug.Log("✅ Кнопка Назад в настройках настроена");
        }

        if (settingsApplyButton != null)
        {
            settingsApplyButton.onClick.RemoveAllListeners();
            settingsApplyButton.onClick.AddListener(ApplySettings);
            Debug.Log("✅ Кнопка Применить настроена");
        }
    }

    // === НАСТРОЙКА СЛАЙДЕРОВ ГРОМКОСТИ ===
    void SetupVolumeSliders()
    {
        Debug.Log("🎵 Настраиваю регуляторы звука...");

        // Загружаем сохраненные настройки
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.9f);

        // Устанавливаем значения слайдеров
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = masterVol;
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            Debug.Log("✅ MasterVolumeSlider настроен");
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = musicVol;
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            Debug.Log("✅ MusicVolumeSlider настроен");
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfxVol;
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            Debug.Log("✅ SFXVolumeSlider настроен");
        }

        // Применяем текущие настройки
        ApplyCurrentVolumeSettings();
    }

    // === ОБРАБОТЧИКИ ИЗМЕНЕНИЙ СЛАЙДЕРОВ ===
    void OnMasterVolumeChanged(float volume)
    {
        Debug.Log($"🔊 Изменена общая громкость: {volume}");

        // НЕМЕДЛЕННО применяем изменение
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }

        // Тестовый звук
        PlayTestSound();
    }

    void OnMusicVolumeChanged(float volume)
    {
        Debug.Log($"🎵 Изменена громкость музыки: {volume}");

        // НЕМЕДЛЕННО применяем изменение
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(volume);
        }
    }

    void OnSFXVolumeChanged(float volume)
    {
        Debug.Log($"🔊 Изменена громкость SFX: {volume}");

        // НЕМЕДЛЕННО применяем изменение
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(volume);
        }

        // Тестовый звук
        PlayTestSound();
    }

    void PlayTestSound()
    {
        // Проигрываем тестовый звук при изменении громкости
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }
    }

    void ApplyCurrentVolumeSettings()
    {
        Debug.Log("🔊 Применяю текущие настройки звука...");

        // Применяем текущие значения слайдеров
        if (masterVolumeSlider != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(masterVolumeSlider.value);
        }

        if (musicVolumeSlider != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(musicVolumeSlider.value);
        }

        if (sfxVolumeSlider != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(sfxVolumeSlider.value);
        }
    }

    // === ОСНОВНЫЕ МЕТОДЫ ПАУЗЫ ===

    public void TogglePause()
    {
        Debug.Log($"🔄 TogglePause: текущее состояние = {isPaused}");

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void ResumeGame()
    {
        Debug.Log("▶ Снимаю паузу");

        isPaused = false;
        Time.timeScale = 1f;

        // Скрываем ВЕСЬ канвас паузы (включая все дочерние элементы)
        if (pauseMenuCanvas != null)
        {
            Debug.Log($"✅ Скрываю PauseMenuCanvas (дочерний pausePanel тоже скроется)");
            pauseMenuCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError("❌ pauseMenuCanvas is null!");
        }

        // Включаем кнопку паузы в HUD
        if (pauseButton != null)
            pauseButton.interactable = true;

        // Синхронизируем с GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = false;
        }

        Debug.Log("✅ Игра продолжена");
    }

    void PauseGame()
    {
        Debug.Log("⏸ Ставлю игру на паузу");

        // Проверяем ссылки
        if (pauseMenuCanvas == null)
        {
            Debug.LogError("❌ pauseMenuCanvas не назначен!");

            // Пробуем найти в сцене
            pauseMenuCanvas = GameObject.Find("PauseMenuCanvas");
            if (pauseMenuCanvas == null)
            {
                Debug.LogError("❌ Не могу найти PauseMenuCanvas в сцене!");
                return;
            }
        }

        isPaused = true;
        Time.timeScale = 0f;

        // Показываем ВЕСЬ канвас паузы
        pauseMenuCanvas.SetActive(true);
        Debug.Log($"✅ PauseMenuCanvas показан");

        // Убедимся что pausePanel активен (если есть)
        if (pausePanel != null && !pausePanel.activeSelf)
        {
            pausePanel.SetActive(true);
        }

        // Скрываем панель настроек если она была открыта
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
        }

        // Отключаем кнопку паузы в игре
        if (pauseButton != null)
            pauseButton.interactable = false;

        // Синхронизируем с GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = true;
        }

        Debug.Log("✅ Игра на паузе");
    }

    void HideAllPanels()
    {
        Debug.Log("❌ Скрываю все панели...");

        // Сначала скрываем панель настроек
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log($"✅ SettingsPanel скрыт: был активен = {!settingsPanel.activeSelf}");
        }

        // Затем скрываем панель паузы
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Debug.Log($"✅ PausePanel скрыт: был активен = {!pausePanel.activeSelf}");
        }

        // НАКОНЕЦ скрываем весь канвас паузы
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
            Debug.Log($"✅ PauseMenuCanvas скрыт: был активен = {!pauseMenuCanvas.activeSelf}");
        }
        else
        {
            Debug.LogError("❌ pauseMenuCanvas is null!");
        }
    }

    // === КНОПКИ МЕНЮ ПАУЗЫ ===

    public void RestartLevel()
    {
        Debug.Log("🔄 Перезапускаю уровень");

        // ВАЖНО: восстанавливаем время перед загрузкой сцены
        Time.timeScale = 1f;
        isPaused = false;

        // Получаем имя текущей сцены
        string currentScene = SceneManager.GetActiveScene().name;

        // Загружаем сцену заново
        SceneManager.LoadScene(currentScene);

        Debug.Log($"✅ Уровень перезапущен: {currentScene}");
    }

    public void GoToMainMenu()
    {
        Debug.Log("🏠 Возвращаюсь в главное меню");

        // ВАЖНО: восстанавливаем время
        Time.timeScale = 1f;
        isPaused = false;

        // Загружаем главное меню
        SceneManager.LoadScene("MainMenu");

        Debug.Log("✅ Возврат в главное меню");
    }

    // === МЕТОДЫ ДЛЯ НАСТРОЕК ===

    public void ShowSettings()
    {
        Debug.Log("⚙️ Открываю настройки");

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);

            // Загружаем ТЕКУЩИЕ настройки в слайдеры (не из PlayerPrefs)
            LoadCurrentVolumeSettings();

            // Применяем текущие настройки
            ApplyCurrentVolumeSettings();
        }
        else
        {
            Debug.LogError("❌ Панель настроек не назначена!");
        }
    }

    void LoadCurrentVolumeSettings()
    {
        Debug.Log("📊 Загружаю текущие настройки звука...");

        float masterVol, musicVol, sfxVol;

        if (AudioManager.Instance != null)
        {
            // Берем ПРЯМО из AudioManager
            masterVol = AudioManager.Instance.masterVolume;
            musicVol = AudioManager.Instance.musicVolume;
            sfxVol = AudioManager.Instance.sfxVolume;
        }
        else
        {
            // Резервный вариант
            masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.9f);
        }

        // Устанавливаем в слайдеры
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVol;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = musicVol;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfxVol;

        Debug.Log($"✅ Настройки загружены: Master={masterVol}, Music={musicVol}, SFX={sfxVol}");
    }

    void ApplySettings()
    {
        Debug.Log("💾 Сохраняю настройки звука...");

        // Сохраняем настройки в PlayerPrefs
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);

        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);

        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

        PlayerPrefs.Save();

        // Проигрываем звук подтверждения
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();

        // Возвращаемся в меню паузы
        HideSettings();

        Debug.Log("✅ Настройки звука сохранены");
    }

    public void HideSettings()
    {
        Debug.Log("⚙️ Закрываю настройки");

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (isPaused && pausePanel != null)
            pausePanel.SetActive(true);
    }

    // === УПРАВЛЕНИЕ С КЛАВИАТУРЫ ===

    void Update()
    {
        // Пауза по Escape (только в игровых сценах)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            string currentScene = SceneManager.GetActiveScene().name;
            bool isGameScene = currentScene != "MainMenu" &&
                             !currentScene.StartsWith("Tutorial");

            if (isGameScene)
            {
                TogglePause();
            }
        }
    }
}