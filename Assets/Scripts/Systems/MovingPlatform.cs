using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 2f;
    public bool pingPong = false;

    private int currentWaypoint = 0;
    private bool isReversing = false;
    private Vector3 lastPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        lastPosition = transform.position;
        GetComponent<Rigidbody2D>().isKinematic = true;
    }

    void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        // Сохраняем позицию ДО движения
        Vector3 positionBeforeMove = transform.position;

        // Движение
        Transform target = waypoints[currentWaypoint];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.fixedDeltaTime);

        // Проверка достижения точки
        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            if (pingPong && waypoints.Length > 1)
            {
                if (isReversing)
                {
                    currentWaypoint--;
                    if (currentWaypoint < 0)
                    {
                        currentWaypoint = 1;
                        isReversing = false;
                    }
                }
                else
                {
                    currentWaypoint++;
                    if (currentWaypoint >= waypoints.Length)
                    {
                        currentWaypoint = waypoints.Length - 2;
                        isReversing = true;
                    }
                }
            }
            else
            {
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
            }
        }

        // Вычисляем скорость за фиксированный кадр
        currentVelocity = (transform.position - positionBeforeMove) / Time.fixedDeltaTime;
    }

    public Vector2 GetPlatformVelocity() => currentVelocity;
}