using Unity.Netcode;
using UnityEngine;

public class ExplosionDamage : NetworkBehaviour // NetworkBehaviour pour checker IsServer
{
    [SerializeField] private int boombeDamage;

    private void OnTriggerEnter(Collider collision)
    {
        // SEUL le serveur inflige les dégâts pour les objets neutres
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            if (collision.transform.TryGetComponent<PlayerStats>(out var enemy))
            {
                // On envoie 999 (ou 0) comme ID pour indiquer que ce n'est pas un joueur
                // Le score ne sera attribué à personne
                enemy.TakeDamage(boombeDamage, 999);
            }
        }

        if (collision.transform.TryGetComponent<Descrutable>(out var environment))
        {
            environment.DestroyObject(collision.transform.position, 1.5f);
        }
    }

    private void OnEnable()
    {
        // Si c'est un NetworkObject, on utilise Despawn sur le serveur
        if (IsServer)
        {
            Invoke(nameof(DespawnMe), 1f);
        }
    }

    private void DespawnMe()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
}