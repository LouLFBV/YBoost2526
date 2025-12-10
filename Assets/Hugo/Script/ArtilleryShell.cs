using UnityEngine;
using System.Collections;

public class ArtilleryShell : MonoBehaviour
{
    [Header("Réglages de vol")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float arcHeight = 10f;
    [SerializeField] private AnimationCurve arcCurve;

    private GameObject zoneToDestroy; // Référence vers la zone rouge

    // On ajoute un paramètre pour récupérer la zone rouge
    public void Initialize(Vector3 targetPosition, GameObject targetZone)
    {
        zoneToDestroy = targetZone; // On mémorise la zone
        StartCoroutine(MoveInArc(targetPosition));
    }

    private IEnumerator MoveInArc(Vector3 destination)
    {
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, destination);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float linearProgress = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, destination, linearProgress);
            float heightOffset = arcCurve.Evaluate(linearProgress) * arcHeight;
            currentPos.y += heightOffset;

            transform.LookAt(currentPos);
            transform.position = currentPos;

            yield return null;
        }

        // --- IMPACT ---
        Debug.Log("BOOM !");

        // C'est ici qu'on détruit la zone rouge, au moment de l'impact
        if (zoneToDestroy != null)
        {
            Destroy(zoneToDestroy);
        }

        Destroy(gameObject); // Destruction de l'obus
    }
}