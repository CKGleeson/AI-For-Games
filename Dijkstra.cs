using System.Collections.Generic;
using UnityEngine;

public class DijkstraPathfinder : MonoBehaviour
{
    public Node FindClosestNode(Vector3 position)
    {
        List<Node> allNodes = GameManager.Instance.allNodes; // Retrieve all nodes from GameManager
        Dictionary<Node, List<Node>> nodeGraph = GameManager.Instance.nodeGraph; // Retrieve graph of nodes and connections from GameManager

        Node closestNode = null;
        float closestDistance = float.MaxValue;

        foreach (Node node in allNodes)
        {
            float distance = Vector3.Distance(position, node.transform.position);

            if (distance < closestDistance)
            {
                closestNode = node;
                closestDistance = distance;
            }
        }

        return closestNode;
    } // finds the closest node to any position

    public Node FindClosestNodeInFront(Vector3 position, Vector3 forwardDirection)
    {
        List<Node> allNodes = GameManager.Instance.allNodes; // Retrieve all nodes from GameManager
        Dictionary<Node, List<Node>> nodeGraph = GameManager.Instance.nodeGraph; // Retrieve graph of nodes from GameManager

        Node closestNodeInFront = null;
        float closestDistance = float.MaxValue;

        foreach (Node node in allNodes)
        {
            Vector3 directionToNode = (node.transform.position - position).normalized;
            float dotProduct = Vector3.Dot(forwardDirection.normalized, directionToNode);

            if (dotProduct > 0) // Node is in front of the player
            {
                float distance = Vector3.Distance(position, node.transform.position);

                if (distance < closestDistance && !Physics.Linecast(position, node.transform.position, LayerMask.GetMask("Objects")))
                {
                    closestNodeInFront = node;
                    closestDistance = distance;
                }
            }
        }

        // If no unobstructed node in front, fallback to general closest node search
        if (closestNodeInFront == null)
        {
            return FindClosestNode(position); 
        }

        return closestNodeInFront;
    } // using a direction check the closest node within 180 degrees in front 

    public List<Node> FindShortestPath(Node startNode, Node targetNode)
    {
        // Fetch all nodes and node graph from GameManager
        List<Node> allNodes = GameManager.Instance.allNodes;
        Dictionary<Node, List<Node>> nodeGraph = GameManager.Instance.nodeGraph;

        // Dictionary to store the shortest known distance to each node
        Dictionary<Node, float> distance = new Dictionary<Node, float>();
        // Dictionary to store the previous node in the shortest path for each node
        Dictionary<Node, Node> previous = new Dictionary<Node, Node>();
        // List of nodes that need to be explored for shorter paths
        List<Node> nodesToExplore = new List<Node>();

        // Initialize distances and set up initial node
        foreach (Node node in allNodes)
        {
            if (!node.isUsable) continue; // Skip blocked nodes

            // Set initial distance to all nodes as unreachable
            distance[node] = float.MaxValue;
            // Set preveious node to null for all nodes  
            previous[node] = null;
        }

        // Add the starting node to the list of nodes to explore and set the distance to it to 0
        distance[startNode] = 0;
        nodesToExplore.Add(startNode);

        // Loop while there are nodes left to explore
        while (nodesToExplore.Count > 0)
        {
            // Sort nodes based on distance
            nodesToExplore.Sort((a, b) => distance[a].CompareTo(distance[b]));
            Node currentNode = nodesToExplore[0];
            nodesToExplore.RemoveAt(0);

            // Exit if the target node is reached
            if (currentNode == targetNode) break;

            foreach (Node neighbor in nodeGraph[currentNode])
            {
                if (!neighbor.isUsable) continue; // Skip blocked neighbors

                // Calculate the distance to the neighbor through the current node
                float newDistance = distance[currentNode] + Vector2.Distance(currentNode.transform.position, neighbor.transform.position);

                if (newDistance < distance[neighbor])
                {
                    // Set the distance to the previous
                    distance[neighbor] = newDistance;
                    // Set the previous node for this neighbor to the current node
                    previous[neighbor] = currentNode;

                    if (!nodesToExplore.Contains(neighbor))
                    {
                        nodesToExplore.Add(neighbor);
                    }
                }
            }
        }

        // Backtrack to find the path
        List<Node> path = new List<Node>();
        Node pathNode = targetNode;

        while (pathNode != null)
        {
            path.Insert(0, pathNode);
            pathNode = previous[pathNode];
        }

        if (path.Count == 0 || path[0] != startNode)
        {
            return new List<Node>(); // No valid path
        }

        return path;
    } //Finds the shortest path from node to node
}
