using System;
using System.Collections;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public float range = 100f;
    public Camera fpsCam;
    public LayerMask targetLayers = -1;

    [Header("Visual Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem muzzleSmoke;
    public GameObject impactEffectPrefab;

    [Header("Ammo System")]
    public int maxAmmo = 30;
    private int currentAmmo;
    public float reloadTime = 1.5f;
    private bool isReloading = false;
    public KeyCode reloadKey = KeyCode.R;

    [Header("Firing Modes")]
    public bool isAutomatic = false;
    public float fireRate = 600f;
    private float nextTimeToFire = 0f;

    [Header("Audio Sources (Assign 3 AudioSources)")]
    public AudioSource shootSource;   // plays every bullet shot
    public AudioSource reloadSource;  // plays once per reload
    public AudioSource emptySource;   // plays only if not already playing

    [Header("Audio Clips")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;

    public event Action<int> OnScoreChanged;
    public event Action<int> OnAmmoChanged;

    private int score = 0;

    void Start()
    {
        if (fpsCam == null) fpsCam = Camera.main;

        currentAmmo = maxAmmo;
        OnAmmoChanged?.Invoke(currentAmmo);
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (isReloading) return;

        bool shouldShoot = isAutomatic ?
            Input.GetButton("Fire1") && Time.time >= nextTimeToFire :
            Input.GetButtonDown("Fire1");

        if (shouldShoot)
        {
            if (currentAmmo > 0)
            {
                Shoot();
            }
            else
            {
                PlayEmptySound();
            }
        }

        if (Input.GetKeyDown(reloadKey) && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
        }
    }

    void Shoot()
    {
        if (isAutomatic)
            nextTimeToFire = Time.time + 60f / fireRate;

        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo);

        if (muzzleFlash) muzzleFlash.Play();
        if (muzzleSmoke) muzzleSmoke.Play();

        PlayShootSound();

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, range, targetLayers))
        {
            ProcessHit(hit);
        }
    }

    void ProcessHit(RaycastHit hit)
    {
        if (impactEffectPrefab)
        {
            GameObject impact = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 0.5f);
        }

        int points = 0;

        BullseyeZone zone = hit.collider.GetComponent<BullseyeZone>();
        if (zone != null)
        {
            zone.Hit();
            points = zone.points;
        }
        else
        {
            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.Hit();
                points = target.points;
            }
        }

        Destructible destructible = hit.transform.GetComponent<Destructible>();
        if (destructible != null)
        {
            destructible.TakeDamage(25f);
        }

        if (points > 0)
        {
            AddScore(points);
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;

        PlayReloadSound();

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        OnAmmoChanged?.Invoke(currentAmmo);
    }
    
    void PlayShootSound()
    {
        if (shootSource && shootSound)
            shootSource.PlayOneShot(shootSound);
    }

    void PlayReloadSound()
    {
        if (reloadSource && reloadSound)
            reloadSource.PlayOneShot(reloadSound);
    }

    void PlayEmptySound()
    {
        if (emptySource && emptySound)
        {
            if (!emptySource.isPlaying)
                emptySource.PlayOneShot(emptySound);
        }
    }

    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }

    public int GetScore() => score;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
}
