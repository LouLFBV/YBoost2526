using UnityEngine;
using UnityEngine.InputSystem;

public class Interact : MonoBehaviour
{
    public static Interact instance;
    public GameObject interactionText;
    public bool canInteract = false;
    public Weapon currentWeapon;


    private PlayerControls controls;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        controls = new PlayerControls();
    }
    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
    private void Start()
    {
        interactionText.SetActive(false);
    }
    void Update()
    {
        if (canInteract)
        {
            interactionText.SetActive(true);

            if(controls.Player.PickUp.triggered)
            {
                Palette.instance.AddWeapon(currentWeapon);
                Destroy(currentWeapon.gameObject);
                canInteract = false;
            }
        }
        else
            interactionText.SetActive(false);
    }
}
