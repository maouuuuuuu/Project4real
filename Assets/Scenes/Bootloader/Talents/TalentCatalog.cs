using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Talent Catalog", fileName = "TalentCatalog")]
public class TalentCatalog : ScriptableObject
{
    [SerializeField] private List<TalentDefinition> talents = new List<TalentDefinition>();
    private Dictionary<string, TalentDefinition> _byId;

    private void OnEnable()
    {
        _byId = new Dictionary<string, TalentDefinition>();
        foreach (var t in talents)
        {
            if (t == null || string.IsNullOrEmpty(t.Id)) continue;
            _byId[t.Id] = t;
        }
    }

    public bool TryGetById(string id, out TalentDefinition def)
    {
        def = null;
        return _byId != null && _byId.TryGetValue(id, out def);
    }

    public TalentDefinition GetByName(string name)
    {
        return talents.Find(t => t && t.TalentName == name);
    }

    public IReadOnlyList<TalentDefinition> All => talents;
}
