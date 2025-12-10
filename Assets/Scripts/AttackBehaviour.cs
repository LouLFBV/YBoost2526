using System.Linq;
using UnityEngine;

public class AttackBehaviour : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    public Weapon weaponUsed;

    void Update()
    {
        if (weaponUsed != null && Input.GetKeyDown(KeyCode.Mouse0))
        {
            Shoot();
        }
    }

    public void Shoot()
    {
        Debug.Log("Tire");
        weaponUsed.PlayMuzzleFlash();
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
                    environment.DestroyObject(hit.point, 1.5f);
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