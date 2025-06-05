using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float sprintSpeed = 8.0f;
    public float crouchSpeed = 2.5f;
    public float jumpHeight = 2.0f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100.0f;
    public Transform playerCamera;

    [Header("Advanced Movement")]
    public bool enableSprinting = true;
    public bool enableCrouching = true;
    public bool enableJumping = true;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;

    [Header("Movement Modifiers")]
    public float movementAcceleration = 10f;
    public float airControl = 0.3f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Private variables
    private CharacterController controller;
    private Vector3 playerVelocity;
    private Vector3 currentMovement;
    private float xRotation = 0f;
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;
    private float currentSpeed;
    private float originalHeight;
    private Vector3 originalCenter;

    // References
    private AdvancedRecoilSystem recoilSystem;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        recoilSystem = GetComponent<AdvancedRecoilSystem>();

        // Store original controller dimensions
        originalHeight = controller.height;
        originalCenter = controller.center;

        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
            if (playerCamera.parent != transform)
            {
                Debug.LogWarning("Player camera should be a child of the player for optimal performance.");
            }
        }

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleMouseLook();
        HandleMovementStates();
        ApplyGravity();

        // Apply final movement
        controller.Move(currentMovement * Time.deltaTime);
        controller.Move(playerVelocity * Time.deltaTime);
    }

    void HandleGroundCheck()
    {
        // More reliable ground check
        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Small downward force to keep grounded
        }
    }

    void HandleMovement()
    {
        // Get input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Calculate movement direction
        Vector3 targetMovement = transform.right * moveX + transform.forward * moveZ;
        targetMovement = Vector3.ClampMagnitude(targetMovement, 1f);

        // Determine current speed based on state
        currentSpeed = moveSpeed;
        if (isSprinting && !isCrouching) currentSpeed = sprintSpeed;
        if (isCrouching) currentSpeed = crouchSpeed;

        // Apply movement curve for more responsive feel
        float inputMagnitude = new Vector2(moveX, moveZ).magnitude;
        float curveMultiplier = movementCurve.Evaluate(inputMagnitude);
        targetMovement *= curveMultiplier;

        // Air control
        float controlMultiplier = isGrounded ? 1f : airControl;

        // Smooth movement acceleration
        currentMovement = Vector3.Lerp(
            currentMovement,
            targetMovement * currentSpeed * controlMultiplier,
            Time.deltaTime * movementAcceleration
        );

        // Handle jumping
        if (enableJumping && Input.GetButtonDown("Jump") && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Handle vertical mouse look - ALWAYS apply mouse input so players can compensate for recoil
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply base camera rotation (recoil system will add on top of this)
        if (playerCamera != null)
        {
            // Store the base rotation that player controls
            Vector3 baseRotation = new Vector3(xRotation, 0f, 0f);

            if (recoilSystem != null)
            {
                // Let recoil system handle the final camera rotation
                // It will add recoil on top of our base rotation
                recoilSystem.SetBaseCameraRotation(baseRotation);
            }
            else
            {
                // No recoil system, apply rotation directly
                playerCamera.localRotation = Quaternion.Euler(baseRotation);
            }
        }

        // Horizontal rotation (player body)
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovementStates()
    {
        // Handle sprinting
        if (enableSprinting)
        {
            isSprinting = Input.GetKey(sprintKey) &&
                         Input.GetAxis("Vertical") > 0 &&
                         isGrounded &&
                         !isCrouching;
        }

        // Handle crouching
        if (enableCrouching)
        {
            bool crouchInput = Input.GetKey(crouchKey);

            if (crouchInput && !isCrouching)
            {
                StartCrouch();
            }
            else if (!crouchInput && isCrouching)
            {
                StopCrouch();
            }
        }
    }

    void StartCrouch()
    {
        isCrouching = true;
        controller.height = originalHeight * 0.5f;
        controller.center = new Vector3(originalCenter.x, originalCenter.y * 0.5f, originalCenter.z);

        // Lower camera
        if (playerCamera != null)
        {
            Vector3 cameraPos = playerCamera.localPosition;
            cameraPos.y -= originalHeight * 0.25f;
            playerCamera.localPosition = cameraPos;
        }
    }

    void StopCrouch()
    {
        // Check if there's space to stand up
        if (CanStandUp())
        {
            isCrouching = false;
            controller.height = originalHeight;
            controller.center = originalCenter;

            // Raise camera
            if (playerCamera != null)
            {
                Vector3 cameraPos = playerCamera.localPosition;
                cameraPos.y += originalHeight * 0.25f;
                playerCamera.localPosition = cameraPos;
            }
        }
    }

    bool CanStandUp()
    {
        // Cast a ray upward to check for obstacles
        float checkDistance = originalHeight * 0.5f;
        return !Physics.Raycast(transform.position, Vector3.up, checkDistance);
    }

    void ApplyGravity()
    {
        playerVelocity.y += gravity * Time.deltaTime;
    }

    // Public getters
    public bool IsGrounded() => isGrounded;
    public bool IsSprinting() => isSprinting;
    public bool IsCrouching() => isCrouching;
    public float GetCurrentSpeed() => currentSpeed;
    public Vector3 GetMovementVelocity() => currentMovement;

    // Public setters for external modifications
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    // Method to handle external camera rotation (for recoil system)
    public void AddCameraRotation(Vector2 rotation)
    {
        xRotation += rotation.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        transform.Rotate(Vector3.up * rotation.x);
    }
}