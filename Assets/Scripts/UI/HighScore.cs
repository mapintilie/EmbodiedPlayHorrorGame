using UnityEngine;

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance;

    private int highScore;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    public int GetHighScore()
    {
        return highScore;
    }

    public void TrySetHighScore(int score)
    {
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();

            Debug.Log("New High Score: " + highScore);
        }
    }
}