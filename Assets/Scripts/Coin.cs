using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1; // Сколько монет даёт при подборе

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Коснулся объекта с тегом: " + other.tag);

        if (other.CompareTag("Player"))
        {
            Debug.Log("✅ Игрок подобрал монетку!");
            Destroy(gameObject);
        }
    }
}