using UnityEngine;

public class ExplosionDamage : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    [SerializeField] private int boombeDamage;
    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log("Babe later gonna watch me");
        if (collision.CompareTag("Player"))
        {
            Debug.Log("You baby cry on me");
            if (collision.transform.TryGetComponent<PlayerStats>(out var enemy))
                enemy.TakeDamage(boombeDamage);
        }
    }

    private void OnEnable()
    {
        Destroy(gameObject, 1f);
    }
}
