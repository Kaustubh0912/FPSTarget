using UnityEngine;

public class Target : MonoBehaviour
{
    public int points = 10; // Points awarded for hitting this target
    public Color hitColor = Color.red; // Color to change to when hit (optional)
    private Renderer rend;
    private Color originalColor;
    private bool hasBeenHit = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }

    public void Hit()
    {
        if (hasBeenHit) return; // Prevent multiple hits on the same target if it's not destroyed

        Debug.Log(gameObject.name + " was hit!");
        hasBeenHit = true;

        if (rend != null)
        {
            rend.material.color = hitColor;
        }

        // You can add more effects here:
        // - Play a sound
        // - Instantiate a destruction particle effect
        // - Destroy(gameObject, 2f); // Destroy after 2 seconds
        // For now, we'll just change color and disable it

        // To make it "disappear" or unhittable again without destroying:
        // gameObject.SetActive(false); // Option 1: Deactivate

        // Option 2: Destroy it
        Destroy(gameObject, 0.1f); // Destroy almost immediately

        // If you want targets to respawn or reset, you'll need a more complex manager.
    }
}