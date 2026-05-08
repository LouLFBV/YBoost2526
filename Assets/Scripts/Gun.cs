using System.Collections;
using UnityEditor;
using UnityEngine;

public class Gun : Weapon, IWeapon
{
    [SerializeField] private Animator animatorVisual;
    [SerializeField] private float timmBeforeDespawn = 0.2f;
    [SerializeField] private float delayBetweenShots = 0.25f;
    private bool _canShoot = true;

    public void Attack()
    {
        Debug.Log("Attack with Gun");
        if (ammunitionAccount == 0 || !_canShoot)
            return;
        ammunitionAccount--;
        palette.UpdateAmmunitionText(WeaponType.Secondary, ammunitionAccount);
        Debug.Log("Tire");
        _canShoot = false;
        PlayMuzzleFlashRpc();
        Debug.DrawRay(shootPoint.position, shootPoint.forward * weaponData.range, Color.red);
        if (Physics.Raycast(shootPoint.position, shootPoint.forward, out RaycastHit hit, weaponData.range))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("Hit " + hit.collider.name);
                if (hit.transform.TryGetComponent<PlayerStats>(out var enemy))
                {
                    GameObject attacker = transform.root.gameObject;
                    enemy.TakeDamage(weaponData.damage, attacker);
                }
            }
        }
        StartCoroutine(CooldownCoroutineShoot());
        if (ammunitionAccount == 0)
            StartCoroutine(CooldownCoroutine());
    }
    public IEnumerator CooldownCoroutineShoot()
    {
        yield return new WaitForSeconds(delayBetweenShots);
        _canShoot = true;
    }

    #region Despawn Méthodes
    public IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(timmBeforeDespawn);
        animatorVisual.SetTrigger("Despawn");
    }
    public void Despawn()
    {
        palette.RemoveWeaponInPalette(WeaponType.Secondary);
        gameObject.SetActive(false);
    }
    #endregion
}
