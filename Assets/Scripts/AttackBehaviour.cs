using UnityEngine;

public class AttackBehaviour : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;

    public bool canShoot = false;
    public bool chargeBow;
    public Weapon weaponUsed;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            ShootArrow();
        }
    }

    public void ShootArrow()
    {
        Debug.Log("Tire");

        AlignArrowSpawnToCamera();
        Debug.DrawRay(weaponUsed.shootPoint.position, weaponUsed.shootPoint.forward * weaponUsed.range, Color.red);
        if (Physics.Raycast(weaponUsed.shootPoint.position, weaponUsed.shootPoint.forward, out RaycastHit hit, weaponUsed.range))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("Hit " + hit.collider.name);
                if (hit.transform.TryGetComponent<PlayerStats>(out var enemy))
                {
                    enemy.TakeDamage(weaponUsed.damage);
                }
            }
            else if (hit.collider.CompareTag("Descrutable"))
            {
                Debug.Log("Hit environment: " + hit.collider.name);
                if (hit.transform.TryGetComponent<Descrutable>(out var environment))
                {
                    environment.DestroyObject();
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

        weaponUsed.shootPoint.LookAt(targetPoint);


        Debug.DrawLine(weaponUsed.shootPoint.position, targetPoint, Color.yellow, 0.5f);
    }
}