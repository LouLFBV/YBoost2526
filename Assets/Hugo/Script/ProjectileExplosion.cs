using UnityEngine;

public class ProjectileExplosion : MonoBehaviour
{
    [Header("Effets Visuels")]
    [SerializeField] private GameObject explosionPrefab; // Glissez votre VFX ici
    [SerializeField] private float vfxDuration = 3f;      // Temps avant de détruire le VFX

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision détectée avec : " + collision.gameObject.name);
        // 1. On évite que l'obus n'explose sur le train (si le tag est "Player")
        if (collision.gameObject.CompareTag("Player")) return;

        // 2. Récupérer le point d'impact précis et la normale (direction de la surface)
        ContactPoint contact = collision.contacts[0];
        Vector3 pos = contact.point;
        Quaternion rot = Quaternion.LookRotation(contact.normal);

        // 3. Faire apparaître l'explosion
        if (explosionPrefab != null)
        {
            GameObject vfx = Instantiate(explosionPrefab, pos, rot);

            // Détruire le VFX après quelques secondes pour ne pas encombrer la mémoire
            Destroy(vfx, vfxDuration);
        }

        // 4. Détruire l'obus immédiatement après l'impact
        Destroy(gameObject);
    }
}