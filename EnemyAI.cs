using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public EnemyFOV enemyFOV;

    [Header("Movement Settings")]
    public float wanderSpeed = 2f;
    public float chaseSpeed = 3f;
    public float rotationSpeed = 100f;

    [Header("Timers")]
    public float wanderTime = 2f;
    public float idleTime = 2f;
    public float searchWaitTime = 2f; // Time to wait before starting the search
    public float searchDuration = 3f; // Duration of the search

    private enum EnemyState { Wandering, Idle, Attack, Searching, WaitingToSearch }
    private EnemyState currentState = EnemyState.Wandering;

    private Vector2 currentDirection;
    private float wanderTimer;
    private float idleTimer;

    private Transform player;
    private Vector2 lastKnownPosition;
    private bool playerVisible;

    private void Start()
    {
        SetNewDirection();
        wanderTimer = wanderTime;
    }

    private void Update()
    {
        if (enemyFOV.IsPlayerInFOV(out Transform detectedPlayer))
        {
            player = detectedPlayer;
            lastKnownPosition = player.position;
            playerVisible = true;

            if (currentState != EnemyState.Attack)
            {
                EnterAttackMode();
            }
        }
        else
        {
            playerVisible = false;

            if (currentState == EnemyState.Attack)
            {
                StartCoroutine(WaitBeforeSearch());
            }
        }

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

        // Ensure the FOV is always aligned with the enemy's forward direction
        enemyFOV.SetDirection(transform.up);
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

    private void Attack()
    {
        if (playerVisible)
        {
            Vector2 directionToPlayer = (player.position - transform.position).normalized;

            RotateTowards(player.position, rotationSpeed * 0.5f, () =>
            {
                enemyFOV.SetDirection(directionToPlayer);
            });

            transform.Translate(directionToPlayer * chaseSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            StartCoroutine(WaitBeforeSearch());
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

    private void SetNewDirection()
    {
        float angle = Random.Range(0f, 360f);
        currentDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
    }

    private void RotateTowards(Vector3 targetPosition, float speed, System.Action onRotationComplete = null)
    {
        Vector2 directionToTarget = (targetPosition - transform.position).normalized;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, speed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
        {
            onRotationComplete?.Invoke();
        }
    }

    private void RotateAndMove(Vector3 targetPosition, float speed, System.Action onRotationComplete = null)
    {
        Vector2 directionToTarget = (targetPosition - transform.position).normalized;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.Translate(directionToTarget * speed * Time.deltaTime, Space.World);
            onRotationComplete?.Invoke();
        }

        enemyFOV.SetDirection(transform.up);
    }
}
