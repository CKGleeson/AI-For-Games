using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public LayerMask obstacleMask;
    public Mesh mesh;
    public float fov = 90f; // Field of view angle
    public float viewDistance = 7f; // Maximum view distance
    public Vector3 origin;
    public float startingAngle;

    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        origin = transform.position;
    }

    private void LateUpdate()
    {
        GenerateMesh(); // Update the FOV mesh
    }

    private void GenerateMesh()
    {
        int rayCount = 50;
        float angle = startingAngle + 90f;
        float angleIncrease = fov / rayCount;

        Vector3[] vertices = new Vector3[rayCount + 2];
        int[] triangles = new int[rayCount * 3];
        vertices[0] = origin;

        int vertexIndex = 1;
        int triangleIndex = 0;

        // Cast rays to determine the FOV shape
        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 vertex;
            RaycastHit2D hit = Physics2D.Raycast(origin, UtilsClass.GetVectorFromAngle(angle), viewDistance, obstacleMask);
            if (hit.collider == null)
            {
                Vector2 direction = UtilsClass.GetVectorFromAngle(angle) * viewDistance;
                vertex = origin + new Vector3(direction.x, direction.y, 0);
            }
            else
            {
                vertex = hit.point;
            }

            vertices[vertexIndex] = vertex;

            if (i > 0)
            {
                triangles[triangleIndex + 0] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;
                triangleIndex += 3;
            }

            vertexIndex++;
            angle -= angleIncrease;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public void SetOrigin(Vector3 origin)
    {
        this.origin = origin; // Set the origin to the position of the character
    }

    public void SetAimDirection(Vector3 aimDirection)
    {
        // Set the FOV direction based on the character's facing direction
        startingAngle = UtilsClass.GetAngleFromVectorFloat(aimDirection) - fov / 2f;
    }
}