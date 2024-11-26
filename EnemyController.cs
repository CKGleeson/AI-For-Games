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
    public AudioClip gunfireSound; 


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
        }
    } // Update 
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
        else
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            
            if (distanceToPlayer < viewDistance)
            {
                Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
                
                if (Vector3.Angle(AimDir, dirToPlayer) < (fov / 2f))
                {
                    //gets this far 
                    
                    RaycastHit2D raycastHit2D = Physics2D.Raycast(transform.position, dirToPlayer, viewDistance, LayerMask.GetMask("Player"));
                    Debug.DrawRay(transform.position, dirToPlayer * viewDistance, Color.red);  // Debug ray

                    if (raycastHit2D.collider != null)
                    {
                        Debug.Log($"Raycast hit: {raycastHit2D.collider.gameObject.name}");

                        if (raycastHit2D.collider.gameObject.GetComponent<Player>() != null)
                        {
                            isPlayerInSight = true;
                            Debug.Log("Player detected!");
                        }
                    }
                    else
                    {
                        Debug.Log("No collider hit by raycast");
                    }
                }
                else
                {
                    Debug.Log("Player not within FOV angle");
                }
            }
            else
            {
                Debug.Log("Player is too far away");
            }
            return isPlayerInSight;
        }
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
            // If no valid path is found, go directly to the target using GoTo
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

    
}


 private enum EnemyState { Wandering, Idle, Attack, Searching, WaitingToSearch }
    private EnemyState currentState = EnemyState.Wandering;

    
    private float wanderTimer;
    private float idleTimer;


switch (currentState)
        {
            case EnemyState.Wandering:
                Wander();
                break;
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Attack:
                Attack();
                break;
            case EnemyState.WaitingToSearch:
                // Waiting logic is handled in the coroutine
                break;
            case EnemyState.Searching:
                // Searching behavior handled in coroutine
                break;
        }

private void Wander()
    {
        Vector3 targetPosition = transform.position + (Vector3)currentDirection;
        RotateAndMove(targetPosition, wanderSpeed, () =>
        {
            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0)
            {
                currentState = EnemyState.Idle;
                idleTimer = idleTime;
                wanderTimer = wanderTime;
            }
        });
    }

private void Idle()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0)
        {
            SetNewDirection();
            currentState = EnemyState.Wandering;
        }
    }

private IEnumerator WaitBeforeSearch()
    {
        currentState = EnemyState.WaitingToSearch;

        // Wait for 2 seconds before starting the search
        float timer = searchWaitTime;
        while (timer > 0)
        {
            if (enemyFOV.IsPlayerInFOV(out Transform detectedPlayer))
            {
                // Player spotted again, cancel wait and enter attack mode
                player = detectedPlayer;
                EnterAttackMode();
                yield break;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        EnterSearchMode();
    }

    private void EnterSearchMode()
    {
        currentState = EnemyState.Searching;
        StartCoroutine(SmoothSearchRoutine());
    }

    private IEnumerator SmoothSearchRoutine()
    {
        float searchTimer = searchDuration;

        while (searchTimer > 0)
        {
            // Perform a smooth left-right-center search
            yield return SmoothRotate(-5f); // Look slightly left
            yield return new WaitForSeconds(0.5f);

            yield return SmoothRotate(50f); // Look far right
            yield return new WaitForSeconds(0.5f);

            yield return SmoothRotate(-45f); // Return to center

            // Check if the player is spotted during the search
            if (enemyFOV.IsPlayerInFOV(out Transform detectedPlayer))
            {
                player = detectedPlayer;
                EnterAttackMode();
                yield break;
            }

            searchTimer -= 1.5f; // Approximate duration of one search cycle
        }

        // After searching for 3 seconds, go back to wandering and reset FOV
        ExitSearchMode();
    }

    private IEnumerator SmoothRotate(float angle)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, 0, transform.eulerAngles.z + angle);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            if (enemyFOV.IsPlayerInFOV(out Transform detectedPlayer))
            {
                // Player spotted during rotation, enter attack mode
                player = detectedPlayer;
                EnterAttackMode();
                yield break;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            enemyFOV.SetDirection(transform.up); // Ensure FOV updates during rotation
            yield return null;
        }
    }

    private void ExitSearchMode()
    {
        currentState = EnemyState.Wandering;

        // Reset the FOV and view distance to defaults
        enemyFOV.ResetToDefault();

        SetNewDirection();
    }

private void EnterAttackMode()
    {
        currentState = EnemyState.Attack;

        // Change the FOV and View Distance for attack mode
        enemyFOV.SetFOV(45f);
        enemyFOV.SetViewDistance(17f);
        enemyFOV.SetAttackMode(true); // Enable attack mode
        enemyFOV.SetDirection((player.position - transform.position).normalized); // Center FOV on the player
    }



