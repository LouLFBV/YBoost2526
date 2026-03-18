using UnityEngine;
using System.Collections;

public class TrainCombat : MonoBehaviour
{
    [Header("Éléments 3D")]
    [SerializeField] private Transform turretPivot;  // Tourne Gauche/Droite
    [SerializeField] private Transform cannonBarrel; // Tourne Haut/Bas

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
        if (isSequenceStarted)
        {
            HandleTargeting();
        }
    }

    public void StartArtillerySequence(Vector3 targetWptPosition)
    {
        if (isSequenceStarted) return;
        isSequenceStarted = true;

        Vector3 center = (zoneCenter != null) ? zoneCenter.position : targetWptPosition;
        Vector2 randomCircle = Random.insideUnitCircle * firingRadius;
        randomTargetPosition = new Vector3(center.x + randomCircle.x, center.y, center.z + randomCircle.y);

        if (targetZonePrefab != null)
        {
            currentTargetZone = Instantiate(targetZonePrefab, randomTargetPosition + Vector3.up * 0.1f, Quaternion.identity);
        }

        StartCoroutine(AimAndFireRoutine());
    }

    private void HandleTargeting()
    {
        if (turretPivot == null) return;

        // 1. Calcul de la direction vers la zone aléatoire
        Vector3 direction = randomTargetPosition - turretPivot.position;
        direction.y = 0; // On ignore la hauteur pour ne pas incliner le canon

        if (direction != Vector3.zero)
        {
            // 2. Calcul de la rotation de base "Regarder vers"
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            // 3. APPLICATION DE LA COMPENSATION PRÉCISE
            // Si ton canon est à -31 au lieu de -110, on ajoute la différence (-79 degrés)
            // Modifie le chiffre -79f ci-dessous pour ajuster si besoin
            Quaternion compensation = Quaternion.Euler(0, 80f, 0);

            Quaternion finalRotation = lookRotation * compensation;

            // 4. Rotation fluide uniquement sur le pivot
            turretPivot.rotation = Quaternion.Slerp(turretPivot.rotation, finalRotation, Time.deltaTime * rotationSpeed);
        }

        // Le CannonBarrel reste fixe ou suit simplement le pivot sans changer son X
    }

    private IEnumerator AimAndFireRoutine()
    {
        yield return new WaitForSeconds(timeToCharge);
        FireBallisticShell();
        isSequenceStarted = false;
        yield return new WaitForSeconds(postFireDelay);
        if (movementScript != null) movementScript.ResumeMoving();
    }

    private void FireBallisticShell()
    {
        if (fireSound != null) AudioSource.PlayClipAtPoint(fireSound, firePoint.position, fireVolume);

        GameObject obus = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        ProjectileExplosion scriptExplosion = obus.GetComponent<ProjectileExplosion>();
        if (scriptExplosion != null) scriptExplosion.zoneToDestroy = currentTargetZone;

        if (muzzleFlashVFX != null)
        {
            GameObject flash = Instantiate(muzzleFlashVFX, firePoint.position, firePoint.rotation);
            Destroy(flash, 2f);
        }

        Rigidbody rb = obus.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = CalculateBallisticVelocity(firePoint.position, randomTargetPosition, launchAngle);
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