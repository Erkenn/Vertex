using UnityEngine;
using System.Collections;

public class DataPacket : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ПАКЕТА ===")]
    public int value = 1;
    public float rotationSpeed = 90f;
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 1f;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public ParticleSystem collectParticles;
    public Light pointLight;

    // Событие сбора
    public System.Action<DataPacket, Vector3> OnCollected;

    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private Color packetColor;
    private bool isCollected = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        // Настройка освещения
        if (pointLight != null)
        {
            pointLight.color = packetColor;
        }
    }

    public void Initialize(Color color, int packetValue)
    {
        packetColor = color;
        value = packetValue;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    void Update()
    {
        if (isCollected) return;

        // Анимация парящего движения
        FloatAnimation();

        // Вращение
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    void FloatAnimation()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    void Collect()
    {
        isCollected = true;

        // Запускаем эффекты сбора
        StartCoroutine(CollectAnimation());

        // Вызываем событие сбора
        OnCollected?.Invoke(this, transform.position);
    }

    IEnumerator CollectAnimation()
    {
        // Визуальные эффекты
        if (collectParticles != null)
        {
            collectParticles.Play();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (pointLight != null)
        {
            pointLight.enabled = false;
        }

        // Ждем завершения эффектов
        yield return new WaitForSeconds(1f);

        // Уничтожаем объект
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        // Визуализация в редакторе
        Gizmos.color = packetColor;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}