using UnityEngine;
using System.Collections;

public class Destructible : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Visual Effects")]
    public GameObject destructionEffect;
    public GameObject[] debrisObjects;
    public float explosionForce = 500f;
    public float explosionRadius = 5f;

    [Header("Audio")]
    public AudioClip[] hitSounds;
    public AudioClip destructionSound;
    private AudioSource audioSource;

    [Header("Material Changes")]
    public Material damagedMaterial;
    public Material heavilyDamagedMaterial;
    private Material originalMaterial;
    private Renderer objectRenderer;

    [Header("Physics")]
    public bool becomePhysicsObject = true;
    public bool destroyAfterTime = true;
    public float destroyDelay = 5f;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        // Play hit sound
        PlayHitSound();

        // Update visual state based on health
        UpdateVisualState();

        // Check if destroyed
        if (currentHealth <= 0f)
        {
            DestroyObject();
        }
    }

    void PlayHitSound()
    {
        if (audioSource != null && hitSounds.Length > 0)
        {
            AudioClip randomHit = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.PlayOneShot(randomHit);
        }
    }

    void UpdateVisualState()
    {
        if (objectRenderer == null) return;

        float healthPercentage = currentHealth / maxHealth;

        if (healthPercentage <= 0.25f && heavilyDamagedMaterial != null)
        {
            objectRenderer.material = heavilyDamagedMaterial;
        }
        else if (healthPercentage <= 0.5f && damagedMaterial != null)
        {
            objectRenderer.material = damagedMaterial;
        }
    }

    void DestroyObject()
    {
        // Play destruction sound
        if (audioSource != null && destructionSound != null)
        {
            audioSource.PlayOneShot(destructionSound);
        }

        // Create destruction effect
        if (destructionEffect != null)
        {
            GameObject effect = Instantiate(destructionEffect, transform.position, transform.rotation);
            Destroy(effect, 3f);
        }

        // Create debris
        CreateDebris();

        // Make physics object or destroy
        if (becomePhysicsObject)
        {
            MakePhysicsObject();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void CreateDebris()
    {
        if (debrisObjects.Length == 0) return;

        for (int i = 0; i < debrisObjects.Length; i++)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 0.5f;
            GameObject debris = Instantiate(debrisObjects[i], spawnPos, Random.rotation);

            // Add physics to debris
            Rigidbody rb = debris.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = debris.AddComponent<Rigidbody>();
            }

            // Apply explosion force
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

            // Destroy debris after time
            Destroy(debris, destroyDelay + Random.Range(0f, 2f));
        }
    }

    void MakePhysicsObject()
    {
        // Add rigidbody if not present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Apply explosion force
        rb.AddExplosionForce(explosionForce * 0.5f, transform.position, explosionRadius);

        // Destroy after time if enabled
        if (destroyAfterTime)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    // Public methods
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    public bool IsDestroyed()
    {
        return currentHealth <= 0f;
    }
}