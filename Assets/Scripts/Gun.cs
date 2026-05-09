using System.Collections;
using Unity.Netcode; // N'oublie pas l'import
using UnityEngine;

public class Gun : Weapon, IWeapon
{
    [SerializeField] private Animator animatorVisual;
    [SerializeField] private float timmBeforeDespawn = 0.2f;
    [SerializeField] private float delayBetweenShots = 0.25f;
    private bool _canShoot = true;

    public void Attack()
    {
        // 1. Vérification d'autorité : Seul le propriétaire peut tirer
        if (!IsOwner) return;

        if (ammunitionAccount == 0 || !_canShoot)
            return;

        ammunitionAccount--;
        palette.UpdateAmmunitionText(WeaponType.Secondary, ammunitionAccount);

        _canShoot = false;

        // 2. On joue les effets visuels chez TOUT LE MONDE (via le RPC de la classe Weapon)
        PlayMuzzleFlashRpc();

        if (Physics.Raycast(shootPoint.position, shootPoint.forward, out RaycastHit hit, weaponData.range))
        {
            if (hit.collider.CompareTag("Player"))
            {
                if (hit.transform.TryGetComponent<PlayerStats>(out var enemy))
                {
                    // 3. ADAPTATION ICI : On envoie le ServerRpc
                    // On récupère notre propre ID réseau (LocalClientId)
                    ulong myId = NetworkManager.Singleton.LocalClientId;

                    // On demande au serveur d'infliger les dégâts
                    enemy.RequestDamageServerRpc(weaponData.damage, myId);

                    Debug.Log($"[CLIENT] Demande de dégâts envoyée pour {hit.collider.name} par {myId}");
                }
            }
        }

        StartCoroutine(CooldownCoroutineShoot());
        if (ammunitionAccount == 0)
            StartCoroutine(CooldownCoroutine());
    }

    public IEnumerator CooldownCoroutineShoot()
    {
        yield return new WaitForSeconds(delayBetweenShots);
        _canShoot = true;
    }

    #region Despawn Méthodes
    public IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(timmBeforeDespawn);
        animatorVisual.SetTrigger("Despawn");
    }
    public void Despawn()
    {
        palette.RemoveWeaponInPalette(WeaponType.Secondary);
        gameObject.SetActive(false);
    }
    #endregion
}
