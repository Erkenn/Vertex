using UnityEngine;

public class AbilityRechargeZone : MonoBehaviour
{
    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public GameObject rechargeEffect;
    public AudioClip rechargeSound;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.RestoreAbilityCharges(999);

                if (rechargeEffect != null)
                {
                    Instantiate(rechargeEffect, transform.position, Quaternion.identity);
                }
                if (rechargeSound != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(rechargeSound.name);
                }

                Debug.Log("🔋 Все способности восполнены!");

                Destroy(gameObject);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Рисуем зону в редакторе
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(transform.position, new Vector3(3, 3, 1));
    }
}