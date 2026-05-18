using UnityEngine;
using System.Collections;
using Unity.Netcode; // OBLIGATOIRE

public class TrainCombat : NetworkBehaviour // On passe en NetworkBehaviour
{
    [Header("Éléments 3D")]
    [SerializeField] private Transform turretPivot;
    [SerializeField] private Transform cannonBarrel;

    [Header("Cibles & Zone")]
    [SerializeField] private Transform zoneCenter;
    [SerializeField] private float firingRadius = 10f;
    [SerializeField] private GameObject targetZonePrefab;

    [Header("Munitions & Tourelle")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float launchAngle = 45f;

    [Header("Réglages Séquence")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float timeToCharge = 5f;
    [SerializeField] private float postFireDelay = 2.5f;

    private GameObject currentTargetZone;
    private Vector3 randomTargetPosition;
    private bool isSequenceStarted = false;
    private WaypointMover movementScript;

    [Header("Effets de Départ")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private GameObject muzzleFlashVFX;
    [SerializeField] private float fireVolume = 1f;

    void Start() { movementScript = GetComponent<WaypointMover>(); }

    void Update()
    {
        // Tout le monde (Host + Clients) fait tourner sa tourelle localement 
        // vers la position cible synchronisée.
        if (isSequenceStarted)
        {
            HandleTargeting();
        }
    }

    // Cette méthode est appelée par ton WaypointMover (sur le Serveur)
    public void StartArtillerySequence(Vector3 targetWptPosition)
    {
        if (!IsServer) return; // Sécurité : Seul le serveur initie le tir
        if (isSequenceStarted) return;

        // 1. Le serveur calcule la position aléatoire unique
        Vector3 center = (zoneCenter != null) ? zoneCenter.position : targetWptPosition;
        Vector2 randomCircle = Random.insideUnitCircle * firingRadius;
        Vector3 targetPos = new Vector3(center.x + randomCircle.x, center.y, center.z + randomCircle.y);

        // 2. Le serveur envoie la position à TOUT LE MONDE
        SyncArtillerySequenceRpc(targetPos);

        // 3. Le serveur gère le timing et le tir physique de l'obus
        StartCoroutine(AimAndFireRoutine());
    }

    [Rpc(SendTo.Everyone)]
    private void SyncArtillerySequenceRpc(Vector3 targetPosition)
    {
        isSequenceStarted = true;
        randomTargetPosition = targetPosition;

        // Tout le monde (Clients inclus) fait spawn la zone visuelle au sol localement
        if (targetZonePrefab != null)
        {
            currentTargetZone = Instantiate(targetZonePrefab, randomTargetPosition + Vector3.up * 0.1f, Quaternion.identity);
        }
    }

    private void HandleTargeting()
    {
        if (turretPivot == null) return;

        Vector3 direction = randomTargetPosition - turretPivot.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Quaternion compensation = Quaternion.Euler(0, 80f, 0);
            Quaternion finalRotation = lookRotation * compensation;

            turretPivot.rotation = Quaternion.Slerp(turretPivot.rotation, finalRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // La Coroutine de timing reste sur le Serveur pour valider le tir au bon moment
    private IEnumerator AimAndFireRoutine()
    {
        yield return new WaitForSeconds(timeToCharge);

        // On demande le spawn de l'obus sur le réseau
        FireBallisticShellServer();

        isSequenceStarted = false;
        yield return new WaitForSeconds(postFireDelay);
        if (movementScript != null) movementScript.ResumeMoving();
    }

    private void FireBallisticShellServer()
    {
        // 1. EFFETS LOCAUX SERVEUR + SPAWN OBUS
        GameObject obus = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // On passe la zone à détruire au script d'explosion (qui est un NetworkBehaviour maintenant)
        ProjectileExplosion scriptExplosion = obus.GetComponent<ProjectileExplosion>();
        if (scriptExplosion != null) scriptExplosion.zoneToDestroy = currentTargetZone;

        // 2. ON SPAWN L'OBUS SUR LE RÉSEAU
        if (obus.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();
        }

        // 3. On applique la force physique (sur le serveur)
        Rigidbody rb = obus.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = CalculateBallisticVelocity(firePoint.position, randomTargetPosition, launchAngle);
        }

        // 4. On dit à tout le monde de jouer le bruit du coup de canon et le flash de lumière
        PlayFireEffectsRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayFireEffectsRpc()
    {
        // Sons et particules exécutés chez tout le monde au moment du tir
        if (fireSound != null) AudioSource.PlayClipAtPoint(fireSound, firePoint.position, fireVolume);

        if (muzzleFlashVFX != null)
        {
            GameObject flash = Instantiate(muzzleFlashVFX, firePoint.position, firePoint.rotation);
            Destroy(flash, 2f);
        }
    }

    private Vector3 CalculateBallisticVelocity(Vector3 start, Vector3 end, float angle)
    {
        Vector3 direction = end - start;
        float height = direction.y;
        direction.y = 0;
        float distance = direction.magnitude;
        float a = angle * Mathf.Deg2Rad;
        direction.y = distance * Mathf.Tan(a);
        distance += height / Mathf.Tan(a);
        float velocity = Mathf.Sqrt(distance * Physics.gravity.magnitude / Mathf.Sin(2 * a));
        return direction.normalized * velocity;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = (zoneCenter != null) ? zoneCenter.position : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, firingRadius);
    }
}