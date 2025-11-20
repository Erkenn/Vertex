using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance;

    [Header("=== НАСТРОЙКИ ВЗЛОМА ===")]
    public float hackDuration = 3f;
    public float hackRadius = 10f;
    public GameObject hackEffectPrefab;
    public Color hackColor = new Color(0.2f, 0.8f, 1f, 1f);

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public Material stunMaterial;
    public AudioClip hackSound;
    public AudioClip shieldSound;

    private AudioSource audioSource;
    private List<Enemy> stunnedEnemies = new List<Enemy>();

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void ActivateHack(float duration)
    {
        StartCoroutine(PerformHack(duration));
    }

    private IEnumerator PerformHack(float duration)
    {
        // 1. Найти всех врагов в радиусе
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(GetPlayerPosition(), hackRadius);
        stunnedEnemies.Clear();

        // 2. Оглушить каждого врага
        foreach (var collider in hitColliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && enemy.isActive)
            {
                enemy.GetStunned(duration);
                stunnedEnemies.Add(enemy);

                // Визуальный эффект оглушения
                ApplyStunEffect(enemy.gameObject);
            }

            // Также оглушить турели
            Turret turret = collider.GetComponent<Turret>();
            if (turret != null && turret.isActive)
            {
                turret.GetStunned(duration);
                ApplyStunEffect(turret.gameObject);
            }
        }

        // 3. Визуальные эффекты взлома
        CreateHackVisualEffect();

        // 4. Звуковой эффект
        PlayHackSound();

        // 5. Уведомление системы достижений
        PlayerPrefs.SetInt("HacksUsed", PlayerPrefs.GetInt("HacksUsed", 0) + 1);

        Debug.Log($"⚡ Взлом активирован! Оглушено врагов: {stunnedEnemies.Count}");

        // 6. Ждем длительность оглушения
        yield return new WaitForSeconds(duration);

        // 7. Снимаем эффекты
        RemoveStunEffects();
    }

    private Vector2 GetPlayerPosition()
    {
        // ИСПРАВЛЕННЫЙ КОД - замена устаревшего метода
        PlayerController player = FindFirstObjectByType<PlayerController>(); // ЗАМЕНА FindObjectOfType
        return player != null ? player.transform.position : Vector2.zero;
    }

    private void ApplyStunEffect(GameObject target)
    {
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            // Сохраняем оригинальный материал и применяем статический эффект
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.originalMaterial = renderer.material;
                renderer.material = stunMaterial;
                renderer.color = hackColor;
            }
        }

        // Добавляем компонент мигания для визуального эффекта
        StunEffect stunEffect = target.GetComponent<StunEffect>();
        if (stunEffect == null)
            stunEffect = target.AddComponent<StunEffect>();

        stunEffect.StartStunEffect(hackColor);
    }

    private void CreateHackVisualEffect()
    {
        if (hackEffectPrefab != null)
        {
            GameObject effect = Instantiate(hackEffectPrefab, GetPlayerPosition(), Quaternion.identity);
            effect.transform.localScale = Vector3.one * hackRadius * 2f;

            // Автоматическое уничтожение эффекта
            Destroy(effect, 2f);
        }

        // Импульс неонового света на всех врагах
        foreach (var enemy in stunnedEnemies)
        {
            if (enemy != null)
                StartCoroutine(NeonPulse(enemy.gameObject));
        }
    }

    private IEnumerator NeonPulse(GameObject target)
    {
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color original = renderer.color;
            for (int i = 0; i < 3; i++)
            {
                renderer.color = hackColor;
                yield return new WaitForSeconds(0.1f);
                renderer.color = original;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void PlayHackSound()
    {
        if (audioSource != null && hackSound != null)
        {
            audioSource.PlayOneShot(hackSound);
        }
    }

    private void RemoveStunEffects()
    {
        foreach (var enemy in stunnedEnemies)
        {
            if (enemy != null)
            {
                SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
                if (renderer != null && enemy.originalMaterial != null)
                {
                    renderer.material = enemy.originalMaterial;
                    renderer.color = Color.white;
                }

                StunEffect stunEffect = enemy.GetComponent<StunEffect>();
                if (stunEffect != null)
                    stunEffect.StopStunEffect();
            }
        }
        stunnedEnemies.Clear();
    }

    public void ActivateShield(GameObject player)
    {
        // Активация визуального эффекта щита на игроке
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Звуковой эффект щита
            if (audioSource != null && shieldSound != null)
            {
                audioSource.PlayOneShot(shieldSound);
            }

            Debug.Log("🛡️ Активна защита щита");
        }
    }

    // Восстановление зарядов способностей при сборе данных
    public void RestoreAbilityCharges(int amount, PlayerController player)
    {
        if (player != null)
        {
            player.RestoreAbilityCharges(amount);
        }
    }
}

// Вспомогательный класс для эффекта оглушения
public class StunEffect : MonoBehaviour
{
    private Coroutine stunCoroutine;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Material originalMaterial;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void StartStunEffect(Color stunColor)
    {
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalMaterial = spriteRenderer.material;
        }

        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunFlash(stunColor));
    }

    private IEnumerator StunFlash(Color stunColor)
    {
        while (true)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = stunColor;
                yield return new WaitForSeconds(0.3f);
                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                yield break;
            }
        }
    }

    public void StopStunEffect()
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.material = originalMaterial;
        }
    }
}