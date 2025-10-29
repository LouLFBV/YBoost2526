using NUnit.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackBehaviour : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;

    public bool canShoot = false;
    public bool chargeBow;
    private Weapon weaponUsed;




    private void Start()
    {
        
    }

    void Update()
    {
    }

   
}