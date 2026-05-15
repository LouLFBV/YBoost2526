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

    [SerializeField]private PlayerInput playerInput;
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

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return; // Seulement pour le joueur local

        // Cache le visuel de TOUTES les armes au début
        foreach (var w in allWeaponsInPalette)
        {
            if (w.visualWeapon != null)
                SetWeaponVisibility(w.visualWeapon, false);
        }
    }

    // Petite fonction utilitaire pour cacher/montrer proprement
    private void SetWeaponVisibility(GameObject visual, bool isVisible)
    {
        // On cache le MeshRenderer (le visuel)
        if (visual.TryGetComponent<MeshRenderer>(out var renderer)) renderer.enabled = isVisible;

        // On cache aussi tous les MeshRenderers des enfants (très important)
        foreach (var r in visual.GetComponentsInChildren<MeshRenderer>())
        {
            r.enabled = isVisible;
        }

        // On peut aussi désactiver le script de tir pour éviter de tirer en étant caché
        if (visual.TryGetComponent<Weapon>(out var weaponScript))
        {
            weaponScript.enabled = isVisible;
        }
    }

    private void Update()
    {
        if(takingMainWeapon && weapons[0].weaponData != null)
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
        GameObject mainVisual = null;
        Debug.Log($"weaponPickUp : {weaponPickUp}"); 
        Debug.Log($" weaponPickUp.weaponData : {weaponPickUp.weaponData}");
        Debug.Log($" weaponPickUp.weaponData.weaponType : {weaponPickUp.weaponData.weaponType} ");
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
        if (weapons[index].weaponData != null)
        {
            int currentAmmunition = 0;
            if (weapons[index].visualWeapon.TryGetComponent<Weapon>(out Weapon weaponInHand))
            {
                currentAmmunition = weaponInHand.ammunitionAccount;
            }

            int dataIndex = Array.FindIndex(allWeapons, w => w.weaponData == weapons[index].weaponData);
            DropWeaponServerRpc(dataIndex, currentAmmunition, transform.position + transform.forward);

            RemoveWeaponInPalette(weapons[index].weaponData.weaponType);
        }
        weapons[index].weaponData = newWeapon.weaponData;
        slotsIconeWeapon[index].sprite = newWeapon.weaponData.icone;
        WeaponInPalette found = Array.Find(
            allWeaponsInPalette,
            w => w != null && w.weaponData == newWeapon.weaponData
        );

        if (found == null)
        {
            Debug.LogError("Weapon non trouvé dans allWeaponsInPalette");
            return;
        }

        visual = found.visualWeapon;
        weapons[index].visualWeapon = visual;
        weapons[index].ammunition.text = newWeapon.ammunitionAccount == 0 ? "" : newWeapon.ammunitionAccount.ToString();
        Debug.Log("Munitions à jour");
        if (!CheckIfOneWeaponIsEquipped())
        {
            weapons[index].isEquipped = true;
            visual.SetActive(true);
            visual.GetComponent<MeshRenderer>().enabled = true;
        }
        if (attackBehaviour.weaponUsed == null)
            attackBehaviour.weaponUsed = Array.Find(allWeapons, w => w.weaponData == newWeapon.weaponData);

        

    }

    public void RemoveWeaponInPalette(WeaponType weaponType)
    {
        Debug.Log("Remove weapon in palette");
        int index = 0;
        GameObject visual = null;
        switch (weaponType)
        {
            case WeaponType.Main:
                index = 0;
                visual = weapons[0].visualWeapon;
                break;
            case WeaponType.Secondary:
                index = 1;
                visual = weapons[1].visualWeapon;
                break;
            case WeaponType.Melee:
                index = 2;
                visual = weapons[2].visualWeapon;
                break;
            case WeaponType.Projectile:
                index = 3;
                visual = weapons[3].visualWeapon;
                break;
        }
        weapons[index].weaponData = null;
        weapons[index].visualWeapon = null;
        weapons[index].ammunition.text = "";
        slotsIconeWeapon[index].sprite = null;
        if (CheckIfOneWeaponIsEquipped())
        {
            weapons[index].isEquipped = false;
            visual.SetActive(false);
        }
        if (attackBehaviour.weaponUsed != null)
            attackBehaviour.weaponUsed = null;
    }

    private void ChangeWeapon(WeaponInPalette weapon)
    {
        weapon.visualWeapon.transform.localScale = Vector3.one;
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
            GameObject visual = Array.Find(weapons, wv => wv.weaponData == newWeapon.weaponData).visualWeapon;

            if (visual != null)
                SetWeaponVisibility(visual, true); // On montre le visuel

            attackBehaviour.weaponUsed = Array.Find(allWeapons, w => w.weaponData == newWeapon.weaponData);
        }
    }

    private void UnequipWeapon(WeaponInPalette oldWeapon)
    {
        if (!oldWeapon.isEquipped) return;

        WeaponInPalette slot = Array.Find(weapons, wv => wv != null && wv.weaponData == oldWeapon.weaponData);

        if (slot != null && slot.visualWeapon != null)
        {
            SetWeaponVisibility(slot.visualWeapon, false); // On cache le visuel
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
        switch (type)
        {
            case WeaponType.Main:
                weapons[0].ammunition.text = newAmmunition.ToString();
                break;
            case WeaponType.Secondary:
                weapons[1].ammunition.text = newAmmunition.ToString();
                break;
            case WeaponType.Melee:
                weapons[2].ammunition.text = newAmmunition.ToString();
                break;
            case WeaponType.Projectile:
                weapons[3].ammunition.text = newAmmunition.ToString();
                break;
        }
    }

    [Rpc(SendTo.Everyone)] // Tout le monde doit savoir que tu as ramassé l'arme pour voir le visuel
    public void SyncPickupClientRpc(WeaponType type, int ammunitions)
    {
        // On simule un faux objet Weapon pour réutiliser ta logique AddWeapon existante
        // Ou mieux : on modifie AddWeapon pour accepter les données directement

        // Ici, on appelle ta logique de mise à jour d'UI et de visuel
        // (Il faudra peut-être ajuster AddWeaponInPalette pour qu'elle ne cherche pas 
        // à lire des infos sur un objet détruit)

        Debug.Log($"[NET] Arme ramassée synchronisée : {type}");
    }


    [ServerRpc]
    public void DropWeaponServerRpc(int weaponDataIndex, int ammo, Vector3 position)
    {
        // 1. On instancie sur le serveur
        GameObject droppedObj = Instantiate(allWeapons[weaponDataIndex].weaponData.weaponPrefab, position, Quaternion.identity);

        // 2. On règle les munitions AVANT le spawn
        if (droppedObj.TryGetComponent<Weapon>(out var weapon))
        {
            weapon.ammunitionAccount = ammo;
        }

        // 3. ON SPAWN SUR LE RÉSEAU
        droppedObj.GetComponent<NetworkObject>().Spawn();
    }

    public void AddWeaponFromNetwork(string weaponDataName, int ammo)
    {
        // On cherche l'arme exacte par son NOM de ScriptableObject
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