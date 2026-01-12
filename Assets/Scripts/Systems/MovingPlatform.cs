using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 2f;
    public bool pingPong = false;

    // Активация
    public bool requireActivation = false;
    [SerializeField] private bool isActive = false;

    // Визуал
    [Header("Визуальные индикаторы")]
    public SpriteRenderer platformRenderer;
    public Color activeColor = new Color(0.3f, 0.7f, 1f, 1f); // Синий
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Серый

    private int currentWaypoint = 0;
    private bool isReversing = false;
    private Vector3 lastPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        lastPosition = transform.position;
        GetComponent<Rigidbody2D>().isKinematic = true;

        if (platformRenderer == null)
        {
            platformRenderer = GetComponent<SpriteRenderer>();
        }

        // Если не требуется активация - сразу активна
        if (!requireActivation)
        {
            isActive = true;
        }

        UpdateVisual();
    }

    void FixedUpdate()
    {
        if (!isActive || waypoints.Length == 0) return;

        Vector3 positionBeforeMove = transform.position;
        Transform target = waypoints[currentWaypoint];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.fixedDeltaTime);

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

        currentVelocity = (transform.position - positionBeforeMove) / Time.fixedDeltaTime;
    }

    public Vector2 GetPlatformVelocity() => currentVelocity;

    public void ActivatePlatform()
    {
        isActive = true;
        UpdateVisual();
        Debug.Log($"🔄 Платформа {gameObject.name} активирована");
    }

    public void DeactivatePlatform()
    {
        isActive = false;
        UpdateVisual();
        Debug.Log($"🔄 Платформа {gameObject.name} остановлена");
    }

    public void TogglePlatform()
    {
        isActive = !isActive;
        UpdateVisual();
        Debug.Log($"🔄 Платформа {gameObject.name}: {(isActive ? "включена" : "выключена")}");
    }

    void UpdateVisual()
    {
        if (platformRenderer != null)
        {
            platformRenderer.color = isActive ? activeColor : inactiveColor;
        }
    }

    // Для отладки
    void OnDrawGizmos()
    {
        if (platformRenderer != null)
        {
            Gizmos.color = isActive ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        }
    }
}