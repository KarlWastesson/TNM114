using UnityEngine;
using System.Linq;

public class GhostMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Sprite lookUp;
    public Sprite lookDown;
    public Sprite lookLeft;
    public Sprite lookRight;
    public float speed = 3.0f;
    private Transform targetWaypoint;
    private float proximityThreshold = 0.05f;
    public Vector2Int ghostPosition;

    void Start()
    {
        SetNextWaypoint();

    }

    void Update()
    {
        Move();
        if (IsAtWaypoint())
        {
            SetNextWaypoint();
            ghostPosition = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(-transform.position.y));

        }
    }

    void Move()
    {
        if (targetWaypoint != null)
        {
            Vector3 direction = targetWaypoint.position - transform.position;

            // Move the ghost towards the target waypoint
            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);

            // Check if there's any movement to avoid unnecessary sprite change
            if (direction.magnitude > 0)
            {
                ChangeSprite(direction);
            }
        }
    }

    void ChangeSprite(Vector3 direction)
    {
        // If moving more horizontally
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            spriteRenderer.sprite = direction.x > 0 ? lookRight : lookLeft;
        }
        // If moving more vertically
        else
        {
            spriteRenderer.sprite = direction.y > 0 ? lookUp : lookDown;
        }
    }



    bool IsAtWaypoint()
    {
        return Vector3.Distance(transform.position, targetWaypoint.position) < proximityThreshold;
    }

    void SetNextWaypoint()
    {
        Collider2D[] adjacentColliders = Physics2D.OverlapCircleAll(transform.position, 1.0f);

        var adjacentWaypoints = adjacentColliders
            .Where(collider => collider.CompareTag("WayPoint") && collider.transform != transform)
            .Where(waypoint => IsAligned(transform.position, waypoint.transform.position))
            .Select(collider => collider.transform)
            .ToArray();

        if (adjacentWaypoints.Length > 0)
        {
            int index = Random.Range(0, adjacentWaypoints.Length);
            targetWaypoint = adjacentWaypoints[index];
        }
        else
        {
            Debug.LogError("No waypoints found!");
        }
    }

    bool IsAligned(Vector3 position1, Vector3 position2)
    {
        float threshold = 0.1f; // Adjust this value as needed
        bool xAligned = Mathf.Abs(position1.x - position2.x) < threshold;
        bool yAligned = Mathf.Abs(position1.y - position2.y) < threshold;
        return (xAligned || yAligned);
    }

    public Vector2Int GhostPosition { get; set; }
}
