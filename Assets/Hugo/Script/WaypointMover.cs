using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Waypoints waypoints;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float distanceThreshold = 0.1f;
    [SerializeField] private float rotationSpeed = 5f;
    

    [Header("Contrôles")]
    [SerializeField] private KeyCode startStopKey = KeyCode.Space;

    private bool isMoving = false;
    private Transform currentWaypoint;
    private TrainCombat trainCombat;

    void Start()
    {
        // Initialisation des waypoints
        // Remplacez 'WaypointMover.cs' par le nom du fichier réel si différent
        currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        transform.position = currentWaypoint.position;

        currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);

        transform.LookAt(currentWaypoint.position);
        trainCombat = GetComponent<TrainCombat>(); // Récupération du script de combat
    }

    void Update()
    {
        // Gestion de la touche Clavier (Start/Stop)
        /*if (Input.GetKeyDown(startStopKey))
        {
            isMoving = !isMoving;
        }*/

        if (isMoving == false) return;

        RotateTowardsWaypoint();

        transform.position = Vector3.MoveTowards(transform.position, currentWaypoint.position, moveSpeed * Time.deltaTime);

        // --- Déclenchement de l'événement de tir ---
        if (Vector3.Distance(transform.position, currentWaypoint.position) < distanceThreshold)
        {
            if (currentWaypoint.name.Contains("FIRE"))
            {
                isMoving = false;
                if (trainCombat != null)
                {
                    trainCombat.StartArtillerySequence(currentWaypoint.position);
                }
            }
            else
            {
                // On demande le prochain waypoint
                Transform nextWP = waypoints.GetNextWaypoint(currentWaypoint);

                if (nextWP == null)
                {
                    // Si c'est null, on s'arrête définitivement
                    isMoving = false;
                    Debug.Log("Dernier waypoint atteint. Arrêt du train.");
                }
                else
                {
                    // Sinon, on continue
                    currentWaypoint = nextWP;
                }
            }
        }
    }

    private void RotateTowardsWaypoint()
    {
        Vector3 direction = currentWaypoint.position - transform.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // Fonction pour arrêter le train (si appelé par un autre script)
    public void ForceStop()
    {
        isMoving = false;
    }

    // Fonction pour redémarrer le train (appelée par TrainCombat après le tir)
    public void ResumeMoving()
    {
        Transform nextWP = waypoints.GetNextWaypoint(currentWaypoint);

        if (nextWP != null)
        {
            currentWaypoint = nextWP;
            isMoving = true;
        }
        else
        {
            isMoving = false;
            Debug.Log("Fin de trajet après combat.");
        }
    }

    // Nouvelle méthode pour lancer le train via le timer
    public void StartTrain()
    {
        if (!isMoving)
        {
            isMoving = true;
            Debug.Log("Le train de combat est en route !");
        }
    }

}