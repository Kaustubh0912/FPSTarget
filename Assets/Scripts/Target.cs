using UnityEngine;

public class Target : MonoBehaviour
{
    public int points = 10; 
    public Color hitColor = Color.red; 
    private Renderer rend;
    private bool hasBeenHit = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    public void Hit()
    {
        if (hasBeenHit) return;

        hasBeenHit = true;

        if (rend != null)
        {
            rend.material.color = hitColor;
        }

        Destroy(gameObject, 0.1f); 
    }
}