using UnityEngine;
using System.Collections.Generic;

public class WeaponSpawner : MonoBehaviour
{
    [SerializeField] private WeaponDataBase weaponDataBase;
    [SerializeField] private List<Transform> spawnPoints; // Liste des points de spawn
    [SerializeField] private float respawnTime = 10f;

    public List<GameObject> spawnedWeapons = new List<GameObject>();

    private void Start()
    {
        if (spawnPoints.Count == 0 || weaponDataBase.weapons.Count == 0)
        {
            Debug.LogWarning("Pas de points de spawn ou d'armes définies !");
            return;
        }

        // Spawner une arme sur chaque point au démarrage
        foreach (Transform spawnPoint in spawnPoints)
        {
            SpawnWeaponAt(spawnPoint);
        }
    }

    private void SpawnWeaponAt(Transform spawnPoint)
    {
        // Choisir une arme aléatoire
        int randomIndex = Random.Range(0, weaponDataBase.weapons.Count);
        GameObject weaponPrefab = weaponDataBase.weapons[randomIndex].weaponPrefab;

        // Instancier l'arme en enfant du point de spawn
        GameObject weapon = Instantiate(weaponPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        spawnedWeapons.Add(weapon);

        // Ajouter un script pour détecter quand l'arme est ramassée
        weapon.GetComponent<Weapon>().OnPickedUp += () => StartCoroutine(RespawnWeapon(spawnPoint, weapon));
    }

    private System.Collections.IEnumerator RespawnWeapon(Transform spawnPoint, GameObject weapon)
    {
        // Supprimer l'arme de la liste
        spawnedWeapons.Remove(weapon);

        // Attendre le temps de respawn
        yield return new WaitForSeconds(respawnTime);

        // Respawn une nouvelle arme
        SpawnWeaponAt(spawnPoint);
    }
}