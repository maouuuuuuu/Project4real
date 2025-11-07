// Assets/Scripts/Battle/BattleSpawner.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleSpawner : MonoBehaviour
{
    [Header("Hierarchy")]
    public Transform spawnerRoot;   // assign the Spawner root
    public Transform teamAParent;   // Spawner/Team A
    public Transform teamBParent;   // Spawner/Team B

    [Header("Options")]
    public bool autoFindFromSpawnerRoot = true; // auto-wire Team A/B under spawnerRoot

    private List<Transform> _teamASlots = new();
    private List<Transform> _teamBSlots = new();

    void Awake()
    {
        if (autoFindFromSpawnerRoot && spawnerRoot)
        {
            teamAParent = FindChildByName(spawnerRoot, "Team A") ?? teamAParent;
            teamBParent = FindChildByName(spawnerRoot, "Team B") ?? teamBParent;
        }

        _teamASlots = CollectOrderedSlots(teamAParent);
        _teamBSlots = CollectOrderedSlots(teamBParent);
    }

    void Start()
    {
        var boot = Bootstrapper.Instance;
        if (!boot)
        {
            Debug.LogError("[BattleSpawner] No Bootstrapper found.");
            return;
        }

        SpawnTeam(boot.teamA, _teamASlots, "A");
        SpawnTeam(boot.teamB, _teamBSlots, "B");
    }

    // ---- helpers ----

    static Transform FindChildByName(Transform root, string name)
    {
        if (!root) return null;
        foreach (Transform c in root)
            if (c.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return c;
        return null;
    }

    static List<Transform> CollectOrderedSlots(Transform parent)
    {
        var result = new List<Transform>();
        if (!parent) return result;

        foreach (Transform c in parent)
            result.Add(c);

        // sort by numeric name if possible (1..6), otherwise by name
        result = result
            .OrderBy(t =>
            {
                if (int.TryParse(t.name.Trim(), out var n)) return n;
                return int.MaxValue; // non-numeric go last
            })
            .ThenBy(t => t.name, StringComparer.Ordinal)
            .ToList();

        return result;
    }

    void SpawnTeam(List<CharacterDefinition> team, List<Transform> slots, string label)
    {
        if (team == null) return;

        int count = Mathf.Min(team.Count, slots.Count);
        for (int i = 0; i < count; i++)
        {
            var character = team[i];
            var slot = slots[i];

            if (!character)
            {
                Debug.LogWarning($"[BattleSpawner] Null character at index {i} (Team {label}).");
                continue;
            }
            if (!character.modelPrefab)
            {
                Debug.LogWarning($"[BattleSpawner] {character.CharacterName} has no modelPrefab.");
                continue;
            }
            if (!slot)
            {
                Debug.LogWarning($"[BattleSpawner] Missing slot #{i + 1} for Team {label}.");
                continue;
            }

            // clear previous occupants in slot (if any)
            for (int c = slot.childCount - 1; c >= 0; c--)
                Destroy(slot.GetChild(c).gameObject);

            var go = Instantiate(character.modelPrefab, slot.position, slot.rotation, slot);
            go.name = $"{character.CharacterName}_Team{label}_{i + 1}";
        }

        if (team.Count > slots.Count)
            Debug.LogWarning($"[BattleSpawner] Team {label} has {team.Count} members but only {slots.Count} slots. Extra members were not spawned.");
    }
}
