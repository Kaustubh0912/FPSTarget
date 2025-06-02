using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 100.0f;
    public Transform playerCamera; // Assign your Main Camera here

    private CharacterController controller;
    private Vector3 playerVelocity;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
        {
            // Try to find the camera if not assigned
            playerCamera = Camera.main.transform;
            if (playerCamera.parent != transform)
            {
                Debug.LogWarning("Player camera should be a child of the player for this script to work optimally.");
            }
        }

        Cursor.lockState = CursorLockMode.Locked; // Lock cursor to center
        Cursor.visible = false; // Hide cursor
    }

    void Update()
    {
        // Player Movement (Ground)
        float moveX = Input.GetAxis("Horizontal"); // A/D keys or Left/Right arrows
        float moveZ = Input.GetAxis("Vertical");   // W/S keys or Up/Down arrows

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Gravity
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // A little force to keep grounded
        }
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // Mouse Look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Camera vertical rotation (Pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Clamp rotation
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        // Player horizontal rotation (Yaw)
        transform.Rotate(Vector3.up * mouseX);
    }
}