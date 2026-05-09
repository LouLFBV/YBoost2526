using Unity.Netcode;
using UnityEngine;

public class GrenadeProjectile : NetworkBehaviour
{
    [Header("Explosion")]
    [SerializeField] private bool isGrenade = true;
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private int damage = 40;
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private AudioSource explosionAudio;

    [Header("Physics")]
    [SerializeField] private float fuseTime = 0f; // optionnel

    private Rigidbody _rb;
    private bool _hasExploded = false;
    private ulong _ownerId;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        explosionAudio = GetComponent<AudioSource>();
    }

    public void Launch(Vector3 force)
    {
        _rb.AddForce(force, ForceMode.Impulse);

        if (fuseTime > 0)
            Invoke(nameof(Explode), fuseTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasExploded) return;

        // On explose au premier impact sol
        if (collision.collider.CompareTag("Ground"))
        {
            Explode();
        }
    }

    public void SetOwner(ulong shooterId)
    {
        _ownerId = shooterId;
    }
    private void Explode()
    {

        if (_hasExploded) return;
        _hasExploded = true;

        // VISUEL : Tout le monde voit l'explosion
        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        // DÉGÂTS : Seul le propriétaire de la grenade demande les dégâts au serveur
        if (IsOwner)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player") && hit.TryGetComponent<PlayerStats>(out var enemy))
                {
                    // On utilise l'ID du lanceur stocké au départ
                    enemy.RequestDamageServerRpc(damage, _ownerId);
                }
                if (hit.transform.TryGetComponent<Descrutable>(out var environment) && isGrenade)
                    environment.DestroyObject(hit.transform.position, 1.5f);
            }
        }
        explosionAudio.PlayOneShot(explosionAudio.clip);

        
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        if (IsServer) GetComponent<NetworkObject>().Despawn();
        else Destroy(gameObject,10); // Backup
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
