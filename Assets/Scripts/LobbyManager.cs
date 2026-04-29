using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject playerEntryTemplate; // Ton texte template
    [SerializeField] private Transform container;           // Le Vertical Layout Group
    //[SerializeField] private GameObject startButton;        // Le bouton "Lancer la Map"

    private List<GameObject> playerEntries = new List<GameObject>();

    private void Start()
    {
        // On s'abonne aux événements de Netcode
        NetworkManager.Singleton.OnClientConnectedCallback += UpdatePlayerList;
        NetworkManager.Singleton.OnClientDisconnectCallback += UpdatePlayerList;

        // Seul l'Host peut voir le bouton "Lancer"
       // startButton.SetActive(false);
    }

    private void UpdatePlayerList(ulong clientId)
    {
        // Nettoyage de l'ancienne liste
        foreach (var entry in playerEntries) Destroy(entry);
        playerEntries.Clear();

        // On crée une ligne pour chaque joueur connecté
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            GameObject newEntry = Instantiate(playerEntryTemplate, container);
            newEntry.SetActive(true);

            // On affiche l'ID ou un nom générique
            string playerName = client.ClientId == NetworkManager.Singleton.LocalClientId ? "Moi (Host)" : $"Joueur {client.ClientId}";
            newEntry.GetComponent<TextMeshProUGUI>().text = playerName;

            playerEntries.Add(newEntry);
        }

        // Si je suis l'host, je peux afficher le bouton "Lancer" dès qu'il y a du monde
        //if (NetworkManager.Singleton.IsHost)
        //{
        //    startButton.SetActive(true);
        //}
    }

    // Fonction appelée par le bouton "Lancer la Map"
    public void StartGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Map desert", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    // N'oublie pas de te désabonner quand l'objet est détruit pour éviter les bugs !
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= UpdatePlayerList;
            NetworkManager.Singleton.OnClientDisconnectCallback -= UpdatePlayerList;
        }
    }
}