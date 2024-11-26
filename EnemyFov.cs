using UnityEngine;

public class EnemyFOV : MonoBehaviour
{
    [Header("Field of View Settings")]
    public float fov = 90f; // Default field of view in degrees
    public float viewDistance = 7f; // Default maximum distance the enemy can see
    public LayerMask obstacleMask; // Mask for obstacles like walls
    public LayerMask playerMask; // Mask for detecting players

    private float defaultFOV; // Original FOV
    private float defaultViewDistance; // Original view distance
    private bool isInAttackMode; // Is the enemy in attack mode?
    private Mesh mesh;
    private float startingAngle; // Starting angle of the FOV
    private Vector3 currentDirection; // Enemy's current facing direction

    private void Start()
    {
        defaultFOV = fov;
        defaultViewDistance = viewDistance;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh; // Assign mesh for FOV visualization
        SetDirection(Vector3.up); // Default facing direction
    }

    public void SetDirection(Vector3 direction)
    {
        currentDirection = direction.normalized;
        UpdateStartingAngle();
        GenerateMesh();
    }

    public void SetFOV(float newFOV)
    {
        fov = newFOV;
        UpdateStartingAngle();
        GenerateMesh(); // Regenerate the FOV mesh with the new angle
    }

    public void SetViewDistance(float newDistance)
    {
        viewDistance = newDistance;
        GenerateMesh(); // Regenerate the FOV mesh with the new distance
    }

    public void SetAttackMode(bool isAttack)
    {
        isInAttackMode = isAttack;
        UpdateStartingAngle();
        GenerateMesh();
    }

    public void ResetToDefault()
    {
        fov = defaultFOV;
        viewDistance = defaultViewDistance;
        isInAttackMode = false;
        UpdateStartingAngle();
        GenerateMesh();
    }

    private void UpdateStartingAngle()
    {
        float forwardAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;

        // Add a 45-degree offset in attack mode
        startingAngle = isInAttackMode
            ? forwardAngle - (fov / 2f) - 45f
            : forwardAngle - (fov / 2f);
    }

    private void GenerateMesh()
    {
        int rayCount = 50;
        float angle = startingAngle + 90f; // Start angle for the rays
        float angleStep = fov / rayCount; // Angle increment per ray

        Vector3[] vertices = new Vector3[rayCount + 2];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = Vector3.zero; // Origin of the FOV

        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 direction = UtilsClass.GetVectorFromAngle(angle).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, viewDistance, obstacleMask);

            Vector3 vertex = hit.collider == null
                ? transform.position + direction * viewDistance
                : hit.point;

            vertices[i + 1] = transform.InverseTransformPoint(vertex);

            if (i > 0)
            {
                int vertexIndex = i + 1;
                triangles[(i - 1) * 3] = 0;
                triangles[(i - 1) * 3 + 1] = vertexIndex - 1;
                triangles[(i - 1) * 3 + 2] = vertexIndex;
            }

            angle -= angleStep;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public bool IsPlayerInFOV(out Transform player)
    {
        player = null;
        Collider2D[] playersInRange = Physics2D.OverlapCircleAll(transform.position, viewDistance, playerMask);

        foreach (Collider2D playerCollider in playersInRange)
        {
            Vector2 directionToPlayer = (playerCollider.transform.position - transform.position).normalized;
            float angleToPlayer = Vector2.Angle(currentDirection, directionToPlayer);

            if (angleToPlayer < fov / 2f)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, viewDistance, obstacleMask);

                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    player = playerCollider.transform;
                    return true;
                }
            }
        }

        return false;
    }
}
