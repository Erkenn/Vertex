using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Добавил эту директиву

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("=== HUD ЭЛЕМЕНТЫ ===")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI hackChargesText;
    public TextMeshProUGUI shieldChargesText;
    public TextMeshProUGUI dataPacketsText;
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

        // ИСПРАВЛЕННЫЙ КОД - замена устаревшего метода
        player = FindFirstObjectByType<PlayerController>(); // ЗАМЕНА FindObjectOfType

        if (player == null)
            Debug.LogWarning("UIManager: PlayerController не найден");
        if (gameManager == null)
            Debug.LogWarning("UIManager: GameManager не найден");
    }

    void InitializeUI()
    {
        // Скрыть все меню при старте
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (gameOverMenu != null) gameOverMenu.SetActive(false);
        if (victoryMenu != null) victoryMenu.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        // Инициализация текстов
        UpdateHealthUI(100);
        UpdateAbilitiesUI(3, 2);
        UpdateDataPacketsUI(0);
        UpdateLevelUI(1);
    }

    void ApplyNeonStyling()
    {
        // Применение неоновых эффектов к UI элементам
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
        // Обновление таймера
        if (timerText != null && gameManager != null)
        {
            timerText.text = FormatTime(gameManager.sessionTimer);
        }

        // Обновление индикатора щита
        if (shieldActiveIndicator != null && player != null)
        {
            shieldActiveIndicator.gameObject.SetActive(player.isShieldActive);

            // Пульсация при активном щите
            if (player.isShieldActive)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                shieldActiveIndicator.color = new Color(neonYellow.r, neonYellow.g, neonYellow.b, pulse * 0.8f + 0.2f);
            }
        }

        // Обновление перезарядки взлома
        if (hackCooldownOverlay != null && player != null)
        {
            // Здесь нужно получить текущее время перезарядки от PlayerController
            hackCooldownOverlay.fillAmount = 0f; // Временная реализация
        }
    }

    // === ОБНОВЛЕНИЕ ОСНОВНЫХ UI ЭЛЕМЕНТОВ ===

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

            // Анимация при получении урона
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

            // Анимация при сборе данных
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

    // === СИСТЕМА МЕНЮ И ЭКРАНОВ ===

    public void ShowPauseMenu()
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
            hudPanel.SetActive(false);
        }
    }

    public void HidePauseMenu()
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
            hudPanel.SetActive(true);
        }
    }

    public void ShowGameOverMenu(string reason)
    {
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(true);
            hudPanel.SetActive(false);

            // Установка причины поражения
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
            hudPanel.SetActive(false);

            // Заполнение статистики
            TextMeshProUGUI statsText = victoryMenu.GetComponentInChildren<TextMeshProUGUI>();
            if (statsText != null)
            {
                statsText.text = $"ВРЕМЯ: {FormatTime(completionTime)}\n" +
                               $"ДАННЫЕ: {dataPackets}\n" +
                               $"УНИЧТОЖЕНО: {enemiesDestroyed}";
            }
        }
    }

    public void ShowAchievementUnlocked(string achievementName, string description)
    {
        // Создание pop-up уведомления о достижении
        StartCoroutine(ShowAchievementPopup(achievementName, description));
    }

    private IEnumerator ShowAchievementPopup(string name, string description)
    {
        // Временная реализация - в будущем можно сделать префаб
        Debug.Log($"🏆 ДОСТИЖЕНИЕ: {name} - {description}");
        yield return new WaitForSeconds(2f);
    }

    public void ShowDeathScreen()
    {
        // Эффект затемнения экрана при смерти
        // Временная реализация
        Debug.Log("💀 Показан экран смерти");
    }

    public void HideDeathScreen()
    {
        // Скрытие эффекта смерти
        Debug.Log("💀 Скрыт экран смерти");
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

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

    // === ОБРАБОТЧИКИ UI СОБЫТИЙ ===

    public void OnResumeButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.TogglePause();
        }
    }

    public void OnRestartButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.StartNewGame();
        }
    }

    public void OnMainMenuButtonClicked()
    {
        // Загрузка главного меню
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}