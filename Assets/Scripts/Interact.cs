using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interact : NetworkBehaviour
{
    public GameObject interactionText;
    public bool canInteract = false;
    public Weapon currentWeapon;


    [SerializeField] private PlayerInput playerInput;
    private bool isInteracting = false;
    private Palette palette;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        palette = GetComponent<Palette>();
    }
    #region Méthodes Player Input
    private void OnEnable()
    {
        playerInput.actions["Interact"].Enable();
        playerInput.actions["Interact"].performed += IsInteractingPerformed;
        playerInput.actions["Interact"].canceled += IsInteractingCanceled;
    }
    private void OnDisable()
    {
        playerInput.actions["Interact"].Disable();
        playerInput.actions["Interact"].performed -= IsInteractingPerformed;
        playerInput.actions["Interact"].canceled -= IsInteractingCanceled;
    }

    private void IsInteractingPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Interacting Performed");
        isInteracting = true;
    }

    private void IsInteractingCanceled(InputAction.CallbackContext context)
    {
        isInteracting = false;
    }
    #endregion 

    private void Start()
    {
        interactionText.SetActive(false);
    }
    void Update()
    {
        if (!IsOwner) return;

        if (canInteract && currentWeapon != null)
        {
            interactionText.SetActive(true);

            if (isInteracting)
            {
                if (currentWeapon.TryGetComponent<NetworkObject>(out var weaponNetObj))
                {
                    // ON AJOUTE CETTE VÉRIFICATION :
                    if (weaponNetObj.IsSpawned)
                    {
                        PickupWeaponServerRpc(weaponNetObj);
                        canInteract = false;
                        isInteracting = false;
                    }
                    else
                    {
                        Debug.LogWarning($"L'objet {currentWeapon.name} n'est pas encore Spawn sur le réseau !");
                    }
                }
            }
        }
        else
        {
            interactionText.SetActive(false);
        }
    }

    [Rpc(SendTo.Server)]
    private void PickupWeaponServerRpc(NetworkObjectReference weaponRef)
    {
        // Le serveur essaie de récupérer l'objet à partir de la référence
        if (weaponRef.TryGet(out NetworkObject weaponNetObj))
        {
            Weapon weaponScript = weaponNetObj.GetComponent<Weapon>();

            // 1. On dit au client qui a ramassé l'arme de l'ajouter à sa palette
            // On utilise l'ID du client qui a appelé le RPC (OwnerClientId)
            AddWeaponToClientPaletteRpc(weaponScript.weaponData.weaponType, weaponScript.ammunitionAccount, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));

            // 2. Le serveur fait disparaître l'arme du sol pour TOUT LE MONDE
            weaponNetObj.Despawn();
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void AddWeaponToClientPaletteRpc(WeaponType type, int ammo, RpcParams rpcParams)
    {
        // Ici, on appelle ta logique de Palette
        // Il faut que ta Palette ait une méthode qui accepte juste le Type et les Munitions
        // car l'objet physique "Weapon" va être détruit par le Despawn
        palette.AddWeaponFromNetwork(type, ammo);
    }
}
