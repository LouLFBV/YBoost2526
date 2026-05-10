using Unity.Netcode;
using UnityEngine;

public abstract class Weapon : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Interact interact;
    [SerializeField] protected Palette palette;

    public WeaponData weaponData;
    public Transform shootPoint;
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;

    [Header("Visual Floating")]
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float startPositionY = 1f;
    private float timeOffset;


    public int ammunitionAccount;


    public event System.Action OnPickedUp;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ammunitionAccount = weaponData.ammunitionInStock;
    }

    private void Start()
    {
        timeOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (!enableFloating) return;
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        float newY = startPositionY + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    //protected virtual void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Player") && transform.CompareTag("Object"))
    //    {
    //        if (interact == null)
    //            interact = other.GetComponent<Interact>();            
    //        if (palette == null)
    //            palette = other.GetComponent<Palette>();
    //        interact.canInteract = true;
    //        interact.currentWeapon = this;
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.CompareTag("Player") && transform.CompareTag("Object"))
    //    {
    //        if (interact == null)
    //            interact = other.GetComponent<Interact>();            
    //        if (palette == null)
    //            palette = other.GetComponent<Palette>();
    //        interact.canInteract = false;
    //        interact.currentWeapon = null;
    //    }
    //}

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && transform.CompareTag("Object"))
        {
            var netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsLocalPlayer)
            {
                if (interact == null)
                    interact = other.GetComponent<Interact>();
                if (palette == null)
                    palette = other.GetComponent<Palette>();
                interact.canInteract = false;
                interact.currentWeapon = null;
            }
        }
    }


    protected virtual void OnTriggerEnter(Collider other)
    {
        // On ne gère l'interaction que si l'objet est au sol (Tag "Object")
        if (other.CompareTag("Player") && transform.CompareTag("Object"))
        {
            var netObj = other.GetComponent<NetworkObject>();
            // On ne propose l'interaction qu'au joueur local (celui qui marche dessus)
            if (netObj != null && netObj.IsLocalPlayer)
            {
                interact = other.GetComponent<Interact>();
                palette = other.GetComponent<Palette>();
                interact.canInteract = true;
                interact.currentWeapon = this;
            }
        }
    }

    // Ajoute un ClientRpc pour synchroniser les effets visuels chez TOUT LE MONDE
    [Rpc(SendTo.Everyone)]
    public void PlayMuzzleFlashRpc()
    {
        Debug.Log($"[WEAPON] PlayMuzzleFlash appelé sur {gameObject.name} par l'ID {NetworkManager.Singleton.LocalClientId}");

        if (muzzleFlash != null) muzzleFlash.Play();
        else Debug.LogWarning("MuzzleFlash est NULL sur " + gameObject.name);

        if (audioSource != null && audioSource.clip != null)
            audioSource.PlayOneShot(audioSource.clip);
    }

    private void OnDestroy()
    {
        OnPickedUp?.Invoke();
    }
}
