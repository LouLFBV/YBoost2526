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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        explosionAudio = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Invoke(nameof(TimeOutDespawn), lifeTime);
        }
    }

    public void Launch(Vector3 velocity)
    {
        if (!IsServer) return;

        ApplyLocalVelocity(velocity);
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
        if (!IsServer) return;
        if (_hasExploded) return;

        ExplodeServer();
    }


    private void ExplodeServer()
    {
        _hasExploded = true;

        Debug.Log($"[ROQUETTE] Explosion déclenchée ! Mon OwnerClientId officiel est : {OwnerClientId}");

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player") && hit.TryGetComponent<PlayerStats>(out var enemy))
            {
                if (enemy.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
                {
                    Debug.Log($"[ROQUETTE] JOUEUR TOUCHÉ ! Cible (ID: {netObj.OwnerClientId}) | Tireur envoyé au RPC: {OwnerClientId}");
                }

                enemy.RequestDamageServerRpc(damage, OwnerClientId);
            }

            if (hit.transform.TryGetComponent<Descrutable>(out var environment))
            {
                Debug.Log($"[ROQUETTE] DÉCOR TOUCHÉ ! Objet: {hit.gameObject.name}. Appels à DestroyObject().");
                environment.DestroyObject(hit.transform.position, 1.5f);
            }
        }

        PlayExplosionEffectsRpc(transform.position);

        if (GetComponent<NetworkObject>() != null)
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