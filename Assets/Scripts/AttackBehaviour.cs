using Unity.Netcode; // Assure-toi d'être en NetworkBehaviour
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackBehaviour : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    public Weapon weaponUsed;

    #region Player Input
    private PlayerInput playerInput;
    private bool isShooting = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    // 1. On Network Spawn arrive JUSTE APRÈS le Awake/Start réseau
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Sécurité absolue : si ce n'est pas NOTRE joueur, on coupe tout
            if (playerInput != null)
            {
                // On désabonne le clone pour qu'il n'écoute JAMAIS nos clics
                playerInput.actions["Attack"].performed -= ShootPerformed;
                playerInput.actions["Attack"].canceled -= ShootCanceled;
                playerInput.enabled = false;
            }

            // On désactive le script complet sur le clone pour couper son Update()
            this.enabled = false;
            return;
        }

        // Si on est l'Owner, on s'abonne proprement ici pour être sûr
        if (IsOwner && playerInput != null)
        {
            playerInput.actions["Attack"].Enable();
            playerInput.actions["Attack"].performed += ShootPerformed;
            playerInput.actions["Attack"].canceled += ShootCanceled;
        }
    }

    private void OnDisable()
    {
        // Nettoyage uniquement pour l'owner (les clones ont déjà été nettoyés)
        if (IsOwner && playerInput != null)
        {
            playerInput.actions["Attack"].performed -= ShootPerformed;
            playerInput.actions["Attack"].canceled -= ShootCanceled;
        }
    }

    private void ShootPerformed(InputAction.CallbackContext context)
    {
        // Barrière réseau : Interdiction d'aller plus loin si ce n'est pas mon perso
        if (!IsOwner) return;
        isShooting = true;
    }

    private void ShootCanceled(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        isShooting = false;
    }
    #endregion

    void Update()
    {
        // Barrière réseau dans l'Update
        if (!IsOwner) return;

        if (weaponUsed != null && isShooting)
            Shoot();
    }

    public void Shoot()
    {
        if (!IsOwner) return; // Sécurité triple

        AlignArrowSpawnToCamera();

        if (weaponUsed != null)
        {
            weaponUsed.GetComponent<IWeapon>().Attack();
        }

        AlignArrowSpawnToCamera();
    }

    private void AlignArrowSpawnToCamera()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int layerMask = ~LayerMask.GetMask("Player");

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * 100f;

        if (weaponUsed != null)
        {
            weaponUsed.shootPoint?.LookAt(targetPoint);
        }
    }
}