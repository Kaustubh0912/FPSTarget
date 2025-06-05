using UnityEngine;

public class LaserSight : MonoBehaviour
{
    [Header("Laser Settings")]
    public LineRenderer lineRenderer;
    public Transform laserOrigin; // << ADD THIS: Assign your "LaserEmitterPoint" here
    public float maxDistance = 100f;
    public LayerMask hitLayers = -1;
    public bool isActive = true;
    public KeyCode toggleKey = KeyCode.L;

    [Header("Visual Effects")]
    public GameObject laserDot;
    public ParticleSystem laserSparkle;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip laserToggleSound;

    private Camera playerCamera;

    void Start()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                Debug.LogError("LaserSight: LineRenderer not found!", this);
                isActive = false; // Disable if no LineRenderer
                return;
            }
        }

        if (laserOrigin == null)
        {
            Debug.LogWarning("LaserSight: Laser Origin Transform not assigned. Laser will originate from this object's position.", this);
            laserOrigin = transform; // Fallback to this object's transform
        }

        playerCamera = Camera.main ?? FindObjectOfType<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("LaserSight: Player camera not found!", this);
            isActive = false; // Disable if no camera
        }

        SetLaserActive(isActive); // Set initial state based on isActive
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleLaser();
        }

        if (isActive && lineRenderer != null && playerCamera != null && laserOrigin != null)
        {
            UpdateLaser();
        }
        else if (isActive && lineRenderer != null) // If active but something is missing, disable visuals
        {
            lineRenderer.enabled = false;
            if (laserDot != null) laserDot.SetActive(false);
            if (laserSparkle != null && laserSparkle.isPlaying) laserSparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void UpdateLaser()
    {
        // Step 1: Determine the target point by raycasting from the camera center
        Vector3 screenCenterTargetPoint;
        Ray cameraRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(cameraRay, out RaycastHit screenCenterHit, maxDistance, hitLayers))
        {
            screenCenterTargetPoint = screenCenterHit.point;
        }
        else
        {
            screenCenterTargetPoint = cameraRay.origin + cameraRay.direction * maxDistance;
        }

        // Step 2: The laser's actual starting point is the laserOrigin on the gun
        Vector3 laserActualStartPoint = laserOrigin.position;

        // Step 3: Set the LineRenderer's first position
        lineRenderer.SetPosition(0, laserActualStartPoint);

        // Step 4: Raycast from the laserOrigin towards the screenCenterTargetPoint
        // This handles objects between the gun and the camera's focal point.
        Vector3 laserDirection = (screenCenterTargetPoint - laserActualStartPoint).normalized;
        float distanceToScreenCenterTarget = Vector3.Distance(laserActualStartPoint, screenCenterTargetPoint);

        if (Physics.Raycast(laserActualStartPoint, laserDirection, out RaycastHit laserHit, distanceToScreenCenterTarget, hitLayers))
        {
            // Laser hit an object before reaching the screenCenterTargetPoint
            lineRenderer.SetPosition(1, laserHit.point);

            if (laserDot != null)
            {
                laserDot.transform.position = laserHit.point;
                laserDot.transform.rotation = Quaternion.LookRotation(laserHit.normal);
                laserDot.SetActive(true);
            }
            if (laserSparkle != null)
            {
                laserSparkle.transform.position = laserHit.point;
                if (!laserSparkle.isPlaying) laserSparkle.Play();
            }
        }
        else
        {
            // Laser reaches the screenCenterTargetPoint (or max distance along that line) without obstruction
            lineRenderer.SetPosition(1, screenCenterTargetPoint);

            if (laserDot != null)
            {
                laserDot.transform.position = screenCenterTargetPoint;
                // Orient dot based on the original screen center hit, if it was a valid hit
                laserDot.transform.rotation = (screenCenterHit.collider != null) ? Quaternion.LookRotation(screenCenterHit.normal) : Quaternion.identity;
                laserDot.SetActive(true);
            }
            // Decide if sparkle plays. Generally, only on a "hard" surface hit.
            // If screenCenterHit was a valid collider, it means the camera was looking at something.
            if (laserSparkle != null)
            {
                if (screenCenterHit.collider != null)
                {
                    laserSparkle.transform.position = screenCenterTargetPoint;
                    if (!laserSparkle.isPlaying) laserSparkle.Play();
                }
                else if (laserSparkle.isPlaying)
                {
                    laserSparkle.Stop();
                }
            }
        }
    }

    public void ToggleLaser()
    {
        SetLaserActive(!isActive);

        if (audioSource && laserToggleSound)
            audioSource.PlayOneShot(laserToggleSound);
    }

    public void SetLaserActive(bool active)
    {
        isActive = active;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = isActive;
        }

        if (!isActive) // If turning OFF
        {
            if (laserDot != null) laserDot.SetActive(false);
            if (laserSparkle != null && laserSparkle.isPlaying)
            {
                // Stop emitting and clear existing particles for a clean cutoff
                laserSparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        // If turning ON, the UpdateLaser() method will handle enabling dot/sparkle
        // based on hit conditions in the next frame.
    }
}