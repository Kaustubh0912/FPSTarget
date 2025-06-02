using UnityEngine;
using TMPro; // Make sure to include this for TextMeshPro

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText; // Assign your ScoreText UI element here
    public PlayerShooting playerShooting; // Assign your Player GameObject (which has PlayerShooting script)

    void Start()
    {
        if (playerShooting == null)
        {
            // Try to find the player if not assigned
            GameObject player = GameObject.FindGameObjectWithTag("Player"); // Make sure your Player has the "Player" tag
            if (player != null)
            {
                playerShooting = player.GetComponent<PlayerShooting>();
            }
        }
        if (playerShooting == null)
        {
            Debug.LogError("PlayerShooting script not found on player or not assigned to UIManager.");
        }
        if (scoreText == null)
        {
            Debug.LogError("ScoreText not assigned to UIManager.");
        }
        UpdateScoreUI();
    }

    void Update()
    {
        // Update score continuously (or call this method when score changes)
        UpdateScoreUI();
    }

    public void UpdateScoreUI()
    {
        if (scoreText != null && playerShooting != null)
        {
            scoreText.text = "Score: " + playerShooting.GetScore();
        }
    }
}