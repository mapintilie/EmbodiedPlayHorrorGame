using UnityEngine;
using TMPro;
using System.Globalization;

public class GameOverScreenManager : MonoBehaviour
{
    public TMP_Text angelsDestroyedText;
    public TMP_Text timeText;

    void Start()
    {
        angelsDestroyedText.text =
            "Angels destroyed: " + GameStats.AngelsDestroyed;

        int mins = (int)(GameStats.SurvivalTime / 60f);
        float secs = GameStats.SurvivalTime - mins * 60f;

        timeText.text = string.Format(
            CultureInfo.InvariantCulture,
            "Time: {0:00}:{1:00.00}",
            mins,
            secs
        );
    }
}