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
    // Conservez fireForce, mais nous allons l'utiliser différemment ou la remplacer.

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

        Vector3 direction = targetPosition - turretPivot.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            // La rotation visée est correcte, mais le modèle est monté de 90° sur son axe.
            // Nous appliquons une rotation additionnelle de 90° (ou -90°) pour compenser l'orientation du modèle 3D.
            // Vous devez tester si c'est +90 ou -90. Nous allons partir sur +90f pour le test.
            Quaternion compensation = Quaternion.Euler(0, -30f, 0);

            // La rotation finale est la rotation visée multipliée par la compensation
            Quaternion finalRotation = lookRotation * compensation;

            turretPivot.rotation = Quaternion.Slerp(turretPivot.rotation, finalRotation, Time.deltaTime * rotationSpeed);
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
        Vector3 fireDirection = firePoint.forward;
        fireDirection.y = 0; // On s'assure d'avoir la direction purement horizontale

        GameObject obus = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity); // Pas besoin de rotation si le RB gère tout
        Rigidbody rb = obus.GetComponent<Rigidbody>();
        rb.AddForce(firePoint.forward * fireForce, ForceMode.Impulse);

    }

    
}