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
            if (collision.transform.TryGetComponent<PlayerStats>(out var enemy))
                enemy.TakeDamage(boombeDamage);
        }
        if (collision.transform.TryGetComponent<Descrutable>(out var environment))
            environment.DestroyObject(collision.transform.position, 1.5f);
    }

    private void OnEnable()
    {
        Destroy(gameObject, 1f);
    }
}
