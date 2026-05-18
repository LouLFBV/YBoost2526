using Unity.Netcode;
using UnityEngine;

public class RocketProjectile : NetworkBehaviour
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
    private ulong _ownerId;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        explosionAudio = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        // Si c'est le serveur, on programme une auto-destruction en cas de non-impact
        if (IsServer)
        {
            Invoke(nameof(TimeOutDespawn), lifeTime);
        }
    }

    public void Launch(Vector3 velocity)
    {
        if (!IsServer) return;

        // 1. Le serveur applique la vitesse chez lui
        ApplyLocalVelocity(velocity);

        // 2. Le serveur envoie l'ordre immédiat aux clients d'appliquer la même vitesse
        LaunchClientRpc(velocity);
    }

    [Rpc(SendTo.NotServer)]
    private void LaunchClientRpc(Vector3 velocity)
    {
        ApplyLocalVelocity(velocity);
    }

    private void ApplyLocalVelocity(Vector3 velocity)
    {
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = velocity;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Seul le serveur gère la détection d'impact pour éviter les doubles explosions
        if (!IsServer) return;

        if (_hasExploded) return;

        ExplodeServer();
    }

    public void SetOwner(ulong shooterId)
    {
        _ownerId = shooterId;
    }

    private void ExplodeServer()
    {
        _hasExploded = true;

        // 1. DÉGÂTS : Le serveur calcule les dégâts (anti-triche)
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player") && hit.TryGetComponent<PlayerStats>(out var enemy))
            {
                enemy.RequestDamageServerRpc(damage, _ownerId);
            }

            if (hit.transform.TryGetComponent<Descrutable>(out var environment))
            {
                environment.DestroyObject(hit.transform.position, 1.5f);
            }
        }

        // 2. VISUEL & SON : On transmet la position exacte de l'impact à TOUT LE MONDE
        PlayExplosionEffectsRpc(transform.position);

        // 3. NETTOYAGE : Le serveur retire l'objet du réseau
        if (GetComponent<NetworkObject>() != null)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void PlayExplosionEffectsRpc(Vector3 impactPosition)
    {
        // Tout le monde instancie l'explosion à la coordonnée absolue de l'impact
        if (explosionVFX != null)
            Instantiate(explosionVFX, impactPosition, Quaternion.identity);

        if (explosionAudio != null && explosionAudio.clip != null)
            explosionAudio.PlayOneShot(explosionAudio.clip);
    }

    private void TimeOutDespawn()
    {
        if (_hasExploded) return;

        if (GetComponent<NetworkObject>() != null && GetComponent<NetworkObject>().IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}