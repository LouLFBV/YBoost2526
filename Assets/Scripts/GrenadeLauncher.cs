using UnityEngine;

public class GrenadeLauncher : Weapon, IWeapon
{
    [SerializeField] private GrenadeProjectile grenadePrefab;
    [SerializeField] private float launchForce = 12f;
    [SerializeField] private Palette palette;

    public void Attack()
    {
        if (shootPoint == null)
        {
            Debug.LogWarning("ShootPoint manquant sur GrenadeLauncher");
            return;
        }

        GrenadeProjectile grenade =
            Instantiate(grenadePrefab, shootPoint.position, shootPoint.rotation);

        grenade.SetOwner(transform.root.gameObject);
        grenade.Launch(shootPoint.forward * launchForce);
        palette.RemoveWeaponInPalette(WeaponType.Projectile);  
    }
}
