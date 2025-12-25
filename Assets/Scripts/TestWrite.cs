using UnityEngine;

public class TestWrite : MonoBehaviour
{
    void Start()
    {
        if (FirebaseRestManager.Instance.IsAuthenticated)
        {
            // Сохраняем тестовые данные
            FirebaseRestManager.Instance.SaveLevelProgress(1, 3, 99.9f);
            FirebaseRestManager.Instance.SavePlayerName("Тестовый Игрок");
            FirebaseRestManager.Instance.SaveLeaderboardEntry(99.9f);

            Debug.Log("🚀 Данные отправлены в Firebase!");
        }
        else
        {
            Debug.Log("❌ Не авторизован — сначала зарегистрируйтесь");
        }
    }
}