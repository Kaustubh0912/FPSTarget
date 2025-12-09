using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI ammoText;

    [Header("References")]
    public PlayerShooting playerShooting;

    void Awake()
    {
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
        if (playerShooting == null)
        {
            playerShooting = FindFirstObjectByType<PlayerShooting>();
        }

        if (playerShooting == null)
        {
            Debug.LogWarning("PlayerShooting not found. UI will not update.");
        }
    }

    void Update()
    {
        if (playerShooting == null) return;

        if (scoreText != null)
        {
            scoreText.text = "Score: " + playerShooting.GetScore();
        }

        if (ammoText != null)
        {
            ammoText.text = $"{playerShooting.GetCurrentAmmo()} / {playerShooting.GetMaxAmmo()}";
        }
    }
}
