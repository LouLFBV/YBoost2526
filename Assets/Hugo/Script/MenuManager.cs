using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement; // Indispensable pour changer de scène

public class MenuManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "NomDeTaSceneDeJeu";

    public void LaunchGame()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();

            // 2. On change de scène via le NetworkSceneManager (important en réseau)
            // Cela synchronise tous les clients connectés
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Il manque le NetworkManager dans la scène de Menu !");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Le jeu a été fermé.");
    }
}