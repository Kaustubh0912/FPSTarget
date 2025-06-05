using UnityEngine;

public class BullseyeTarget : MonoBehaviour
{
    [Header("Target Settings")]
    public int totalHits = 0;
    public int totalScore = 0;
    public bool resetAfterHit = false;
    public float resetDelay = 2f;

    [Header("Audio & Effects")]
    public AudioClip hitSound;
    public GameObject hitEffect;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Optional: Set up zone points if you want to do it from the parent
        SetupZonePoints();
    }

    void SetupZonePoints()
    {
        // Find all child zones and set their points
        BullseyeZone[] zones = GetComponentsInChildren<BullseyeZone>();

        foreach (BullseyeZone zone in zones)
        {
            // Set points based on zone name or you can do this manually in inspector
            switch (zone.name.ToLower())
            {
                case "yellow":
                    zone.points = 50;
                    zone.zoneName = "Bullseye";
                    break;
                case "red":
                    zone.points = 25;
                    zone.zoneName = "Inner Ring";
                    break;
                case "blue":
                    zone.points = 15;
                    zone.zoneName = "Middle Ring";
                    break;
                case "black":
                    zone.points = 10;
                    zone.zoneName = "Outer Ring";
                    break;
                case "backplate":
                    zone.points = 5;
                    zone.zoneName = "Edge";
                    break;
            }
        }
    }

    public void OnZoneHit(BullseyeZone zone)
    {
        totalHits++;
        totalScore += zone.points;

        Debug.Log($"Target hit! Zone: {zone.zoneName}, Points: {zone.points}, Total Score: {totalScore}");

        // Play sound effect
        if (audioSource && hitSound)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Spawn hit effect
        if (hitEffect)
        {
            Instantiate(hitEffect, zone.transform.position, Quaternion.identity);
        }

        // Optional: Reset target after hit
        if (resetAfterHit)
        {
            Invoke("ResetTarget", resetDelay);
        }

   }

    void ResetTarget()
    {
        totalHits = 0;
        totalScore = 0;
        Debug.Log("Target reset!");
    }

    // Method to manually reset the target
    public void ManualReset()
    {
        ResetTarget();
    }
}