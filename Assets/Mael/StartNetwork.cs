using UnityEngine;
using Unity.Netcode;

public class StartNetwork : MonoBehaviour
{
    public void StartServer()
    {
        if (NetworkManager.Singleton.StartServer())
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Zone1", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        // Le client NE charge PAS de scène ici.
        // Le serveur va le forcer automatiquement à rejoindre "Zone1"
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Zone1", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
