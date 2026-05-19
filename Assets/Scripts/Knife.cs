using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Knife : Weapon, IWeapon
{
    private bool canAttack = true;
    public bool isAttacking = false;
    [SerializeField] private Animator animator;

    private BoxCollider attackCollider;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        attackCollider = GetComponent<BoxCollider>();
        if (attackCollider != null)
            attackCollider.enabled = false;

        animator = GetComponent<Animator>();

        // Log pour vérifier si l'animator est trouvé au démarrage
        if (animator == null)
        {
            Debug.LogWarning($"[KNIFE-LOG] Aucun Animator trouvé sur le GameObject '{gameObject.name}' via GetComponent !");
        }
        else
        {
            Debug.Log($"[KNIFE-LOG] Animator initialisé avec succès sur '{gameObject.name}'.");
        }

        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    public void Attack()
    {
        NetworkObject playerNetObj = GetComponentInParent<NetworkObject>();

        if (playerNetObj == null)
        {
            Debug.LogError($"[KNIFE-LOG] Impossible de trouver un NetworkObject dans les parents de '{gameObject.name}' ! L'attaque est bloquée.");
            return;
        }

        // Log pour comprendre qui clique et qui possède l'objet
        Debug.Log($"[KNIFE-LOG] Attack() appelée. Client Local ID: {NetworkManager.Singleton.LocalClientId} | Est Propriétaire du joueur parent (IsOwner): {playerNetObj.IsOwner}");

        // Si on trouve un NetworkObject et qu'on n'est PAS le propriétaire de ce joueur, on refuse l'attaque
        if (!playerNetObj.IsOwner)
        {
            Debug.LogWarning($"[KNIFE-LOG] Attaque refusée : Le client {NetworkManager.Singleton.LocalClientId} n'est pas l'owner de ce personnage.");
            return;
        }

        if (!canAttack)
        {
            Debug.LogWarning($"[KNIFE-LOG] Attaque refusée : 'canAttack' est false (l'animation précédente n'est peut-être pas finie).");
            return;
        }

        if (canAttack)
        {
            if (animator != null)
            {
                Debug.Log($"[KNIFE-LOG] execution LOCALE de animator.SetTrigger(\"Attack\") pour l'émetteur.");
                animator.SetTrigger("Attack");
            }
            else
            {
                Debug.LogError($"[KNIFE-LOG] Erreur : L'animator est NULL au moment de cliquer !");
            }

            // On prévient le serveur pour les autres
            RequestKnifeAttackServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestKnifeAttackServerRpc()
    {
        Debug.Log($"[KNIFE-LOG] Serveur reçu : Demande d'attaque du client {OwnerClientId}. Transmission aux autres...");
        PlayKnifeAnimationToOthersRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void PlayKnifeAnimationToOthersRpc()
    {
        Debug.Log($"[KNIFE-LOG] RPC Reçu chez un tiers (Client ID: {NetworkManager.Singleton.LocalClientId}). Lecture de l'animation sur le clone réseau.");
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        else
        {
            Debug.LogError($"[KNIFE-LOG] RPC Reçu mais l'animator du clone est NULL sur ce client !");
        }
    }

    protected override void OnTriggerEnter(Collider collision)
    {
        base.OnTriggerEnter(collision);

        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        if (collision.CompareTag("Player") && isAttacking)
        {
            if (collision.transform.TryGetComponent<PlayerStats>(out var enemy))
            {
                if (enemy.OwnerClientId == netObj.OwnerClientId) return;

                Debug.Log($"[KNIFE-LOG] Impact valide détecté par l'owner sur {collision.name}. Envoi des dégâts au serveur.");
                enemy.RequestDamageServerRpc(weaponData.damage, netObj.OwnerClientId);
                isAttacking = false;
            }
        }
    }

    // Appelé par l'Animation Event
    public void ActiveAttack()
    {
        Debug.Log($"[KNIFE-LOG] Animation Event : ActiveAttack() déclenché sur la machine du client : {NetworkManager.Singleton.LocalClientId}");

        NetworkObject netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            if (attackCollider != null) attackCollider.enabled = true;
            isAttacking = true;
        }

        canAttack = false;
    }

    // Appelé par l'Animation Event
    public void DesactiveAttack()
    {
        Debug.Log($"[KNIFE-LOG] Animation Event : DesactiveAttack() déclenché sur la machine du client : {NetworkManager.Singleton.LocalClientId}");

        if (attackCollider != null) attackCollider.enabled = false;
        isAttacking = false;
        canAttack = true;
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
    }
}