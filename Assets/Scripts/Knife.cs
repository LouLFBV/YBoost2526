using System.Collections;
using Unity.Netcode; // Ajout obligatoire
using UnityEngine;

public class Knife : Weapon, IWeapon
{
    private bool canAttack = true;
    public bool isAttacking = false;
    [SerializeField] private Animator animator;

    private BoxCollider attackCollider;
    private Vector3 startPosition;
    private Quaternion startRotation;

    // On transforme Knife en NetworkBehaviour (via l'héritage de Weapon si possible, 
    // sinon change Weapon en NetworkBehaviour)

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
        // Seul le propriétaire du couteau peut initier l'attaque
        // GetComponentInParent<NetworkObject>() car le couteau est souvent un enfant du joueur
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && !netObj.IsOwner) return;

        if (canAttack)
        {
            animator.SetTrigger("Attack");
            // Optionnel : Envoyer un RPC pour jouer l'animation chez les autres
        }
    }

    protected override void OnTriggerEnter(Collider collision)
    {
        base.OnTriggerEnter(collision);

        // 1. On vérifie si on est bien l'owner (celui qui donne le coup)
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        // 2. Détection de la cible
        if (collision.CompareTag("Player") && isAttacking)
        {
            if (collision.transform.TryGetComponent<PlayerStats>(out var enemy))
            {
                // Vérifier qu'on ne se tape pas soi-même
                if (enemy.OwnerClientId == netObj.OwnerClientId) return;

                // 3. ENVOI AU RÉSEAU
                // On appelle le ServerRpc que tu as créé dans PlayerStats
                enemy.RequestDamageServerRpc(weaponData.damage, netObj.OwnerClientId);

                // On désactive immédiatement pour ne pas mettre 5 coups en une frame
                isAttacking = false;
            }
        }
    }

    // Appelé par l'Animation Event
    public void ActiveAttack()
    {
        attackCollider.enabled = true;
        isAttacking = true;
        canAttack = false;
    }

    // Appelé par l'Animation Event
    public void DesactiveAttack()
    {
        attackCollider.enabled = false;
        isAttacking = false;
        canAttack = true;
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
    }
}