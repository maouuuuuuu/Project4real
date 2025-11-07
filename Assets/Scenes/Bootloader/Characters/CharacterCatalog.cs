using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Catalog", fileName = "CharacterCatalog")]
public class CharacterCatalog : ScriptableObject
{
    [SerializeField] private List<CharacterDefinition> characters = new();
    private Dictionary<string, CharacterDefinition> _byId;

    private void OnEnable()
    {
        _byId = new Dictionary<string, CharacterDefinition>();
        foreach (var c in characters)
        {
            if (c == null || string.IsNullOrEmpty(c.Id)) continue;
            _byId[c.Id] = c;
        }
    }

    public bool TryGetById(string id, out CharacterDefinition def)
    {
        def = null;
        return _byId != null && _byId.TryGetValue(id, out def);
    }

    public CharacterDefinition GetByName(string name) =>
        characters.Find(c => c && c.CharacterName == name);

    public IReadOnlyList<CharacterDefinition> All => characters;
}
