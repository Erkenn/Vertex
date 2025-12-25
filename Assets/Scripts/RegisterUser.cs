using UnityEngine;

public class RegisterUser : MonoBehaviour
{
    // ВРЕМЕННЫЕ ДАННЫЕ ДЛЯ ТЕСТИРОВАНИЯ
    public string testEmail = "player1@test.com";
    public string testPassword = "password123";

    void Start()
    {
        if (FirebaseRestManager.Instance == null)
        {
            Debug.LogError("🔥 FirebaseRestManager не найден!");
            return;
        }

        FirebaseRestManager.Instance.SignUp(
            testEmail,
            testPassword,
            () => {
                Debug.Log("✅ Регистрация успешна!");

                // 🔥 ВРЕМЕННО: сохраняем тестовый прогресс
                FirebaseRestManager.Instance.SaveLevelProgress(1, 3, 45.5f);
                FirebaseRestManager.Instance.SavePlayerName("Тестер");

                Destroy(this);
            },
            (error) => {
                Debug.LogError($"❌ Ошибка регистрации: {error}");
            }
        );
    }
}