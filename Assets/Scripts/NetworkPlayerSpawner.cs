using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSpawner : NetworkBehaviour
{
    public static NetworkPlayerSpawner Instance;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private int lastSpawnIndex = -1;
    private void Awake() => Instance = this; // Initialise l'instance
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // On s'abonne à l'événement : "Un client a fini de charger la scène"
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        // On spawn uniquement pour les clients qui viennent de finir le chargement
        foreach (ulong clientId in clientsCompleted)
        {
            // On vérifie si ce client n'a pas déjà un objet joueur (pour éviter les doublons)
            if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject == null)
            {
                SpawnPlayer(clientId);
            }
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        Vector3 spawnPos = GetSpawnPoint();
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // Pas besoin de MoveGameObjectToScene si tu es en LoadSceneMode.Single, 
        // mais ça ne fait pas de mal.

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"[SPAWNER] Joueur spawné pour le client {clientId} à {spawnPos}");
    }

    public Vector3 GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return Vector3.zero;

        int randomIndex = Random.Range(0, spawnPoints.Length);

        // Si on a plus d'un point, on s'assure qu'il est différent du précédent
        if (spawnPoints.Length > 1)
        {
            while (randomIndex == lastSpawnIndex)
            {
                randomIndex = Random.Range(0, spawnPoints.Length);
            }
        }

        lastSpawnIndex = randomIndex;
        return spawnPoints[randomIndex].position;
    }
}