using UnityEngine;
using TMPro;
using Unity.Netcode;

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

    private void Start()
    {
        timeRemaining = totalTime;
        if (NetworkManager.Singleton.IsServer)
        {
            timerIsRunning = true;
        }
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
                // EndGame();
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
        // Logique de fin de partie (ex: Score, menu de fin, etc.)
        // Dans CS:GO, si le temps finit et que la bombe n'est pas posée, les CT gagnent.
        Debug.Log("La partie est terminée ! Victoire des Anti-Terroristes.");
    }
}