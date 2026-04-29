using System.Collections.Generic;
using UnityEngine;

public class WeaponDataBase : MonoBehaviour
{
    public static WeaponDataBase Instance;

    public List<WeaponData> weapons;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.Log("Destruction Singleton WeaponDataBase");
            Destroy(gameObject);
        }
    }
    public WeaponData GetItemByID(string id)
    {
        return weapons.Find(item => item.weaponID == id);
    }
}