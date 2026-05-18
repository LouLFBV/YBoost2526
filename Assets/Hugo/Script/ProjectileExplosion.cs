using UnityEngine;
using Unity.Netcode; // OBLIGATOIRE pour utiliser les RPC

public class ProjectileExplosion : NetworkBehaviour // On change MonoBehaviour en NetworkBehaviour
{
    [SerializeField] private float explosionRadius = 40f;
    [SerializeField] private LayerMask playerLayer;

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
        // Seul le Serveur (l'Host) gère la physique de la collision de l'obus
        if (!IsServer) return;

        // On évite d'exploser sur le train lui-même
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.name.Contains("Train")) return;

        // On ne déclenche l'explosion qu'une seule fois
        if (!hasExploded)
        {
            ExplodeServer();
        }
    }

    // Cette fonction ne tourne QUE sur le Serveur/Host
    private void ExplodeServer()
    {
        hasExploded = true;
        Debug.Log("Impact et élimination de la zone (Serveur)");

        // 1. DÉGÂTS / ÉLIMINATION : Géré uniquement par le serveur pour éviter la triche
        Collider[] victims = Physics.OverlapSphere(transform.position, explosionRadius, playerLayer);
        foreach (Collider victim in victims)
        {
            Debug.Log("Joueur éliminé par le serveur : " + victim.name);
            // Si tes joueurs ont un script de stats / vie en réseau :
            // victim.GetComponent<PlayerStats>().ApplyDamage(100);
        }

        // 2. NETTOYAGE PHYSIQUE : Le serveur détruit la zone rouge au sol
        if (zoneToDestroy != null)
        {
            // Si la zone rouge a un NetworkObject, utilise zoneToDestroy.GetComponent<NetworkObject>().Despawn();
            // Sinon, si c'est géré localement, le Rpc s'occupera de la nettoyer chez tout le monde.
            Destroy(zoneToDestroy, 0.1f);
        }

        // 3. EFFETS VISUELS ET SONORES : On dit à tout le monde de les afficher
        PlayExplosionEffectsRpc(transform.position);

        // 4. NETTOYAGE DU PROJECTILE : Le serveur despawn l'obus du réseau
        if (TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Despawn(); // Détruit proprement l'objet sur toutes les machines simultanément
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Ce RPC est envoyé par le serveur et s'exécute chez TOUS les joueurs (Serveur + Clients)
    [Rpc(SendTo.Everyone)]
    private void PlayExplosionEffectsRpc(Vector3 explosionPosition)
    {
        // 1. Effets Sonores chez tout le monde
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, explosionPosition, volume);
        }

        // 2. Effets Visuels chez tout le monde
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, explosionPosition, Quaternion.identity);
            Destroy(vfx, vfxDuration);
        }

        // Sécurité pour nettoyer la zone rouge chez les clients si ce n'est pas un NetworkObject
        if (zoneToDestroy != null)
        {
            Destroy(zoneToDestroy, 0.1f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}