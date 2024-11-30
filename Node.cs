using System;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public bool isUsable = true;
    public float checkRadius = 1.0f; // Radius to check for obstacles
    private LayerMask obstacleLayer;   // Hardcoded to the "Obstacles" layer

    private void Awake()
    {
        // Set the obstacle layer to always be "Obstacles"
        obstacleLayer = LayerMask.GetMask("Objects");
    }

    // Initialization method to set up the node
    public void Initialize(Vector3 position, float radius)
    {
        // Set the node's position
        transform.position = position;

        // Set the check radius
        checkRadius = radius;

        // Check if the node is usable by detecting obstacles
        CheckIfUsable();
    }

    // Method to check for obstacles in the defined radius
    private void CheckIfUsable()
    {
        Collider2D[] obstacles = Physics2D.OverlapCircleAll(transform.position, checkRadius,LayerMask.GetMask("Objects"));
        if (obstacles.Length > 0)
        {
            isUsable = false;
        }
    }

    public bool CheckIfTouching()
    {
        // Create a list to store touching objects
        List<GameObject> touchingObjects = new List<GameObject>();

        // Get all colliders in the area
        Collider2D[] touches = Physics2D.OverlapCircleAll(transform.position, checkRadius);

        foreach (Collider2D touch in touches)
        {
            // Make sure it's not the node itself
            if (touch != null && touch.gameObject != gameObject)
            {
                touchingObjects.Add(touch.gameObject);
                
            }
        }

        // If there are any objects in the list, return true
        if (touchingObjects.Count > 0)
        {
            return true;
        }
        else
        {
         
            return false;
        }
    }

    // Start method only triggers obstacle check if Initialize is not used
    void Start()
    {
        // Avoid duplicate checks if Initialize was already used
        if (!Application.isPlaying) return;

        // Perform obstacle check during Start if Initialize wasn't called
        CheckIfUsable();
    }

    // Visualize the check radius in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isUsable ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}
