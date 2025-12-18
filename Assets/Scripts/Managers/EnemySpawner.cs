using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("=== ПРЕФАБЫ ВРАГОВ ===")]
    public GameObject scannerPrefab;
    // public GameObject guardianPrefab; // Закомментируйте, если нет таких врагов
    // public GameObject turretPrefab;   // Закомментируйте, если нет таких врагов

    [Header("=== НАСТРОЙКИ СПАВНА ===")]
    public Transform[] spawnAreas;
    public float spawnInterval = 5f;
    public int maxEnemiesPerWave = 10;

    [Header("=== ТЕКУЩАЯ ВОЛНА ===")]
    public int currentWave = 1;
    public int enemiesAlive = 0;
    public int enemiesSpawnedThisWave = 0;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private LevelManager.LevelConfig currentLevelConfig;
    private bool isSpawning = false;
    private Coroutine spawningCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("👹 EnemySpawner инициализирован");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ConfigureSpawner(LevelManager.LevelConfig config)
    {
        if (config == null)
        {
            Debug.LogError("EnemySpawner: Config is null!");
            return;
        }

        currentLevelConfig = config;
        currentWave = 1;
        enemiesAlive = 0;
        enemiesSpawnedThisWave = 0;

        // Очистка предыдущих врагов
        ClearAllEnemies();

        // Начальный спавн врагов
        if (currentLevelConfig.enemyCount > 0)
        {
            SpawnInitialEnemies(config);
        }

        // Запуск волнового спавна
        if (spawningCoroutine != null)
            StopCoroutine(spawningCoroutine);

        spawningCoroutine = StartCoroutine(WaveSpawningRoutine());
    }

    void SpawnInitialEnemies(LevelManager.LevelConfig config)
    {
        int enemiesToSpawn = Mathf.Min(config.enemyCount, maxEnemiesPerWave);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }

        enemiesSpawnedThisWave = enemiesToSpawn;
        Debug.Log($"👹 Начальный спавн: {enemiesToSpawn} врагов");
    }

    IEnumerator WaveSpawningRoutine()
    {
        isSpawning = true;

        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Проверяем нужно ли спавнить новых врагов
            if (enemiesAlive < maxEnemiesPerWave / 2 &&
                enemiesSpawnedThisWave < currentLevelConfig.enemyCount)
            {
                SpawnEnemy();
            }

            // Проверяем завершение волны
            if (enemiesAlive == 0 && enemiesSpawnedThisWave >= currentLevelConfig.enemyCount)
            {
                CompleteWave();
            }
        }
    }

    void SpawnEnemy()
    {
        if (currentLevelConfig == null)
        {
            Debug.LogError("EnemySpawner: No level config set!");
            return;
        }

        if (spawnAreas == null || spawnAreas.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: Нет зон спавна!");
            return;
        }

        // Выбираем случайную зону спавна
        Transform spawnArea = spawnAreas[Random.Range(0, spawnAreas.Length)];
        Vector2 spawnPosition = GetRandomSpawnPosition(spawnArea);

        // Выбираем тип врага в зависимости от уровня и доступности
        GameObject enemyToSpawn = SelectEnemyType();

        if (enemyToSpawn != null)
        {
            GameObject enemy = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
            enemy.transform.SetParent(transform); // Организуем иерархию

            // Настройка врага
            Enemy enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                // Подписываемся на событие смерти врага
                enemyComponent.OnEnemyDestroyed += OnEnemyDestroyed;
                enemyComponent.isActive = true;
            }

            activeEnemies.Add(enemy);
            enemiesAlive++;
            enemiesSpawnedThisWave++;

            Debug.Log($"👹 Заспавнен {enemy.name} на позиции {spawnPosition}");
        }
    }

    GameObject SelectEnemyType()
    {
        if (currentLevelConfig == null) return null;

        // Если нет доступных врагов
        if (!currentLevelConfig.hasScanners) // && !currentLevelConfig.hasGuardians && !currentLevelConfig.hasTurrets
            return null;

        // Веса спавна в зависимости от уровня
        float[] weights = CalculateSpawnWeights();

        float randomValue = Random.Range(0f, weights[2]);

        if (randomValue < weights[0] && currentLevelConfig.hasScanners && scannerPrefab != null)
            return scannerPrefab;
        // else if (randomValue < weights[1] && currentLevelConfig.hasGuardians && guardianPrefab != null)
        //     return guardianPrefab;
        // else if (currentLevelConfig.hasTurrets && turretPrefab != null)
        //     return turretPrefab;

        // Fallback
        if (currentLevelConfig.hasScanners && scannerPrefab != null)
            return scannerPrefab;
        // else if (currentLevelConfig.hasGuardians && guardianPrefab != null)
        //     return guardianPrefab;
        // else if (currentLevelConfig.hasTurrets && turretPrefab != null)
        //     return turretPrefab;

        return null;
    }

    float[] CalculateSpawnWeights()
    {
        if (currentLevelConfig == null) return new float[] { 1, 2, 3 };

        float scannerWeight = 0f;
        float guardianWeight = 0f;
        float turretWeight = 0f;

        // Настройка весов в зависимости от уровня
        switch (currentLevelConfig.levelName)
        {
            case "Внешний Периметр":
                scannerWeight = 1f;
                break;
            case "Архив":
                scannerWeight = 0.6f;
                guardianWeight = 0.4f;
                break;
            case "Серверная":
                scannerWeight = 0.4f;
                guardianWeight = 0.4f;
                turretWeight = 0.2f;
                break;
            case "Путь к Ядру":
                scannerWeight = 0.3f;
                guardianWeight = 0.4f;
                turretWeight = 0.3f;
                break;
            case "Центральное Ядро":
                scannerWeight = 0.2f;
                guardianWeight = 0.4f;
                turretWeight = 0.4f;
                break;
            default:
                // Если имя уровня не распознано, используем стандартные веса
                scannerWeight = 0.5f;
                guardianWeight = 0.3f;
                turretWeight = 0.2f;
                break;
        }

        return new float[] { scannerWeight, scannerWeight + guardianWeight, scannerWeight + guardianWeight + turretWeight };
    }

    Vector2 GetRandomSpawnPosition(Transform spawnArea)
    {
        // Получаем случайную позицию в зоне спавна
        Collider2D spawnCollider = spawnArea.GetComponent<Collider2D>();
        if (spawnCollider != null)
        {
            Bounds bounds = spawnCollider.bounds;
            return new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );
        }

        // Fallback - позиция трансформа
        return spawnArea.position;
    }

    void OnEnemyDestroyed()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0 && enemiesSpawnedThisWave >= currentLevelConfig.enemyCount)
        {
            CompleteWave();
        }
    }

    void CompleteWave()
    {
        currentWave++;
        enemiesSpawnedThisWave = 0;

        Debug.Log($"🎉 Волна {currentWave - 1} завершена! Начинается волна {currentWave}");

        // Увеличиваем сложность
        IncreaseDifficulty();

        // Перезапускаем спавн
        if (spawningCoroutine != null)
            StopCoroutine(spawningCoroutine);

        spawningCoroutine = StartCoroutine(WaveSpawningRoutine());
    }

    void IncreaseDifficulty()
    {
        if (currentLevelConfig == null) return;

        // Увеличиваем количество врагов и скорость спавна
        currentLevelConfig.enemyCount = Mathf.RoundToInt(currentLevelConfig.enemyCount * 1.3f);
        spawnInterval = Mathf.Max(1f, spawnInterval * 0.9f);

        Debug.Log($"📈 Сложность увеличена: врагов {currentLevelConfig.enemyCount}, интервал {spawnInterval:F1}с");
    }

    public void ClearAllEnemies()
    {
        if (activeEnemies == null) return;

        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Enemy enemyComponent = enemy.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    enemyComponent.OnEnemyDestroyed -= OnEnemyDestroyed;
                }
                Destroy(enemy);
            }
        }

        activeEnemies.Clear();
        enemiesAlive = 0;
        enemiesSpawnedThisWave = 0;

        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }

        isSpawning = false;
    }

    public void StopSpawning()
    {
        isSpawning = false;
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
    }

    void OnDestroy()
    {
        ClearAllEnemies();
    }
}