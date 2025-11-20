using UnityEngine;
using System.Collections;

public class PerformanceOptimizer : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ОПТИМИЗАЦИИ ===")]
    public bool enableOptimization = true;
    public int targetFrameRate = 60;
    public float gcCollectionInterval = 30f;
    public bool enableObjectPooling = true;

    [Header("=== МОНИТОРИНГ ПРОИЗВОДИТЕЛЬНОСТИ ===")]
    public float currentFPS;
    public float memoryUsage;
    public bool isPerformanceGood = true;

    private float fpsRefreshTime = 0.5f;
    // УДАЛЕНЫ неиспользуемые переменные: frameCount и timer
    private Coroutine gcCoroutine;

    void Awake()
    {
        if (!enableOptimization) return;

        SetupPerformance();
        StartMonitoring();
    }

    void SetupPerformance()
    {
        // Установка целевого FPS
        Application.targetFrameRate = targetFrameRate;

        // Настройка качества графики в runtime
        QualitySettings.vSyncCount = 0;

        // Запуск периодической сборки мусора
        if (gcCollectionInterval > 0)
        {
            gcCoroutine = StartCoroutine(GarbageCollectionRoutine());
        }

        Debug.Log("⚡ PerformanceOptimizer активирован");
    }

    void StartMonitoring()
    {
        InvokeRepeating("UpdatePerformanceStats", 0f, fpsRefreshTime);
    }

    void UpdatePerformanceStats()
    {
        // Расчет FPS
        currentFPS = 1f / Time.unscaledDeltaTime;

        // Использование памяти
        memoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f;

        // Проверка производительности
        isPerformanceGood = currentFPS >= 55f && memoryUsage < 512f;

        if (!isPerformanceGood)
        {
            ApplyPerformanceMeasures();
        }
    }

    void ApplyPerformanceMeasures()
    {
        if (currentFPS < 45f)
        {
            // Снижение качества при низком FPS
            QualitySettings.SetQualityLevel(Mathf.Max(0, QualitySettings.GetQualityLevel() - 1));
            Debug.Log("📉 FPS низкий, качество снижено");
        }

        if (memoryUsage > 512f)
        {
            // Принудительная сборка мусора
            System.GC.Collect();
            Debug.Log("🧹 Высокое использование памяти, сборка мусора");
        }
    }

    IEnumerator GarbageCollectionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(gcCollectionInterval);
            System.GC.Collect();
        }
    }

    public void OptimizeForBossFight()
    {
        // Специальная оптимизация для битвы с боссом
        QualitySettings.SetQualityLevel(1);
        Application.targetFrameRate = 60;
        Debug.Log("🎯 Оптимизация для битвы с боссом активирована");
    }

    public void RestoreNormalSettings()
    {
        // Восстановление нормальных настроек
        int savedQuality = PlayerPrefs.GetInt("QualityLevel", 2);
        QualitySettings.SetQualityLevel(savedQuality);
        Application.targetFrameRate = targetFrameRate;
    }

    void OnDestroy()
    {
        if (gcCoroutine != null)
            StopCoroutine(gcCoroutine);
    }
}