using UnityEngine;
using UnityEngine.InputSystem;

public class AttackBehaviour : MonoBehaviour
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

    private void OnEnable()
    {
        playerInput.actions["Attack"].Enable();
        playerInput.actions["Attack"].performed += ShootPerformed;
        playerInput.actions["Attack"].canceled += ShootCanceled;
    }

    private void OnDisable()
    {
        playerInput.actions["Attack"].Disable();
        playerInput.actions["Attack"].performed -= ShootPerformed;
        playerInput.actions["Attack"].canceled -= ShootCanceled;
    }

    private void ShootPerformed(InputAction.CallbackContext context)
    {
        isShooting = true;
    }

    private void ShootCanceled(InputAction.CallbackContext context)
    {
        isShooting = false;
    }
    #endregion
    void Update()
    {
        if (weaponUsed != null && isShooting)
        {
            Shoot();
            isShooting = false;
        }
    }

    public void Shoot()
    {
        AlignArrowSpawnToCamera();
        weaponUsed.GetComponent<IWeapon>().Attack();

    }

    private void AlignArrowSpawnToCamera()
    {
        if (weaponUsed.shootPoint == null)
            return;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int layerMask = ~LayerMask.GetMask("Player");

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * 100f;

        weaponUsed.shootPoint.LookAt(targetPoint);


        Debug.DrawLine(weaponUsed.shootPoint.position, targetPoint, Color.yellow, 0.5f);
    }
}