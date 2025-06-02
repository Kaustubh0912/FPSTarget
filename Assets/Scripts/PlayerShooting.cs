using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerShooting : MonoBehaviour
{
    public float range = 100f;
    public Camera fpsCam; // Assign your Main Camera (or the camera you shoot from)
    public int score = 0;

    // Optional: For visual feedback
    public ParticleSystem muzzleFlash; // Assign a particle system if you have one
    public GameObject impactEffectPrefab; // Assign a prefab for hit impact

    void Start()
    {
        if (fpsCam == null)
        {
            fpsCam = Camera.main;
            Debug.LogWarning("FPS Camera not assigned to PlayerShooting script. Attempting to use Camera.main.");
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1")) // "Fire1" is usually Left Mouse Button
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name); // Log what was hit

            // Optional: Instantiate impact effect
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.Hit();
                score += target.points; // Assuming Target script has a public 'points' field
                Debug.Log("Score: " + score);
                // Update UI here if you have one (see Step 6)
            }
        }
    }

    // Public method to get score if needed by a UI manager
    public int GetScore()
    {
        return score;
    }
}