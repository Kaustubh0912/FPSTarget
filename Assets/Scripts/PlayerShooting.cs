using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public float range = 100f;
    public Camera fpsCam;
    public LayerMask targetLayers = -1; // What layers can be hit

    [Header("Visual Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem muzzleSmoke;
    public GameObject impactEffectPrefab;
    //public LineRenderer laserSight; // Optional laser sight

    [Header("Ammo System")]
    public int maxAmmo = 30;
    private int currentAmmo;
    public float reloadTime = 1.5f;
    private bool isReloading = false;
    public KeyCode reloadKey = KeyCode.R;
    public bool autoReload = true; // Auto reload when empty

    [Header("Firing Modes")]
    public bool isAutomatic = false;
    public float fireRate = 600f; // Rounds per minute
    private float nextTimeToFire = 0f;

    [Header("Recoil System")]
    public AdvancedRecoilSystem recoilSystem;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    private float nextEmptySoundTime = 0f;
    public float emptySoundCooldown = 0.1f;

    [Header("Weapon Sway")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float swaySmoothness = 6f;
    public Transform weaponSwayObject;
    private Vector3 initialSwayPosition;

    [Header("Score System")]
    private int score = 0;
    public UnityEvent<int> OnScoreChanged; // Event for score changes
    public UnityEvent<int> OnAmmoChanged;  // Event for ammo changes
    public UnityEvent OnReloadStart;       // Event when reload starts
    public UnityEvent OnReloadComplete;    // Event when reload completes

    // Performance optimization - cache hit results
    private RaycastHit lastHit;
    private bool hasValidHit = false;

    // Crosshair expansion for accuracy feedback
    [Header("Accuracy Feedback")]
    public bool showAccuracyFeedback = true;
    public float baseAccuracy = 0.95f;
    public float movementAccuracyPenalty = 0.3f;
    private float currentAccuracy = 1f;

    void Start()
    {
        InitializeComponents();
        InitializeAmmo();
        InitializeWeaponSway();
        InitializeAudio();
    }

    void InitializeComponents()
    {
        if (fpsCam == null)
        {
            fpsCam = Camera.main ?? FindObjectOfType<Camera>();
            if (fpsCam == null)
            {
                Debug.LogError("No camera found for PlayerShooting!");
            }
        }

        if (recoilSystem == null)
        {
            recoilSystem = GetComponent<AdvancedRecoilSystem>();
        }
    }

    void InitializeAmmo()
    {
        currentAmmo = maxAmmo;
        OnAmmoChanged?.Invoke(currentAmmo);
    }

    void InitializeWeaponSway()
    {
        if (weaponSwayObject != null)
        {
            initialSwayPosition = weaponSwayObject.localPosition;
        }
    }

    void InitializeAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Update()
    {
        HandleInput();
        HandleWeaponSway();
        UpdateAccuracy();
        //UpdateLaserSight();
    }

    void HandleInput()
    {
        // Handle shooting input
        bool shouldShoot = isAutomatic ?
            Input.GetButton("Fire1") && Time.time >= nextTimeToFire :
            Input.GetButtonDown("Fire1");

        if (shouldShoot)
        {
            Shoot();
        }

        // Handle reload input
        if (Input.GetKeyDown(reloadKey) && CanReload())
        {
            StartCoroutine(Reload());
        }

        // Auto reload when empty (if enabled)
        if (autoReload && currentAmmo <= 0 && !isReloading && CanReload())
        {
            StartCoroutine(Reload());
        }
    }

    void UpdateAccuracy()
    {
        if (!showAccuracyFeedback) return;

        // Check if player is moving
        bool isMoving = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;

        float targetAccuracy = isMoving ?
            baseAccuracy - movementAccuracyPenalty :
            baseAccuracy;

        currentAccuracy = Mathf.Lerp(currentAccuracy, targetAccuracy, Time.deltaTime * 5f);
    }

    //void UpdateLaserSight()
    //{
    //    if (laserSight == null) return;

    //    Vector3 startPoint = fpsCam.transform.position;
    //    Vector3 direction = fpsCam.transform.forward;

    //    if (Physics.Raycast(startPoint, direction, out RaycastHit hit, range, targetLayers))
    //    {
    //        laserSight.SetPosition(0, startPoint);
    //        laserSight.SetPosition(1, hit.point);
    //    }
    //    else
    //    {
    //        laserSight.SetPosition(0, startPoint);
    //        laserSight.SetPosition(1, startPoint + direction * range);
    //    }
    //}

    void HandleWeaponSway()
    {
        if (weaponSwayObject == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Vector3 targetPosition = new Vector3(
            -mouseX * swayAmount,
            -mouseY * swayAmount,
            0
        );

        targetPosition.x = Mathf.Clamp(targetPosition.x, -maxSwayAmount, maxSwayAmount);
        targetPosition.y = Mathf.Clamp(targetPosition.y, -maxSwayAmount, maxSwayAmount);

        weaponSwayObject.localPosition = Vector3.Lerp(
            weaponSwayObject.localPosition,
            initialSwayPosition + targetPosition,
            Time.deltaTime * swaySmoothness
        );
    }

    void Shoot()
    {
        if (!CanShoot())
        {
            HandleEmptyShoot();
            return;
        }

        // Set next fire time for automatic weapons
        if (isAutomatic)
        {
            nextTimeToFire = Time.time + 60f / fireRate;
        }

        // Apply accuracy
        Vector3 shootDirection = CalculateShootDirection();

        // Perform raycast
        if (Physics.Raycast(fpsCam.transform.position, shootDirection, out lastHit, range, targetLayers))
        {
            hasValidHit = true;
            ProcessHit(lastHit);
        }
        else
        {
            hasValidHit = false;
            // Show miss feedback
            UIManager.Instance?.ShowHitFeedback("Miss", 0, false);
        }

        // Execute shoot effects
        ExecuteShootEffects();

        // Consume ammo
        ConsumeAmmo();
    }

    Vector3 CalculateShootDirection()
    {
        Vector3 baseDirection = fpsCam.transform.forward;

        if (!showAccuracyFeedback || currentAccuracy >= 1f)
        {
            return baseDirection;
        }

        // Add inaccuracy based on current accuracy
        float inaccuracy = 1f - currentAccuracy;
        Vector3 randomSpread = new Vector3(
            Random.Range(-inaccuracy, inaccuracy),
            Random.Range(-inaccuracy, inaccuracy),
            0
        ) * 0.1f; // Spread multiplier

        return (baseDirection + randomSpread).normalized;
    }

    void ProcessHit(RaycastHit hit)
    {
        Debug.Log("Hit: " + hit.transform.name);

        // Create impact effect
        CreateImpactEffect(hit);

        // Check for different target types
        if (TryHitBullseyeZone(hit) || TryHitTarget(hit) || TryHitDestructible(hit))
        {
            return; // Hit was processed
        }
    }

    bool TryHitBullseyeZone(RaycastHit hit)
    {
        BullseyeZone zone = hit.collider.GetComponent<BullseyeZone>();
        if (zone != null)
        {
            zone.Hit();
            AddScore(zone.points);

            // Show hit feedback through UIManager
            UIManager.Instance?.ShowHitFeedback(zone.zoneName, zone.points, true);
            return true;
        }
        return false;
    }

    bool TryHitTarget(RaycastHit hit)
    {
        Target target = hit.transform.GetComponent<Target>();
        if (target != null)
        {
            target.Hit();
            AddScore(target.points);

            // Show hit feedback
            UIManager.Instance?.ShowHitFeedback("Target", target.points, true);
            return true;
        }
        return false;
    }

    bool TryHitDestructible(RaycastHit hit)
    {
        Destructible destructible = hit.transform.GetComponent<Destructible>();
        if (destructible != null)
        {
            destructible.TakeDamage(25f);
            return true;
        }
        return false;
    }

    void CreateImpactEffect(RaycastHit hit)
    {
        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, hit.point,
                Quaternion.LookRotation(hit.normal));
            Destroy(impact, .2f); // Slightly longer lifetime
        }
    }

    void ExecuteShootEffects()
    {
        // Play muzzle effects
        if (muzzleFlash != null) muzzleFlash.Play();
        if (muzzleSmoke != null) muzzleSmoke.Play();

        // Play shoot sound
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // Apply recoil
        recoilSystem?.FireWeapon();
    }

    void ConsumeAmmo()
    {
        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo);

        // Check for low ammo warning
        if (currentAmmo <= maxAmmo * 0.25f && currentAmmo > 0)
        {
            UIManager.Instance?.ShowLowAmmoWarning();
        }
    }

    void HandleEmptyShoot()
    {
        if (currentAmmo <= 0 && Time.time >= nextEmptySoundTime)
        {
            if (audioSource != null && emptySound != null)
            {
                audioSource.PlayOneShot(emptySound);
            }
            nextEmptySoundTime = Time.time + emptySoundCooldown;
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        OnReloadStart?.Invoke();
        Debug.Log("Reloading...");

        // Play reload sound
        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        //// Reset recoil during reload
        //recoilSystem?.ResetRecoil();

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        OnAmmoChanged?.Invoke(currentAmmo);
        OnReloadComplete?.Invoke();

        Debug.Log("Reload complete.");
    }

    // Public methods
    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }

    public int GetScore() => score;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool CanShoot() => !isReloading && currentAmmo > 0;
    public bool CanReload() => currentAmmo < maxAmmo && !isReloading;
    public bool IsReloading() => isReloading;
    public float GetAccuracy() => currentAccuracy;

    // Weapon modification methods
    public void SetFireRate(float newFireRate) => fireRate = newFireRate;
    public void SetAutomatic(bool automatic) => isAutomatic = automatic;
    public void SetRecoilMultiplier(float multiplier) => recoilSystem?.SetRecoilMultiplier(multiplier);
    public void SetRecoilPattern(Vector2[] pattern) => recoilSystem?.SetCustomPattern(pattern);

    // Reset method for game restart
    public void ResetWeapon()
    {
        score = 0;
        currentAmmo = maxAmmo;
        isReloading = false;
        nextTimeToFire = 0f;

        OnScoreChanged?.Invoke(score);
        OnAmmoChanged?.Invoke(currentAmmo);
    }
}