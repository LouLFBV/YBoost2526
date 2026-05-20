using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject playerEntryTemplate; // Ton texte template
    [SerializeField] private Transform container;           // Le Vertical Layout Group

    private List<GameObject> playerEntries = new List<GameObject>();

    private void Start()
    {
        // On s'abonne aux événements de Netcode
        NetworkManager.Singleton.OnClientConnectedCallback += UpdatePlayerList;
        NetworkManager.Singleton.OnClientDisconnectCallback += UpdatePlayerList;
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
            string playerName = client.ClientId == NetworkManager.Singleton.LocalClientId ? "Moi" : $"Joueur {client.ClientId}";
            newEntry.GetComponent<TextMeshProUGUI>().text = playerName;

            playerEntries.Add(newEntry);
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