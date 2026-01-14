using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1; // Сколько монет даёт при подборе
    public AudioClip collectSound; // Звук сбора монетки

    private AudioSource audioSource;

    void Start()
    {
        // Получаем или создаем AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Настраиваем AudioSource
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D звук
        audioSource.maxDistance = 10f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Коснулся объекта с тегом: " + other.tag);

        if (other.CompareTag("Player"))
        {
            Debug.Log("✅ Игрок подобрал монетку!");

            // Воспроизводим звук сбора
            PlayCollectSound();

            // Уничтожаем монетку
            Destroy(gameObject, 0.1f); // Задержка чтобы звук успел сыграть
        }
    }

    void PlayCollectSound()
    {
        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
    }
}