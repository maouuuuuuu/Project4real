using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon Catalog", fileName = "WeaponCatalog")]
public class WeaponCatalog : ScriptableObject
{
    [SerializeField] private List<WeaponDefinition> weapons = new List<WeaponDefinition>();
    private Dictionary<string, WeaponDefinition> _byId;

    private void OnEnable()
    {
        _byId = new Dictionary<string, WeaponDefinition>();
        foreach (var w in weapons)
        {
            if (w == null || string.IsNullOrEmpty(w.Id)) continue;
            _byId[w.Id] = w;
        }
    }

    public bool TryGetById(string id, out WeaponDefinition def)
    {
        def = null;
        return _byId != null && _byId.TryGetValue(id, out def);
    }
}
