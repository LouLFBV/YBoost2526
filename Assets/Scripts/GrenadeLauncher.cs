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

        if (shootPoint == null || grenadePrefab == null)
        {
            Debug.LogWarning("ShootPoint ou grenadePrefab manquant sur GrenadeLauncher");
            return;
        }

        RequestSpawnGrenadeServerRpc(shootPoint.position, shootPoint.rotation, shootPoint.forward * launchForce);

        palette.RemoveWeaponInPalette(WeaponType.Projectile);

        gameObject.SetActive(false);
    }

    [Rpc(SendTo.Server)]
    private void RequestSpawnGrenadeServerRpc(Vector3 pos, Quaternion rot, Vector3 force, RpcParams rpcParams = default)
    {
        // On instancie la grenade avec la bonne position et rotation reçues
        GameObject grenadeObj = Instantiate(grenadePrefab, pos, rot);
        NetworkObject netObj = grenadeObj.GetComponent<NetworkObject>();

        // On récupère l'ID du vrai lanceur grâce aux RpcParams
        ulong projectileOwnerId = rpcParams.Receive.SenderClientId;

        // On donne la grenade au joueur qui a lancé le RPC
        netObj.SpawnWithOwnership(projectileOwnerId);

        if (grenadeObj.TryGetComponent<GrenadeProjectile>(out var grenadeScript))
        {
            grenadeScript.Launch(force);
        }
    }
}