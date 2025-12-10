using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public WeaponData weaponData;
    public Transform shootPoint;
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }


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

    public void PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();
        if (audioSource != null)
            audioSource.PlayOneShot(audioSource.clip);
    }
}
