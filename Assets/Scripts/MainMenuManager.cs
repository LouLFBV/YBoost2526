using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class MainMenuManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MatchmakingManager matchmaker;

    [Header("Panels")]
    //[SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject joinPanel;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI inputField;
    [SerializeField] private TextMeshProUGUI displayCodeText;
    [SerializeField] private GameObject boutonLancerPartie;
    [SerializeField] private GameObject boutonAnnulerSession; 

    // Appelé par le bouton "Créer une partie"
    // 1. L'Host crée la session mais RESTE dans le menu
    public async void OnClickCreateSession()
    {
        string code = await MatchmakingManager.Instance.StartHostWithRelay();
        if (!string.IsNullOrEmpty(code))
        {
            displayCodeText.text = "CODE : " + code;
            // On affiche un bouton "Lancer la Map" qui était caché
            boutonLancerPartie.SetActive(true);
        }
    }

    // 2. L'Host clique sur ce nouveau bouton quand ses potes sont là
    public void OnClickStartGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            // C'est ICI qu'on change de scène pour tout le monde
            NetworkManager.Singleton.SceneManager.LoadScene("Map desert", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    // Appelé par le bouton "Arrêter la session"
    public void OnClickStopSession()
    {
        // 1. On déconnecte Netcode (coupe le Host/Serveur proprement)
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[MAIN MENU] Session arrêtée et NetworkManager éteint.");
        }

        // 2. On remet l'interface à zéro pour l'Host
        if (displayCodeText != null) displayCodeText.text = "CODE : ---";
        if (boutonLancerPartie != null) boutonLancerPartie.SetActive(false);
        if (boutonAnnulerSession != null) boutonAnnulerSession.SetActive(false); // On le recache
    }

    // Appelé par le bouton "Rejoindre une partie"
    public void OnClickShowJoinPanel()
    {
        //mainMenuPanel?.SetActive(false);
        joinPanel.SetActive(true);
    }

    // Appelé par le bouton "Valider" du JoinPanel
    public async void OnClickJoin()
    {
        string codeAtaper = inputField.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(codeAtaper) || codeAtaper.Length < 6)
        {
            Debug.LogError("Code invalide ou trop court");
            return;
        }

        bool success = await MatchmakingManager.Instance.JoinClientWithRelay(codeAtaper);
    }
}