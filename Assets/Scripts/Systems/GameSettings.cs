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

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioSource[] sfxSources;

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
            settingsPanel.SetActive(false);
    }

    void InitializeResolutions()
    {
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            // Фильтруем разрешения (только стандартные)
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

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    void SetupUIListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);

        if (backButton != null)
            backButton.onClick.AddListener(HideSettings);
    }

    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            Time.timeScale = 0f; // Пауза игры при открытии настроек
            Debug.Log("Settings panel opened");
        }
        else
        {
            Debug.LogError("Settings panel is not assigned!");
        }
    }

    public void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Time.timeScale = 1f; // Возобновляем игру
        }
    }

    void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        Debug.Log($"Master volume set to: {volume}");
    }

    void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    void SetSFXVolume(float volume)
    {
        foreach (AudioSource source in sfxSources)
        {
            if (source != null)
                source.volume = volume;
        }
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    void ApplySettings()
    {
        // Применяем разрешение
        Resolution selectedResolution = filteredResolutions[resolutionDropdown.value];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);

        PlayerPrefs.SetInt("ResolutionWidth", selectedResolution.width);
        PlayerPrefs.SetInt("ResolutionHeight", selectedResolution.height);

        PlayerPrefs.Save();
        Debug.Log("Settings applied and saved!");

        // Можно добавить звук подтверждения
        PlayApplySound();
    }

    void LoadSettings()
    {
        // Загрузка громкости
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.9f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;

        AudioListener.volume = masterVolume;
        if (musicSource != null) musicSource.volume = musicVolume;

        // Загрузка полноэкранного режима
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
        Screen.fullScreen = fullscreen;

        // Загрузка разрешения
        int savedWidth = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);

        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            if (filteredResolutions[i].width == savedWidth &&
                filteredResolutions[i].height == savedHeight)
            {
                resolutionDropdown.value = i;
                break;
            }
        }
    }

    void PlayApplySound()
    {
        // Воспроизведение звука подтверждения
        if (sfxSources.Length > 0 && sfxSources[0] != null)
        {
            sfxSources[0].Play();
        }
    }

    // Быстрые настройки для тестирования
    public void SetLowQuality()
    {
        QualitySettings.SetQualityLevel(0);
        Debug.Log("Quality set to Low");
    }

    public void SetMediumQuality()
    {
        QualitySettings.SetQualityLevel(2);
        Debug.Log("Quality set to Medium");
    }

    public void SetHighQuality()
    {
        QualitySettings.SetQualityLevel(4);
        Debug.Log("Quality set to High");
    }
}