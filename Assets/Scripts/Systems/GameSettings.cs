using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    [Header("UI Elements")]
    public GameObject settingsPanel;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public Button applyButton;
    public Button backButton;

    [Header("Audio Sources (Optional)")]
    public AudioSource musicSource; // Если не используется AudioManager
    public AudioSource[] sfxSources; // Если не используется AudioManager

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;
    private bool isPaused = false; // Локальная переменная для отслеживания паузы

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ GameSettings создан");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeResolutions();
        LoadSettings();
        SetupUIListeners();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("✅ SettingsPanel скрыт при старте");
        }
        else
        {
            Debug.LogError("❌ SettingsPanel не назначен!");
        }
    }

    void InitializeResolutions()
    {
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();

        if (resolutionDropdown == null)
        {
            Debug.LogError("❌ resolutionDropdown не назначен!");
            return;
        }

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            // Фильтруем разрешения (только стандартные 60Hz)
            if (resolutions[i].refreshRate == 60)
            {
                filteredResolutions.Add(resolutions[i]);
                string option = $"{resolutions[i].width} x {resolutions[i].height}";
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }

        if (options.Count == 0)
        {
            Debug.LogWarning("⚠️ Не найдено подходящих разрешений, добавляю текущее");
            filteredResolutions.Add(Screen.currentResolution);
            options.Add($"{Screen.currentResolution.width} x {Screen.currentResolution.height}");
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        Debug.Log($"✅ Инициализировано {filteredResolutions.Count} разрешений");
    }

    void SetupUIListeners()
    {
        Debug.Log("🔧 Настраиваю слушателей UI...");

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            Debug.Log("✅ MasterVolumeSlider слушатель добавлен");
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            Debug.Log("✅ MusicVolumeSlider слушатель добавлен");
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            Debug.Log("✅ SFXVolumeSlider слушатель добавлен");
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            Debug.Log("✅ FullscreenToggle слушатель добавлен");
        }

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySettings);
            Debug.Log("✅ ApplyButton слушатель добавлен");
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(HideSettings);
            Debug.Log("✅ BackButton слушатель добавлен");
        }
    }

    public void ShowSettings()
    {
        Debug.Log("⚙️ GameSettings: Открываю настройки");

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            Debug.Log($"✅ SettingsPanel показан");

            // Загружаем текущие настройки в UI
            LoadSettings();
        }
        else
        {
            Debug.LogError("❌ SettingsPanel не назначен в GameSettings!");
        }
    }

    public void HideSettings()
    {
        Debug.Log("⚙️ GameSettings: Закрываю настройки");

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log($"✅ SettingsPanel скрыт");
        }
    }

    // ========== МЕТОДЫ ДЛЯ ЗВУКА ==========

    void SetMasterVolume(float volume)
    {
        Debug.Log($"🔊 Устанавливаю общую громкость: {volume}");

        // Используем AudioManager если он есть, иначе локальные источники
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }

        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    void SetMusicVolume(float volume)
    {
        Debug.Log($"🎵 Устанавливаю громкость музыки: {volume}");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(volume);
        }
        else if (musicSource != null)
        {
            musicSource.volume = volume;
        }

        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    void SetSFXVolume(float volume)
    {
        Debug.Log($"🔊 Устанавливаю громкость SFX: {volume}");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(volume);
        }
        else if (sfxSources != null && sfxSources.Length > 0)
        {
            foreach (AudioSource source in sfxSources)
            {
                if (source != null)
                    source.volume = volume;
            }
        }

        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    // ========== МЕТОДЫ ДЛЯ ЭКРАНА ==========

    void SetFullscreen(bool isFullscreen)
    {
        Debug.Log($"🖥️ Устанавливаю полноэкранный режим: {isFullscreen}");
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    void ApplySettings()
    {
        Debug.Log("💾 Применяю и сохраняю настройки...");

        // Применяем разрешение
        if (resolutionDropdown != null && filteredResolutions != null &&
            resolutionDropdown.value < filteredResolutions.Count)
        {
            Resolution selectedResolution = filteredResolutions[resolutionDropdown.value];
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);

            PlayerPrefs.SetInt("ResolutionWidth", selectedResolution.width);
            PlayerPrefs.SetInt("ResolutionHeight", selectedResolution.height);

            Debug.Log($"✅ Разрешение установлено: {selectedResolution.width}x{selectedResolution.height}");
        }

        PlayerPrefs.Save();
        Debug.Log("✅ Настройки сохранены");

        // Проигрываем звук подтверждения
        PlayApplySound();

        // Скрываем панель настроек
        HideSettings();
    }

    void LoadSettings()
    {
        Debug.Log("📥 Загружаю сохраненные настройки...");

        // Загрузка громкости
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.9f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;

        // Применяем громкость
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(masterVolume);
            AudioManager.Instance.SetMusicVolume(musicVolume);
            AudioManager.Instance.SetSFXVolume(sfxVolume);
        }
        else
        {
            if (musicSource != null) musicSource.volume = musicVolume;
            if (sfxSources != null)
            {
                foreach (AudioSource source in sfxSources)
                {
                    if (source != null) source.volume = sfxVolume;
                }
            }
        }

        // Загрузка полноэкранного режима
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
        Screen.fullScreen = fullscreen;

        // Загрузка разрешения
        int savedWidth = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);

        if (resolutionDropdown != null && filteredResolutions != null)
        {
            for (int i = 0; i < filteredResolutions.Count; i++)
            {
                if (filteredResolutions[i].width == savedWidth &&
                    filteredResolutions[i].height == savedHeight)
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
            resolutionDropdown.RefreshShownValue();
        }

        Debug.Log($"✅ Настройки загружены: Master={masterVolume}, Music={musicVolume}, SFX={sfxVolume}");
    }

    void PlayApplySound()
    {
        // Воспроизведение звука подтверждения
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }
        else if (sfxSources != null && sfxSources.Length > 0 && sfxSources[0] != null)
        {
            sfxSources[0].Play();
        }
    }

    // ========== УПРАВЛЕНИЕ ПАУЗОЙ ==========

    // Этот метод вызывается из PauseManager или других менеджеров
    public void SetPausedState(bool paused)
    {
        isPaused = paused;
        Debug.Log($"GameSettings: Пауза установлена в {paused}");
    }

    // ========== МЕТОДЫ ДЛЯ КАЧЕСТВА ГРАФИКИ ==========

    public void SetLowQuality()
    {
        QualitySettings.SetQualityLevel(0, true);
        Debug.Log("✅ Качество установлено: Low");
    }

    public void SetMediumQuality()
    {
        QualitySettings.SetQualityLevel(2, true);
        Debug.Log("✅ Качество установлено: Medium");
    }

    public void SetHighQuality()
    {
        QualitySettings.SetQualityLevel(4, true);
        Debug.Log("✅ Качество установлено: High");
    }

    // ========== ОБНОВЛЕНИЕ В РЕАЛЬНОМ ВРЕМЕНИ ==========

    void Update()
    {
        // Обработка горячих клавиш для настроек
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ShowSettings();
        }
    }

    // ========== ОТЛАДОЧНЫЕ МЕТОДЫ ==========

    public void DebugCurrentSettings()
    {
        Debug.Log("=== ТЕКУЩИЕ НАСТРОЙКИ ===");
        Debug.Log($"Разрешение: {Screen.currentResolution}");
        Debug.Log($"Полноэкранный: {Screen.fullScreen}");

        if (musicSource != null)
            Debug.Log($"Громкость музыки: {musicSource.volume}");

        if (AudioManager.Instance != null)
            Debug.Log($"AudioManager: Master={AudioManager.Instance.masterVolume}, Music={AudioManager.Instance.musicVolume}");
    }
}