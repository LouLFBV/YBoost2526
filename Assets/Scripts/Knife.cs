using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Knife : Weapon, IWeapon // Assure-toi que Weapon hérite de NetworkBehaviour, sinon écris : Weapon : NetworkBehaviour
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
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    public void Attack()
    {
        // 1. Seul le propriétaire lance l'action
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && !netObj.IsOwner) return;

        if (canAttack)
        {
            // On demande au serveur de synchroniser le coup de couteau
            RequestKnifeAttackServerRpc();
        }
    }

    // 2. L'Owner demande au Serveur de valider l'attaque
    [Rpc(SendTo.Server)]
    private void RequestKnifeAttackServerRpc()
    {
        // Le serveur ordonne à TOUT LE MONDE de jouer l'animation
        PlayKnifeAnimationRpc();
    }

    // 3. Tout le monde reçoit l'ordre et joue l'animation localement
    [Rpc(SendTo.Everyone)]
    private void PlayKnifeAnimationRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    protected override void OnTriggerEnter(Collider collision)
    {
        base.OnTriggerEnter(collision);

        // Les dégâts ne doivent être détectés que par l'Owner (ou le Serveur si tu préfères)
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        if (collision.CompareTag("Player") && isAttacking)
        {
            if (collision.transform.TryGetComponent<PlayerStats>(out var enemy))
            {
                if (enemy.OwnerClientId == netObj.OwnerClientId) return;

                // Envoi des dégâts au serveur
                enemy.RequestDamageServerRpc(weaponData.damage, netObj.OwnerClientId);

                isAttacking = false;
            }
        }
    }

    // Appelé par l'Animation Event (S'exécute désormais sur toutes les machines !)
    public void ActiveAttack()
    {
        // Optionnel : On peut restreindre l'activation du collider à l'Owner uniquement 
        // pour éviter que les clients calculent des faux impacts physiques.
        var netObj = GetComponentInParent<NetworkObject>();
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
        if (attackCollider != null) attackCollider.enabled = false;
        isAttacking = false;
        canAttack = true;
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
    }
}