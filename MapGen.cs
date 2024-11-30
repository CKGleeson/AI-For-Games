using System.Collections.Generic;
using UnityEngine;

public class MapGen : MonoBehaviour
{
    [Header("Room Prefabs")]
    public GameObject entrancePrefab;   // Prefab for the entrance
    public GameObject exitPrefab;       // Prefab for the exit
    public GameObject openRoomPrefab;   // Prefab for the open room
    public GameObject wallPrefab;       // Prefab for the wall
    public GameObject vipRoomPrefab;    // Prefab for the VIP room
    public GameObject playerPrefab;

    [Header("Sizes")]
    public int tileSize = 10;           // Size of each grid cell (e.g., 10x10)
    public int gridDimension = 20;      // Size of the grid (20x20)
    public int PathLength = 10;
    private int Count = 0;

    [Header("Pathfinding")]
    public GameObject nodeSpawnerPrefab;
    public GameObject pathfinderPrefab;

    private GameObject nodeSpawnerInstance;
    private GameObject pathfinderInstance;

    private Vector3 playerSpawnPosition;


    private void Start()
    {
        int Mapsize = GameManager.Instance.mapSize;
        gridDimension = Mapsize;
        PathLength = (Mapsize * 10);

        List<string> directions = Directions(PathLength);
        GenerateDungeonFromDirections(directions);
        SpawnNodeSpawnerAndPathfinder();

    }
    void SpawnNodeSpawnerAndPathfinder()
    {
        // Instantiate the NodeSpawner at the MapGenerator's position
        nodeSpawnerInstance = Instantiate(nodeSpawnerPrefab, playerSpawnPosition, Quaternion.identity);

        // Instantiate the Pathfinder at the same position or a custom position if needed
        pathfinderInstance = Instantiate(pathfinderPrefab, playerSpawnPosition, Quaternion.identity);
    }
    public void GenerateDungeonFromDirections(List<string> directions)
    {
        int[,] matrix = new int[gridDimension, gridDimension];

        int startX = gridDimension / 2; // Center of the grid
        int startY = gridDimension / 2;

        // Place entrance at the start position
        matrix[startX, startY] = 1;  // Entrance

        int currentX = startX;
        int currentY = startY;

        // Calculate position for the VIP room (3/4 of the path)
        int vipRoomIndex = directions.Count * 3 / 4;
        int vipRoomX = startX;
        int vipRoomY = startY;

        // Generate rooms based on the direction list
        for (int i = 0; i < directions.Count; i++)
        {
            string direction = directions[i];
            switch (direction)
            {
                case "u":
                    currentY++;
                    break;
                case "d":
                    currentY--;
                    break;
                case "l":
                    currentX--;
                    break;
                case "r":
                    currentX++;
                    break;
                default:
                    Debug.LogWarning("Invalid direction: " + direction);
                    continue; // Skip to next direction if invalid
            }

            // Check bounds and place open room
            if (IsInBounds(currentX, currentY) && matrix[currentX, currentY] != 1)
            {
                matrix[currentX, currentY] = 4; // Open room
                PlaceWallsAround(matrix, currentX, currentY);
            }
            else
            {
                List<string> spare = Directions(PathLength);
                Count = Count + 1;
                if (PathLength < 6)
                {
                    
                    GenerateDungeonFromDirections(spare);
                    return;

                }
                else if (Count % 5 == 0)
                {
                    PathLength = PathLength - 1;
                }
                GenerateDungeonFromDirections(spare);
                return;
            }

            // Set VIP room coordinates when 3/4 of the path is reached
            if (i == vipRoomIndex)
            {
                vipRoomX = currentX;
                vipRoomY = currentY;
            }
        }

        // Ensure VIP room, entrance, and exit are not in the same position
        if ((vipRoomX == startX && vipRoomY == startY) ||
            (currentX == startX && currentY == startY) ||
            (currentX == vipRoomX && currentY == vipRoomY))
        {


            List<string> spare = Directions(PathLength);
            GenerateDungeonFromDirections(spare);
            return;
        }

        // Place exit at the end position
        matrix[currentX, currentY] = 2;  // Exit

        // Place the VIP room at the 3/4 point
        matrix[vipRoomX, vipRoomY] = 5;  // VIP room
        PlaceWallsAround(matrix, vipRoomX, vipRoomY);

        // Place walls around the entrance
        PlaceWallsAround(matrix, startX, startY);


        playerSpawnPosition = new Vector3(startX * tileSize, startY * tileSize, 0);

        BuildGridFromMatrix(matrix);
    } // turns the list of directions into a matrix 
    public void BuildGridFromMatrix(int[,] matrix)
    {
        for (int x = 0; x < matrix.GetLength(0); x++)
        {
            for (int y = 0; y < matrix.GetLength(1); y++)
            {
                Vector3 spawnPosition = new Vector3(x * tileSize, y * tileSize, 0);  // Position based on matrix indices

                // Instantiate prefabs based on matrix values
                switch (matrix[x, y])
                {
                    case 1:  // Entrance
                        Instantiate(entrancePrefab, spawnPosition, Quaternion.identity);
                        break;
                    case 2:  // Exit
                        Instantiate(exitPrefab, spawnPosition, Quaternion.identity);
                        break;
                    case 4:  // Open room
                        Instantiate(openRoomPrefab, spawnPosition, Quaternion.identity);
                        break;
                    case 5:  // VIP room
                        Instantiate(vipRoomPrefab, spawnPosition, Quaternion.identity);
                        break;
                    case 3:  // Wall
                        Instantiate(wallPrefab, spawnPosition, Quaternion.identity);
                        break;
                }
            }
        }
        SpawnPlayerAtEntrance();
    } // spawns the prefabs in game from a matrix 
    private bool IsInBounds(int x, int y)
    {
        return x >= 1 && x < gridDimension - 1 && y >= 1 && y < gridDimension - 1;
    } // checks if a position is within map size
    private void PlaceWallsAround(int[,] matrix, int x, int y)
    {
        // Check and place walls around the current position (x, y)
        // Up
        if (IsInBounds(x, y) && matrix[x, y + 1] != 4 && matrix[x, y + 1] != 1 && matrix[x, y + 1] != 5 && matrix[x, y + 1] != 2)
            matrix[x, y + 1] = 3;

        // Down
        if (IsInBounds(x, y) && matrix[x, y - 1] != 4 && matrix[x, y - 1] != 1 && matrix[x, y - 1] != 5 && matrix[x, y - 1] != 2)
            matrix[x, y - 1] = 3;

        // Left
        if (IsInBounds(x, y) && matrix[x - 1, y] != 4 && matrix[x - 1, y] != 1 && matrix[x - 1, y] != 5 && matrix[x - 1, y] != 2)
            matrix[x - 1, y] = 3;

        // Right
        if (IsInBounds(x, y) && matrix[x + 1, y] != 4 && matrix[x + 1, y] != 1 && matrix[x + 1, y] != 5 && matrix[x + 1, y + 1] != 2)
            matrix[x + 1, y] = 3;

        // Up-right
        if (IsInBounds(x, y) && matrix[x + 1, y + 1] != 4 && matrix[x + 1, y + 1] != 1 && matrix[x + 1, y + 1] != 5 && matrix[x + 1, y + 1] != 2)
            matrix[x + 1, y + 1] = 3;

        // Up-left
        if (IsInBounds(x, y) && matrix[x - 1, y + 1] != 4 && matrix[x - 1, y + 1] != 1 && matrix[x - 1, y + 1] != 5 && matrix[x - 1, y + 1] != 2)
            matrix[x - 1, y + 1] = 3;

        // Down-right
        if (IsInBounds(x, y) && matrix[x + 1, y - 1] != 4 && matrix[x + 1, y - 1] != 1 && matrix[x + 1, y - 1] != 5 && matrix[x + 1, y - 1] != 2)
            matrix[x + 1, y - 1] = 3;

        // Down-left
        if (IsInBounds(x, y) && matrix[x - 1, y - 1] != 4 && matrix[x - 1, y - 1] != 1 && matrix[x - 1, y - 1] != 5 && matrix[x - 1, y - 1] != 2)
            matrix[x - 1, y - 1] = 3;
    } // within the matrix checks if it is apropriate to put walls around a position 
    public List<string> Directions(int x)
    {
        List<string> directions = new List<string>();
        int prevDirection = -1;  // -1 means no previous direction

        for (int i = 0; i < x;)
        {
            int direc = Random.Range(0, 4);

            if (direc == 0 && prevDirection != 1)  // Up, avoid if the previous move was down
            {
                directions.Add("u");
                i++;
                prevDirection = direc;
            }
            else if (direc == 1 && prevDirection != 0)  // Down, avoid if the previous move was up
            {
                directions.Add("d");
                i++;
                prevDirection = direc;
            }
            else if (direc == 2 && prevDirection != 3)  // Left, avoid if the previous move was right
            {
                directions.Add("l");
                i++;
                prevDirection = direc;
            }
            else if (direc == 3 && prevDirection != 2)  // Right, avoid if the previous move was left
            {
                directions.Add("r");
                i++;
                prevDirection = direc;
            }
        }
        return directions;
    }  // generates a list of directions for the path of the dungeon 
    private void SpawnPlayerAtEntrance()
    {
        // Instantiate the player at the saved spawn position
        Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
    } // spawns a player at the on the entrance prefab 

}
