using UnityEngine;
using System.Collections;

public class SimpleProjectile : MonoBehaviour
{
    // Délai de sécurité avant d'activer l'explosion
    private float safetyDelay = 1f;
    private float birthTime;

    void Start()
    {
        birthTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Si l'obus est trop jeune, on ignore la collision (c'est sûrement le canon)
        if (Time.time < birthTime + safetyDelay) return;

        Debug.Log("BOOM sur : " + collision.gameObject.name);
        Destroy(gameObject);
    }
}