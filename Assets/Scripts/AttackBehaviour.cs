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

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (playerInput != null)
            {
                playerInput.actions["Attack"].performed -= ShootPerformed;
                playerInput.actions["Attack"].canceled -= ShootCanceled;
                playerInput.enabled = false;
            }

            this.enabled = false;
            return;
        }

        // On force un rafraîchissement des abonnements si on vient de spawn
        OnEnable();
    }

    private void OnEnable()
    {
        // On s'assure de n'abonner QUE le joueur local, pas les clones
        if (IsOwner && playerInput != null)
        {
            // Sécurité : On se désabonne avant pour éviter les doubles abonnements accidentels
            playerInput.actions["Attack"].performed -= ShootPerformed;
            playerInput.actions["Attack"].canceled -= ShootCanceled;

            // On réabonne proprement les événements de tir
            playerInput.actions["Attack"].Enable();
            playerInput.actions["Attack"].performed += ShootPerformed;
            playerInput.actions["Attack"].canceled += ShootCanceled;

            // Sécurité : On réinitialise l'état de tir à la résurrection
            isShooting = false;
        }
    }

    private void OnDisable()
    {
        if (IsOwner && playerInput != null)
        {
            playerInput.actions["Attack"].performed -= ShootPerformed;
            playerInput.actions["Attack"].canceled -= ShootCanceled;
        }

        // On s'assure que l'arme arrête d'essayer de tirer
        isShooting = false;
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

        if (TryGetComponent<FirstPersonController_Networked>(out var controller))
        {
            if (controller.isDead.Value) return; // Interdiction de tirer
        }

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