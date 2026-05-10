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
        if (weaponRef.TryGet(out NetworkObject weaponNetObj))
        {
            Weapon weaponScript = weaponNetObj.GetComponent<Weapon>();

            // ON ENVOIE LE NOM DU PREFAB OU DE LA DATA
            AddWeaponToClientPaletteRpc(weaponScript.weaponData.name, weaponScript.ammunitionAccount, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));

            weaponNetObj.Despawn();
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void AddWeaponToClientPaletteRpc(string weaponDataName, int ammo, RpcParams rpcParams)
    {
        palette.AddWeaponFromNetwork(weaponDataName, ammo);
    }
}
