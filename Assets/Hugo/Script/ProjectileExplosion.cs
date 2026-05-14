using UnityEngine;

public class ProjectileExplosion : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 40f;
    [SerializeField] private LayerMask playerLayer; // Assignez le Layer "Player" dans l'inspecteur

    [Header("Effets Visuels")]
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private float vfxDuration = 3f;

    [Header("Effets Sonores")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private float volume = 1f;

    [HideInInspector] public GameObject zoneToDestroy;

    private bool hasExploded = false;

    private void Start()
    {
        if (zoneToDestroy != null)
        {
            Destroy(zoneToDestroy, 10f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // On évite d'exploser sur le train lui-même
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.name.Contains("Train")) return;

        // On ne déclenche l'explosion qu'une seule fois
        if (!hasExploded)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;
        Debug.Log("Impact et élimination de la zone");

        // 1. Détection et élimination des joueurs
        // Physics.OverlapSphere crée une sphère invisible de rayon 'explosionRadius'
        Collider[] victims = Physics.OverlapSphere(transform.position, explosionRadius, playerLayer);

        foreach (Collider victim in victims)
        {
            // Ici, on part du principe que si l'objet est touché, il est éliminé.
            // Vous pouvez appeler une fonction spécifique de mort si vous en avez une :
            // victim.GetComponent<PlayerHealth>().Die(); 

            Debug.Log("Joueur éliminé : " + victim.name);
            //Destroy(victim.gameObject);
        }

        // 2. Effets Sonores
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, volume);
        }

        // 3. Effets Visuels
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(vfx, vfxDuration);
        }

        // 4. Nettoyage de la zone rouge au sol
        if (zoneToDestroy != null)
        {
            Destroy(zoneToDestroy, 0.1f);
        }

        // 5. Détruire l'obus
        Destroy(gameObject);
    }

    // Visualisation du rayon d'action dans l'éditeur
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}