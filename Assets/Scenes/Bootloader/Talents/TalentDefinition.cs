using System.Collections.Generic;
using UnityEngine;

public enum TalentClass { Passive, Active, Behavior }

[CreateAssetMenu(menuName = "Game/Talent Definition", fileName = "NewTalent")]
public class TalentDefinition : ScriptableObject
{
    [Header("Identity")]
    public string Id = System.Guid.NewGuid().ToString();
    public string TalentName;
    [Min(1)] public int Level = 1;

    [Header("Stats Affected")]
    [Tooltip("List of stat names this talent affects (e.g. atk, def, int).")]
    public List<string> stats = new List<string>();

    [Header("Classification")]
    public TalentClass talentClass;
}
