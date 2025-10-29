using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    public float gameTime = 300f; // temps en secondes
    public TMP_Text timerText;

    void Update()
    {
        if (gameTime > 0)
        {
            gameTime -= Time.deltaTime;

            float minutes = Mathf.Floor(gameTime / 60);
            float seconds = Mathf.Floor(gameTime % 60);

            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            timerText.text = "Time's Up!";
        }
    }
}