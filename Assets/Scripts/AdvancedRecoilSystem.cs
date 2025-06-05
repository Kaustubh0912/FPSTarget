using UnityEngine;
using System.Collections;

[System.Serializable]
public class RecoilPattern
{
    [Header("Recoil Pattern")]
    public Vector2[] recoilPattern = new Vector2[]
    {
            // --- Initial Strong Kick (First 1-3 shots) ---
        new Vector2(0.1f, 1.8f),    // Shot 1: Strong up, slight right tendency
        new Vector2(-0.3f, 1.5f),   // Shot 2: Pulls left, still very strong up
        new Vector2(0.4f, 1.2f),    // Shot 3: Pulls right, vertical starts to lessen

        // --- Mid-Spray Instability & Horizontal Sway (Shots 4-8) ---
        new Vector2(-0.6f, 0.9f),   // Shot 4: Strong pull left, moderate vertical
        new Vector2(0.7f, 0.7f),    // Shot 5: Sharp pull right, vertical lessens
        new Vector2(-0.4f, 0.8f),   // Shot 6: Pulls left again, slight increase in vertical (the "hiccup")
        new Vector2(0.5f, 0.6f),    // Shot 7: Pulls right, vertical continues to decrease
        new Vector2(-0.2f, 0.5f),   // Shot 8: Gentle pull left, settling vertically

        // --- Late Spray - Still requires control (Shots 9-12) ---
        new Vector2(0.3f, 0.4f),    // Shot 9: Slight pull right, less vertical
        new Vector2(-0.35f, 0.35f), // Shot 10: A bit more pronounced left, low vertical
        new Vector2(0.2f, 0.3f),    // Shot 11: Gentle right, minimal vertical
        new Vector2(0f, 0.25f)      // Shot 12: Almost centered, very low vertical (end of distinct pattern)
    };

    [Header("Pattern Settings")]
    public float baseRecoilMultiplier = 1f;
    public float recoilIntensity = 1f;
    public AnimationCurve recoilFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f);
}

[System.Serializable]
public class RecoilSettings
{
    [Header("Camera Recoil")]
    public float cameraRecoilMultiplier = 1f;
    public float cameraRecoverySpeed = 8f;
    public float maxCameraRecoil = 15f;

    [Header("Visual Recoil (Gun Model)")]
    public float gunRecoilMultiplier = 1f;
    public float gunRecoverySpeed = 12f;
    public Vector3 gunRecoilRotation = new Vector3(-5f, 0f, 0f);
    public Vector3 gunRecoilPosition = new Vector3(0f, 0f, -0.1f);

    [Header("Screen Shake")]
    public bool enableScreenShake = true;
    public float screenShakeIntensity = 0.5f;
    public float screenShakeDuration = 0.1f;

    [Header("Recovery")]
    public float recoilRecoveryDelay = 0.3f; // Time before recoil starts recovering
    public float patternResetTime = 1f; // Time before pattern resets to first shot
}

public class AdvancedRecoilSystem : MonoBehaviour
{
    [Header("Recoil Configuration")]
    public RecoilPattern recoilPattern;
    public RecoilSettings recoilSettings;

    [Header("References")]
    public Transform playerCamera;
    public Transform gunModel;
    public Transform gunOriginalParent;

    // Internal state
    private Vector2 currentRecoil = Vector2.zero;
    private Vector2 targetRecoil = Vector2.zero;
    private int currentShotInPattern = 0;
    private float lastShotTime = 0f;
    private bool isRecovering = false;

    // Original transforms
    private Vector3 originalCameraRotation;
    private Vector3 baseCameraRotation; // Player's base rotation input
    private Vector3 originalGunPosition;
    private Vector3 originalGunRotation;

    // Screen shake
    private Vector3 originalCameraPosition;
    private Coroutine screenShakeCoroutine;

    // Recovery coroutine
    private Coroutine recoveryCoroutine;

    void Start()
    {
        // Store original transforms
        if (playerCamera != null)
        {
            originalCameraRotation = playerCamera.localEulerAngles;
            originalCameraPosition = playerCamera.localPosition;
        }

        if (gunModel != null)
        {
            originalGunPosition = gunModel.localPosition;
            originalGunRotation = gunModel.localEulerAngles;
        }

        // Initialize recoil pattern if empty
        if (recoilPattern.recoilPattern == null || recoilPattern.recoilPattern.Length == 0)
        {
            InitializeDefaultPattern();
        }
    }

    void InitializeDefaultPattern()
    {
        recoilPattern.recoilPattern = new Vector2[]
        {
            new Vector2(0f, 1f),
            new Vector2(-0.3f, 0.8f),
            new Vector2(0.5f, 0.9f),
            new Vector2(-0.2f, 0.7f),
            new Vector2(0.4f, 0.6f),
            new Vector2(-0.6f, 0.5f),
            new Vector2(0.7f, 0.4f),
            new Vector2(-0.4f, 0.3f),
            new Vector2(0.3f, 0.2f),
            new Vector2(0f, 0.1f)
        };
    }

    void Update()
    {
        // Smoothly apply recoil to camera
        if (playerCamera != null)
        {
            ApplyCameraRecoil();
        }

        // Apply visual recoil to gun
        if (gunModel != null)
        {
            ApplyGunRecoil();
        }

        // Check if we need to reset the pattern
        if (Time.time - lastShotTime > recoilSettings.patternResetTime)
        {
            currentShotInPattern = 0;
        }
    }

    public void FireWeapon()
    {
        lastShotTime = Time.time;

        // Stop any ongoing recovery
        if (recoveryCoroutine != null)
        {
            StopCoroutine(recoveryCoroutine);
        }
        isRecovering = false;

        // Get recoil from pattern
        Vector2 patternRecoil = GetRecoilFromPattern();

        // Apply recoil multipliers and randomization
        float randomMultiplier = Random.Range(0.8f, 1.2f); // Add some randomness
        Vector2 finalRecoil = patternRecoil * recoilSettings.cameraRecoilMultiplier *
                             recoilPattern.recoilIntensity * randomMultiplier;

        // Clamp recoil to maximum values
        finalRecoil.y = Mathf.Min(finalRecoil.y, recoilSettings.maxCameraRecoil);
        finalRecoil.x = Mathf.Clamp(finalRecoil.x, -recoilSettings.maxCameraRecoil, recoilSettings.maxCameraRecoil);

        // Add to current recoil
        targetRecoil += finalRecoil;

        // Apply screen shake
        if (recoilSettings.enableScreenShake)
        {
            ApplyScreenShake();
        }

        // Start recovery after delay
        recoveryCoroutine = StartCoroutine(StartRecoveryAfterDelay());

        // Advance pattern
        currentShotInPattern = (currentShotInPattern + 1) % recoilPattern.recoilPattern.Length;
    }

    Vector2 GetRecoilFromPattern()
    {
        if (recoilPattern.recoilPattern == null || recoilPattern.recoilPattern.Length == 0)
        {
            return Vector2.up; // Default upward recoil
        }

        Vector2 baseRecoil = recoilPattern.recoilPattern[currentShotInPattern];

        // Apply falloff curve based on shot number
        float falloffMultiplier = recoilPattern.recoilFalloff.Evaluate(
            (float)currentShotInPattern / recoilPattern.recoilPattern.Length);

        return baseRecoil * recoilPattern.baseRecoilMultiplier * falloffMultiplier;
    }

    void ApplyCameraRecoil()
    {
        // Smoothly interpolate current recoil towards target
        if (!isRecovering)
        {
            currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, Time.deltaTime * 15f);
        }
        else
        {
            // Recovery - move back towards zero
            currentRecoil = Vector2.Lerp(currentRecoil, Vector2.zero,
                Time.deltaTime * recoilSettings.cameraRecoverySpeed);
            targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero,
                Time.deltaTime * recoilSettings.cameraRecoverySpeed);
        }

        // Combine base camera rotation (from player mouse input) with recoil
        Vector3 recoilRotation = new Vector3(-currentRecoil.y, currentRecoil.x, 0f);
        Vector3 finalRotation = baseCameraRotation + recoilRotation;

        // Apply to camera rotation
        playerCamera.localRotation = Quaternion.Euler(finalRotation);
    }

    // Method called by PlayerMovement to set base camera rotation
    public void SetBaseCameraRotation(Vector3 baseRotation)
    {
        baseCameraRotation = baseRotation;
    }

    void ApplyGunRecoil()
    {
        // Calculate gun recoil based on camera recoil
        float recoilIntensity = currentRecoil.magnitude / recoilSettings.maxCameraRecoil;

        // Position recoil (gun kicks back)
        Vector3 targetGunPosition = originalGunPosition +
            (recoilSettings.gunRecoilPosition * recoilIntensity * recoilSettings.gunRecoilMultiplier);

        // Rotation recoil (gun tilts up)
        Vector3 targetGunRotation = originalGunRotation +
            (recoilSettings.gunRecoilRotation * recoilIntensity * recoilSettings.gunRecoilMultiplier);

        // Smoothly interpolate
        gunModel.localPosition = Vector3.Lerp(gunModel.localPosition, targetGunPosition,
            Time.deltaTime * recoilSettings.gunRecoverySpeed);
        gunModel.localRotation = Quaternion.Lerp(gunModel.localRotation,
            Quaternion.Euler(targetGunRotation), Time.deltaTime * recoilSettings.gunRecoverySpeed);
    }

    void ApplyScreenShake()
    {
        if (screenShakeCoroutine != null)
        {
            StopCoroutine(screenShakeCoroutine);
        }
        screenShakeCoroutine = StartCoroutine(ScreenShake());
    }

    IEnumerator ScreenShake()
    {
        float elapsed = 0f;

        while (elapsed < recoilSettings.screenShakeDuration)
        {
            float intensity = Mathf.Lerp(recoilSettings.screenShakeIntensity, 0f,
                elapsed / recoilSettings.screenShakeDuration);

            Vector3 randomOffset = Random.insideUnitSphere * intensity * 0.1f;
            playerCamera.localPosition = originalCameraPosition + randomOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.localPosition = originalCameraPosition;
    }

    IEnumerator StartRecoveryAfterDelay()
    {
        yield return new WaitForSeconds(recoilSettings.recoilRecoveryDelay);
        isRecovering = true;
    }

    // Public methods for external scripts
    public void ResetRecoil()
    {
        currentRecoil = Vector2.zero;
        targetRecoil = Vector2.zero;
        currentShotInPattern = 0;
        isRecovering = false;

        if (recoveryCoroutine != null)
        {
            StopCoroutine(recoveryCoroutine);
        }
    }

    public void SetRecoilMultiplier(float multiplier)
    {
        recoilSettings.cameraRecoilMultiplier = multiplier;
    }

    public Vector2 GetCurrentRecoil()
    {
        return currentRecoil;
    }

    public int GetCurrentShotInPattern()
    {
        return currentShotInPattern;
    }

    // Method to create custom recoil patterns
    public void SetCustomPattern(Vector2[] newPattern)
    {
        recoilPattern.recoilPattern = newPattern;
        currentShotInPattern = 0;
    }
}