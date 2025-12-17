using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("=== HUD ЭЛЕМЕНТЫ ===")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI hackChargesText;
    public TextMeshProUGUI shieldChargesText;
    public TextMeshProUGUI dataPacketsText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI levelText;

    [Header("=== ИНДИКАТОРЫ СПОСОБНОСТЕЙ ===")]
    public Image hackCooldownOverlay;
    public Image shieldActiveIndicator;
    public Slider healthBar;

    [Header("=== МЕНЮ И ЭКРАНЫ ===")]
    public GameObject pauseMenu;
    public GameObject gameOverMenu;
    public GameObject victoryMenu;
    public GameObject hudPanel;

    [Header("=== МЕНЮ ПАУЗЫ ===")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public Button settingsButton;

    [Header("=== НЕОНОВЫЕ ЦВЕТА ===")]
    public Color neonBlue = new Color(0.2f, 0.6f, 1f, 1f);
    public Color neonGreen = new Color(0.2f, 1f, 0.4f, 1f);
    public Color neonRed = new Color(1f, 0.2f, 0.4f, 1f);
    public Color neonYellow = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("=== АНИМАЦИИ ===")]
    public Animator healthAnimator;
    public Animator abilitiesAnimator;

    private PlayerController player;
    private GameManager gameManager;

    public class Coin : MonoBehaviour
    {
        public int value = 1;
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                GameManager.Instance?.CollectCoin(value);
                Destroy(gameObject);
            }
        }
    }

    public void UpdateCoinsUI(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = $"МОНЕТ: {coins}";
            coinsText.color = neonYellow; // или любой цвет по желанию
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("📊 UIManager инициализирован");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FindGameReferences();
        InitializeUI();
        ApplyNeonStyling();
    }

    void Update()
    {
        if (gameManager != null && gameManager.isGameActive && !gameManager.isPaused)
        {
            UpdateDynamicUI();
        }
    }

    void FindGameReferences()
    {
        gameManager = GameManager.Instance;
        player = FindFirstObjectByType<PlayerController>();

        if (player == null)
            Debug.LogWarning("UIManager: PlayerController не найден");
        if (gameManager == null)
            Debug.LogWarning("UIManager: GameManager не найден");
    }

    void InitializeUI()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (gameOverMenu != null) gameOverMenu.SetActive(false);
        if (victoryMenu != null) victoryMenu.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        UpdateHealthUI(100);
        UpdateAbilitiesUI(3, 2);
        UpdateDataPacketsUI(0);
        UpdateLevelUI(1);
        SetupPauseMenuButtons();
    }

    void ApplyNeonStyling()
    {
        if (healthBar != null)
        {
            healthBar.fillRect.GetComponent<Image>().color = neonGreen;
        }

        if (hackCooldownOverlay != null)
        {
            hackCooldownOverlay.color = neonBlue;
        }

        if (shieldActiveIndicator != null)
        {
            shieldActiveIndicator.color = neonYellow;
        }
    }

    void UpdateDynamicUI()
    {
        if (timerText != null && gameManager != null)
        {
            timerText.text = FormatTime(gameManager.sessionTimer);
        }

        if (shieldActiveIndicator != null && player != null)
        {
            shieldActiveIndicator.gameObject.SetActive(player.isShieldActive);

            if (player.isShieldActive)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                shieldActiveIndicator.color = new Color(neonYellow.r, neonYellow.g, neonYellow.b, pulse * 0.8f + 0.2f);
            }
        }

        if (hackCooldownOverlay != null && player != null)
        {
            hackCooldownOverlay.fillAmount = 0f;
        }
    }

    public void UpdateHealthUI(int currentHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"ЗДОРОВЬЕ: {currentHealth}%";
            healthText.color = GetHealthColor(currentHealth);
        }

        if (healthBar != null)
        {
            healthBar.value = currentHealth / 100f;

            if (healthAnimator != null && currentHealth < 100)
            {
                healthAnimator.Play("HealthPulse");
            }
        }
    }

    public void UpdateAbilitiesUI(int hackCharges, int shieldCharges)
    {
        if (hackChargesText != null)
        {
            hackChargesText.text = $"ВЗЛОМ: {hackCharges}";
            hackChargesText.color = hackCharges > 0 ? neonBlue : neonRed;
        }

        if (shieldChargesText != null)
        {
            shieldChargesText.text = $"ЩИТ: {shieldCharges}";
            shieldChargesText.color = shieldCharges > 0 ? neonYellow : neonRed;
        }
    }

    public void UpdateDataPacketsUI(int packets)
    {
        if (dataPacketsText != null)
        {
            dataPacketsText.text = $"ДАННЫЕ: {packets}";

            if (packets > 0)
            {
                dataPacketsText.transform.localScale = Vector3.one * 1.2f;
                CancelInvoke("ResetDataPacketScale");
                Invoke("ResetDataPacketScale", 0.3f);
            }
        }
    }

    public void UpdateLevelUI(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"УРОВЕНЬ: {level}/5";
        }
    }

    // Показать и спятать меню паузы при нажатии кнопки паузы
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);
        }
    }

    public void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
        }
    }

    public void ShowGameOverMenu(string reason)
    {
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);

            TextMeshProUGUI reasonText = gameOverMenu.GetComponentInChildren<TextMeshProUGUI>();
            if (reasonText != null)
            {
                reasonText.text = reason;
                reasonText.color = neonRed;
            }
        }
    }

    public void ShowVictoryScreen(float completionTime, int dataPackets, int enemiesDestroyed)
    {
        if (victoryMenu != null)
        {
            victoryMenu.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);

            TextMeshProUGUI statsText = victoryMenu.GetComponentInChildren<TextMeshProUGUI>();
            if (statsText != null)
            {
                statsText.text = $"ВРЕМЯ: {FormatTime(completionTime)}\n" +
                               $"ДАННЫЕ: {dataPackets}\n" +
                               $"УНИЧТОЖЕНО: {enemiesDestroyed}";
            }
        }
    }

    void SetupPauseMenuButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeButtonClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButtonClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
    }

    public void OnResumeButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
            PlayUIClick();
        }
    }

    public void OnRestartButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
            PlayUIClick();
        }
    }

    public void OnMainMenuButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        PlayUIClick();
    }

    public void OnSettingsButtonClicked()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ShowSettings();
        }
        PlayUIClick();
    }

    public void ShowAchievementUnlocked(string achievementName, string description)
    {
        StartCoroutine(ShowAchievementPopup(achievementName, description));
    }

    private IEnumerator ShowAchievementPopup(string name, string description)
    {
        Debug.Log($"🏆 ДОСТИЖЕНИЕ: {name} - {description}");
        yield return new WaitForSeconds(2f);
    }

    public void ShowDeathScreen()
    {
        Debug.Log("💀 Показан экран смерти");
    }

    public void HideDeathScreen()
    {
        Debug.Log("💀 Скрыт экран смерти");
    }

    private Color GetHealthColor(int health)
    {
        if (health > 70) return neonGreen;
        if (health > 30) return neonYellow;
        return neonRed;
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private void ResetDataPacketScale()
    {
        if (dataPacketsText != null)
        {
            dataPacketsText.transform.localScale = Vector3.one;
        }
    }

    private void PlayUIClick()
    {
        Debug.Log("🔊 Воспроизведение звука клика UI");
    }
}