using System.Collections;
using UnityEngine;

public class RPG7 : Weapon, IWeapon
{
    [SerializeField] private RocketProjectile rocketPrefab;
    [SerializeField] private float launchForce = 50f;
    //[SerializeField] private float delayBetweenShots = 2.0f;
    [SerializeField] private GameObject visualRocket;
    [SerializeField] private Animator animatorVisual;
    [SerializeField] private float timmBeforeDespawn = 0.2f;
    private bool canAttack = true;

    public void Attack()
    {
        if (!canAttack)
            return;

        if (shootPoint == null || rocketPrefab == null)
            return;

        // FX arme
        PlayMuzzleFlashRpc();

        RocketProjectile rocket = Instantiate(
            rocketPrefab,
            shootPoint.position,
            shootPoint.rotation
        );

        rocket.SetOwner(transform.root.gameObject);
        rocket.Launch(shootPoint.forward * launchForce);
        visualRocket.SetActive(false);
        canAttack = false;
        StartCoroutine(CooldownCoroutine());
        //StartCoroutine(CooldownCoroutine());
    }
    public IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(timmBeforeDespawn);
        animatorVisual.SetTrigger("Despawn");
    }
    public void Despawn()
    {
        canAttack = true;
        palette.RemoveWeaponInPalette(WeaponType.Main);
        gameObject.SetActive(false);
    }

    //#region Si on veut un lance rocket avec plusieurs munitions
    //public IEnumerator CooldownCoroutine()
    //{
    //    yield return new WaitForSeconds(delayBetweenShots);
    //    animatorVisualRocket.SetTrigger("ActiveRocket");
    //}
    //public void ActiveVisualRocket()
    //{
    //    canAttack = true;
    //    visualRocket.SetActive(true);
    //}
    //#endregion
}
