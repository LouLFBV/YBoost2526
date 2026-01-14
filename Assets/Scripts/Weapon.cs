using NUnit.Framework.Interfaces;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public WeaponData weaponData;
    public Transform shootPoint;
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;

    [Header("Visual Floating")]
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private bool enableFloating = true;
    private Vector3 startPosition;
    private float timeOffset;
    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (!enableFloating) return;
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
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
