using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interact : MonoBehaviour
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
        if (canInteract)
        {
            interactionText.SetActive(true);

            if(isInteracting)
            {
                palette.AddWeapon(currentWeapon);
                Destroy(currentWeapon.gameObject);
                canInteract = false;
                isInteracting = false;
            }
        }
        else
            interactionText.SetActive(false);
    }
}
