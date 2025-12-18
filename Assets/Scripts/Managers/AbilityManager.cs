using UnityEngine;
using System.Collections;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance;

    [Header("=== НАСТРОЙКИ ВЗЛОМА ===")]
    public float hackDuration = 5.5f;  // Длительность оглушения
    public float hackRadius = 900f;     // Радиус действия
    public LayerMask hackableLayers;  // Слои, на которые действует взлом

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("⚡ AbilityManager инициализирован");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Публичный метод для активации взлома извне
    public void ActivateHack()
    {
        StartCoroutine(HackRoutine());
    }

    private IEnumerator HackRoutine()
    {
        // 1. Найти всех врагов в радиусе
        Collider2D[] colliders = Physics2D.OverlapCircleAll(GetPlayerPosition(), hackRadius, hackableLayers);

        int stunnedCount = 0;
        foreach (Collider2D col in colliders)
        {
            // Ищем врагов
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null && enemy.isActive)
            {
                enemy.GetStunned(hackDuration);
                stunnedCount++;
            }
        }

        Debug.Log($"⚡ Взлом активирован! Оглушено врагов: {stunnedCount}");

        // 2. Ждём окончания действия
        yield return new WaitForSeconds(hackDuration);

        Debug.Log("⚡ Взлом завершён");
    }

    private Vector2 GetPlayerPosition()
    {
        PlayerController player = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
        return player != null ? player.transform.position : Vector2.zero;
    }
}