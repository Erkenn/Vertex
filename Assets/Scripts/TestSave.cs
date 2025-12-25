using UnityEngine;

public class TestSave : MonoBehaviour
{
    void Start()
    {
        if (FirebaseRestManager.Instance.IsAuthenticated)
        {
            FirebaseRestManager.Instance.SaveLevelProgress(1, 3, 120.5f);
            Debug.Log("✅ Данные отправлены в Firebase");
        }
        else
        {
            Debug.Log("❌ Нет авторизации");
        }
    }
}