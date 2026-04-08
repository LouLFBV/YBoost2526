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
            Shoot();
    }

    public void Shoot()
    {
        AlignArrowSpawnToCamera();
        weaponUsed.GetComponent<IWeapon>().Attack();
        Debug.Log("Tire");

        AlignArrowSpawnToCamera();
        Debug.DrawRay(weaponUsed.shootPoint.position, weaponUsed.shootPoint.forward * weaponUsed.weaponData.range, Color.red);
        if (Physics.Raycast(weaponUsed.shootPoint.position, weaponUsed.shootPoint.forward, out RaycastHit hit, weaponUsed.weaponData.range))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("Hit " + hit.collider.name);
                if (hit.transform.TryGetComponent<PlayerStats>(out var enemy))
                {
                    enemy.TakeDamage(weaponUsed.weaponData.damage);
                }
            }
            else
            {
                // If the hit object has a Descrutable component, call DestroyObject()
                if (hit.transform.TryGetComponent<Descrutable>(out var environment))
                {
                    Debug.Log("Hit Descrutable: " + hit.collider.name);
                    // Call partial destruction centered on the hit point with a default radius
                    // while respecting the per-object player destruction permission.
                    bool destroyed = environment.TryDestroyFromPlayer(hit.point, 1.5f);
                    if (!destroyed)
                        Debug.Log("Destruction blocked for players on: " + hit.collider.name);
                }
            }
        }
    }
    private void AlignArrowSpawnToCamera()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int layerMask = ~LayerMask.GetMask("Player");

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * 100f;

        if (weaponUsed.shootPoint != null)
            weaponUsed.shootPoint.LookAt(targetPoint);


        Debug.DrawLine(weaponUsed.shootPoint.position, targetPoint, Color.yellow, 0.5f);
    }
}