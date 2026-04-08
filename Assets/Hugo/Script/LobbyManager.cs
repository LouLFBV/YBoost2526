using UnityEngine;
using Unity.Netcode; // Si tu utilises Netcode for GameObjects
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string gameSceneName = "GameScene1";

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        // Une fois Host, on peut afficher un bouton "Démarrer" que seul le Host voit
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    // Cette fonction sera liée à ton bouton "Démarrer la partie"
    public void StartGame()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // Utilise le NetworkSceneManager pour changer tout le monde de scène
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
    }
}