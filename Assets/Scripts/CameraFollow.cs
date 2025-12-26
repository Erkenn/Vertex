using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0f, 5f, -10f);

    // Границы уровня (в мировых координатах)
    public float leftLimit = -10f;
    public float rightLimit = 50f;
    public float bottomLimit = -10f;
    public float topLimit = 15f;

    void LateUpdate()
    {
        if (player == null) return;

        // Вычисляем желаемую позицию камеры за игроком с учётом смещения
        Vector3 targetPosition = player.position + offset;

        // Ограничиваем только X и Y
        float clampedX = Mathf.Clamp(targetPosition.x, leftLimit, rightLimit);
        float clampedY = Mathf.Clamp(targetPosition.y, bottomLimit, topLimit);

        transform.position = new Vector3(clampedX, clampedY, offset.z);
    }
}