using System.Collections;
using UnityEngine;

public class AssaultRifle : Weapon, IWeapon
{
    [SerializeField] private Animator animatorVisual;
    [SerializeField] private float timeBeforeDespawn = 0.2f;
    [SerializeField] private float delayBetweenShots = 0.1f;

    private bool _canShoot = true;

    public void Attack()
    {
        if (!_canShoot)
            return;

        if (ammunitionAccount <= 0)
            return;

        StartCoroutine(ShootRoutine());
    }

    private IEnumerator ShootRoutine()
    {
        _canShoot = false;

        // 🔫 Consomme la balle
        ammunitionAccount--;
        Palette.instance.UpdateAmmunitionText(WeaponType.Main, ammunitionAccount);

        // 🔥 Effets
        PlayMuzzleFlash();
        Debug.DrawRay(shootPoint.position, shootPoint.forward * weaponData.range, Color.red);

        if (Physics.Raycast(shootPoint.position, shootPoint.forward, out RaycastHit hit, weaponData.range))
        {
            if (hit.transform.TryGetComponent<PlayerStats>(out var enemy))
                enemy.TakeDamage(weaponData.damage);

            if (hit.transform.TryGetComponent<Descrutable>(out var environment))
                environment.DestroyObject(hit.point, 1.5f);
        }

        // ⏱ Cadence de tir
        yield return new WaitForSeconds(delayBetweenShots);
        _canShoot = true;

        // 🔻 Plus de munitions ?
        if (ammunitionAccount == 0)
            StartCoroutine(DespawnRoutine());
    }

    private IEnumerator DespawnRoutine()
    {
        yield return new WaitForSeconds(timeBeforeDespawn);
        animatorVisual.SetTrigger("Despawn");
    }

    public void Despawn()
    {
        Palette.instance.RemoveWeaponInPalette(WeaponType.Main);
        gameObject.SetActive(false);
    }
}