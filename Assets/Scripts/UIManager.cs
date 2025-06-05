using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI hitText; // Shows "Hit!" or zone name
    public TextMeshProUGUI totalScoreText; // Optional: separate total score display

    [Header("Hit Feedback")]
    public float hitTextDuration = 1f;
    public Color hitTextColor = Color.green;
    public Color missTextColor = Color.red;

    [Header("Hit Feedback Animation")]
    public float hitTextDriftAmountY = 50f;     // How many UI units it moves upwards
    public float hitTextInitialScaleFactor = 1.2f; // How much it "pops" initially
    public float hitTextRandomOffsetX = 20f;    // Max random horizontal offset
    public float hitTextFadeInTime = 0.1f;      // Time to fade in and pop
    public float hitTextFadeOutStartTime = 0.6f; // When to start fading out (as a fraction of total duration)

    [Header("Screen Clamping")]
    public float screenPadding = 20f; // Padding from screen edges

    [Header("UI References")]
    public RectTransform mainCanvasRect;

    [Header("References")]
    public PlayerShooting playerShooting;

    // Cached values to avoid unnecessary updates
    private int lastScore = -1;
    private int lastAmmo = -1;
    private int lastMaxAmmo = -1;

    // Static instance for easy access from other scripts
    public static UIManager Instance { get; private set; }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeReferences();
        InitializeUI();
    }

    void InitializeReferences()
    {
        // Auto-find PlayerShooting if not assigned
        if (playerShooting == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerShooting = player.GetComponent<PlayerShooting>();
            }

            // If still not found, try finding by component
            if (playerShooting == null)
            {
                playerShooting = FindObjectOfType<PlayerShooting>();
            }
        }

        // Auto-find main canvas if not assigned
        if (mainCanvasRect == null)
        {
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                mainCanvasRect = mainCanvas.GetComponent<RectTransform>();
            }
        }

        // Log warnings for missing components
        if (playerShooting == null)
        {
            Debug.LogWarning("PlayerShooting not found. Score and ammo updates will not work.");
        }

        if (scoreText == null)
            Debug.LogWarning("Score Text not assigned to UIManager.");

        if (ammoText == null)
            Debug.LogWarning("Ammo Text not assigned to UIManager.");

        if (mainCanvasRect == null)
            Debug.LogWarning("Main Canvas RectTransform not found. Hit text clamping will not work properly.");
    }

    void InitializeUI()
    {
        // Set initial UI values
        UpdateScoreUI(true); // Force update
        UpdateAmmoUI(true); // Force update

        // Hide hit text initially
        if (hitText != null)
        {
            hitText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Only update UI when values actually change (performance optimization)
        UpdateScoreUI();
        UpdateAmmoUI();
    }

    public void UpdateScoreUI(bool forceUpdate = false)
    {
        if (scoreText == null || playerShooting == null) return;

        int currentScore = playerShooting.GetScore();

        // Only update if score changed or forced
        if (forceUpdate || currentScore != lastScore)
        {
            scoreText.text = "Score: " + currentScore;
            lastScore = currentScore;
        }
    }

    public void UpdateAmmoUI(bool forceUpdate = false)
    {
        if (ammoText == null || playerShooting == null) return;

        int currentAmmo = playerShooting.GetCurrentAmmo();
        int maxAmmo = playerShooting.GetMaxAmmo();

        // Only update if ammo changed or forced
        if (forceUpdate || currentAmmo != lastAmmo || maxAmmo != lastMaxAmmo)
        {
            ammoText.text = $"Ammo: {currentAmmo} / {maxAmmo}";

            // Change color based on ammo level
            if (currentAmmo <= 0)
            {
                ammoText.color = Color.red;
            }
            else if (currentAmmo <= maxAmmo * 0.25f) // Less than 25%
            {
                ammoText.color = Color.yellow;
            }
            else
            {
                ammoText.color = Color.white;
            }

            lastAmmo = currentAmmo;
            lastMaxAmmo = maxAmmo;
        }
    }

    // Call this when a target is hit
    public void ShowHitFeedback(string zoneName, int points, bool isHit = true)
    {
        if (hitText == null) return;

        // Stop any existing coroutine
        StopAllCoroutines();

        // Set text and color
        hitText.text = isHit ? $"{zoneName} +{points}" : "Miss!";
        hitText.color = isHit ? hitTextColor : missTextColor;

        // Reset position to center before starting animation
        if (mainCanvasRect != null)
        {
            hitText.rectTransform.anchoredPosition = Vector3.zero; // Center of the canvas
        }

        // Show hit feedback
        StartCoroutine(ShowHitTextCoroutine());
    }

    private IEnumerator ShowHitTextCoroutine()
    {
        if (hitText == null) yield break;

        hitText.gameObject.SetActive(true);

        RectTransform hitTextRect = hitText.rectTransform;
        Vector3 originalScale = hitText.transform.localScale;
        Color baseColor = hitText.color;

        // Reset to initial scale and get text dimensions for clamping
        hitText.transform.localScale = originalScale;

        // Force canvas update to get accurate text dimensions
        Canvas.ForceUpdateCanvases();

        // Get the center of the screen as our safe starting point
        Vector3 screenCenter = Vector3.zero; // This is usually the center for anchored UI elements

        // Calculate safe starting position with random offset
        float randomX = Random.Range(-hitTextRandomOffsetX, hitTextRandomOffsetX);
        Vector3 desiredStartPosition = new Vector3(randomX, 0, 0); // Keep Y at center (0)

        // Clamp the starting position to ensure it's safe
        Vector3 safeStartPosition = ClampTextToScreen(desiredStartPosition, hitTextRect, originalScale);

        // Set the starting position
        hitTextRect.anchoredPosition = safeStartPosition;
        Vector3 basePosition = safeStartPosition;

        float elapsedTime = 0f;

        while (elapsedTime < hitTextDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / hitTextDuration);

            // --- Scale Animation ---
            float scaleProgress = Mathf.Clamp01(elapsedTime / hitTextFadeInTime);
            float currentScaleFactor = Mathf.Lerp(hitTextInitialScaleFactor, 1f, scaleProgress);
            hitText.transform.localScale = originalScale * currentScaleFactor;

            // --- Calculate Desired Position (Upward Drift) ---
            Vector3 desiredPosition = Vector3.Lerp(basePosition,
                                                 basePosition + new Vector3(0, hitTextDriftAmountY, 0),
                                                 progress);

            // --- Improved Clamping Logic ---
            Vector3 clampedPosition = ClampTextToScreen(desiredPosition, hitTextRect, hitText.transform.localScale);
            hitTextRect.anchoredPosition = clampedPosition;

            // --- Alpha (Fade) Animation ---
            float alpha;
            if (elapsedTime < hitTextFadeInTime)
            {
                alpha = Mathf.Lerp(0f, 1f, elapsedTime / hitTextFadeInTime);
            }
            else if (progress > hitTextFadeOutStartTime)
            {
                float fadeOutProgress = (progress - hitTextFadeOutStartTime) / (1f - hitTextFadeOutStartTime);
                alpha = Mathf.Lerp(1f, 0f, fadeOutProgress);
            }
            else
            {
                alpha = 1f;
            }
            hitText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }

        // --- Cleanup & Reset ---
        hitText.gameObject.SetActive(false);
        // Reset to screen center for next time
        hitTextRect.anchoredPosition = Vector3.zero; // Center position
        hitText.transform.localScale = originalScale;
        hitText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
    }

    private Vector3 ClampTextToScreen(Vector3 desiredPosition, RectTransform textRect, Vector3 textScale)
    {
        if (mainCanvasRect == null) return desiredPosition;

        Vector3 clampedPosition = desiredPosition;

        // Get the actual size of the text element (including scale)
        float textWidth = textRect.rect.width * textScale.x;
        float textHeight = textRect.rect.height * textScale.y;

        // Get canvas boundaries (for screen-space overlay canvas, this should be screen dimensions)
        Rect canvasRect = mainCanvasRect.rect;

        // Convert to proper screen space bounds
        float canvasHalfWidth = canvasRect.width * 0.5f;
        float canvasHalfHeight = canvasRect.height * 0.5f;

        float canvasLeft = -canvasHalfWidth + screenPadding;
        float canvasRight = canvasHalfWidth - screenPadding;
        float canvasBottom = -canvasHalfHeight + screenPadding;
        float canvasTop = canvasHalfHeight - screenPadding;

        // Get text bounds (assuming center pivot)
        float textHalfWidth = textWidth * 0.5f;
        float textHalfHeight = textHeight * 0.5f;

        // Clamp horizontal position
        if (clampedPosition.x - textHalfWidth < canvasLeft)
        {
            clampedPosition.x = canvasLeft + textHalfWidth;
        }
        else if (clampedPosition.x + textHalfWidth > canvasRight)
        {
            clampedPosition.x = canvasRight - textHalfWidth;
        }

        // Clamp vertical position
        if (clampedPosition.y - textHalfHeight < canvasBottom)
        {
            clampedPosition.y = canvasBottom + textHalfHeight;
        }
        else if (clampedPosition.y + textHalfHeight > canvasTop)
        {
            clampedPosition.y = canvasTop - textHalfHeight;
        }

        return clampedPosition;
    }

    // Method to add score (can be called from target scripts)
    public void AddScore(int points)
    {
        if (playerShooting != null)
        {
            playerShooting.AddScore(points);
        }
    }

    // Method to update total score if you have a separate display
    public void UpdateTotalScore(int totalScore)
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = "Total: " + totalScore;
        }
    }

    // Method to reset UI (useful for game restart)
    public void ResetUI()
    {
        lastScore = -1;
        lastAmmo = -1;
        lastMaxAmmo = -1;

        UpdateScoreUI(true);
        UpdateAmmoUI(true);

        if (hitText != null)
        {
            hitText.gameObject.SetActive(false);
        }
    }

    // Method to show low ammo warning
    public void ShowLowAmmoWarning()
    {
        if (ammoText != null)
        {
            StartCoroutine(BlinkAmmoText());
        }
    }

    private IEnumerator BlinkAmmoText()
    {
        Color originalColor = ammoText.color;

        for (int i = 0; i < 6; i++) // Blink 3 times
        {
            ammoText.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            ammoText.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }
}