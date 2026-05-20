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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        explosionAudio = GetComponent<AudioSource>();
    }

    public void Launch(Vector3 force)
    {
        if (!IsServer) return;

        ApplyLocalForce(force);

        LaunchClientRpc(force);

        if (fuseTime > 0)
            Invoke(nameof(ExplodeServer), fuseTime); 
    }

    [Rpc(SendTo.NotServer)]
    private void LaunchClientRpc(Vector3 force)
    {
        ApplyLocalForce(force);
    }

    private void ApplyLocalForce(Vector3 force)
    {
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(force, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (_hasExploded) return;

        if (collision.collider.CompareTag("Ground"))
        {
            ExplodeServer();
        }
    }

    private void ExplodeServer()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // 1. DÉGÂTS & LOGIQUE DE JEU (Strictement côté Serveur)
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player") && hit.TryGetComponent<PlayerStats>(out var enemy))
            {
                enemy.RequestDamageServerRpc(damage, OwnerClientId);
            }

            if (hit.transform.TryGetComponent<Descrutable>(out var environment) && isGrenade)
            {
                // Si ton script Descrutable est corrigé pour ne plus donner de points à l'Host :
                environment.DestroyObject(hit.transform.position, 1.5f);
            }
        }

        // 2. EFFETS VISUELS ET SONORES (Envoyés à tout le monde)
        PlayExplosionEffectsRpc(transform.position);

        // 3. DESPAWN RÉSEAU PROPRE
        if (GetComponent<NetworkObject>() != null && GetComponent<NetworkObject>().IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void PlayExplosionEffectsRpc(Vector3 impactPosition)
    {
        if (explosionVFX != null)
            Instantiate(explosionVFX, impactPosition, Quaternion.identity);

        if (explosionAudio != null && explosionAudio.clip != null)
            explosionAudio.PlayOneShot(explosionAudio.clip);

        // On cache l'objet localement chez tout le monde en attendant la destruction réseau
        if (TryGetComponent<MeshRenderer>(out var renderer)) renderer.enabled = false;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}