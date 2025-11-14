using UnityEngine;
using System.Collections.Generic;

public class DataPacketSpawner : MonoBehaviour
{
    public static DataPacketSpawner Instance;

    [Header("=== ПРЕФАБЫ ===")]
    public GameObject dataPacketPrefab;

    [Header("=== НАСТРОЙКИ СПАВНА ===")]
    public Transform[] spawnPoints;
    public Color[] packetColors = new Color[]
    {
        new Color(0.2f, 0.8f, 1f, 1f),    // Синий
        new Color(0.2f, 1f, 0.4f, 1f),    // Зеленый
        new Color(1f, 0.8f, 0.2f, 1f),    // Желтый
        new Color(1f, 0.4f, 0.8f, 1f)     // Розовый
    };

    [Header("=== ЭФФЕКТЫ ===")]
    public GameObject collectEffect;
    public AudioClip collectSound;

    private List<GameObject> activePackets = new List<GameObject>();
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("💾 DataPacketSpawner инициализирован");
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void SpawnDataPackets(int count)
    {
        ClearAllPackets();

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("DataPacketSpawner: Нет точек спавна!");
            return;
        }

        if (dataPacketPrefab == null)
        {
            Debug.LogError("DataPacketSpawner: Не назначен префаб пакета данных!");
            return;
        }

        int packetsToSpawn = Mathf.Min(count, spawnPoints.Length);

        for (int i = 0; i < packetsToSpawn; i++)
        {
            SpawnDataPacket(i);
        }

        Debug.Log($"💾 Заспавнено {packetsToSpawn} пакетов данных");
    }

    void SpawnDataPacket(int index)
    {
        if (index >= spawnPoints.Length) return;

        Transform spawnPoint = spawnPoints[index];
        GameObject packet = Instantiate(dataPacketPrefab, spawnPoint.position, Quaternion.identity);
        packet.transform.SetParent(transform);

        // Настройка внешнего вида
        DataPacket packetScript = packet.GetComponent<DataPacket>();
        if (packetScript != null)
        {
            Color packetColor = packetColors[Random.Range(0, packetColors.Length)];
            packetScript.Initialize(packetColor, 1); // Каждый пакет дает 1 заряд

            // Подписываемся на событие сбора
            packetScript.OnCollected += OnDataPacketCollected;
        }

        activePackets.Add(packet);
    }

    void OnDataPacketCollected(DataPacket packet, Vector3 position)
    {
        // Визуальный эффект сбора
        if (collectEffect != null)
        {
            Instantiate(collectEffect, position, Quaternion.identity);
        }

        // Звуковой эффект
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }

        // Уведомление системы
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectDataPacket(packet.value);
        }

        // Удаляем из списка
        activePackets.Remove(packet.gameObject);

        Debug.Log("💾 Пакет данных собран!");
    }

    public void ClearAllPackets()
    {
        foreach (GameObject packet in activePackets)
        {
            if (packet != null)
            {
                DataPacket packetScript = packet.GetComponent<DataPacket>();
                if (packetScript != null)
                {
                    packetScript.OnCollected -= OnDataPacketCollected;
                }
                Destroy(packet);
            }
        }

        activePackets.Clear();
    }

    public int GetRemainingPackets()
    {
        return activePackets.Count;
    }

    void OnDestroy()
    {
        ClearAllPackets();
    }
}