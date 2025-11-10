using System;
using System.Collections.Generic;
using UnityEngine;

public enum RuntimeStatus { Active, Inactive, Casting, Ragdolling, Dead, Cooldown }

public class CharacterRuntime : MonoBehaviour
{
    [Header("Sources (immutable assets)")]
    public CharacterDefinition sourceCharacter;
    public WeaponDefinition sourceWeapon;

    // In CharacterRuntime
    [Header("Talents (view-only)")]
    [SerializeField] private List<TalentDefinition> _talentsView = new();  // shows in Inspector
    public IReadOnlyList<TalentDefinition> Talents => _talentsView;        // your read-only API

    [Header("Identity & Links")]
    public Team team = Team.None;
    public CharacterRuntime currentTarget;

    [Header("Mutable State")]
    public RuntimeStatus status = RuntimeStatus.Inactive;

    [Tooltip("Snapshot copied from CharacterDefinition at spawn, do not mutate.")]
    public Stats baseStats;
    public Stats currentStats;
    public int currentHP;

    public void Init(CharacterDefinition def, Team teamAssignment)
    {
        sourceCharacter = def;
        sourceWeapon = def ? def.weapon : null;
        team = teamAssignment;

        baseStats = Clone(def ? def.Characterstats : new Stats());
        currentStats = Clone(baseStats);

        // --- 1) TALENTS FIRST (Passive): flat + % based on table ---
        if (def && def.talents != null)
            ApplyPassiveTalentBonuses(def.talents, currentStats, baseStats);

        // --- 2) WEAPON SECOND (flat add; comment out if not desired) ---
        if (sourceWeapon)
            AddInto(currentStats, sourceWeapon.Weaponstats);

        currentHP = currentStats.hp;
        status = RuntimeStatus.Inactive;

        _talentsView.Clear();
        if (def && def.talents != null) _talentsView.AddRange(def.talents);
    }
    public void ApplyDelta(Stats delta)
    {
        if (delta == null) return;
        AddInto(currentStats, delta);
        currentHP = Mathf.Clamp(currentHP, 0, Mathf.Max(0, currentStats.hp));
        if (currentHP <= 0) status = RuntimeStatus.Dead;
    }

    public void SetStatus(RuntimeStatus s) => status = s;

    // ----------------- TALENT LOGIC -----------------

    // Level table: flat / percent (as fraction)
    // lvl: 1   2   3   4   5   6   7   8    9    10
    // flat:10,15, 20, 25, 30, 35, 40, 45,  50,  55
    //  %: .05,.07,.10,.12,.15,.17,.20,.24,.26, .30
    static readonly int[] TL_FLAT = { 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };
    static readonly float[] TL_RATE = { .05f, .07f, .10f, .12f, .15f, .17f, .20f, .24f, .26f, .30f };

    void ApplyPassiveTalentBonuses(List<TalentDefinition> talents, Stats target, Stats baseRef)
    {
        // aggregate per-stat totals so % applies once on (base + totalFlat)
        var flat = new StatAcc();    // sums of flat bonuses
        var rate = new StatAccF();   // sums of percentage rates (additive)

        foreach (var t in talents)
        {
            if (!t || t.talentClass != TalentClass.Passive) continue;

            int lvlIdx = Mathf.Clamp((t.Level <= 0 ? 1 : t.Level) - 1, 0, 9);
            int f = TL_FLAT[lvlIdx];
            float r = TL_RATE[lvlIdx];

            if (t.stats == null || t.stats.Count == 0) continue;

            foreach (var name in t.stats)
            {
                var key = (name ?? "").Trim().ToLowerInvariant();
                flat.Add(key, f);
                rate.Add(key, r);
            }
        }

        // apply to each stat key that has any bonus
        foreach (var key in flat.KeysUnion(rate))
        {
            int baseVal = GetStat(baseRef, key);
            int flatVal = flat.Get(key);
            float pct = rate.Get(key); // e.g., 0.05 means +5%

            int afterFlat = baseVal + flatVal;
            int percentAdd = Mathf.RoundToInt(afterFlat * pct);
            int total = afterFlat + percentAdd;

            SetStat(target, key, total);
        }
    }

    // ----------------- STAT HELPERS -----------------

    static Stats Clone(Stats s) => new Stats
    {
        hp = s.hp,
        atk = s.atk,
        def = s.def,
        spd = s.spd,
        dex = s.dex,
        @int = s.@int,
        mana = s.mana,
        wis = s.wis,
        foc = s.foc
    };

    static void AddInto(Stats dst, Stats add)
    {
        if (dst == null || add == null) return;
        dst.hp += add.hp; dst.atk += add.atk; dst.def += add.def; dst.spd += add.spd;
        dst.dex += add.dex; dst.@int += add.@int; dst.mana += add.mana; dst.wis += add.wis; dst.foc += add.foc;
    }

    static int GetStat(Stats s, string key)
    {
        switch (key)
        {
            case "hp": return s.hp;
            case "atk": return s.atk;
            case "def": return s.def;
            case "spd": return s.spd;
            case "dex": return s.dex;
            case "int": return s.@int;
            case "mana": return s.mana;
            case "wis": return s.wis;
            case "foc": return s.foc;
            default: return 0;
        }
    }

    static void SetStat(Stats s, string key, int value)
    {
        switch (key)
        {
            case "hp": s.hp = value; break;
            case "atk": s.atk = value; break;
            case "def": s.def = value; break;
            case "spd": s.spd = value; break;
            case "dex": s.dex = value; break;
            case "int": s.@int = value; break;
            case "mana": s.mana = value; break;
            case "wis": s.wis = value; break;
            case "foc": s.foc = value; break;
        }
    }

    // small accumulator structs
    struct StatAcc
    {
        Dictionary<string, int> d;
        public void Add(string k, int v) { if (d == null) d = new(); d[k] = Get(k) + v; }
        public int Get(string k) => d != null && d.TryGetValue(k, out var v) ? v : 0;
        public IEnumerable<string> KeysUnion(StatAccF other)
        {
            var seen = new HashSet<string>();
            if (d != null) foreach (var k in d.Keys) seen.Add(k);
            if (other.d != null) foreach (var k in other.d.Keys) seen.Add(k);
            return seen;
        }
    }
    struct StatAccF
    {
        public Dictionary<string, float> d;
        public void Add(string k, float v) { if (d == null) d = new(); d[k] = Get(k) + v; }
        public float Get(string k) => d != null && d.TryGetValue(k, out var v) ? v : 0f;
    }
}
