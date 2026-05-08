using System.Collections;
using UnityEngine;

public class AssaultRifle : Weapon, IWeapon
{
    [SerializeField] private Animator animatorVisual;
    [SerializeField] private float timeBeforeDespawn = 0.2f;
    [SerializeField] private float delayBetweenShots = 0.1f;

    [Header("Sniper")]
    [SerializeField] private GameObject sniperViseur;
    [SerializeField] private GameObject sniperCurseur;

    private bool _canShoot = true;

    private void OnEnable()
    {
        _canShoot = true;
    }
    public void Attack()
    {
        if (!_canShoot)
        {
            return;
        }

        if (ammunitionAccount <= 0)
        {
            Debug.Log("No ammunition left!");
            return;
        }

        ShootRoutine();
    }

    private void ShootRoutine()
    {
        Debug.Log("Shooting Assault Rifle!");
        _canShoot = false;

        // 🔫 Consomme la balle
        ammunitionAccount--;

        palette.UpdateAmmunitionText(WeaponType.Main, ammunitionAccount);

        // 🔥 Effets
        PlayMuzzleFlashRpc();
        Debug.DrawRay(shootPoint.position, shootPoint.forward * weaponData.range, Color.red);

        if (Physics.Raycast(shootPoint.position, shootPoint.forward, out RaycastHit hit, weaponData.range))
        {
            if (hit.transform.TryGetComponent<PlayerStats>(out var enemy))
            {
                GameObject attacker = transform.root.gameObject;
                enemy.TakeDamage(weaponData.damage, attacker);
            }

            if (hit.transform.TryGetComponent<Descrutable>(out var environment) && weaponData.weaponFamilyType == WeaponFamilyType.Explosive)
                environment.DestroyObject(hit.point, 1.5f);
        }

        // ⏱ Cadence de tir
        StartCoroutine(CadenceDeTir());

        // 🔻 Plus de munitions ?
        if (ammunitionAccount == 0)
            StartCoroutine(DespawnRoutine());
    }

    private IEnumerator DespawnRoutine()
    {
        yield return new WaitForSeconds(timeBeforeDespawn);
        animatorVisual.SetTrigger("Despawn");
    }

    private IEnumerator CadenceDeTir()
    {
        yield return new WaitForSeconds(delayBetweenShots);
        _canShoot = true;
    }
    public void Despawn()
    {
        palette.RemoveWeaponInPalette(WeaponType.Main);
        if (weaponData.weaponFamilyType == WeaponFamilyType.Sniper)
        {
            sniperViseur.SetActive(false);
            sniperCurseur.SetActive(false);
        }
        gameObject.SetActive(false);
    }
}