using UnityEngine;

public class BossActivationTrigger : MonoBehaviour
{
    public CoreBoss boss;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && boss != null)
        {
            boss.ActivateBoss();
            Destroy(gameObject);
        }
    }
}