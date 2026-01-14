using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Video;
using System.Linq;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    // === ПАНЕЛИ ===
    public GameObject helpPanel;
    public GameObject accountPanel;
    public GameObject statsPanel;
    public GameObject tutorialOfferPanelAfterRegister;
    public GameObject tutorialOfferPanelAfterLogin;
    public GameObject settingsPanel;

    // === ЭЛЕМЕНТЫ НАСТРОЕК (если управляем из этого скрипта) ===
    [Header("Настройки")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public Button settingsApplyButton;
    public Button settingsCloseButton;

    // === ИНФОРМАЦИОННЫЕ ПОДПАНЕЛИ ===
    public GameObject controlsInfo;
    public GameObject abilityInfo;
    public GameObject enemiesInfo;
    public GameObject contactsInfo;

    // === ПОЛЯ ВВОДА ===
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerPasswordRepInput;

    // === ТЕКСТОВЫЕ ПОЛЯ ===
    public TextMeshProUGUI authStatusText;
    public TextMeshProUGUI statsContent;

    // === КНОПКИ ГЛАВНОГО МЕНЮ ===
    public Button startGameButton;
    public Button helpButton;
    public Button accountButton;

    // === КНОПКИ В ПАНЕЛЯХ ===
    public Button loginButton;
    public Button registerButton;
    public Button viewStatsButton;
    public Button backToMenuButton;
    public Button submitLoginButton;
    public Button submitRegisterButton;
    public Button controlsButton;
    public Button abilitiesButton;
    public Button enemiesButton;
    public Button contactsButton;
    public Button helpExitButton;
    public Button backFromStatsButton;

    // === ВНУТРЕННИЕ ПАНЕЛИ АККАУНТА ===
    public GameObject loginFields;
    public GameObject registerFields;

    // === КНОПКИ ТУТОРИАЛА ===
    public Button startTutorialFromRegisterButton;
    public Button startTutorialFromLoginButton;
    public Button skipTutorialButton;
    public Button exitGameButton;
    public Button settingsButton;

    private GameObject currentHelpSection;
    [HideInInspector] public bool isPlayerButtonEnabled = false;
    private bool uiInitialized = false;

    // Для настроек
    private Resolution[] availableResolutions;

    private Dictionary<int, float> userBestTimes = new Dictionary<int, float>();
    private Dictionary<int, int> userCoins = new Dictionary<int, int>();
    private float userBestGameTime = Mathf.Infinity;
    private List<LeaderboardEntry> globalLeaderboard = new List<LeaderboardEntry>();
    private int currentPlayerRank = -1;

    void Awake()
    {
        // Система синглтона с защитой от дублирования
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            FirebaseRestManager.OnAuthStateChanged += OnFirebaseAuthChanged;
            Debug.Log("✅ MainMenuManager создан как постоянный объект");
        }
        else if (Instance != this)
        {
            Debug.Log("⚠️ Уничтожен дубликат MainMenuManager");
            Destroy(gameObject);
            return;
        }
    }

    void OnFirebaseAuthChanged()
    {
        Debug.Log("🔐 Состояние авторизации изменилось — загружаем статистику");
        UpdateAuthStatus();
        FirebaseRestManager.Instance.LoadGlobalLeaderboard((entries) =>
        {
            globalLeaderboard = entries.Take(3).ToList(); // Топ-3
            UpdateCurrentPlayerRank();
            UpdateStatsContent(); // обновляем UI
        });
        LoadAllLevelProgressFromFirebase();
    }

    void UpdateCurrentPlayerRank()
    {
        if (FirebaseRestManager.Instance == null || string.IsNullOrEmpty(FirebaseRestManager.Instance.CurrentUserId))
        {
            currentPlayerRank = -1;
            return;
        }

        string currentUserId = FirebaseRestManager.Instance.CurrentUserId;
        for (int i = 0; i < globalLeaderboard.Count; i++)
        {
            if (globalLeaderboard[i].UserId == currentUserId)
            {
                currentPlayerRank = i + 1;
                return;
            }
        }

        FirebaseRestManager.Instance.LoadGlobalLeaderboard((allEntries) =>
        {
            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].UserId == currentUserId)
                {
                    currentPlayerRank = i + 1;
                    UpdateStatsContent();
                    return;
                }
            }
            currentPlayerRank = -1;
        });
    }

    void Start()
    {
        Debug.Log("🎮 MainMenuManager запущен!");

        // Инициализируем разрешения экрана
        InitializeResolutions();

        // Загружаем сохраненные настройки
        LoadSettings();

        // Если мы уже в главном меню, инициализируем UI сразу
        if (SceneManager.GetActiveScene().name == "MainMenu" && !uiInitialized)
        {
            InitializeUI();
        }
    }

    // === ИНИЦИАЛИЗАЦИЯ РАЗРЕШЕНИЙ ===
    void InitializeResolutions()
    {
        availableResolutions = Screen.resolutions;

        // Можно отфильтровать повторяющиеся разрешения
        System.Collections.Generic.HashSet<string> uniqueResolutions = new System.Collections.Generic.HashSet<string>();

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < availableResolutions.Length; i++)
            {
                // Пропускаем слишком маленькие разрешения
                if (availableResolutions[i].width < 800 || availableResolutions[i].height < 600)
                    continue;

                string option = $"{availableResolutions[i].width} x {availableResolutions[i].height}";

                // Добавляем только уникальные
                if (uniqueResolutions.Add(option))
                {
                    options.Add(option);

                    // Проверяем текущее разрешение
                    if (availableResolutions[i].width == Screen.currentResolution.width &&
                        availableResolutions[i].height == Screen.currentResolution.height)
                    {
                        currentResolutionIndex = options.Count - 1;
                    }
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }
    }

    // === ЗАГРУЗКА НАСТРОЕК ===
    void LoadSettings()
    {
        // Громкость
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.9f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;

        // Полноэкранный режим
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
        Screen.fullScreen = fullscreen;

        // Разрешение
        int savedWidth = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);

        if (resolutionDropdown != null)
        {
            for (int i = 0; i < resolutionDropdown.options.Count; i++)
            {
                string resText = resolutionDropdown.options[i].text;
                if (resText.Contains($"{savedWidth} x {savedHeight}"))
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
        }
    }

    // === СОХРАНЕНИЕ НАСТРОЕК ===
    void SaveSettings()
    {
        // Сохраняем громкость
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

        // Сохраняем полноэкранный режим
        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

        // Сохраняем разрешение
        if (resolutionDropdown != null && resolutionDropdown.value < availableResolutions.Length)
        {
            Resolution selectedRes = availableResolutions[resolutionDropdown.value];
            PlayerPrefs.SetInt("ResolutionWidth", selectedRes.width);
            PlayerPrefs.SetInt("ResolutionHeight", selectedRes.height);
        }

        PlayerPrefs.Save();
        Debug.Log("✅ Настройки сохранены");
    }

    // === ОБРАБОТЧИК ЗАГРУЗКИ СЦЕН ===
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔄 Загружена сцена: {scene.name}");

        if (scene.name == "MainMenu")
        {
            if (FirebaseRestManager.Instance != null && FirebaseRestManager.Instance.IsAuthenticated)
            {
                LoadAllLevelProgressFromFirebase();
            }
            // Если UI уже инициализирован, просто обновляем ссылки
            if (uiInitialized)
            {
                ReconnectUIReferences();
            }
            else
            {
                InitializeUI();
            }
        }
        else
        {
            // На других сценах скрываем UI главного меню
            HideAllUIPanels();
        }
    }

    // === ПЕРВОНАЧАЛЬНАЯ ИНИЦИАЛИЗАЦИЯ UI ===
    private void InitializeUI()
    {
        Debug.Log("🔧 Инициализация UI главного меню...");

        // Находим все элементы UI
        FindAllUIElements();

        // Настраиваем кнопки
        SetupUI();

        // Обновляем статус авторизации
        UpdateAuthStatus();

        // Показываем форму входа по умолчанию
        ShowLoginFields();

        // Активируем кнопку старта если нужно
        if (startGameButton != null)
        {
            startGameButton.interactable = FirebaseRestManager.Instance != null &&
                                           FirebaseRestManager.Instance.IsAuthenticated &&
                                           PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 1;
        }

        uiInitialized = true;
        Debug.Log("✅ UI главного меню инициализирован");
    }

    // === ПЕРЕПОДКЛЮЧЕНИЕ ССЫЛОК ПРИ ПОВТОРНОЙ ЗАГРУЗКЕ ===
    private void ReconnectUIReferences()
    {
        Debug.Log("🔌 Переподключение UI ссылок...");

        // Находим элементы заново
        FindAllUIElements();

        // Переподписываем события кнопок
        ReconnectButtonListeners();

        // Обновляем состояние
        UpdateAuthStatus();

        Debug.Log("✅ UI ссылки восстановлены");
    }

    // === ПОИСК ВСЕХ UI ЭЛЕМЕНТОВ ===
    private void FindAllUIElements()
    {
        // Находим Canvas
        GameObject canvas = GameObject.Find("MainMenuCanvas");
        if (canvas == null)
        {
            Debug.LogError("❌ Canvas 'MainMenuCanvas' не найден!");
            return;
        }

        // === ПОИСК ПАНЕЛЕЙ ===
        helpPanel = FindChild(canvas.transform, "HelpPanel");
        accountPanel = FindChild(canvas.transform, "AccountPanel");
        statsPanel = FindChild(canvas.transform, "StatsPanel");
        Debug.Log($"viewStatsButton found: {viewStatsButton != null}");
        tutorialOfferPanelAfterRegister = FindChild(canvas.transform, "TutorPanel2");
        tutorialOfferPanelAfterLogin = FindChild(canvas.transform, "TutorPanel1");
        settingsPanel = FindChild(canvas.transform, "SettingsPanel"); // Ищем панель настроек

        // === ПОИСК ЭЛЕМЕНТОВ НАСТРОЕК ===
        if (settingsPanel != null)
        {
            masterVolumeSlider = FindChild(settingsPanel.transform, "MasterVolumeSlider")?.GetComponent<Slider>();
            musicVolumeSlider = FindChild(settingsPanel.transform, "MusicVolumeSlider")?.GetComponent<Slider>();
            sfxVolumeSlider = FindChild(settingsPanel.transform, "SFXVolumeSlider")?.GetComponent<Slider>();
            fullscreenToggle = FindChild(settingsPanel.transform, "FullscreenToggle")?.GetComponent<Toggle>();
            resolutionDropdown = FindChild(settingsPanel.transform, "ResolutionDropdown")?.GetComponent<TMP_Dropdown>();
            settingsApplyButton = FindChild(settingsPanel.transform, "ApplyButton")?.GetComponent<Button>();
            settingsCloseButton = FindChild(settingsPanel.transform, "CloseButton")?.GetComponent<Button>();
        }

        // === ПОИСК ИНФО-ПАНЕЛЕЙ ===
        controlsInfo = FindChild(helpPanel?.transform, "ControlsInfo");
        abilityInfo = FindChild(helpPanel?.transform, "AbilityInfo");
        enemiesInfo = FindChild(helpPanel?.transform, "EnemiesInfo");
        contactsInfo = FindChild(helpPanel?.transform, "ContactsInfo");

        // === ПОИСК ПОЛЕЙ ВВОДА ===
        loginFields = FindChild(canvas.transform, "LoginFields");
        registerFields = FindChild(canvas.transform, "RegisterFields");

        // Поля входа
        if (loginFields != null)
        {
            loginEmailInput = FindChild(loginFields.transform, "EmailInputField")?.GetComponent<TMP_InputField>();
            loginPasswordInput = FindChild(loginFields.transform, "PasswordInputField")?.GetComponent<TMP_InputField>();
            submitLoginButton = FindChild(loginFields.transform, "LoginButton")?.GetComponent<Button>();
        }

        // Поля регистрации
        if (registerFields != null)
        {
            registerEmailInput = FindChild(registerFields.transform, "EmailInputField")?.GetComponent<TMP_InputField>();
            registerPasswordInput = FindChild(registerFields.transform, "PasswordInputField")?.GetComponent<TMP_InputField>();
            registerPasswordRepInput = FindChild(registerFields.transform, "PasswordInputFieldRep")?.GetComponent<TMP_InputField>();
            submitRegisterButton = FindChild(registerFields.transform, "RegisterButton")?.GetComponent<Button>();
        }

        // === ПОИСК ТЕКСТОВЫХ ПОЛЕЙ ===
        authStatusText = FindChild(canvas.transform, "AuthStatusText")?.GetComponent<TextMeshProUGUI>();
        statsContent = FindChild(canvas.transform, "StatsContent")?.GetComponent<TextMeshProUGUI>();

        // === ПОИСК КНОПОК ГЛАВНОГО МЕНЮ ===
        startGameButton = FindChild(canvas.transform, "StartGame")?.GetComponent<Button>();
        helpButton = FindChild(canvas.transform, "Help")?.GetComponent<Button>();
        accountButton = FindChild(canvas.transform, "Account")?.GetComponent<Button>();
        viewStatsButton = FindChild(canvas?.transform, "Stats")?.GetComponent<Button>();

        // === ПОИСК КНОПОК В ПАНЕЛЯХ ===
        // В AccountPanel
        loginButton = FindChild(accountPanel?.transform, "LoginButton")?.GetComponent<Button>();
        registerButton = FindChild(accountPanel?.transform, "RegisterButton")?.GetComponent<Button>();
        backToMenuButton = FindChild(accountPanel?.transform, "BackToMenuButton")?.GetComponent<Button>();

        // В HelpPanel
        controlsButton = FindChild(helpPanel?.transform, "ControlsButton")?.GetComponent<Button>();
        abilitiesButton = FindChild(helpPanel?.transform, "AbilitiesButton")?.GetComponent<Button>();
        enemiesButton = FindChild(helpPanel?.transform, "EnemiesButton")?.GetComponent<Button>();
        contactsButton = FindChild(helpPanel?.transform, "SupportButton")?.GetComponent<Button>();
        helpExitButton = FindChild(helpPanel?.transform, "Exit")?.GetComponent<Button>();

        // В StatsPanel
        backFromStatsButton = FindChild(statsPanel?.transform, "BackFromStatsButton")?.GetComponent<Button>();

        // В панелях туториала
        if (tutorialOfferPanelAfterRegister != null)
        {
            startTutorialFromRegisterButton = FindChild(tutorialOfferPanelAfterRegister.transform, "Yes")?.GetComponent<Button>();
            skipTutorialButton = FindChild(tutorialOfferPanelAfterRegister.transform, "No")?.GetComponent<Button>();
        }

        if (tutorialOfferPanelAfterLogin != null)
        {
            startTutorialFromLoginButton = FindChild(tutorialOfferPanelAfterLogin.transform, "Yes")?.GetComponent<Button>();
            if (skipTutorialButton == null)
                skipTutorialButton = FindChild(tutorialOfferPanelAfterLogin.transform, "No")?.GetComponent<Button>();
        }

        // === ДОПОЛНИТЕЛЬНЫЕ КНОПКИ ===
        exitGameButton = FindChild(canvas.transform, "ExitGameButton")?.GetComponent<Button>();
        settingsButton = FindChild(canvas.transform, "SettingsButton")?.GetComponent<Button>();

        // Отладочные сообщения
        Debug.Log($"Settings panel found: {settingsPanel != null}");
        Debug.Log($"Exit button found: {exitGameButton != null}");
        Debug.Log($"Settings button found: {settingsButton != null}");

        Debug.Log($"✅ Найдены UI элементы: Панели={helpPanel != null}, Кнопки={startGameButton != null}");
    }

    // === ВСПОМОГАТЕЛЬНЫЙ МЕТОД ДЛЯ ПОИСКА ДОЧЕРНИХ ОБЪЕКТОВ ===
    private GameObject FindChild(Transform parent, string childName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child.gameObject;

            // Рекурсивный поиск
            GameObject found = FindChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    public void Logout()
    {
        FirebaseRestManager.Instance.SignOut();

        // Очищаем кэш статистики
        userBestTimes.Clear();
        userCoins.Clear();
        userBestGameTime = Mathf.Infinity;
        UpdateStatsContent(); // покажет "Нет данных"

        UpdateAuthStatus();
    }

    // === НАСТРОЙКА КНОПОК И СОБЫТИЙ ===
    private void SetupUI()
    {
        // Удаляем старые обработчики
        RemoveAllButtonListeners();

        // Главное меню
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGame);
        if (helpButton != null) helpButton.onClick.AddListener(ShowHelpPanel);
        if (accountButton != null) accountButton.onClick.AddListener(ShowAccountPanel);

        // Кнопки открытия форм
        if (loginButton != null) loginButton.onClick.AddListener(ShowLoginFields);
        if (registerButton != null) registerButton.onClick.AddListener(ShowRegisterFields);
        if (viewStatsButton != null) viewStatsButton.onClick.AddListener(OnViewStats);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(HideAccountPanel);

        // Кнопки отправки данных
        if (submitLoginButton != null) submitLoginButton.onClick.AddListener(OnLogin);
        if (submitRegisterButton != null) submitRegisterButton.onClick.AddListener(OnRegister);

        // Help Panel навигация
        if (controlsButton != null) controlsButton.onClick.AddListener(() => ShowHelpSection(controlsInfo));
        if (abilitiesButton != null) abilitiesButton.onClick.AddListener(() => ShowHelpSection(abilityInfo));
        if (enemiesButton != null) enemiesButton.onClick.AddListener(() => ShowHelpSection(enemiesInfo));
        if (contactsButton != null) contactsButton.onClick.AddListener(() => ShowHelpSection(contactsInfo));
        if (helpExitButton != null) helpExitButton.onClick.AddListener(HideHelpPanel);

        // Кнопки "Назад"
        if (backFromStatsButton != null) backFromStatsButton.onClick.AddListener(HideStatsPanel);

        // Кнопки туториала
        if (startTutorialFromRegisterButton != null) startTutorialFromRegisterButton.onClick.AddListener(StartTutorialAfterRegister);
        if (startTutorialFromLoginButton != null) startTutorialFromLoginButton.onClick.AddListener(StartTutorialAfterLogin);
        if (skipTutorialButton != null) skipTutorialButton.onClick.AddListener(SkipTutorial);

        // Кнопка выхода из игры
        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveAllListeners();
            exitGameButton.onClick.AddListener(ExitGame);
            exitGameButton.onClick.AddListener(() => Debug.Log("Exit button clicked!"));
            Debug.Log("✅ Exit button listeners added");
        }
        else
        {
            Debug.LogError("❌ Exit button is null!");
        }

        // Кнопка настроек
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(ShowSettings);
            settingsButton.onClick.AddListener(() => Debug.Log("Settings button clicked!"));
            Debug.Log("✅ Settings button listeners added");
        }
        else
        {
            Debug.LogError("❌ Settings button is null!");
        }

        // Настройки элементов панели настроек
        SetupSettingsUI();
    }

    // === НАСТРОЙКА ЭЛЕМЕНТОВ НАСТРОЕК ===
    private void SetupSettingsUI()
    {
        // Слайдеры громкости
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        // Переключатель полноэкранного режима
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // Кнопка применения настроек
        if (settingsApplyButton != null)
        {
            settingsApplyButton.onClick.AddListener(ApplySettings);
        }

        // Кнопка закрытия настроек
        if (settingsCloseButton != null)
        {
            settingsCloseButton.onClick.AddListener(HideSettings);
        }
    }

    // === МЕТОДЫ ДЛЯ НАСТРОЕК ===
    void SetMasterVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(volume);
    }

    void SetMusicVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(volume);
        // НЕ трогаем AudioListener — это для master!
    }

    void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(volume);
    }

    void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"Fullscreen: {isFullscreen}");
    }

    void ApplySettings()
    {
        // Применяем разрешение
        if (resolutionDropdown != null && resolutionDropdown.value < availableResolutions.Length)
        {
            Resolution selectedRes = availableResolutions[resolutionDropdown.value];
            Screen.SetResolution(selectedRes.width, selectedRes.height, Screen.fullScreen);
            Debug.Log($"Resolution set to: {selectedRes.width}x{selectedRes.height}");
        }

        // Сохраняем настройки
        SaveSettings();

        // Закрываем панель
        HideSettings();
    }


    // === ПЕРЕПОДКЛЮЧЕНИЕ СОБЫТИЙ КНОПОК ===
    private void ReconnectButtonListeners()
    {
        // Удаляем все старые обработчики
        RemoveAllButtonListeners();

        // Добавляем новые
        SetupUI();
    }

    // === УДАЛЕНИЕ ВСЕХ ОБРАБОТЧИКОВ СОБЫТИЙ ===
    private void RemoveAllButtonListeners()
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            btn.onClick.RemoveAllListeners();
        }
    }

    // === СКРЫТИЕ ВСЕХ ПАНЕЛЕЙ ===
    private void HideAllUIPanels()
    {
        if (helpPanel != null) helpPanel.SetActive(false);
        if (accountPanel != null) accountPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        if (tutorialOfferPanelAfterRegister != null) tutorialOfferPanelAfterRegister.SetActive(false);
        if (tutorialOfferPanelAfterLogin != null) tutorialOfferPanelAfterLogin.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false); // Скрываем и настройки

        HideAllHelpSections();
    }

    // === МЕТОДЫ УПРАВЛЕНИЯ UI ===
    public void ShowHelpPanel()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(true);
            DisableMainButtons();
            ShowHelpSection(controlsInfo);
        }
    }

    public void HideHelpPanel()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
            EnableMainButtons();
            HideAllHelpSections();
            currentHelpSection = null;
        }
    }

    void ShowHelpSection(GameObject section)
    {
        HideAllHelpSections();
        if (section != null)
        {
            section.SetActive(true);
            currentHelpSection = section;
        }
    }

    void HideAllHelpSections()
    {
        if (controlsInfo != null) controlsInfo.SetActive(false);
        if (abilityInfo != null) abilityInfo.SetActive(false);
        if (enemiesInfo != null) enemiesInfo.SetActive(false);
        if (contactsInfo != null) contactsInfo.SetActive(false);
    }

    public void ShowAccountPanel()
    {
        if (accountPanel != null)
        {
            accountPanel.SetActive(true);
            DisableMainButtons();
            UpdateAuthStatus();
            ShowLoginFields();
        }
    }

    public void HideAccountPanel()
    {
        if (accountPanel != null)
        {
            accountPanel.SetActive(false);
            EnableMainButtons();
        }
    }

    public void ShowLoginFields()
    {
        if (loginFields != null) loginFields.SetActive(true);
        if (registerFields != null) registerFields.SetActive(false);
    }

    public void ShowRegisterFields()
    {
        if (loginFields != null) loginFields.SetActive(false);
        if (registerFields != null) registerFields.SetActive(true);
        else Debug.LogError("❌ registerFields == null! Проверьте имя объекта в сцене.");
    }

    public void ShowStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(true);
            DisableMainButtons();
            UpdateStatsContent();
        }
    }

    public void HideStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
            EnableMainButtons();
        }
    }

    // === ПАНЕЛЬ НАСТРОЕК ===
    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            DisableMainButtons();
            Debug.Log("✅ Settings panel opened");
        }
        else
        {
            Debug.LogError("❌ Settings panel is null!");

            // Пробуем найти
            GameObject canvas = GameObject.Find("MainMenuCanvas");
            if (canvas != null)
            {
                settingsPanel = FindChild(canvas.transform, "SettingsPanel");
                if (settingsPanel != null)
                {
                    settingsPanel.SetActive(true);
                    DisableMainButtons();
                    Debug.Log("✅ Settings panel found and opened");
                }
            }
        }
    }

    public void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            EnableMainButtons();
            Debug.Log("Settings panel closed");
        }
    }

    void DisableMainButtons()
    {
        if (startGameButton != null) startGameButton.interactable = false;
        if (helpButton != null) helpButton.interactable = false;
        if (accountButton != null) accountButton.interactable = false;
        if (settingsButton != null) settingsButton.interactable = false;
    }

    void EnableMainButtons()
    {
        if (startGameButton != null) startGameButton.interactable = true;
        if (helpButton != null) helpButton.interactable = true;
        if (accountButton != null) accountButton.interactable = true;
        if (settingsButton != null) settingsButton.interactable = true;
    }

    // === ОСНОВНЫЕ МЕТОДЫ ===
    void OnStartGame()
    {
        if (FirebaseRestManager.Instance != null && FirebaseRestManager.Instance.IsAuthenticated)
        {
            bool hasTutorial = PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 1;
            if (!hasTutorial)
            {
                ShowTutorialPrompt();
            }
            else
            {
                StartCoroutine(PlayIntroCutscenesAndLoadLevel1());
            }
        }
        else
        {
            ShowAccountPanel();
        }
    }

    IEnumerator PlayIntroAndLoadLevel1()
    {
        // Отключаем кнопки, чтобы не нажимали во время кадры
        DisableMainButtons();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        // Убеждаемся, что CutsceneManager существует
        if (CutsceneManager.Instance == null)
        {
            Debug.LogError("❌ CutsceneManager не найден! Загружаем Level_1 напрямую.");
            SceneManager.LoadScene("Level_1");
            yield break;
        }

        // Загружаем видео из Resources
        VideoClip introA = Resources.Load<VideoClip>("Cutscenes/IntroA");
        VideoClip introB = Resources.Load<VideoClip>("Cutscenes/IntroB");

        if (introA == null || introB == null)
        {
            Debug.LogError("❌ Одно или оба видео не найдены в Resources/Cutscenes/");
            SceneManager.LoadScene("Level_1");
            yield break;
        }

        yield return CutsceneManager.Instance.PlayCutscene(introA);
        yield return CutsceneManager.Instance.PlayCutscene(introB);

        CutsceneManager.DestroyInstance();

        SceneManager.LoadScene("Level_1");
    }

    void OnLogin()
    {
        string email = loginEmailInput?.text ?? "";
        string password = loginPasswordInput?.text ?? "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (authStatusText != null)
            {
                authStatusText.text = "Заполните email и пароль";
                authStatusText.color = Color.red;
            }
            return;
        }

        FirebaseRestManager.Instance.SignIn(email, password,
            () => {
                PlayerPrefs.SetString("PlayerEmail", email);
                PlayerPrefs.Save();
                UpdateAuthStatus();
                LoadAllLevelProgressFromFirebase();
                ShowTutorialOfferAfterLogin();
            },
            (error) => {
                if (authStatusText != null)
                {
                    authStatusText.text = $"Ошибка входа: {error}";
                    authStatusText.color = Color.red;
                }
            }
        );
    }

    void LoadAllLevelProgressFromFirebase()
    {
        if (!FirebaseRestManager.Instance.IsAuthenticated) return;

        userBestTimes.Clear();
        userCoins.Clear();
        userBestGameTime = Mathf.Infinity;

        // Загружаем общий рекорд
        FirebaseRestManager.Instance.LoadBestGameTime((time) =>
        {
            userBestGameTime = time;
            UpdateStatsContent();
        });

        // Загружаем уровни
        for (int level = 1; level <= 5; level++)
        {
            int lvl = level;
            FirebaseRestManager.Instance.LoadLevelProgress(lvl, (coins, time) =>
            {
                if (time > 0)
                {
                    userBestTimes[lvl] = time;
                    userCoins[lvl] = coins;
                }
                UpdateStatsContent();
            });
        }
    }

    void OnRegister()
    {
        string email = registerEmailInput?.text ?? "";
        string password = registerPasswordInput?.text ?? "";
        string passwordRep = registerPasswordRepInput?.text ?? "";

        if (string.IsNullOrEmpty(email) || password.Length < 6)
        {
            if (authStatusText != null)
            {
                authStatusText.text = "Пароль должен быть минимум 6 символов";
                authStatusText.color = Color.red;
            }
            return;
        }

        if (password != passwordRep)
        {
            if (authStatusText != null)
            {
                authStatusText.text = "Пароли не совпадают";
                authStatusText.color = Color.red;
            }
            return;
        }

        FirebaseRestManager.Instance.SignUp(email, password,
            () => {
                PlayerPrefs.SetString("PlayerEmail", email);
                PlayerPrefs.SetInt("HasCompletedTutorial", 0);
                PlayerPrefs.Save();
                UpdateAuthStatus();
                ShowTutorialOfferAfterRegister();
            },
            (error) => {
                if (authStatusText != null)
                {
                    authStatusText.text = $"Ошибка регистрации: {error}";
                    authStatusText.color = Color.red;
                }
            }
        );
    }

    void OnViewStats()
    {
        Debug.Log("📊 OnViewStats вызван!");

        if (FirebaseRestManager.Instance != null && FirebaseRestManager.Instance.IsAuthenticated)
        {
            Debug.Log("✅ Пользователь авторизован — загружаем статистику и лидерборд");

            // Загружаем ГЛОБАЛЬНЫЙ лидерборд
            FirebaseRestManager.Instance.LoadGlobalLeaderboard((entries) =>
            {
                globalLeaderboard = entries;

                // Находим место текущего игрока
                string currentUserId = FirebaseRestManager.Instance.CurrentUserId;
                currentPlayerRank = -1;

                for (int i = 0; i < globalLeaderboard.Count; i++)
                {
                    if (globalLeaderboard[i].UserId == currentUserId)
                    {
                        currentPlayerRank = i + 1; // 1-based
                        break;
                    }
                }

                // Обновляем UI
                UpdateStatsContent();
            });

            // Показываем панель (даже если данные ещё грузятся)
            ShowStatsPanel();
        }
        else
        {
            Debug.Log("❌ Пользователь НЕ авторизован");
            if (authStatusText != null)
            {
                authStatusText.text = "Сначала войдите в аккаунт";
                authStatusText.color = Color.red;
            }
        }
    }

    void UpdateStatsContent()
    {
        if (statsContent == null) return;

        StringBuilder sb = new StringBuilder();

        // === ЛИЧНАЯ СТАТИСТИКА ===
        if (userBestGameTime < Mathf.Infinity)
        {
            sb.AppendLine($"<b>🏆 ВАШЕ ЛУЧШЕЕ ВРЕМЯ</b>");
            sb.AppendLine($"⏱ {FormatTime(userBestGameTime)}\n");
        }

        // === ГЛОБАЛЬНЫЙ ТОП-3 ===
        if (globalLeaderboard.Count > 0)
        {
            sb.AppendLine("<b>🌍 ГЛОБАЛЬНЫЙ ТОП-3</b>");
            for (int i = 0; i < Mathf.Min(3, globalLeaderboard.Count); i++)
            {
                var entry = globalLeaderboard[i];
                string medal = i == 0 ? "🥇" : (i == 1 ? "🥈" : "🥉");
                sb.AppendLine($"{medal} {entry.DisplayName}: {FormatTime(entry.TotalTime)}");
            }
            sb.AppendLine("");
        }

        // === ВАШЕ МЕСТО ===
        if (currentPlayerRank > 0)
        {
            string placeText = currentPlayerRank <= 3 ? "🏆 Вы в топ-3!" : $"📍 Ваше место: #{currentPlayerRank}";
            sb.AppendLine(placeText);
            sb.AppendLine("");
        }

        // === РЕКОРДЫ ПО УРОВНЯМ ===
        bool hasLevelData = false;
        for (int level = 1; level <= 5; level++)
        {
            if (userBestTimes.TryGetValue(level, out float bestTime) && bestTime > 0)
            {
                hasLevelData = true;
                int coins = userCoins.TryGetValue(level, out int c) ? c : 0;
                sb.AppendLine($"<b>Уровень {level}</b>");
                sb.AppendLine($"  ⏱ Время: {FormatTime(bestTime)}");
                sb.AppendLine($"  🪙 Монеты: {coins}");
                sb.AppendLine("");
            }
        }

        if (userBestGameTime == Mathf.Infinity && !hasLevelData)
        {
            sb.AppendLine("Нет сохранённых рекордов.");
        }

        statsContent.text = sb.ToString();
    }

    void ShowTutorialOfferAfterRegister()
    {
        HideAccountPanel();
        if (tutorialOfferPanelAfterRegister != null)
        {
            tutorialOfferPanelAfterRegister.SetActive(true);
            DisableMainButtons();
        }
    }

    void ShowTutorialOfferAfterLogin()
    {
        HideAccountPanel();
        if (tutorialOfferPanelAfterLogin != null)
        {
            tutorialOfferPanelAfterLogin.SetActive(true);
            DisableMainButtons();
        }
    }

    void StartTutorialAfterRegister()
    {
        CloseTutorialPanels();
        CutsceneManager.DestroyInstance();
        LoadScene("Tutorial");
    }

    void StartTutorialAfterLogin()
    {
        CloseTutorialPanels();
        CutsceneManager.DestroyInstance();
        LoadScene("Tutorial");
    }

    void SkipTutorial()
    {
        PlayerPrefs.SetInt("HasCompletedTutorial", 1);
        PlayerPrefs.Save();
        CloseTutorialPanels();
        EnableStartGameButton();
    }

    void CloseTutorialPanels()
    {
        if (tutorialOfferPanelAfterRegister != null) tutorialOfferPanelAfterRegister.SetActive(false);
        if (tutorialOfferPanelAfterLogin != null) tutorialOfferPanelAfterLogin.SetActive(false);
        EnableMainButtons();
    }

    void EnableStartGameButton()
    {
        if (startGameButton != null)
        {
            startGameButton.interactable = true;
            isPlayerButtonEnabled = true;
        }
    }

    void ExitGame()
    {
        Debug.Log("🛑 ExitGame вызван!");

        // Сохраняем все данные перед выходом
        PlayerPrefs.Save();

#if UNITY_EDITOR
        Debug.Log("✅ Выход из игры в редакторе Unity");
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Debug.Log("✅ Выход из приложения");
            Application.Quit();
#endif
    }

    public void CompleteTutorialAndStartGame()
    {
        PlayerPrefs.SetInt("HasCompletedTutorial", 1);
        PlayerPrefs.Save();

        StartCoroutine(PlayIntroCutscenesAndLoadLevel1());
    }

    void UpdateAuthStatus()
    {
        if (FirebaseRestManager.Instance == null || authStatusText == null) return;

        if (FirebaseRestManager.Instance.IsAuthenticated)
        {
            string email = PlayerPrefs.GetString("PlayerEmail", "user");
            authStatusText.text = $"Авторизован: {email}";
            authStatusText.color = new Color(0.2f, 1f, 0.4f);
            if (viewStatsButton != null) viewStatsButton.interactable = true;
        }
        else
        {
            authStatusText.text = "Не авторизован";
            authStatusText.color = Color.red;
            if (viewStatsButton != null) viewStatsButton.interactable = false;
        }
    }

    private IEnumerator PlayIntroCutscenesAndLoadLevel1()
    {
        // Отключаем кнопки, чтобы не нажимали во время кат-сцен
        DisableMainButtons();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        if (CutsceneManager.Instance == null)
        {
            Debug.Log("🔄 Загружаем CutsceneManager из префаба");

            GameObject prefab = Resources.Load<GameObject>("Prefabs/CutsceneManager");
            if (prefab != null)
            {
                Instantiate(prefab);
            }
            else
            {
                Debug.LogError("❌ Префаб CutsceneManager не найден в Resources/Prefabs/");
                SceneManager.LoadScene("Level_1");
                yield break;
            }
        }

        // Загружаем видео из Resources
        VideoClip introA = Resources.Load<VideoClip>("Cutscenes/IntroA");
        VideoClip introB = Resources.Load<VideoClip>("Cutscenes/IntroB");

        if (introA == null || introB == null)
        {
            Debug.LogError("❌ Одно или оба видео не найдены в Resources/Cutscenes/");
            SceneManager.LoadScene("Level_1");
            yield break;
        }

        // Проигрываем кат-сцены
        yield return CutsceneManager.Instance.PlayCutscene(introA);
        yield return CutsceneManager.Instance.PlayCutscene(introB);

        // Удаляем CutsceneManager перед загрузкой уровня
        CutsceneManager.DestroyInstance();

        // Загружаем первый уровень
        SceneManager.LoadScene("Level_1");
    }

    void ShowTutorialPrompt()
    {
        LoadScene("Tutorial");
    }

    void LoadScene(string sceneName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }
        SceneManager.LoadScene(sceneName);
    }

    string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds <= 0) return "--:--";
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    // === ОЧИСТКА ПРИ УНИЧТОЖЕНИИ ===
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        FirebaseRestManager.OnAuthStateChanged -= OnFirebaseAuthChanged;
    }
}