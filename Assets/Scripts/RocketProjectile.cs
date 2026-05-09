using Unity.Netcode;
using UnityEngine;

public class RocketProjectile : NetworkBehaviour // 1. Passage en NetworkBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private int damage = 80;
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private AudioSource explosionAudio;

    [Header("Movement")]
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody _rb;
    private bool _hasExploded = false;
    private ulong _ownerId; // 2. Stockage de l'ID au lieu du GameObject

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        explosionAudio = GetComponent<AudioSource>();
    }

    public void Launch(Vector3 velocity)
    {
        _rb.linearVelocity = velocity;
        // On ne détruit pas localement, on laisse le serveur gérer le Despawn
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Seul le serveur ou l'Owner devrait détecter la collision pour éviter les doublons
        if (!IsServer && !IsOwner) return;

        if (_hasExploded) return;

        Explode();
    }

    public void SetOwner(ulong shooterId)
    {
        _ownerId = shooterId;
    }

    private void Explode()
    {
        _hasExploded = true;

        // VISUEL : Apparaît chez tout le monde si c'est un objet simple, 
        // ou via un RPC si tu veux être ultra précis.
        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        if (explosionAudio != null)
            explosionAudio.PlayOneShot(explosionAudio.clip);

        // DÉGÂTS : Seul l'Owner demande les dégâts au serveur
        if (IsOwner)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player") && hit.TryGetComponent<PlayerStats>(out var enemy))
                {
                    // Utilisation du ServerRpc avec l'ID stocké
                    enemy.RequestDamageServerRpc(damage, _ownerId);
                }

                if (hit.transform.TryGetComponent<Descrutable>(out var environment))
                {
                    environment.DestroyObject(hit.transform.position, 1.5f);
                }
            }
        }

        // NETTOYAGE RÉSEAU
        if (IsServer)
        {
            // On attend un tout petit peu pour que les clients voient l'impact
            GetComponent<NetworkObject>().Despawn();
        }
        else if (!IsOwner)
        {
            // Pour les autres clients, on cache juste visuellement en attendant le Despawn du serveur
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}