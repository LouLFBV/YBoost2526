using Unity.Netcode; // Obligatoire
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WeaponSpawner : NetworkBehaviour // Doit être NetworkBehaviour
{
    [SerializeField] private WeaponDataBase weaponDataBase;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private float respawnTime = 10f;

    // Pas besoin de synchroniser cette liste, le serveur gère la logique
    private List<GameObject> spawnedWeapons = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[SPAWNER] OnNetworkSpawn appelé ! IsServer: {IsServer}");

        if (!IsServer) return;

        Debug.Log($"[SPAWNER] Nombre de points : {spawnPoints.Count} | Armes en DB : {weaponDataBase.weapons.Count}");

        foreach (Transform spawnPoint in spawnPoints)
        {
            SpawnWeaponAt(spawnPoint);
        }
    }

    private void SpawnWeaponAt(Transform spawnPoint)
    {
        if (!IsServer) return;

        // Sécurité 1 : Vérifier la database
        if (weaponDataBase == null || weaponDataBase.weapons == null || weaponDataBase.weapons.Count == 0)
        {
            Debug.LogError("[SPAWNER] La WeaponDataBase est manquante ou vide !");
            return;
        }

        int randomIndex = Random.Range(0, weaponDataBase.weapons.Count);
        var weaponEntry = weaponDataBase.weapons[randomIndex];

        // Sécurité 2 : Vérifier si l'entrée dans la liste n'est pas vide
        if (weaponEntry == null)
        {
            Debug.LogError($"[SPAWNER] L'entrée à l'index {randomIndex} de la Database est NULL !");
            return;
        }

        // Sécurité 3 : Vérifier le prefab
        GameObject weaponPrefab = weaponEntry.weaponPrefab;
        if (weaponPrefab == null)
        {
            Debug.LogError($"[SPAWNER] Le prefab pour l'arme à l'index {randomIndex} n'est pas assigné dans la Database !");
            return;
        }

        // Si tout est OK, on spawn
        GameObject weapon = Instantiate(weaponPrefab, spawnPoint.position, spawnPoint.rotation);
        weapon.SetActive(true);

        if (weapon.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();
            spawnedWeapons.Add(weapon);
            StartCoroutine(MonitorWeaponExistence(spawnPoint, weapon));
        }
        else
        {
            Debug.LogError($"[SPAWNER] Le prefab {weaponPrefab.name} n'a pas de composant NetworkObject !");
        }
    }

    private IEnumerator MonitorWeaponExistence(Transform spawnPoint, GameObject weapon)
    {
        // On attend que l'objet soit nul (après le Despawn du serveur)
        while (weapon != null)
        {
            yield return new WaitForSeconds(1f);
        }

        yield return new WaitForSeconds(respawnTime);
        SpawnWeaponAt(spawnPoint);
    }
}