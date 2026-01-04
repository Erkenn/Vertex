using UnityEngine;

public class UIManagerLoader : MonoBehaviour
{
    public GameObject uiManagerPrefab;

    void Awake()
    {
        // Если UIManager еще не существует
        if (UIManager.Instance == null)
        {
            if (uiManagerPrefab != null)
            {
                Instantiate(uiManagerPrefab);
                Debug.Log("✅ UIManager префаб загружен");
            }
            else
            {
                Debug.LogError("❌ UIManager префаб не назначен!");
            }
        }
    }
}
