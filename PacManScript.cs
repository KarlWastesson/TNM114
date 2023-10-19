using UnityEngine;

public class PacManScript : MonoBehaviour
{
    private int[,] map;
    public Vector2Int pacManPosition;
    public float speed = 5.0f; // Speed of Pac-Man movement

    // Start is called before the first frame update
    void Start()
    {
        map = PlayfieldGenerator.Map;
        for (int y = 0; y < PlayfieldGenerator.height; y++)
        {
            for (int x = 0; x < PlayfieldGenerator.width; x++)
            {
                if (map[y, x] == 2) // 2 represents Pac-Man
                {
                    pacManPosition = new Vector2Int(x, y);
                    transform.position = new Vector3(x, -y, 0); // Update initial position
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        MovePacMan();
        pacManPosition = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(-transform.position.y));
        // Update Pac-Man logic here if needed
    }

    void MovePacMan()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, vertical, 0).normalized;

        Vector3 newPosition = transform.position + direction * speed * Time.deltaTime;
        Vector2Int newGridPosition = new Vector2Int(Mathf.FloorToInt(newPosition.x), Mathf.FloorToInt(-newPosition.y));

        // Check if the new position is within bounds and not a wall
        if (IsMoveValid(newGridPosition))
        {
            // Move Pac-Man to the new position
            transform.position = newPosition;

            // Check if there's any movement to avoid unnecessary rotation
            if (direction.magnitude > 0)
            {
                // Calculate the rotation angle
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                // Apply the rotation to Pac-Man
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }


    bool IsMoveValid(Vector2Int newGridPosition)
    {
        if (newGridPosition.x < 0 || newGridPosition.x >= PlayfieldGenerator.width ||
            newGridPosition.y < 0 || newGridPosition.y >= PlayfieldGenerator.height)
        {
            return false; // Out of bounds
        }

        if (map[newGridPosition.y, newGridPosition.x] == 1)
        {
            return false; // Is a wall
        }

        return true; // Valid move
    }

    public Vector2Int GetPacManPosition()
    {
        return pacManPosition;
    }
}
