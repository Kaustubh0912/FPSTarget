using UnityEngine;

public class BullseyeZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public int points = 10;
    public Color hitColor = Color.white;
    public string zoneName = "Zone";

    private Renderer rend;
    private Color originalColor;
    private BullseyeTarget parentTarget;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }

        // Find the parent BullseyeTarget
        parentTarget = GetComponentInParent<BullseyeTarget>();
    }

    public void Hit()
    {
        // Visual feedback
        if (rend != null)
        {
            rend.material.color = hitColor;
            // Reset color after a short time
            Invoke("ResetColor", 0.5f);
        }

        // Notify parent target
        if (parentTarget != null)
        {
            parentTarget.OnZoneHit(this);
        }
    }

    void ResetColor()
    {
        if (rend != null)
        {
            rend.material.color = originalColor;
        }
    }
}