using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Definition", fileName = "NewCharacter")]
public class CharacterDefinition : ScriptableObject
{
    [Header("Identity")]
    public string Id = System.Guid.NewGuid().ToString();
    public string CharacterName;

    [Header("Model")]
    public GameObject modelPrefab;

    [Header("Loadout (direct refs)")]
    public WeaponDefinition weapon;                 // direct reference
    public List<TalentDefinition> talents = new();  // direct references
}
