using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections; // Ne pas oublier pour les Coroutines !
using UnityEngine.SceneManagement;

public class GameTimer : NetworkBehaviour
{
    [Header("Paramètres du Timer")]
    public float totalTime = 300f;
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>(300f);
    private bool timerIsRunning = false;
    private bool trainTriggered = false;

    [Header("Références")]
    public TextMeshProUGUI timerText;
    public WaypointMover trainMover;
    [SerializeField] private LeaderboardManager leaderboardManager;

    [Header("Paramètres Fin de Partie")]
    [SerializeField] private float delayBeforeMenu = 10f; // Temps d'affichage du tableau (10s)

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
        if (IsServer && timerIsRunning)
        {
            if (timeRemaining.Value > 0)
            {
                timeRemaining.Value -= Time.deltaTime;

                if (!trainTriggered && timeRemaining.Value <= (totalTime / 2))
                {
                    if (trainMover != null)
                    {
                        trainMover.StartTrain();
                        trainTriggered = true;
                    }
                }
            }
            else
            {
                timeRemaining.Value = 0;
                timerIsRunning = false;

                // 1. Le serveur prévient tout le monde d'afficher le tableau des scores
                EndGameRpc();

                // 2. Le serveur lance son propre compte à rebours avant de couper la partie
                StartCoroutine(ServerEndGameSequence());
            }
        }

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
        if (leaderboardManager != null)
        {
            leaderboardManager.ShowLeaderboard();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Optionnel : Si tu veux que les clients locaux lancent aussi la coroutine au cas où, 
        // mais la méthode ServerRpc ci-dessous gère déjà la fermeture propre globale.
    }

    // Cette coroutine tourne UNIQUEMENT sur le serveur
    private IEnumerator ServerEndGameSequence()
    {
        // On attend les 10 secondes pendant que les joueurs regardent le tableau
        yield return new WaitForSeconds(delayBeforeMenu);

        // On ordonne à TOUT LE MONDE (Serveur + Clients) de charger le menu principal
        ReturnToMenuRpc();

        // Petit délai technique pour laisser le RPC s'envoyer avant de couper le réseau
        yield return new WaitForSeconds(0.5f);

        // Fermeture propre de Netcode
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Chargement de la scène pour le Serveur (ou l'Hôte)
        SceneManager.LoadScene("MainMenu");
    }

    [Rpc(SendTo.NotServer)]
    private void ReturnToMenuRpc()
    {
        // Exécuté uniquement sur les clients : ils coupent Netcode et chargent le menu
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("MainMenu");
    }

    // Gardé au cas où un joueur clique sur le bouton "Menu Principal" manuellement
    //public void GoToMainMenu()
    //{
    //    if (NetworkManager.Singleton != null)
    //    {
    //        NetworkManager.Singleton.Shutdown();
    //    }
    //    SceneManager.LoadScene("MainMenu");
    //}
}