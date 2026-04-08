using UnityEngine;
using System;

public class WeaponPickup : MonoBehaviour
{
    public event Action OnPickedUp;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPickedUp?.Invoke();
        }
    }
}