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
    [SerializeField] private WeaponInPalette[] allWeaponsInPalette;
    [SerializeField] private Weapon[] allWeapons;

    private PlayerControls controls;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        controls = new PlayerControls();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();


    private void Update()
    {
        if(controls.Weapons.MainWeapon.triggered && weapons[0] != null)
            ChangeWeapon(weapons[0]);
        else if (controls.Weapons.SecondaryWeapon.triggered && weapons[1] != null)
            ChangeWeapon(weapons[1]);
        else if (controls.Weapons.MeleeWeapon.triggered && weapons[2] != null)
            ChangeWeapon(weapons[2]);   
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
            Array.Find(weapons, wv => wv.weaponData == newWeapon.weaponData).visualWeapon.SetActive(true);
            attackBehaviour.weaponUsed = Array.Find(allWeapons, w => w.weaponData == newWeapon.weaponData);
        }
    }

    private void UnequipWeapon(WeaponInPalette newWeapon)
    {
        if (newWeapon.isEquipped)
        {
            newWeapon.isEquipped = false;
            Array.Find(weapons, wv => wv.weaponData == newWeapon.weaponData).visualWeapon.SetActive(false);
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
    Melee
}


[System.Serializable]
public class WeaponInPalette
{
    public WeaponData weaponData;
    public GameObject visualWeapon;
    public bool isEquipped;
}

#endregion