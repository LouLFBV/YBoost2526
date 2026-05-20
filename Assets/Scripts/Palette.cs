using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using TMPro;
using Unity.Netcode;

public class Palette : NetworkBehaviour
{
    [Header("Slots for Weapons")]
    public WeaponInPalette[] weapons;
    [SerializeField] private Image[] slotsIconeWeapon;

    [Header("References")]
    [SerializeField] private AttackBehaviour attackBehaviour;
    public WeaponInPalette[] allWeaponsInPalette;
    [SerializeField] private Weapon[] allWeapons;

    [SerializeField] private PlayerInput playerInput;
    private bool takingMainWeapon;
    private bool takingSecondaryWeapon;
    private bool takingMeleeWeapon;
    private bool takingProjectile;

    private bool isInitialized = false;

    public event Action<bool> DestroySniperViseur;

    private void Awake()
    {
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

    private void ProjectileCanceled(InputAction.CallbackContext context) => takingProjectile = false;
    private void MeleeWeaponCanceled(InputAction.CallbackContext context) => takingMeleeWeapon = false;
    private void SecondaryWeaponCanceled(InputAction.CallbackContext context) => takingSecondaryWeapon = false;
    private void MainWeaponCanceled(InputAction.CallbackContext context) => takingMainWeapon = false;
    private void ProjectilePerformed(InputAction.CallbackContext context) => takingProjectile = true;
    private void MeleeWeaponPerformed(InputAction.CallbackContext context) => takingMeleeWeapon = true;
    private void SecondaryWeaponPerformed(InputAction.CallbackContext context) => takingSecondaryWeapon = true;
    private void MainWeaponPerformed(InputAction.CallbackContext context) => takingMainWeapon = true;
    #endregion 

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        // Cache le visuel de TOUTES les armes au début
        foreach (var w in allWeaponsInPalette)
        {
            if (w.visualWeapon != null)
                SetWeaponVisibility(w.visualWeapon, false);
        }
    }

    private void SetWeaponVisibility(GameObject visual, bool isVisible)
    {
        if (visual == null) return;

        if (visual.TryGetComponent<MeshRenderer>(out var renderer)) renderer.enabled = isVisible;

        foreach (var r in visual.GetComponentsInChildren<MeshRenderer>())
        {
            r.enabled = isVisible;
        }

        if (visual.TryGetComponent<Weapon>(out var weaponScript))
        {
            weaponScript.enabled = isVisible;
        }
        else
        {
            var childWeapon = visual.GetComponentInChildren<Weapon>();
            if (childWeapon != null) childWeapon.enabled = isVisible;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (takingMainWeapon && weapons[0].weaponData != null)
        {
            ChangeWeapon(weapons[0]);
            takingMainWeapon = false;
        }
        else if (takingSecondaryWeapon && weapons[1].weaponData != null)
        {
            ChangeWeapon(weapons[1]);
            takingSecondaryWeapon = false;
        }
        else if (takingMeleeWeapon && weapons[2].weaponData != null)
        {
            ChangeWeapon(weapons[2]);
            takingMeleeWeapon = false;
        }
        else if (takingProjectile && weapons[3].weaponData != null)
        {
            ChangeWeapon(weapons[3]);
            takingProjectile = false;
        }
    }

    public void AddWeapon(Weapon weaponPickUp)
    {
        if (weaponPickUp == null || weaponPickUp.weaponData == null) return;

        switch (weaponPickUp.weaponData.weaponType)
        {
            case WeaponType.Main:
                AddWeaponInPalette(weaponPickUp, 0);
                break;
            case WeaponType.Secondary:
                AddWeaponInPalette(weaponPickUp, 1);
                break;
            case WeaponType.Melee:
                AddWeaponInPalette(weaponPickUp, 2);
                break;
            case WeaponType.Projectile:
                AddWeaponInPalette(weaponPickUp, 3);
                break;
        }
    }

    private void AddWeaponInPalette(Weapon newWeapon, int index)
    {
        // 1. Si une arme occupe déjà ce slot, on la drop proprement
        if (weapons[index].weaponData != null)
        {
            int currentAmmunition = 0;
            if (weapons[index].visualWeapon != null && weapons[index].visualWeapon.TryGetComponent<Weapon>(out Weapon weaponInHand))
            {
                currentAmmunition = weaponInHand.ammunitionAccount;
            }

            int dataIndex = Array.FindIndex(allWeapons, w => w.weaponData == weapons[index].weaponData);
            DropWeaponServerRpc(dataIndex, currentAmmunition, transform.position + transform.forward);

            // Supprime l'ancienne arme (et la déséquipe proprement si elle était en main)
            RemoveWeaponInPalette(weapons[index].weaponData.weaponType);
        }

        // 2. Attribution des nouvelles données dans le slot
        weapons[index].weaponData = newWeapon.weaponData;
        slotsIconeWeapon[index].sprite = newWeapon.weaponData.icone;

        WeaponInPalette found = Array.Find(allWeaponsInPalette, w => w != null && w.weaponData == newWeapon.weaponData);
        if (found == null)
        {
            Debug.LogError($"Weapon [{newWeapon.weaponData.name}] non trouvé dans allWeaponsInPalette");
            return;
        }

        weapons[index].visualWeapon = found.visualWeapon;
        weapons[index].ammunition.text = newWeapon.ammunitionAccount == 0 ? "" : newWeapon.ammunitionAccount.ToString();

        // On synchronise les munitions sur le script de l'arme visuelle
        if (weapons[index].visualWeapon != null && weapons[index].visualWeapon.TryGetComponent<Weapon>(out var vWeapon))
        {
            vWeapon.ammunitionAccount = newWeapon.ammunitionAccount;
        }

        // 3. Gestion de l'équipement automatique :
        // Si le joueur n'a STRICTEMENT AUCUNE arme équipée actuellement, on équipe celle-ci immédiatement.
        if (!CheckIfOneWeaponIsEquipped())
        {
            EquipWeapon(weapons[index]);
        }
    }

    public void RemoveWeaponInPalette(WeaponType weaponType)
    {
        int index = 0;
        switch (weaponType)
        {
            case WeaponType.Main: index = 0; break;
            case WeaponType.Secondary: index = 1; break;
            case WeaponType.Melee: index = 2; break;
            case WeaponType.Projectile: index = 3; break;
        }

        GameObject visual = weapons[index].visualWeapon;
        bool wasEquipped = weapons[index].isEquipped;

        // Nettoyage complet des données du slot
        weapons[index].weaponData = null;
        weapons[index].visualWeapon = null;
        weapons[index].ammunition.text = "";
        slotsIconeWeapon[index].sprite = null;
        weapons[index].isEquipped = false;

        if (wasEquipped && visual != null)
        {
            SetWeaponVisibility(visual, false);
            if (attackBehaviour.weaponUsed != null)
            {
                attackBehaviour.weaponUsed = null;
            }
            if (IsOwner) GetComponent<FirstPersonController_Networked>().ResetZoom();
        }
    }

    private void ChangeWeapon(WeaponInPalette weapon)
    {
        if (weapon.visualWeapon == null) return;
        weapon.visualWeapon.transform.localScale = Vector3.one;

        if (weapon.isEquipped)
        {
            UnequipWeapon(weapon);
        }
        else
        {
            // Déséquipe l'arme actuellement portée (s'il y en a une)
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
        if (newWeapon == null || newWeapon.weaponData == null) return;

        newWeapon.isEquipped = true;

        if (newWeapon.visualWeapon != null)
        {
            SetWeaponVisibility(newWeapon.visualWeapon, true);

            if (newWeapon.visualWeapon.TryGetComponent<Weapon>(out var localWeaponScript))
            {
                attackBehaviour.weaponUsed = localWeaponScript;
            }
            else
            {
                attackBehaviour.weaponUsed = newWeapon.visualWeapon.GetComponentInChildren<Weapon>();
            }
        }
    }

    private void UnequipWeapon(WeaponInPalette oldWeapon)
    {
        if (oldWeapon == null || !oldWeapon.isEquipped) return;

        if (oldWeapon.visualWeapon != null)
        {
            SetWeaponVisibility(oldWeapon.visualWeapon, false);
        }

        oldWeapon.isEquipped = false;
        attackBehaviour.weaponUsed = null;
        if (IsOwner) GetComponent<FirstPersonController_Networked>().ResetZoom();
    }

    private bool CheckIfOneWeaponIsEquipped()
    {
        return Array.Exists(weapons, w => w != null && w.isEquipped);
    }

    public void UpdateAmmunitionText(WeaponType type, int newAmmunition)
    {
        int index = (int)type; // Aligné sur l'index de ton enum (Main=0, Secondary=1...)
        if (index >= 0 && index < weapons.Length && weapons[index] != null && weapons[index].ammunition != null)
        {
            weapons[index].ammunition.text = newAmmunition.ToString();
        }
    }

    [ServerRpc]
    public void DropWeaponServerRpc(int weaponDataIndex, int ammo, Vector3 position)
    {
        if (weaponDataIndex < 0 || weaponDataIndex >= allWeapons.Length) return;

        GameObject droppedObj = Instantiate(allWeapons[weaponDataIndex].weaponData.weaponPrefab, position, Quaternion.identity);

        if (droppedObj.TryGetComponent<Weapon>(out var weapon))
        {
            weapon.ammunitionAccount = ammo;
        }

        droppedObj.GetComponent<NetworkObject>().Spawn();
    }

    public void AddWeaponFromNetwork(string weaponDataName, int ammo)
    {
        Weapon foundWeapon = Array.Find(allWeapons, w => w.weaponData.name == weaponDataName);

        if (foundWeapon != null)
        {
            foundWeapon.ammunitionAccount = ammo;
            AddWeapon(foundWeapon);
        }
        else
        {
            Debug.LogError($"[PALETTE] Impossible de trouver l'arme nommée : {weaponDataName} dans allWeapons !");
        }
    }
}
[System.Serializable]
public class WeaponInPalette
{
    public WeaponData weaponData;
    public GameObject visualWeapon;
    public TextMeshProUGUI ammunition;
    public bool isEquipped;
}