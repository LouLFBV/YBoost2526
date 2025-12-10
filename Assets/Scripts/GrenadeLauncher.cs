using UnityEngine;

public class GrenadeLauncher : Weapon, IWeapon
{
    public void Attack()
    {
        Debug.Log("Lance Grenade !");
    }
}
