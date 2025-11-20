using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("=== UI ЭЛЕМЕНТЫ НАСТРОЕК ===")]
    public GameObject settingsPanel;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle musicToggle;
    public Toggle sfxToggle;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown controlSchemeDropdown;
    public Button applyButton;
    public Button resetButton;
    public Button closeButton;

    [Header("=== ТЕКСТЫ НАСТРОЕК ===")]
    public TextMeshProUGUI masterVolumeText;
    public TextMeshProUGUI musicVolumeText;
    public TextMeshProUGUI sfxVolumeText;
    public TextMeshProUGUI currentResolutionText;
    public TextMeshProUGUI currentQualityText;

    [Header("=== НАСТРОЙКИ УПРАВЛЕНИЯ ===")]
    public string[] controlSchemes = { "Стандартная", "Альтернативная", "Профессиональная" };
    public KeyCode[][] controlPresets;

    // Приватные переменные
    private Resolution[] availableResolutions;
    private string[] qualitySettings;
    private bool isInitialized = false;
    private bool settingsChanged = false;

    // Текущие настройки (для отката)
    private float currentMasterVolume;
    private float currentMusicVolume;
    private float currentSFXVolume;
    private bool currentMusicEnabled;
    private bool currentSFXEnabled;
    private bool currentFullscreen;
    private int currentResolutionIndex;
    private int currentQualityLevel;
    private int currentControlScheme;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("⚙️ SettingsManager инициализирован");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeControlPresets();
        InitializeSettingsSystem();
        LoadAllSettings();
        isInitialized = true;

        // Скрыть панель настроек при старте
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void InitializeControlPresets()
    {
        // Инициализация пресетов управления
        controlPresets = new KeyCode[3][];

        // Стандартная схема
        controlPresets[0] = new KeyCode[]
        {
            KeyCode.W,        // Вверх
            KeyCode.S,        // Вниз
            KeyCode.A,        // Влево
            KeyCode.D,        // Вправо
            KeyCode.Space,    // Взлом
            KeyCode.LeftShift,// Щит
            KeyCode.Escape    // Пауза
        };

        // Альтернативная схема
        controlPresets[1] = new KeyCode[]
        {
            KeyCode.UpArrow,  // Вверх
            KeyCode.DownArrow,// Вниз
            KeyCode.LeftArrow,// Влево
            KeyCode.RightArrow,// Вправо
            KeyCode.E,        // Взлом
            KeyCode.Q,        // Щит
            KeyCode.P         // Пауза
        };

        // Профессиональная схема
        controlPresets[2] = new KeyCode[]
        {
            KeyCode.W,        // Вверх
            KeyCode.S,        // Вниз
            KeyCode.A,        // Влево
            KeyCode.D,        // Вправо
            KeyCode.Mouse0,   // Взлом
            KeyCode.Mouse1,   // Щит
            KeyCode.Tab       // Пауза
        };
    }

    void InitializeSettingsSystem()
    {
        // Получение доступных разрешений и настроек качества
        availableResolutions = Screen.resolutions;
        qualitySettings = QualitySettings.names;

        // Настройка UI элементов
        SetupResolutionDropdown();
        SetupQualityDropdown();
        SetupControlSchemeDropdown();

        // Подписка на события UI
        SubscribeToUIEvents();

        // Настройка кнопок
        if (applyButton != null)
            applyButton.interactable = false;
    }

    void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        int currentResolutionIndex = 0;
        var options = new List<string>();

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            // Пропускаем низкие разрешения
            if (availableResolutions[i].width < 800 || availableResolutions[i].height < 600)
                continue;

            string option = $"{availableResolutions[i].width} x {availableResolutions[i].height}";
            options.Add(option);

            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Обновление текста текущего разрешения
        UpdateResolutionText();
    }

    void SetupQualityDropdown()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();

        var options = new List<string>();
        foreach (string quality in qualitySettings)
        {
            // Локализация названий качеств
            string localizedName = GetLocalizedQualityName(quality);
            options.Add(localizedName);
        }

        qualityDropdown.AddOptions(options);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();

        // Обновление текста текущего качества
        UpdateQualityText();
    }

    void SetupControlSchemeDropdown()
    {
        if (controlSchemeDropdown == null) return;

        controlSchemeDropdown.ClearOptions();

        var options = new List<string>();
        foreach (string scheme in controlSchemes)
        {
            options.Add(scheme);
        }

        controlSchemeDropdown.AddOptions(options);
        controlSchemeDropdown.value = PlayerPrefs.GetInt("ControlScheme", 0);
        controlSchemeDropdown.RefreshShownValue();
    }

    string GetLocalizedQualityName(string qualityName)
    {
        switch (qualityName.ToLower())
        {
            case "very low": return "Очень низкое";
            case "low": return "Низкое";
            case "medium": return "Среднее";
            case "high": return "Высокое";
            case "very high": return "Очень высокое";
            case "ultra": return "Ультра";
            default: return qualityName;
        }
    }

    void SubscribeToUIEvents()
    {
        // Слайдеры громкости
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener((value) => OnSettingsChanged());
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener((value) => OnSettingsChanged());
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener((value) => OnSettingsChanged());
        }

        // Переключатели
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            musicToggle.onValueChanged.AddListener((value) => OnSettingsChanged());
        }

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
            sfxToggle.onValueChanged.AddListener((value) => OnSettingsChanged());
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
            fullscreenToggle.onValueChanged.AddListener((value) => OnSettingsChanged());
        }

        // Выпадающие списки
        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            resolutionDropdown.onValueChanged.AddListener((value) => OnSettingsChanged());
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            qualityDropdown.onValueChanged.AddListener((value) => OnSettingsChanged());
        }

        if (controlSchemeDropdown != null)
        {
            controlSchemeDropdown.onValueChanged.AddListener(OnControlSchemeChanged);
            controlSchemeDropdown.onValueChanged.AddListener((value) => OnSettingsChanged());
        }

        // Кнопки
        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyButtonClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    // === ОБРАБОТЧИКИ ИЗМЕНЕНИЙ НАСТРОЕК ===

    void OnMasterVolumeChanged(float volume)
    {
        if (!isInitialized) return;

        currentMasterVolume = volume;
        UpdateVolumeText(masterVolumeText, "Общая громкость", volume);

        // Тестовый звук при изменении громкости
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
            if (volume > 0.1f)
                PlayTestSound();
        }
    }

    void OnMusicVolumeChanged(float volume)
    {
        if (!isInitialized) return;

        currentMusicVolume = volume;
        UpdateVolumeText(musicVolumeText, "Музыка", volume);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(volume);
        }
    }

    void OnSFXVolumeChanged(float volume)
    {
        if (!isInitialized) return;

        currentSFXVolume = volume;
        UpdateVolumeText(sfxVolumeText, "Звуки", volume);

        // Тестовый звук при изменении громкости SFX
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(volume);
            if (volume > 0.1f)
                PlayTestSound();
        }
    }

    void OnMusicToggleChanged(bool enabled)
    {
        if (!isInitialized) return;

        currentMusicEnabled = enabled;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMusic(enabled);
        }
    }

    void OnSFXToggleChanged(bool enabled)
    {
        if (!isInitialized) return;

        currentSFXEnabled = enabled;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleSFX(enabled);
            if (enabled)
                PlayTestSound();
        }
    }

    void OnFullscreenToggleChanged(bool enabled)
    {
        if (!isInitialized) return;

        currentFullscreen = enabled;
        Screen.fullScreen = enabled;
    }

    void OnResolutionChanged(int resolutionIndex)
    {
        if (!isInitialized) return;

        currentResolutionIndex = resolutionIndex;
        UpdateResolutionText();
    }

    void OnQualityChanged(int qualityIndex)
    {
        if (!isInitialized) return;

        currentQualityLevel = qualityIndex;
        UpdateQualityText();
    }

    void OnControlSchemeChanged(int schemeIndex)
    {
        if (!isInitialized) return;

        currentControlScheme = schemeIndex;
        ApplyControlScheme(schemeIndex);
    }

    void OnSettingsChanged()
    {
        settingsChanged = true;
        if (applyButton != null)
            applyButton.interactable = true;
    }

    // === ОБРАБОТЧИКИ КНОПОК ===

    void OnApplyButtonClicked()
    {
        ApplyAllSettings();
        settingsChanged = false;

        if (applyButton != null)
            applyButton.interactable = false;

        PlayUIClick();
        Debug.Log("✅ Настройки применены");
    }

    void OnResetButtonClicked()
    {
        ResetToDefaults();
        PlayUIClick();
    }

    void OnCloseButtonClicked()
    {
        if (settingsChanged)
        {
            // Спросить подтверждение закрытия без сохранения
            ShowCloseConfirmation();
        }
        else
        {
            CloseSettings();
        }
    }

    // === ПРИМЕНЕНИЕ НАСТРОЕК ===

    void ApplyAllSettings()
    {
        // Применение разрешения
        if (currentResolutionIndex >= 0 && currentResolutionIndex < availableResolutions.Length)
        {
            Resolution resolution = availableResolutions[currentResolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, currentFullscreen);
        }

        // Применение качества графики
        QualitySettings.SetQualityLevel(currentQualityLevel);

        // Применение схемы управления
        ApplyControlScheme(currentControlScheme);

        // Сохранение всех настроек
        SaveAllSettings();
    }

    void ApplyControlScheme(int schemeIndex)
    {
        if (schemeIndex < 0 || schemeIndex >= controlPresets.Length) return;

        // Здесь можно добавить логику применения схемы управления к игроку
        // Например, через PlayerPrefs или прямой вызов в PlayerController

        Debug.Log($"🎮 Применена схема управления: {controlSchemes[schemeIndex]}");
    }

    // === СИСТЕМА СОХРАНЕНИЯ И ЗАГРУЗКИ ===

    void LoadAllSettings()
    {
        // Загрузка аудио настроек
        currentMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        currentMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.9f);
        currentMusicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        currentSFXEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

        // Загрузка графических настроек
        currentFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        currentQualityLevel = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        currentControlScheme = PlayerPrefs.GetInt("ControlScheme", 0);

        // Загрузка разрешения
        int savedWidth = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);
        currentResolutionIndex = FindResolutionIndex(savedWidth, savedHeight);

        // Применение настроек к UI элементам
        ApplySettingsToUI();

        // Применение настроек к системам
        ApplySettingsToSystems();

        settingsChanged = false;
    }

    void ApplySettingsToUI()
    {
        // Применение к слайдерам и переключателям
        if (masterVolumeSlider != null) masterVolumeSlider.value = currentMasterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = currentMusicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = currentSFXVolume;
        if (musicToggle != null) musicToggle.isOn = currentMusicEnabled;
        if (sfxToggle != null) sfxToggle.isOn = currentSFXEnabled;
        if (fullscreenToggle != null) fullscreenToggle.isOn = currentFullscreen;
        if (qualityDropdown != null) qualityDropdown.value = currentQualityLevel;
        if (controlSchemeDropdown != null) controlSchemeDropdown.value = currentControlScheme;
        if (resolutionDropdown != null && currentResolutionIndex >= 0)
            resolutionDropdown.value = currentResolutionIndex;

        // Обновление текстов
        UpdateVolumeText(masterVolumeText, "Общая громкость", currentMasterVolume);
        UpdateVolumeText(musicVolumeText, "Музыка", currentMusicVolume);
        UpdateVolumeText(sfxVolumeText, "Звуки", currentSFXVolume);
        UpdateResolutionText();
        UpdateQualityText();
    }

    void ApplySettingsToSystems()
    {
        // Применение к AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(currentMasterVolume);
            AudioManager.Instance.SetMusicVolume(currentMusicVolume);
            AudioManager.Instance.SetSFXVolume(currentSFXVolume);
            AudioManager.Instance.ToggleMusic(currentMusicEnabled);
            AudioManager.Instance.ToggleSFX(currentSFXEnabled);
        }

        // Применение графических настроек
        Screen.fullScreen = currentFullscreen;
        QualitySettings.SetQualityLevel(currentQualityLevel);

        // Применение разрешения
        if (currentResolutionIndex >= 0 && currentResolutionIndex < availableResolutions.Length)
        {
            Resolution resolution = availableResolutions[currentResolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, currentFullscreen);
        }

        // Применение схемы управления
        ApplyControlScheme(currentControlScheme);
    }

    void SaveAllSettings()
    {
        // Сохранение аудио настроек
        PlayerPrefs.SetFloat("MasterVolume", currentMasterVolume);
        PlayerPrefs.SetFloat("MusicVolume", currentMusicVolume);
        PlayerPrefs.SetFloat("SFXVolume", currentSFXVolume);
        PlayerPrefs.SetInt("MusicEnabled", currentMusicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SFXEnabled", currentSFXEnabled ? 1 : 0);

        // Сохранение графических настроек
        PlayerPrefs.SetInt("Fullscreen", currentFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("QualityLevel", currentQualityLevel);
        PlayerPrefs.SetInt("ControlScheme", currentControlScheme);

        // Сохранение разрешения
        if (currentResolutionIndex >= 0 && currentResolutionIndex < availableResolutions.Length)
        {
            Resolution resolution = availableResolutions[currentResolutionIndex];
            PlayerPrefs.SetInt("ResolutionWidth", resolution.width);
            PlayerPrefs.SetInt("ResolutionHeight", resolution.height);
        }

        PlayerPrefs.Save();
    }

    // === СБРОС НАСТРОЕК ===

    public void ResetToDefaults()
    {
        // Сброс к значениям по умолчанию
        currentMasterVolume = 1f;
        currentMusicVolume = 0.8f;
        currentSFXVolume = 0.9f;
        currentMusicEnabled = true;
        currentSFXEnabled = true;
        currentFullscreen = true;
        currentQualityLevel = 2; // Среднее качество
        currentControlScheme = 0; // Стандартная схема

        // Находим разрешение 1920x1080 или ближайшее доступное
        currentResolutionIndex = FindResolutionIndex(1920, 1080);
        if (currentResolutionIndex == -1)
            currentResolutionIndex = 0;

        // Применяем сброшенные настройки к UI
        ApplySettingsToUI();

        // Включаем кнопку применения
        settingsChanged = true;
        if (applyButton != null)
            applyButton.interactable = true;

        Debug.Log("🔄 Настройки сброшены к значениям по умолчанию");
    }

    // === УТИЛИТЫ ===

    int FindResolutionIndex(int width, int height)
    {
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == width && availableResolutions[i].height == height)
                return i;
        }
        return 0; // Возвращаем первое разрешение если не нашли
    }

    void UpdateVolumeText(TextMeshProUGUI textElement, string settingName, float volume)
    {
        if (textElement != null)
        {
            int percentage = Mathf.RoundToInt(volume * 100);
            textElement.text = $"{settingName}: {percentage}%";
        }
    }

    void UpdateResolutionText()
    {
        if (currentResolutionText != null && currentResolutionIndex >= 0 &&
            currentResolutionIndex < availableResolutions.Length)
        {
            Resolution res = availableResolutions[currentResolutionIndex];
            currentResolutionText.text = $"Разрешение: {res.width} x {res.height}";
        }
    }

    void UpdateQualityText()
    {
        if (currentQualityText != null && currentQualityLevel >= 0 &&
            currentQualityLevel < qualitySettings.Length)
        {
            string qualityName = GetLocalizedQualityName(qualitySettings[currentQualityLevel]);
            currentQualityText.text = $"Качество: {qualityName}";
        }
    }

    void PlayTestSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }
    }

    void PlayUIClick()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }
    }

    // === УПРАВЛЕНИЕ ПАНЕЛЬЮ НАСТРОЕК ===

    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            // Сохраняем текущие настройки для возможного отката
            SaveCurrentSettingsAsBackup();
        }
    }

    public void CloseSettings()
    {
        if (settingsChanged)
        {
            // Восстанавливаем настройки из бекапа
            RestoreSettingsFromBackup();
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        PlayUIClick();
    }

    void ShowCloseConfirmation()
    {
        // Здесь можно реализовать диалог подтверждения закрытия
        // Для простоты сразу закрываем без сохранения
        CloseSettings();
    }

    void SaveCurrentSettingsAsBackup()
    {
        // Сохраняем текущие настройки в PlayerPrefs как бекап
        PlayerPrefs.SetFloat("Backup_MasterVolume", currentMasterVolume);
        PlayerPrefs.SetFloat("Backup_MusicVolume", currentMusicVolume);
        PlayerPrefs.SetFloat("Backup_SFXVolume", currentSFXVolume);
        PlayerPrefs.SetInt("Backup_MusicEnabled", currentMusicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("Backup_SFXEnabled", currentSFXEnabled ? 1 : 0);
        PlayerPrefs.SetInt("Backup_Fullscreen", currentFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("Backup_QualityLevel", currentQualityLevel);
        PlayerPrefs.SetInt("Backup_ControlScheme", currentControlScheme);
        PlayerPrefs.SetInt("Backup_ResolutionIndex", currentResolutionIndex);
    }

    void RestoreSettingsFromBackup()
    {
        // Восстанавливаем настройки из бекапа
        currentMasterVolume = PlayerPrefs.GetFloat("Backup_MasterVolume", 1f);
        currentMusicVolume = PlayerPrefs.GetFloat("Backup_MusicVolume", 0.8f);
        currentSFXVolume = PlayerPrefs.GetFloat("Backup_SFXVolume", 0.9f);
        currentMusicEnabled = PlayerPrefs.GetInt("Backup_MusicEnabled", 1) == 1;
        currentSFXEnabled = PlayerPrefs.GetInt("Backup_SFXEnabled", 1) == 1;
        currentFullscreen = PlayerPrefs.GetInt("Backup_Fullscreen", 1) == 1;
        currentQualityLevel = PlayerPrefs.GetInt("Backup_QualityLevel", 2);
        currentControlScheme = PlayerPrefs.GetInt("Backup_ControlScheme", 0);
        currentResolutionIndex = PlayerPrefs.GetInt("Backup_ResolutionIndex", 0);

        // Применяем восстановленные настройки
        ApplySettingsToUI();
        ApplySettingsToSystems();

        settingsChanged = false;
        if (applyButton != null)
            applyButton.interactable = false;
    }

    // === ДОСТУП К НАСТРОЙКАМ ИЗ ДРУГИХ СКРИПТОВ ===

    public KeyCode[] GetCurrentControlScheme()
    {
        if (currentControlScheme >= 0 && currentControlScheme < controlPresets.Length)
        {
            return controlPresets[currentControlScheme];
        }
        return controlPresets[0]; // Стандартная схема по умолчанию
    }

    public float GetMasterVolume()
    {
        return currentMasterVolume;
    }

    public bool IsMusicEnabled()
    {
        return currentMusicEnabled;
    }

    public bool IsSFXEnabled()
    {
        return currentSFXEnabled;
    }

    public string GetCurrentControlSchemeName()
    {
        if (currentControlScheme >= 0 && currentControlScheme < controlSchemes.Length)
        {
            return controlSchemes[currentControlScheme];
        }
        return controlSchemes[0];
    }

    // === ОЧИСТКА РЕСУРСОВ ===

    void OnDestroy()
    {
        UnsubscribeFromUIEvents();
    }

    void UnsubscribeFromUIEvents()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveAllListeners();

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveAllListeners();

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();

        if (musicToggle != null)
            musicToggle.onValueChanged.RemoveAllListeners();

        if (sfxToggle != null)
            sfxToggle.onValueChanged.RemoveAllListeners();

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveAllListeners();

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveAllListeners();

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.RemoveAllListeners();

        if (controlSchemeDropdown != null)
            controlSchemeDropdown.onValueChanged.RemoveAllListeners();

        if (applyButton != null)
            applyButton.onClick.RemoveAllListeners();

        if (resetButton != null)
            resetButton.onClick.RemoveAllListeners();

        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
    }
}