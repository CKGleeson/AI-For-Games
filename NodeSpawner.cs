using System.Collections.Generic;
using UnityEngine;

public class NodeSpawner : MonoBehaviour
{
    public GameObject nodePrefab;        // Drag the Node prefab here in the inspector
    public float gridWidth = 10f;        // Total width of the grid in world units
    public float gridHeight = 10f;       // Total height of the grid in world units
    public float spacing = 2.0f;         // Space between nodes
    public float connectionDistance = 1f; // Max distance to consider nodes as neighbors

    private List<Node> allNodes = new List<Node>();
    private Dictionary<Node, List<Node>> nodeGraph = new Dictionary<Node, List<Node>>();

    void Start()
    {
        gridHeight = GameManager.Instance.mapSize*10;
        gridWidth = gridHeight;
        SpawnNodeGrid();
        ConnectNodes();
    } 
    void SpawnNodeGrid()
    {
        // Clear any existing nodes before generating new ones
        GameManager.Instance.allNodes.Clear(); // Optionally clear the existing list first

        // Calculate the number of nodes based on grid dimensions and spacing
        int nodeCountX = Mathf.FloorToInt(gridWidth / spacing);
        int nodeCountY = Mathf.FloorToInt(gridHeight / spacing);

        float offsetX = -gridWidth / 2f;
        float offsetY = -gridHeight / 2f;

        for (int x = 0; x < nodeCountX; x++)
        {
            for (int y = 0; y < nodeCountY; y++)
            {
                Vector3 spawnPosition = new Vector3(
                    transform.position.x + offsetX + x * spacing,
                    transform.position.y + offsetY + y * spacing,
                    0
                );

                GameObject nodeObject = Instantiate(nodePrefab, spawnPosition, Quaternion.identity, transform);

                if (nodeObject.GetComponent<Node>().CheckIfTouching() == false)
                {
                    Destroy(nodeObject);
                }
                else
                {
                    Node node = nodeObject.GetComponent<Node>();
                    if (node != null && node.isUsable)
                    {
                        node.Initialize(spawnPosition, node.checkRadius);
                        GameManager.Instance.allNodes.Add(node); // Add immediately to GameManager
                    }
                }
            }
        }
    } //instantiates multiplr nodes in a grid 
    void ConnectNodes()
    {
        foreach (Node node in GameManager.Instance.allNodes)
        {
            GameManager.Instance.nodeGraph[node] = new List<Node>();

            if (!node.isUsable) continue;

            foreach (Node otherNode in GameManager.Instance.allNodes)
            {
                if (otherNode != node && otherNode.isUsable)
                {
                    RaycastHit2D hit = Physics2D.Linecast(node.transform.position, otherNode.transform.position, LayerMask.GetMask("Objects"));
                    if (hit.collider == null)
                    {
                        float distance = Vector2.Distance(node.transform.position, otherNode.transform.position);
                        if (distance <= connectionDistance)
                        {
                            GameManager.Instance.nodeGraph[node].Add(otherNode);
                        }
                    }
                }
            }
        }
    } // fills the dictionary with the list of the nodes collections
    // getters
    public List<Node> GetAllNodes()
    {
        return allNodes;
    }
    public Dictionary<Node, List<Node>> GetNodeGraph()
    {
        return nodeGraph;
    }
}
