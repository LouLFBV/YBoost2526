using System.Collections;
using UnityEngine;

public class Knife : Weapon, IWeapon
{
    private bool canAttack = true;
    public bool isAttacking = false;
    [SerializeField] private Animator animator;
    
    private BoxCollider attackCollider;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        attackCollider = GetComponent<BoxCollider>();
        if (attackCollider != null ) 
            attackCollider.enabled = false;
        animator = GetComponent<Animator>();
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    public void Attack()
    {
        if (canAttack)
        {
            Debug.Log("Coup de couteau !");
            animator.SetTrigger("Attack");
        }
    }
    protected override void OnTriggerEnter(Collider collision)
    {
        // Appelle le OnTriggerEnter du parent
        base.OnTriggerEnter(collision);

        // Ensuite gère l’attaque
        if (collision.CompareTag("Player") && isAttacking && !transform.CompareTag("Object"))
        {
            if (collision.transform.TryGetComponent<PlayerStats>(out var enemy))
                enemy.TakeDamage(weaponData.damage);
        }
    }

    public void ActiveAttack()
    {
        attackCollider.enabled = true;
        isAttacking = true;
        canAttack = false;
    }
    public void DesactiveAttack()
    {
        attackCollider.enabled = false;
        isAttacking = false;
        canAttack = true;
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
    }
}
