using System.Collections;
using UnityEngine;

public class Knife : Weapon, IWeapon
{
    private bool canAttack = false;
    [SerializeField] private Animator animator;
    
    private BoxCollider attackCollider;

    private void Awake()
    {
        attackCollider = GetComponent<BoxCollider>();
        attackCollider.enabled = false;
        animator = GetComponent<Animator>();
    }

    public void Attack()
    {
        if (canAttack)
        {
            Debug.Log("Coup de couteau !");
            animator.SetTrigger("Atttack");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player") && canAttack)
        {
            Debug.Log("Hit " + collision.collider.name);
            if (collision.transform.TryGetComponent<PlayerStats>(out var enemy))
            {
                enemy.TakeDamage(weaponData.damage);
            }
        }
    }
    public void ActiveAttack()
    {
        attackCollider.enabled = true;
        canAttack = false;
    }
    public void DesactiveAttack()
    {
        canAttack = true;
        attackCollider.enabled = false;
    }
}
