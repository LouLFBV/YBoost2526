using UnityEngine;
using TMPro;
using Unity.Netcode;

public class GameTimer : NetworkBehaviour // On change ici
{
    [Header("Paramètres du Timer")]
    public float totalTime = 300f;
    // On synchronise le temps restant du serveur vers les clients
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>(300f);
    private bool timerIsRunning = false;
    private bool trainTriggered = false;

    [Header("Références")]
    public TextMeshProUGUI timerText;
    public WaypointMover trainMover;

    // Référence vers ton nouveau LeaderboardManager
    [SerializeField] private LeaderboardManager leaderboardManager;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timeRemaining.Value = totalTime;
            timerIsRunning = true;
        }
    }

    void Update()
    {
        // Seul le serveur gère le décompte
        if (IsServer && timerIsRunning)
        {
            if (timeRemaining.Value > 0)
            {
                timeRemaining.Value -= Time.deltaTime;

                // Logique du train (Serveur uniquement)
                if (!trainTriggered && timeRemaining.Value <= (totalTime / 2))
                {
                    if (trainMover != null)
                    {
                        trainMover.StartTrain(); // Assure-toi que StartTrain gère son propre réseau ou est un RPC
                        trainTriggered = true;
                    }
                }
            }
            else
            {
                timeRemaining.Value = 0;
                timerIsRunning = false;
                EndGameRpc(); // On prévient tout le monde que c'est fini
            }
        }

        // Tout le monde affiche le temps synchronisé
        DisplayTime(timeRemaining.Value);
    }

    void DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    [Rpc(SendTo.Everyone)]
    private void EndGameRpc()
    {
        // On affiche le tableau des scores qu'on a créé juste avant !
        if (leaderboardManager != null)
        {
            leaderboardManager.ShowLeaderboard();
        }

        // On libère la souris
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Note: Évite Time.timeScale = 0 en réseau, cela peut casser Netcode.
        // Il vaut mieux désactiver les scripts de tir/mouvement des joueurs.
    }

    public void GoToMainMenu()
    {
        // Avant de quitter, on se déconnecte proprement
        NetworkManager.Singleton.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}