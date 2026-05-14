using Unity.Netcode;
using UnityEngine;

public class ExplosionDamage : NetworkBehaviour
{
    [SerializeField] private int boombeDamage = 50;

    private void OnTriggerEnter(Collider collision)
    {
        // SEUL le serveur calcule les dégâts
        if (!IsServer) return;

        // 1. On cherche PlayerStats dans l'objet ou ses parents (comme pour le tir)
        PlayerStats stats = collision.GetComponentInParent<PlayerStats>();

        if (stats != null)
        {
            // On vérifie si le joueur est déjà mort pour ne pas "overkill"
            if (stats.currentHealth.Value > 0)
            {
                // On utilise 999 pour dire "Mort par l'environnement"
                // La méthode TakeDamage sur le serveur lancera la coroutine de Respawn
                stats.TakeDamage(boombeDamage, 999);
                Debug.Log($"[SERVER] Explosion a touché {collision.name}");
            }
        }

        // 2. Gestion du décor destructible
        if (collision.TryGetComponent<Descrutable>(out var environment))
        {
            environment.DestroyObject(collision.transform.position, 1.5f);
        }
    }

    // L'explosion elle-même doit disparaître du réseau après 1 seconde
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // On attend 0.1s ou 1s pour laisser le temps au Trigger de détecter les gens
            Invoke(nameof(DespawnMe), 0.5f);
        }
    }

    private void DespawnMe()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}