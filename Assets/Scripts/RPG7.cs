using System;
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
        visualRocket.GetComponent<MeshRenderer>().enabled = false;

        StartCoroutine(CooldownCoroutine());
    }
    private void OnEnable()
    {
        // 1. On s'assure qu'on peut attaquer au spawn/équipement
        canAttack = true;

        // 2. On FORCE la roquette visuelle sur le modèle à redevenir active !
        if (visualRocket != null)
        {
            visualRocket.SetActive(true);
            visualRocket.GetComponent<MeshRenderer>().enabled = true;
            Debug.Log($"[RPG7] VisualRocket réactivé sur {gameObject.name} par l'ID {NetworkManager.Singleton.LocalClientId}");
        }
        else
            Debug.LogWarning($"[RPG7] visualRocket n'est pas assigné sur {gameObject.name} !");

        // 3. On reset l'Animator pour annuler le trigger "Despawn" précédent
        if (animatorVisual != null)
        {
            animatorVisual.Rebind();
            animatorVisual.Update(0f);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestSpawnRocketServerRpc(Vector3 pos, Quaternion rot, Vector3 force, RpcParams rpcParams = default)
    {
        // 1. Le serveur instancie la roquette
        GameObject rocketObj = Instantiate(rocketPrefab, pos, rot);

        NetworkObject netObj = rocketObj.GetComponent<NetworkObject>();

        ulong projectileOwnerId = rpcParams.Receive.SenderClientId;

        // 2. On donne l'ownership au vrai tireur
        netObj.SpawnWithOwnership(projectileOwnerId);

        if (rocketObj.TryGetComponent<RocketProjectile>(out var rocketScript))
        {
            rocketScript.Launch(force);
        }
    }

    public IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(timmBeforeDespawn);
        animatorVisual.SetTrigger("Despawn");
    }

    public void AE_Despawn()
    {
        canAttack = true;
        palette.RemoveWeaponInPalette(WeaponType.Main);
        gameObject.SetActive(false);
    }
}