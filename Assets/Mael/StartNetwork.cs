using UnityEngine;
using Unity.Netcode;

public class StartNetwork : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void StartServer()
    {
        if (NetworkManager.Singleton.StartServer())
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    public void StartClient()
    {
        // Le client ne charge JAMAIS de scène lui-même. 
        // Il attend que le serveur lui dise "On change de scène".
        NetworkManager.Singleton.StartClient();
    }

    public void StartHost()
    {
        // 1. On lance le Host d'abord
        if (NetworkManager.Singleton.StartHost())
        {
            // 2. SEULEMENT APRES le succès du lancement, le serveur (Host) demande le changement de scène
            // C'est cette ligne qui synchronise tout le monde (Host + Clients)
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);

            Debug.Log("[NETCODE] Host lancé et changement de scène vers : " + sceneName);
        }
    }
}