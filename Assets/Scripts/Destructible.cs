using UnityEngine;

public class Destructible : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Material Changes")]
    public Material damagedMaterial;
    private Renderer objectRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        objectRenderer = GetComponent<Renderer>();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        UpdateVisualState();

        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void UpdateVisualState()
    {
        if (objectRenderer == null) return;

        float healthPercentage = currentHealth / maxHealth;

        if (healthPercentage <= 0.9f && damagedMaterial != null)
        {
            objectRenderer.material = damagedMaterial;
        }
    }
}
