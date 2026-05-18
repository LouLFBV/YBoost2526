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
    [SerializeField] private float fuseTime = 0f;

    private Rigidbody _rb;
    private bool _hasExploded = false;
    private ulong _ownerId;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        explosionAudio = GetComponent<AudioSource>();
    }

    // Plus besoin de OnNetworkSpawn pour la force !

    public void Launch(Vector3 force)
    {
        if (!IsServer) return;

        // 1. Le serveur applique la force chez lui
        ApplyLocalForce(force);

        // 2. Le serveur ordonne instantanément à tous les clients d'appliquer la même force
        LaunchClientRpc(force);

        if (fuseTime > 0)
            Invoke(nameof(Explode), fuseTime);
    }

    [Rpc(SendTo.NotServer)] // S'exécute uniquement sur les clients
    private void LaunchClientRpc(Vector3 force)
    {
        ApplyLocalForce(force);
    }

    private void ApplyLocalForce(Vector3 force)
    {
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero; // Sécurité : on remet à zéro avant l'impulse
            _rb.AddForce(force, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasExploded) return;

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

        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        if (IsOwner)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player") && hit.TryGetComponent<PlayerStats>(out var enemy))
                {
                    enemy.RequestDamageServerRpc(damage, _ownerId);
                }
                if (hit.transform.TryGetComponent<Descrutable>(out var environment) && isGrenade)
                    environment.DestroyObject(hit.transform.position, 1.5f);
            }
        }

        if (explosionAudio != null && explosionAudio.clip != null)
            explosionAudio.PlayOneShot(explosionAudio.clip);

        if (TryGetComponent<MeshRenderer>(out var renderer)) renderer.enabled = false;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        if (IsServer)
            GetComponent<NetworkObject>().Despawn();
        else
            Destroy(gameObject, 10);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}