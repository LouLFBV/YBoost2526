using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData weaponData;
    public Transform shootPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Interact.instance.canInteract = true;
            Interact.instance.currentWeapon = this;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Interact.instance.canInteract = false;
            Interact.instance.currentWeapon = null;
        }
    }
}
