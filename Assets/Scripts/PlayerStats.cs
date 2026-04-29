using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("Player Health")]
    [SerializeField] private int health = 100;
    private int currentHealth;

    [Header("Components")]
    [SerializeField] private Image life;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private TextMeshProUGUI healthQuantity;
    public ScoreSystem scoreSystem;

    public GameObject lastAttacker;


    private void Start()
    {
        currentHealth = health;
    }

    public void TakeDamage(int damage, GameObject attacker = null)
    {
        currentHealth -= damage;

        lastAttacker = attacker;
        UpdateHealthbar();
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        if (lastAttacker != null)
        {
            if (lastAttacker.TryGetComponent<ScoreSystem>(out var score))
            {
                score.AddTues();
            }
        }
        scoreSystem.AddMorts();
        Debug.Log("Player has died.");
        Destroy(gameObject);
    }

    private void UpdateHealthbar()
    {
        life.fillAmount = (float)currentHealth / health;
        healthQuantity.text = $"{currentHealth}/{health}";
    }
}
