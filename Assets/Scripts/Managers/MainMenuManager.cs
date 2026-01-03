using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text;

public class MainMenuManager : MonoBehaviour
{
    // === ПАНЕЛИ ===
    public GameObject helpPanel;
    public GameObject accountPanel;
    public GameObject statsPanel;

    // === ИНФОРМАЦИОННЫЕ ПОДПАНЕЛИ (внутри HelpPanel) ===
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
    // Account Panel - кнопки открытия форм
    public Button loginButton;        // ← открывает LoginFields
    public Button registerButton;     // ← открывает RegisterFields
    public Button viewStatsButton;
    public Button backToMenuButton;

    // Кнопки отправки данных (внутри форм)
    public Button submitLoginButton;   // ← НОВАЯ: кнопка "Войти" внутри LoginFields
    public Button submitRegisterButton; // ← НОВАЯ: кнопка "Зарегистрироваться" внутри RegisterFields

    // Help Panel навигация
    public Button controlsButton;
    public Button abilitiesButton;
    public Button enemiesButton;
    public Button contactsButton;
    public Button helpExitButton;

    // Кнопки "Назад"
    public Button backFromStatsButton;

    // === ВНУТРЕННИЕ ПАНЕЛИ АККАУНТА ===
    public GameObject loginFields;
    public GameObject registerFields;

    private GameObject currentHelpSection;

    void Start()
    {
        SetupUI();
        UpdateAuthStatus();

        // При старте показываем форму входа
        ShowLoginFields();
    }

    void SetupUI()
    {
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

        // Кнопки в HelpPanel
        if (controlsButton != null) controlsButton.onClick.AddListener(() => ShowHelpSection(controlsInfo));
        if (abilitiesButton != null) abilitiesButton.onClick.AddListener(() => ShowHelpSection(abilityInfo));
        if (enemiesButton != null) enemiesButton.onClick.AddListener(() => ShowHelpSection(enemiesInfo));
        if (contactsButton != null) contactsButton.onClick.AddListener(() => ShowHelpSection(contactsInfo));
        if (helpExitButton != null) helpExitButton.onClick.AddListener(HideHelpPanel);

        // Кнопки "Назад"
        if (backFromStatsButton != null) backFromStatsButton.onClick.AddListener(HideStatsPanel);
    }

    // === УПРАВЛЕНИЕ КНОПКАМИ ГЛАВНОГО МЕНЮ ===
    void DisableMainButtons()
    {
        if (startGameButton != null) startGameButton.interactable = false;
        if (helpButton != null) helpButton.interactable = false;
        if (accountButton != null) accountButton.interactable = false;
    }

    void EnableMainButtons()
    {
        if (startGameButton != null) startGameButton.interactable = true;
        if (helpButton != null) helpButton.interactable = true;
        if (accountButton != null) accountButton.interactable = true;
    }

    // === ПАНЕЛЬ ПОМОЩИ ===
    public void ShowHelpPanel()
    {
        helpPanel.SetActive(true);
        DisableMainButtons();

        // Показываем первую секцию по умолчанию
        if (controlsInfo != null)
        {
            ShowHelpSection(controlsInfo);
        }
        else
        {
            HideAllHelpSections();
        }
    }

    public void HideHelpPanel()
    {
        helpPanel.SetActive(false);
        EnableMainButtons();
        HideAllHelpSections();
        currentHelpSection = null;
    }

    void ShowHelpSection(GameObject section)
    {
        // Сначала скрываем ВСЕ секции
        HideAllHelpSections();

        // Затем показываем нужную
        if (section != null)
        {
            Debug.Log($"Показываем секцию: {section.name}");
            section.SetActive(true);
            currentHelpSection = section;
        }
    }

    void HideAllHelpSections()
    {
        // Явно скрываем КАЖДУЮ секцию
        if (controlsInfo != null) controlsInfo.SetActive(false);
        if (abilityInfo != null) abilityInfo.SetActive(false);
        if (enemiesInfo != null) enemiesInfo.SetActive(false);
        if (contactsInfo != null) contactsInfo.SetActive(false);
    }

    // === ПАНЕЛЬ АККАУНТА ===
    public void ShowAccountPanel()
    {
        accountPanel.SetActive(true);
        DisableMainButtons();
        UpdateAuthStatus();
        ShowLoginFields(); // Показываем форму входа при открытии панели
    }

    public void HideAccountPanel()
    {
        accountPanel.SetActive(false);
        EnableMainButtons();
    }

    // === ФОРМЫ ВХОДА И РЕГИСТРАЦИИ ===
    public void ShowLoginFields()
    {
        if (loginFields != null) loginFields.SetActive(true);
        if (registerFields != null) registerFields.SetActive(false);
    }

    public void ShowRegisterFields()
    {
        if (loginFields != null) loginFields.SetActive(false);
        if (registerFields != null) registerFields.SetActive(true);
    }

    // === ПАНЕЛЬ СТАТИСТИКИ ===
    public void ShowStatsPanel()
    {
        statsPanel.SetActive(true);
        DisableMainButtons();
        UpdateStatsContent();
    }

    public void HideStatsPanel()
    {
        statsPanel.SetActive(false);
        EnableMainButtons();
    }

    // === ОСНОВНЫЕ ДЕЙСТВИЯ ===
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
                StartNewGame();
            }
        }
        else
        {
            ShowAccountPanel();
        }
    }

    void OnLogin()
    {
        string email = loginEmailInput?.text ?? "";
        string password = loginPasswordInput?.text ?? "";

        Debug.Log($"Вход: email='{email}', password='{password}'");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            authStatusText.text = "Заполните email и пароль";
            authStatusText.color = Color.red;
            return;
        }

        FirebaseRestManager.Instance.SignIn(email, password,
            () => {
                PlayerPrefs.SetString("PlayerEmail", email);
                PlayerPrefs.Save();
                UpdateAuthStatus();
                UpdateStatsContent();

                bool hasTutorial = PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 1;
                if (!hasTutorial)
                {
                    ShowTutorialPrompt();
                }
                else
                {
                    HideAccountPanel();
                }
            },
            (error) => {
                authStatusText.text = $"Ошибка входа: {error}";
                authStatusText.color = Color.red;
            }
        );
    }

    void OnRegister()
    {
        string email = registerEmailInput?.text ?? "";
        string password = registerPasswordInput?.text ?? "";
        string passwordRep = registerPasswordRepInput?.text ?? "";

        Debug.Log($"Регистрация: email='{email}', password='{password}', confirm='{passwordRep}'");

        if (string.IsNullOrEmpty(email) || password.Length < 6)
        {
            authStatusText.text = "Пароль должен быть минимум 6 символов";
            authStatusText.color = Color.red;
            return;
        }

        if (password != passwordRep)
        {
            authStatusText.text = "Пароли не совпадают";
            authStatusText.color = Color.red;
            return;
        }

        FirebaseRestManager.Instance.SignUp(email, password,
            () => {
                PlayerPrefs.SetString("PlayerEmail", email);
                PlayerPrefs.SetInt("HasCompletedTutorial", 0);
                PlayerPrefs.Save();
                LoadScene("Tutorial");
            },
            (error) => {
                authStatusText.text = $"Ошибка регистрации: {error}";
                authStatusText.color = Color.red;
            }
        );
    }

    void OnViewStats()
    {
        if (FirebaseRestManager.Instance != null && FirebaseRestManager.Instance.IsAuthenticated)
        {
            ShowStatsPanel();
        }
        else
        {
            authStatusText.text = "Сначала войдите в аккаунт";
            authStatusText.color = Color.red;
        }
    }

    // === СТАТИСТИКА ===
    void UpdateStatsContent()
    {
        if (statsContent == null)
        {
            Debug.LogError("statsContent не назначен в инспекторе!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b><color=#33ccff>ЛИЧНАЯ СТАТИСТИКА</color></b>\n");

        bool hasAnyData = false;
        for (int level = 1; level <= 5; level++)
        {
            int coins = PlayerPrefs.GetInt($"Level{level}_Coins", 0);
            float time = PlayerPrefs.GetFloat($"Level{level}_Time", 0f);
            int packets = PlayerPrefs.GetInt($"Level{level}_Packets", 0);

            if (coins > 0 || time > 0 || packets > 0)
            {
                hasAnyData = true;
                sb.AppendLine($"<b>Уровень {level}</b>");
                sb.AppendLine($"  Монеты: {coins}");
                sb.AppendLine($"  Данные: {packets}");
                if (time > 0)
                    sb.AppendLine($"  Время: {FormatTime(time)}");
                sb.AppendLine("");
            }
        }

        if (!hasAnyData)
        {
            sb.AppendLine("Нет сохранённой статистики.");
        }

        statsContent.text = sb.ToString();
    }

    // === АВТОРИЗАЦИЯ ===
    void UpdateAuthStatus()
    {
        if (FirebaseRestManager.Instance == null)
        {
            if (authStatusText != null)
                authStatusText.text = "Firebase не готов";
            return;
        }

        if (authStatusText == null) return;

        if (FirebaseRestManager.Instance.IsAuthenticated)
        {
            string email = PlayerPrefs.GetString("PlayerEmail", "user");
            authStatusText.text = $"Авторизован: {email}";
            authStatusText.color = new Color(0.2f, 1f, 0.4f);
            if (viewStatsButton != null)
            {
                viewStatsButton.interactable = true;
            }
        }
        else
        {
            authStatusText.text = "Не авторизован";
            authStatusText.color = Color.red;
            if (viewStatsButton != null)
            {
                viewStatsButton.interactable = false;
            }
        }
    }

    // === ОБУЧЕНИЕ И ЗАГРУЗКА ===
    void ShowTutorialPrompt()
    {
        LoadScene("Tutorial");
    }

    void StartNewGame()
    {
        PlayerPrefs.DeleteKey("CurrentLevel");
        PlayerPrefs.DeleteKey("CoinsCollected");
        PlayerPrefs.DeleteKey("DataPackets");
        PlayerPrefs.SetInt("HasCompletedTutorial", 1);
        PlayerPrefs.Save();
        LoadScene("Level_1");
    }

    void LoadScene(string sceneName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        if (sceneName == "Level_1" || sceneName == "Tutorial")
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"Сцена '{sceneName}' не добавлена в Build Settings!");
        }
    }

    string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds <= 0) return "--:--";
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}