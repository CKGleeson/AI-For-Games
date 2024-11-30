using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    private Mesh mesh ;
    private float fov= 90f;
    private Vector3 origin;
    private float startingAngle;
    private float viewDistance=50f;

    [Header("Visibility Settings")]
    public Transform player;        // Reference to the player object
    public float maxDistance = 100f; // Distance within which the FOV mesh should always be visible
    public float expandAmount = 1000f; // Amount to expand the MeshRenderer bounds by

    // Start is called before the first frame update
    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (player == null)
        {
            Debug.LogError("Player object not found! Ensure there is a GameObject tagged 'Player'.");
        }

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerName = "Mesh"; // Set the sorting layer to "Mesh"
        meshRenderer.sortingOrder = 0;
       
        if (meshRenderer != null)
        {
            // Expanding bounds to keep it visible
            meshRenderer.bounds.Expand(1000f);  // Large value to ensure it is always rendered
        }


    }

    private void LateUpdate() 
    {  

        int rayCount = 60;
        float angle = startingAngle + fov;
        float angleIncrease = fov / rayCount;
        
        Vector3[] vertices = new Vector3[rayCount+2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount*3];

        vertices[0] = origin;

        int vertexIndex = 1;
        int triangleIndex = 0;
        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 vertex;
            RaycastHit2D raycastHit2D = Physics2D.Raycast(origin , GetVectorFromAngle(angle), viewDistance, layerMask);
            if (raycastHit2D.collider == null)
            {
                //No Hit
                vertex = origin + GetVectorFromAngle(angle) * viewDistance;
            } else
            {
                //Hit
                vertex = raycastHit2D.point;
            }
            vertices[vertexIndex] = vertex;
            if (i > 0)
            {
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex ;

                triangleIndex += 3;
            }
            vertexIndex++;
            angle -= angleIncrease;
        }
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(origin, Vector3.one * 1000f);

    }
    // makes sure that the mesh of the enemy fov is always rendered
    
    public void setFov(float fov1)
    {
        fov = fov1;
    }

    public void setViewDistance(float viewDistance1)
    {
        viewDistance = viewDistance1;
    }

    public void SetOrigin(Vector3 origin)
    {
        this.origin = origin;
    }

    public void SetAimDirection(Vector3 aimDirection)
    {
        startingAngle = GetAngleFromVectorFloat(aimDirection) - fov / 2f;
    }

    public static float GetAngleFromVectorFloat(Vector3 dir)
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (n < 0)
        {
            n += 360;
        }
        return n;
    }
    public static Vector3 GetVectorFromAngle(float angle)
    {
        //angle 0 - 360 
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }
}
