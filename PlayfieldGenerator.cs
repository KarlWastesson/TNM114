using System.Collections.Generic;
using UnityEngine;

public class PlayfieldGenerator : MonoBehaviour
{
    
    public GameObject wallPrefab;
    public GameObject WayPoint;
    public GameObject pacManPrefab;
    public GameObject ghostPrefab;
    public static int width = 25;
    public static int height = 16;
   

    private int[,] map;
    public static int[,] Map;

    void Awake()
    {
        map = new int[height, width];
        Map = map;
        GeneratePlayfield();
    }

    void GeneratePlayfield()
    {
        List<Vector2Int> availablePositions = new List<Vector2Int>();

        // Place walls on the border of the grid and fill available positions
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y == 0 || y == height - 1 || x == 0 || x == width - 1)
                {
                    map[y, x] = 1; // Place Wall
                }
                else
                {
                    availablePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // Place Pac-Man and Ghost at random positions from available positions
        PlaceObjectRandomly(2, availablePositions); // 2 for Pac-Man
        PlaceObjectRandomly(3, availablePositions); // 3 for Ghost
        

        // Place some random walls inside the grid
        int numWalls = Random.Range(10, 31);
        for (int i = 0; i < numWalls && availablePositions.Count > 0; i++)
        {
            PlaceObjectRandomly(1, availablePositions); // 1 for Wall
        }

         
        // Instantiate GameObjects based on the generated map
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
               
                Vector3 position = new Vector3(x + 0.5f, -y - 0.5f, 0); // Offset by 0.5
                Vector3 position1 = new Vector3(x, -y, 0);

                int cell = map[y, x];
                if (cell == 1) Instantiate(wallPrefab, position, Quaternion.identity);
                else if (cell == 2) 
                {
                    Instantiate(WayPoint, position, Quaternion.identity);
                    Instantiate(pacManPrefab, position1, Quaternion.identity); 
                }
                else if (cell == 3)
                {
                    Instantiate(WayPoint, position, Quaternion.identity);
                    Instantiate(ghostPrefab, position, Quaternion.identity);
                }
                else if (cell == 0) 
                {
                    
                    Instantiate(WayPoint, position, Quaternion.identity); 
                }
            }
        }

    }

    void PlaceObjectRandomly(int objectCode, List<Vector2Int> availablePositions)
    {
        if (availablePositions.Count == 0) return;

        int index = Random.Range(0, availablePositions.Count);
        Vector2Int position = availablePositions[index];
        map[position.y, position.x] = objectCode;
        availablePositions.RemoveAt(index); // Remove the position from available positions
    }
}
