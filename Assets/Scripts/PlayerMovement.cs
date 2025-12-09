using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 2.5f;
    public float jumpHeight = 2f;
    public float gravity = -10f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float crouchTransitionSpeed = 8f;
    public bool crouchToggle = true;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform playerCamera;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool isGrounded;

    private float targetSpeed;
    private bool isCrouching = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleGroundCheck();
        HandleCrouch();
        HandleMovement();
        HandleMouseLook();
        ApplyGravity();
        SmoothCrouchHeight();
    }

    void HandleGroundCheck()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (isCrouching)
            targetSpeed = crouchSpeed;
        else if (Input.GetKey(KeyCode.LeftShift))
            targetSpeed = sprintSpeed;
        else
            targetSpeed = walkSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * targetSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void HandleCrouch()
    {
        if (crouchToggle)
        {
            if (Input.GetKeyDown(KeyCode.C))
                isCrouching = !isCrouching;
        }
        else
        {
            isCrouching = Input.GetKey(KeyCode.C);
        }
    }

    void SmoothCrouchHeight()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

        Vector3 camPos = playerCamera.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetHeight - 0.2f, Time.deltaTime * crouchTransitionSpeed);
        playerCamera.localPosition = camPos;
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public bool IsGrounded() => isGrounded;
    public float GetCurrentSpeed() => targetSpeed;
    public Vector3 GetMovementVelocity() => controller.velocity;
    public bool IsCrouching() => isCrouching;
}
