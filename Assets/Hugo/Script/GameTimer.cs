using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Paramètres du Timer")]
    public float totalTime = 300f; // Durée totale (ex: 5 min)
    private float timeRemaining;
    private bool timerIsRunning = false;
    private bool trainTriggered = false; // Pour ne lancer le train qu'une seule fois

    [Header("Références")]
    public TextMeshProUGUI timerText;
    public WaypointMover trainMover; // Glisse ton train ici dans l'Inspecteur

    [Header("Panel de fin")]
    [SerializeField] private GameObject endGamePanel;

    private void Start()
    {
        timeRemaining = totalTime;
        timerIsRunning = true;
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;

                // --- Logique du déclenchement du train ---
                // Si on a dépassé la moitié du temps et que le train n'est pas encore parti
                if (!trainTriggered && timeRemaining <= (totalTime / 2))
                {
                    if (trainMover != null)
                    {
                        trainMover.StartTrain();
                        trainTriggered = true; // Empêche de relancer l'ordre à chaque frame
                    }
                }

                DisplayTime(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                timerIsRunning = false;
                EndGame();
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void EndGame()
    {
        endGamePanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}