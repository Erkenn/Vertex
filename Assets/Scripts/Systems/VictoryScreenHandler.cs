using UnityEngine;
using TMPro;

public class VictoryScreenHandler : MonoBehaviour
{
    [Header("Ссылки на UI элементы")]
    public TextMeshProUGUI statsText;

    private void Start()
    {
        UpdateStats();
    }

    public void UpdateStats()
    {
        if (statsText == null || GameManager.Instance == null) return;

        // Получаем лучшее время из PlayerPrefs
        float bestTime = PlayerPrefs.GetFloat("BestCompletionTime", Mathf.Infinity);
        string bestTimeStr = bestTime < Mathf.Infinity ? FormatTime(bestTime) : "Нет данных";

        statsText.text = $"<b>ФИНАЛЬНАЯ СТАТИСТИКА</b>\n\n" +
                        $"Общее время: {FormatTime(GameManager.Instance.sessionTimer)}\n" +
                        $"Лучшее время: {bestTimeStr}\n" +
                        $"Собрано данных: {GameManager.Instance.dataPacketsCollected}\n" +
                        $"Уничтожено врагов: {GameManager.Instance.enemiesDestroyed}\n\n" +
                        $"<i>Результат сохранён в облаке!</i>";
    }

    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}