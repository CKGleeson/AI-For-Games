using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float normalSpeed = 5f; // Normal movement speed
    public float shootingSpeed = 2.5f; // Speed when shooting
    private float currentSpeed; // Current movement speed
    [SerializeField] private FieldOfView fieldOfView; // Link FieldOfView in the Inspector
    private bool isShooting = false; // Is the player currently shooting

    void Start()
    {
        currentSpeed = normalSpeed; // Set initial speed to normal speed
        if (fieldOfView == null)
        {
            Debug.LogError("FieldOfView component is not assigned in the Inspector.");
        }
    }

    void Update()
    {
        // Update the speed based on whether the player is shooting or not
        if (isShooting)
        {
            currentSpeed = shootingSpeed; // Slow down the player while shooting
        }
        else
        {
            currentSpeed = normalSpeed; // Normal speed when not shooting
        }

        // Handle player movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(horizontal, vertical, 0).normalized;
        transform.Translate(direction * currentSpeed * Time.deltaTime, Space.World);

        // Rotate the player and update FOV to face the mouse position
        RotateTowardsMouse();

        // If space is pressed, toggle the shooting state
        if (Input.GetButtonDown("Fire1"))
        {
            isShooting = true;
        }
        if (Input.GetButtonUp("Fire1"))
        {
            isShooting = false;
        }
    }

    private void RotateTowardsMouse()
    {
        // Get the mouse position in world space
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0; // Ensure z position is zero for 2D

        // Calculate the direction from the player to the mouse
        Vector3 aimDirection = (mousePosition - transform.position).normalized;

        // Set the player's rotation to face the mouse
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        // Lock rotation to z-axis only
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Update the FieldOfView to follow the player and aim at the mouse
        if (fieldOfView != null)
        {
            fieldOfView.SetOrigin(transform.position); // Update FOV origin to player's position
            fieldOfView.SetAimDirection(aimDirection); // Set aim direction for the FOV
        }
    }
}
