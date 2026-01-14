using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

public class Palette : MonoBehaviour
{
    public static Palette instance;

    [Header("Slots for Weapons")]
    public WeaponInPalette[] weapons;
    [SerializeField] private Image[] slotsIconeWeapon;


    [Header("References")]
    [SerializeField] private AttackBehaviour attackBehaviour;
    public WeaponInPalette[] allWeaponsInPalette;
    [SerializeField] private Weapon[] allWeapons;

    [SerializeField]private PlayerInput playerInput;
    private bool takingMainWeapon;
    private bool takingSecondaryWeapon;
    private bool takingMeleeWeapon;
    private bool takingProjectile;

    private bool isInitialized = false;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return; 
        }

        playerInput = GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError("Palette : PlayerInput manquant !");
            return;
        }

        isInitialized = true;
    }


    #region Méthodes Player Input 
    private void OnEnable()
    {
        if (!isInitialized || playerInput == null)
            return;
        playerInput.actions["MainWeapon"].Enable();
        playerInput.actions["SecondaryWeapon"].Enable();
        playerInput.actions["MeleeWeapon"].Enable();
        playerInput.actions["Projectile"].Enable();

        playerInput.actions["MainWeapon"].performed += MainWeaponPerformed;
        playerInput.actions["SecondaryWeapon"].performed += SecondaryWeaponPerformed;
        playerInput.actions["MeleeWeapon"].performed += MeleeWeaponPerformed;
        playerInput.actions["Projectile"].performed += ProjectilePerformed;

        playerInput.actions["MainWeapon"].canceled += MainWeaponCanceled;
        playerInput.actions["SecondaryWeapon"].canceled += SecondaryWeaponCanceled;
        playerInput.actions["MeleeWeapon"].canceled += MeleeWeaponCanceled;
        playerInput.actions["Projectile"].canceled += ProjectileCanceled;
    }
    private void OnDisable()
    {

        if (!isInitialized || playerInput == null)
            return;
        playerInput.actions["MainWeapon"].Disable();
        playerInput.actions["SecondaryWeapon"].Disable();
        playerInput.actions["MeleeWeapon"].Disable();
        playerInput.actions["Projectile"].Disable();

        playerInput.actions["MainWeapon"].performed -= MainWeaponPerformed;
        playerInput.actions["SecondaryWeapon"].performed -= SecondaryWeaponPerformed;
        playerInput.actions["MeleeWeapon"].performed -= MeleeWeaponPerformed;
        playerInput.actions["Projectile"].performed -= ProjectilePerformed;

        playerInput.actions["MainWeapon"].canceled -= MainWeaponCanceled;
        playerInput.actions["SecondaryWeapon"].canceled -= SecondaryWeaponCanceled;
        playerInput.actions["MeleeWeapon"].canceled -= MeleeWeaponCanceled;
        playerInput.actions["Projectile"].canceled -= ProjectileCanceled;
    }

    private void ProjectileCanceled(InputAction.CallbackContext context)
    {
        takingProjectile = false;
    }

    private void MeleeWeaponCanceled(InputAction.CallbackContext context)
    {
        takingMeleeWeapon = false;
    }

    private void SecondaryWeaponCanceled(InputAction.CallbackContext context)
    {
        takingSecondaryWeapon = false;
    }

    private void MainWeaponCanceled(InputAction.CallbackContext context)
    {
        takingMainWeapon = false;
    }

    private void ProjectilePerformed(InputAction.CallbackContext context)
    {
        takingProjectile = true;
    }

    private void MeleeWeaponPerformed(InputAction.CallbackContext context)
    {
        takingMeleeWeapon = true;
    }

    private void SecondaryWeaponPerformed(InputAction.CallbackContext context)
    {
        takingSecondaryWeapon = true;
    }

    private void MainWeaponPerformed(InputAction.CallbackContext context)
    {
        takingMainWeapon = true;
    }
    #endregion 

    private void Update()
    {
        if(takingMainWeapon && weapons[0] != null)
        {
            ChangeWeapon(weapons[0]);
            takingMainWeapon = false;
        }
        else if (takingSecondaryWeapon && weapons[1] != null)
        {
            ChangeWeapon(weapons[1]);
            takingSecondaryWeapon = false;
        }
        else if (takingMeleeWeapon && weapons[2] != null)
        {
            ChangeWeapon(weapons[2]);
            takingMeleeWeapon = false;
        }
        else if (takingProjectile && weapons[3] != null)
        {
            ChangeWeapon(weapons[3]);
            takingProjectile = false;
        }
    }
    public void AddWeapon(Weapon weaponPickUp)
    {
        GameObject mainVisual = null;
        switch (weaponPickUp.weaponData.weaponType)
        {
            case WeaponType.Main:
                AddWeaponInPalette(weaponPickUp, mainVisual, 0);
                break;
            case WeaponType.Secondary:
                AddWeaponInPalette(weaponPickUp, mainVisual, 1);
                break;
            case WeaponType.Melee:
                AddWeaponInPalette(weaponPickUp, mainVisual, 2);
                break;
            case WeaponType.Projectile:
                AddWeaponInPalette(weaponPickUp, mainVisual, 3);
                break;
        }
    }

    private void AddWeaponInPalette(Weapon newWeapon, GameObject visual, int index)
    {
        weapons[index].weaponData = newWeapon.weaponData;
        slotsIconeWeapon[index].sprite = newWeapon.weaponData.icone;
        visual = Array.Find(allWeaponsInPalette, w => w.weaponData == newWeapon.weaponData).visualWeapon;
        weapons[index].visualWeapon = visual;
        if (!CheckIfOneWeaponIsEquipped())
        {
            weapons[index].isEquipped = true;
            visual.SetActive(true);
        }
        if (attackBehaviour.weaponUsed == null)
            attackBehaviour.weaponUsed = Array.Find(allWeapons, w => w.weaponData == newWeapon.weaponData);
    }


    private void ChangeWeapon(WeaponInPalette weapon)
    {
        if (weapon.isEquipped)
        {
            UnequipWeapon(weapon);
        }
        else
        {
            foreach (var w in weapons)
            {
                if (w != null && w.isEquipped)
                {
                    UnequipWeapon(w);
                }
            }
            EquipWeapon(weapon);
        }
    }

    private void EquipWeapon(WeaponInPalette newWeapon)
    {
        if (!newWeapon.isEquipped)
        {
            newWeapon.isEquipped = true;
            GameObject currentWeapon = Array.Find(weapons, wv => wv.weaponData == newWeapon.weaponData).visualWeapon;
            if (currentWeapon != null) currentWeapon.SetActive(true);
            attackBehaviour.weaponUsed = Array.Find(allWeapons, w => w.weaponData == newWeapon.weaponData);
        }
    }

    private void UnequipWeapon(WeaponInPalette newWeapon)
    {
        if (newWeapon.isEquipped)
        {
            newWeapon.isEquipped = false;
            GameObject currentWeapon = Array.Find(weapons, wv => wv.weaponData == newWeapon.weaponData).visualWeapon;
            if (currentWeapon != null) currentWeapon.SetActive(false);
            attackBehaviour.weaponUsed = null;
        }
    }


    private bool CheckIfOneWeaponIsEquipped()
    {
        return Array.Exists(weapons, w => w != null && w.isEquipped);
    }
}

#region WeaponType and WeaponInPalette Classes
[System.Serializable]
public enum WeaponType
{
    Main,
    Secondary,
    Melee,
    Projectile
}


[System.Serializable]
public class WeaponInPalette
{
    public WeaponData weaponData;
    public GameObject visualWeapon;
    public bool isEquipped;
}

#endregion