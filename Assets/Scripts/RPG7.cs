using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RPG7 : Weapon, IWeapon
{
    [SerializeField] private GameObject rocketPrefab; // Changé en GameObject pour le Spawn
    [SerializeField] private float launchForce = 50f;
    [SerializeField] private GameObject visualRocket;
    [SerializeField] private Animator animatorVisual;
    [SerializeField] private float timmBeforeDespawn = 0.2f;
    private bool canAttack = true;

    public void Attack()
    {
        // 1. Seul le propriétaire peut décider de tirer
        if (!IsOwner || !canAttack)
            return;

        if (shootPoint == null || rocketPrefab == null)
            return;

        canAttack = false;

        // 2. On joue les effets visuels chez TOUT LE MONDE
        PlayMuzzleFlashRpc();

        // 3. On demande au serveur de spawn la roquette
        // On passe la position, rotation et force
        RequestSpawnRocketServerRpc(shootPoint.position, shootPoint.rotation, shootPoint.forward * launchForce);

        // 4. On cache la roquette visuelle sur notre arme localement
        visualRocket.SetActive(false);

        StartCoroutine(CooldownCoroutine());
    }

    [Rpc(SendTo.Server)]
    private void RequestSpawnRocketServerRpc(Vector3 pos, Quaternion rot, Vector3 force)
    {
        // 5. Le serveur instancie et spawn
        GameObject rocketObj = Instantiate(rocketPrefab, pos, rot);

        NetworkObject netObj = rocketObj.GetComponent<NetworkObject>();
        // On donne la propriété au tireur pour que SA machine calcule l'explosion
        netObj.SpawnWithOwnership(OwnerClientId);

        if (rocketObj.TryGetComponent<RocketProjectile>(out var rocketScript))
        {
            rocketScript.SetOwner(OwnerClientId);
            rocketScript.Launch(force);
        }
    }

    public IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(timmBeforeDespawn);
        animatorVisual.SetTrigger("Despawn");
    }

    public void Despawn()
    {
        canAttack = true;
        palette.RemoveWeaponInPalette(WeaponType.Main);
        gameObject.SetActive(false);
    }
}