using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private int damage = 40;
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private AudioSource explosionAudio;

    [Header("Physics")]
    [SerializeField] private float fuseTime = 0f; // optionnel

    private Rigidbody rb;
    private bool hasExploded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        explosionAudio = GetComponent<AudioSource>();
    }

    public void Launch(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);

        if (fuseTime > 0)
            Invoke(nameof(Explode), fuseTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // On explose au premier impact sol
        if (collision.collider.CompareTag("Ground"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;

        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        explosionAudio.PlayOneShot(explosionAudio.clip);
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (hit.transform.TryGetComponent<PlayerStats>(out var enemy))
                {
                    enemy.TakeDamage(damage);
                }
            }
        }

        Destroy(gameObject,10);
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
