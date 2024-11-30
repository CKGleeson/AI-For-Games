using TMPro.Examples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;

public class EnemyController : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;        // Reference to the bullet prefab
    public Transform firePoint;            // Point from where the bullets will spawn
    public AudioClip gunfireSound;         // Gunfire sound clip

    [Header("Look-Around Settings")]
    public bool canLookAround = true;  // Control whether the enemy can look around
    public int SpinSpeed = 20;
    [SerializeField] private Transform pfFieldOfView;
    private FieldOfView fieldOfView;
    [SerializeField] public float fov = 30f;
    [SerializeField] public float viewDistance = 1f;
    public GameObject player;
    private bool IsPlayerInView;

    [Header("Pathfinding Settings")]
    public DijkstraPathfinder pathfinder;  // Reference to the DijkstraPathfinder
    public float moveSpeed = 2.0f;         // Speed at which the enemy moves along the path
    private List<Node> currentPath;
    private int currentNodeIndex = 0;
    private bool isMoving = false;

    private AudioSource audioSource;
    private Vector3 AimDir;

    private Vector3 lastTargetPosition;

    public int timer = 50;
    private int timerTime = 0;

    public int rotationTimer = 2;
    private int rotationTimerCount = 0;

    private float stuckThreshold = 0.1f; // Threshold for stuck detection
    private float timeStuck = 0f; // Timer to track if the enemy has been stuck

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        // Initialize AudioSource
        audioSource = GetComponent<AudioSource>();
        // Initialize Field of View
        fieldOfView = Instantiate(pfFieldOfView, null).GetComponent<FieldOfView>();

        lastTargetPosition = transform.position;  // Initialize to the enemy's starting position
        IsPlayerInView = false;
    } // Start
    
    void Update()
    {
        if (isPlayerInSight())
        {
            FacePlayer();
            IsPlayerInView = true;
            Debug.Log("Player found by enemy");
            //ShootBullet();
            MoveToHere(player.transform.position);
        }
        else
        {
            IsPlayerInView = false;
        }

        UpdateFieldOfView();
        AimDir = transform.up;


        // If the enemy is currently moving, continue following the path
        if (isMoving && currentPath != null)
        {
            FollowPath();
            DrawPathLines();
        }
    } // Update  

    /*
    private Vector3 mousePosition;

    void Update()
    {
        // Detect if the left mouse button is clicked
        if (Input.GetMouseButtonDown(0)) // 0 is the left mouse button
        {
            // Get the mouse position in world coordinates
            mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0; // Ensure Z-coordinate is 0 for 2D movement

            // Use pathfinding to move towards the clicked position
            MoveToHere(mousePosition);
        }

        // Continue following the path
        if (isMoving && currentPath != null)
        {
            FollowPath();
            DrawPathLines();
        }

        // Update Field of View (if necessary)
        UpdateFieldOfView();
    }
     // code for using the mouse for pathfining */

    private void FacePlayer()
    {
        if (player == null) return;

        // Calculate direction to the player
        Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;

        // Calculate the angle between the enemy and the player
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;

        // Adjust the angle to account for sprite orientation
        angle -= 90;

        // Apply the calculated angle to the rotation
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    } //Makes the enemy face the player 
    private void Spin()
    {
        transform.Rotate(Vector3.forward, SpinSpeed * Time.deltaTime);
    } // makes the enemy spin 
    private void UpdateFieldOfView()
    {
        AimDir = transform.up; // Current facing direction
        fieldOfView.SetAimDirection(AimDir);
        fieldOfView.SetOrigin(transform.position);
        fieldOfView.setFov(fov);
        fieldOfView.setViewDistance(viewDistance);
    } // Updates the atributes of the fov
    private bool isPlayerInSight()
    {
        bool isPlayerInSight = false;
        if (player == null)
        {
            Debug.Log("No player assigned");
            return false;
        }
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer >= viewDistance)
        {
            Debug.Log("Player is too far away");
            return false;
        }
        Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
        if (Vector3.Angle(AimDir, dirToPlayer) >= (fov / 2f))
        {
            Debug.Log("Player not within FOV angle");
            return false;
        }
        RaycastHit2D raycastHit2D = Physics2D.Raycast(transform.position, dirToPlayer, viewDistance, LayerMask.GetMask("Player"));
        Debug.DrawRay(transform.position, dirToPlayer * viewDistance, Color.red);  // Debug ray

        if (raycastHit2D.collider == null)
        {
            Debug.Log("No collider hit by raycast");
            return false;
        }
        Debug.Log($"Raycast hit: {raycastHit2D.collider.gameObject.name}");
        if (raycastHit2D.collider.gameObject.GetComponent<Player>() != null)
        {
            isPlayerInSight = true;
            Debug.Log("Player detected!");
        }
        return isPlayerInSight;
    } // checks if the player is inside the fov 
    private void ShootBullet()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("BulletPrefab or FirePoint is not assigned!");
            return;
        }

        // Instantiate the bullet at the spawn point
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Debug.Log("Bullet instantiated at position: " + firePoint.position);

        // Play the gunfire sound
        if (audioSource != null && gunfireSound != null)
        {
            audioSource.PlayOneShot(gunfireSound);
        }
    } // attack 
    private void GoTo(Vector3 targetPosition)
    {
        // Calculate direction to the target
        Vector3 directionToTarget = targetPosition - transform.position;

        // Normalize the direction to ensure consistent movement speed
        directionToTarget.Normalize();

        // Rotate the enemy to face the target
        if (directionToTarget != Vector3.zero)
        {
            float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            angle -= 90; // Adjust angle to account for sprite orientation
            if (!IsPlayerInView)
            {
                transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
            }
        }

        // Move the enemy towards the target position in a straight line
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    } // Method to move enemy straight to position with no pathfinding
    public void MoveToHere(Vector3 targetPosition)
    {
        timerTime ++ ;

        // If timerCounter hasn't reached the timer threshold, do nothing
        if (timerTime < timer)
        {
            Debug.Log(timerTime);
           return;
        }

        // Reset the timer counter when it's time to recalculate
        timerTime = 0;
        // Find the closest nodes to the enemy's current position and the target
        Vector3 forwardDirection = transform.up;

        Node startNode = pathfinder.FindClosestNodeInFront(transform.position, forwardDirection);
        Node targetNode = pathfinder.FindClosestNode(targetPosition);

        

        // Get the shortest path from the pathfinder
        currentPath = pathfinder.FindShortestPath(startNode, targetNode);

        // Start moving along the path if a valid path is found
        if (currentPath != null && currentPath.Count > 0)
        {
            currentNodeIndex = 0;
            isMoving = true;
        }
        else
        {
            // When the path is finished go to the exact final position
            GoTo(targetPosition);
        }
    } // Method to move the enemy to a specific location using pathfinding
    private void FollowPath()
    {
        if (currentNodeIndex >= currentPath.Count)
        {
            isMoving = false;
            return;
        }

        Node targetNode = currentPath[currentNodeIndex];

        // Calculate direction to the target node
        Vector3 directionToTarget = targetNode.transform.position - transform.position;

        // Rotate the enemy to face the direction it's moving towards, with a -90-degree offset
        if (directionToTarget != Vector3.zero) // Avoid division by zero or invalid rotation
        {
            if (rotationTimerCount > rotationTimer && currentNodeIndex > 0)
            {
                float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
                // Adjust the angle by -90 degrees (opposite direction)
                angle -= 90;



                // Apply the calculated angle to the rotation (make sure to keep it in 2D)
                if (!IsPlayerInView)
                {
                    transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
                }
                rotationTimerCount = 0;
            } else
            {
                rotationTimerCount++;
            }

        }
            
        

        // Move towards the target node
        transform.position = Vector3.MoveTowards(transform.position, targetNode.transform.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetNode.transform.position) < stuckThreshold)
        {
            timeStuck += Time.deltaTime;
        }
        else
        {
            timeStuck = 0f; // Reset stuck time if movement occurs
        }

        // If the enemy has been stuck for a while, recalculate the path
        if (timeStuck > 2f) // Time threshold before considering the enemy stuck (e.g., 2 seconds)
        {
            Debug.Log("Enemy is stuck, recalculating path!");
            // Recalculate the path to the target
           
            MoveToHere(targetNode.transform.position);
            timeStuck = 0f; // Reset the stuck timer
        }

        // If close to the current target node, move to the next one
        if (Vector3.Distance(transform.position, targetNode.transform.position) < 0.1f)
        {
            currentNodeIndex++;
        }
    } // uses pathfinding to decide where to go next 

    private void DrawPathLines()
    {
        if (currentPath == null || currentPath.Count < 2)
            return;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 start = currentPath[i].transform.position;
            Vector3 end = currentPath[i + 1].transform.position;

            Debug.DrawLine(start, end, Color.white); // Draws a green line between nodes
        }
    }
}
