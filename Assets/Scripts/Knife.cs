using System.Collections;
using UnityEngine;

public class Knife : Weapon, IWeapon
{
    private bool canAttack = false;
    public float attackDistance = 0.5f;
    public float attackDuration = 0.2f;
    public float attackArcHeight = 0.2f;

    private Collider attackCollider;

    private void Awake()
    {
        attackCollider = GetComponent<Collider>();
        attackCollider.enabled = false;
    }

    public void Attack()
    {
        if (!canAttack)
        {
            Debug.Log("Coup de couteau !");
            StartCoroutine(AttackMovement());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && canAttack)
        {
            Debug.Log("Hit " + other.name);
            if (other.transform.TryGetComponent<PlayerStats>(out var enemy))
            {
                enemy.TakeDamage(weaponData.damage);
            }
        }
    }

    private IEnumerator AttackMovement()
    {

        canAttack = true;
        attackCollider.enabled = true;

        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + transform.forward * attackDistance;

        float elapsed = 0f;

        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / attackDuration;

            // Mouvement circulaire : utilise une courbe parabolique
            float height = Mathf.Sin(t * Mathf.PI) * attackArcHeight;
            transform.localPosition = Vector3.Lerp(startPos, endPos, t) + transform.up * height;

            yield return null;
        }

        transform.localPosition = startPos;
        attackCollider.enabled = false;
        canAttack = false;
    }
}
