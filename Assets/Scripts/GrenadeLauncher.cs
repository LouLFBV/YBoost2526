using Unity.Netcode;
using UnityEngine;

public class GrenadeLauncher : Weapon, IWeapon
{
    [SerializeField] private GameObject grenadePrefab; // Utilise GameObject pour Instantiate
    [SerializeField] private float launchForce = 12f;

    public void Attack()
    {
        // 1. Sécurité : Seul le propriétaire lance la grenade
        if (!IsOwner) return;

        if (shootPoint == null)
        {
            Debug.LogWarning("ShootPoint manquant sur GrenadeLauncher");
            return;
        }

        // 2. On demande au serveur de créer la grenade pour tout le monde
        // On envoie la position, la rotation et la force calculées localement
        RequestSpawnGrenadeServerRpc(shootPoint.position, shootPoint.rotation, shootPoint.forward * launchForce);

        // 3. On retire l'arme de l'inventaire localement (UI)
        palette.RemoveWeaponInPalette(WeaponType.Projectile);
    }

    [Rpc(SendTo.Server)]
    private void RequestSpawnGrenadeServerRpc(Vector3 pos, Quaternion rot, Vector3 force)
    {
        // 4. L'Hôte instancie le prefab
        GameObject grenadeObj = Instantiate(grenadePrefab, pos, rot);

        // 5. IMPORTANT : On "Spawn" l'objet sur le réseau
        // On donne la propriété (Ownership) à celui qui a appelé le RPC (OwnerClientId)
        // Cela permet à la grenade de savoir qui est l'Owner pour le futur IsOwner de l'explosion
        NetworkObject netObj = grenadeObj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(OwnerClientId);

        // 6. On configure le script de la grenade
        if (grenadeObj.TryGetComponent<GrenadeProjectile>(out var grenadeScript))
        {
            grenadeScript.SetOwner(OwnerClientId);
            grenadeScript.Launch(force);
        }
    }
}