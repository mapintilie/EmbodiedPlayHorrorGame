    using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public float timeElapsed = 0f;
    public TextMeshProUGUI timerText;

    void Update()
    {
        timeElapsed += Time.deltaTime;

        int minutes = Mathf.FloorToInt(timeElapsed / 60);
        int seconds = Mathf.FloorToInt(timeElapsed % 60);

        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}