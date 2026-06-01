using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public float timeElapsed = 0f;
    public TextMeshProUGUI timerText;

    void Update()
    {
        timeElapsed += Time.deltaTime;

        int minutes = Mathf.FloorToInt(timeElapsed / 60f);
        float seconds = timeElapsed - minutes * 60f;

        timerText.text = string.Format(
            "{0:00}:{1:00.00}",
            minutes,
            seconds
        );
    }
}