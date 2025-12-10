using UnityEngine;
using System.Collections;

public class TrainCombat : MonoBehaviour
{
    [Header("Cibles & Munitions")]
    [SerializeField] private Transform targetZone; // Référence de la zone prédéfinie (peut être ignorée ici)
    [SerializeField] private GameObject targetZonePrefab; // Le prefab de la zone rouge
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform turretPivot; // L'objet 'turret' à faire tourner

    [Header("Puissance de Feu")]
    [SerializeField] private float fireForce = 50f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float timeToCharge = 5f;
    [SerializeField] private float postFireDelay = 2.5f;

    private GameObject currentTargetZone;
    private Vector3 targetPosition;
    private bool isSequenceStarted = false;
    private WaypointMover movementScript;

    void Start()
    {
        movementScript = GetComponent<WaypointMover>();
    }

    void Update()
    {
        if (isSequenceStarted)
        {
            HandleTargeting();
        }
    }

    // Fonction appelée par WaypointMover
    public void StartArtillerySequence(Vector3 targetWptPosition)
    {
        if (isSequenceStarted) return;

        isSequenceStarted = true;
        targetPosition = targetWptPosition; // La position cible est le Waypoint de tir

        // Fait apparaître la zone rouge
        currentTargetZone = Instantiate(targetZonePrefab, targetPosition + Vector3.up * 0.1f, Quaternion.identity);

        // Lance la routine d'attente/tir
        StartCoroutine(AimAndFireRoutine());
    }

    private void HandleTargeting()
    {
        if (turretPivot == null) return;

        // Utilise la position CIBLE et la position du PIVOT de la tourelle
        Vector3 direction = targetPosition - turretPivot.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            // On s'assure que C'EST BIEN turretPivot QUI TOURNE
            turretPivot.rotation = Quaternion.Slerp(turretPivot.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // TrainCombat.cs - Fonction AimAndFireRoutine()

    private IEnumerator AimAndFireRoutine()
    {
        float angleThreshold = 5f;
        float maxRotationTime = 5f; // NOUVEAU : Temps maximum alloué pour la rotation
        float elapsedRotationTime = 0f; // NOUVEAU : Compteur de temps

        // 1. Attente de l'alignement
        while (true)
        {
            elapsedRotationTime += Time.deltaTime;

            // Sécurité : Si le temps max est dépassé, on sort et on tire
            if (elapsedRotationTime >= maxRotationTime)
            {
                Debug.LogWarning("Tourelle non alignée après 5s, tir forcé.");
                break;
            }

            // Vérification de l'alignement (Logique inchangée)
            Vector3 direction = targetPosition - turretPivot.position;
            direction.y = 0;

            if (direction == Vector3.zero || turretPivot == null) break;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float angle = Quaternion.Angle(turretPivot.rotation, targetRotation);

            if (angle <= angleThreshold)
            {
                break; // Alignement OK
            }

            yield return null;
        }

        // 2. Délai de chargement de 5 secondes
        yield return new WaitForSeconds(timeToCharge);

        // 3. Tir
        FirePhysicalShell();

        // 4. Fin de la séquence (isSequenceStarted = false)
        isSequenceStarted = false;

        // 5. Destruction de la zone rouge
        if (currentTargetZone != null)
        {
            Destroy(currentTargetZone);
        }

        // --- NOUVEAU : Délai de sécurité après le tir ---
        // On attend que l'obus soit loin avant de bouger
        yield return new WaitForSeconds(postFireDelay);

        // 6. Redémarrage du train
        if (movementScript != null)
        {
            movementScript.ResumeMoving();
        }
    }

    private void FirePhysicalShell()
    {
        GameObject obus = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = obus.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Tir physique
            rb.AddForce(firePoint.forward * fireForce, ForceMode.Impulse);
        }
    }
}