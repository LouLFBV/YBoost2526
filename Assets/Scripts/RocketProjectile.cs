using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private int damage = 80;
    [SerializeField] private GameObject explosionVFX;

    [Header("Movement")]
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody rb;
    private bool hasExploded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (hit.TryGetComponent<PlayerStats>(out var enemy))
                {
                    enemy.TakeDamage(damage);
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
