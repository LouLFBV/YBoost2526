using UnityEngine;

public class AssaultRifle : Weapon , IWeapon
{
    public void Attack()
    {
        Debug.Log("Tire");
        PlayMuzzleFlash();
        Debug.DrawRay(shootPoint.position, shootPoint.forward * weaponData.range, Color.red);
        if (Physics.Raycast(shootPoint.position, shootPoint.forward, out RaycastHit hit, weaponData.range))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("Hit " + hit.collider.name);
                if (hit.transform.TryGetComponent<PlayerStats>(out var enemy))
                {
                    enemy.TakeDamage(weaponData.damage);
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
}
