using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using System.Text.RegularExpressions;

public class MatchmakingManager : MonoBehaviour
{
    // On met ça en static pour y accéder facilement depuis l'UI
    public static MatchmakingManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    async void Start()
    {
        // INITIALISATION DES SERVICES UNITY (Obligatoire pour Relay)
        try
        {
            await UnityServices.InitializeAsync();

            // On se connecte anonymement au service
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[RELAY] Connecté aux services avec l'ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RELAY] Erreur d'initialisation : {e.Message}");
        }
    }

    // --- FONCTION POUR L'HOST ---
    public async Task<string> StartHostWithRelay(int maxPlayers = 4)
    {
        try
        {
            // 1. On demande au serveur Relay de nous réserver une place
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            // 2. On récupère le code de jointure (ex: AB12CD)
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 3. On configure le transport NetworkManager pour passer par le Relay
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // 4. On lance l'Host
            NetworkManager.Singleton.StartHost();

            Debug.Log($"[RELAY] Host démarré ! Code : {joinCode}");
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RELAY] Erreur Host : {e.Message}");
            return null;
        }
    }

    public async Task<bool> JoinClientWithRelay(string joinCode)
    {
        try
        {
            // 1. NETTOYAGE RADICAL
            // On enlève les espaces, on met en majuscule
            string cleanedCode = joinCode.Trim().ToUpper();

            // 2. FILTRAGE DES CARACTÈRES INVISIBLES
            // On ne garde QUE les lettres et chiffres (A-Z, 0-9)
            cleanedCode = Regex.Replace(cleanedCode, @"[^A-Z0-9]", "");

            Debug.Log($"[RELAY] Tentative avec le code nettoyé : '{cleanedCode}' (Longueur: {cleanedCode.Length})");

            if (cleanedCode.Length != 6)
            {
                Debug.LogError("[RELAY] Le code doit faire exactement 6 caractères après nettoyage !");
                return false;
            }

            // 3. CONNEXION
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(cleanedCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            return NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RELAY] Erreur Join : {e.Message}");
            return false;
        }
    }
}