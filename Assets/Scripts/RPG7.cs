using System.Collections;
using UnityEngine;

public class RPG7 : Weapon, IWeapon
{
    [SerializeField] private RocketProjectile rocketPrefab;
    [SerializeField] private float launchForce = 25f;
    [SerializeField] private float delayBetweenShots = 2.0f;

    private bool canAttack = true;

    public void Attack()
    {
        if (!canAttack)
            return;

        if (shootPoint == null || rocketPrefab == null)
            return;

        // FX arme
        PlayMuzzleFlash();

        RocketProjectile rocket = Instantiate(
            rocketPrefab,
            shootPoint.position,
            shootPoint.rotation
        );

        rocket.Launch(shootPoint.forward * launchForce);

        canAttack = false;
        StartCoroutine(CooldownCoroutine());
    }

    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(delayBetweenShots);
        canAttack = true;
    }
}
