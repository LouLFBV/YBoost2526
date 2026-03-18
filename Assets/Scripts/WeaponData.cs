using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponID;
    public int damage = 10;
    public int ammunitionInStock = 30;
    public int range = 100;
    public Sprite icone;
    public WeaponType weaponType;
    public GameObject weaponPrefab;
}
