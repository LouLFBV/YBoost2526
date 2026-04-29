using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        // Seulement le serveur gère le spawn
        if (!IsServer) return;

        // Spawner pour chaque client déjà connecté
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            SpawnPlayer(client.ClientId);
        }

        // Écouter les nouvelles connexions
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
    }

    private void SpawnPlayer(ulong clientId)
    {
        // ✅ Instantiate avec le PlayerInput déjà désactivé dans le prefab
        GameObject player = Instantiate(playerPrefab, GetSpawnPoint(clientId), Quaternion.identity);

        // ✅ MoveToScene AVANT Spawn
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
            player, gameObject.scene
        );

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        else
            Debug.LogError("NetworkObject manquant sur le prefab joueur !");
    }

    private Vector3 GetSpawnPoint(ulong clientId)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[(int)(clientId % (ulong)spawnPoints.Length)].position;
        return Vector3.zero;
    }
}