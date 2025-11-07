using UnityEngine;

public enum WeaponType { Ranged, Melee }
public enum WeaponClass { Magic, Physical }

[System.Serializable]
public class WeaponStats
{
    public int atk, def, spd, dex, @int, mana, wis, foc;
}


[CreateAssetMenu(menuName = "Game/Weapon Definition", fileName = "NewWeapon")]
public class WeaponDefinition : ScriptableObject
{
    public string weaponName;
    public string Id = System.Guid.NewGuid().ToString();

    public WeaponType type;
    public WeaponClass weaponClass;

    [Header("Prefabs (optional)")]
    public GameObject attackPrefab;   // spawned when attacking
    public GameObject hitVfxPrefab;   // spawned on hit

    [Header("Behavior")]
    [Min(0f)] public float range = 5f;
    [Range(0f, 1f)] public float aim = 0f;              // aim assist 0..1
    [Min(0f)] public float projectileSpeed = 12f;
    [Min(0f)] public float projectileLifetime = 4f;

    [Header("Stats")]
    public WeaponStats stats = new WeaponStats();
}
