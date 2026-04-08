using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class GrenadeProjectile : MonoBehaviour
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
    private GameObject _owner;

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

    public void SetOwner(GameObject shooter)
    {
        _owner = shooter;
    }
    private void Explode()
    {
        _hasExploded = true;

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
                    enemy.TakeDamage(damage, _owner);
                }
            }
            if (hit.transform.TryGetComponent<Descrutable>(out var environment) && isGrenade)
                environment.DestroyObject(hit.transform.position, 1.5f);
        }
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject,10);
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
