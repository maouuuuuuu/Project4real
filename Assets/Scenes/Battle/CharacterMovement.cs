// Assets/Scripts/Battle/CharacterMovement.cs
using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterRuntime))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Base acceleration applied as force each FixedUpdate (scaled by SPD).")]
    public float baseAcceleration = 20f;
    [Tooltip("If true, we’ll face the movement direction.")]
    public bool rotateTowardVelocity = true;
    [Tooltip("Clamp max speed (m/s). 0 = no clamp.")]
    public float maxSpeed = 8f;

    [Header("Attack")]
    public Transform projectileSpawn; // optional; if null we use a default offset
    [SerializeField] Transform initPoint;

    [Header("Targeting")]
    [Tooltip("How often to refresh nearest enemy (seconds).")]
    public float retargetInterval = 0.25f;

    [Header("Orientation")]
    public bool keepUprightWhenActive = true;
    [Tooltip("How fast to align upright/face target.")]
    public float uprightTurnSpeed = 12f; // higher = snappier

    Rigidbody _rb;
    CharacterRuntime _rt;
    CharacterRuntime _target;
    float _cooldownTimer;
    float _retargetTimer;
    float _castingTimer;


    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rt = GetComponent<CharacterRuntime>();

        // Let state machine decide constraints per-frame
        _rb.constraints = RigidbodyConstraints.None;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (!initPoint)
        {
            var w = _rt.sourceWeapon;  // ✅ use the weapon from CharacterRuntime
            string wanted = (w && w.type == WeaponType.Melee) ? "Minit" : "Rinit";

            // case-insensitive, deep search
            initPoint = FindChildIgnoreCase(transform, wanted);

            // optional: last-resort fallback so you still fire from somewhere
            if (!initPoint) initPoint = transform;
        }
    }

    // --- Update(): handle both Cooldown and Casting timers ---
    void Update()
    {
        if (_rt.status == RuntimeStatus.Cooldown)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
                _rt.status = RuntimeStatus.Active;
        }
        else if (_rt.status == RuntimeStatus.Casting)
        {
            _castingTimer -= Time.deltaTime;
            if (_castingTimer <= 0f)
            {
                DoAttack();
            }
        }
    }

    void FixedUpdate()
    {
        switch (_rt.status)
        {
            case RuntimeStatus.Active:
                TickActive();
                break;
                // Casting/Cooldown/Inactive: no movement force, but we still manage orientation below
        }

        MaintainUprightAndFacing();
    }

    static Transform FindChildIgnoreCase(Transform root, string childName)
    {
        foreach (Transform c in root)
        {
            if (string.Equals(c.name, childName, StringComparison.OrdinalIgnoreCase))
                return c;

            var found = FindChildIgnoreCase(c, childName);
            if (found) return found;
        }
        return null;
    }
    void MaintainUprightAndFacing()
    {
        // Ragdolling (and Dead): free rotations, no upright enforcement
        if (_rt.status == RuntimeStatus.Ragdolling || _rt.status == RuntimeStatus.Dead)
        {
            if (_rb.constraints != RigidbodyConstraints.None)
                _rb.constraints = RigidbodyConstraints.None;
            return;
        }

        if (!keepUprightWhenActive) return;

        // Active/Casting/Cooldown/Inactive: freeze X/Z rotation, allow yaw
        var wanted = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (_rb.constraints != wanted)
            _rb.constraints = wanted;

        // compute desired forward: toward target if we have one; else keep current forward or velocity dir
        Vector3 desiredFwd;
        if (_rt.currentTarget)
            desiredFwd = (_rt.currentTarget.transform.position - transform.position);
        else if (_rb.linearVelocity.sqrMagnitude > 0.01f)
            desiredFwd = _rb.linearVelocity;
        else
            desiredFwd = transform.forward;

        // project to horizontal (no tilt), normalize
        desiredFwd = Vector3.ProjectOnPlane(desiredFwd, Vector3.up);
        if (desiredFwd.sqrMagnitude < 1e-6f) desiredFwd = transform.forward;
        desiredFwd.Normalize();

        // upright, zero roll/pitch: up = world up, forward = desiredFwd
        var targetRot = Quaternion.LookRotation(desiredFwd, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, uprightTurnSpeed * Time.fixedDeltaTime);
    }

    void TickActive()
    {
        // find/refresh target
        _retargetTimer -= Time.fixedDeltaTime;
        if (_retargetTimer <= 0f || _target == null || _target.status == RuntimeStatus.Dead || _target.team == _rt.team)
        {
            _target = FindNearestEnemy();
            _rt.currentTarget = _target; // debug visibility
            _retargetTimer = retargetInterval;
        }
        if (_target == null) return;

        // compute distances
        Vector3 toTarget = _target.transform.position - transform.position;
        float dist = toTarget.magnitude;

        // attack range from weapon; fallback if missing
        float attackRange = _rt.sourceWeapon ? Mathf.Max(0.01f, _rt.sourceWeapon.range) : 1.5f;

        // reached attack range?
        if (dist <= attackRange)
        {
            BeginCasting();
            return;
        }

        // move toward target using physics “push”
        Vector3 dir = (dist > 0.0001f) ? (toTarget / dist) : Vector3.zero;

        float factor = SpeedFactor();

        // Acceleration scales with SPD factor (1x..3x)
        float accel = baseAcceleration * factor;
        _rb.AddForce(dir * accel, ForceMode.Acceleration);

        // Clamp horizontal speed with the same factor
        if (maxSpeed > 0f)
        {
            Vector3 v = _rb.linearVelocity;   // or .velocity, both fine
            Vector3 horiz = new Vector3(v.x, 0f, v.z);
            float cap = maxSpeed * factor;

            if (horiz.sqrMagnitude > cap * cap)
            {
                horiz = horiz.normalized * cap;
                _rb.linearVelocity = new Vector3(horiz.x, v.y, horiz.z);
            }
        }

        if (rotateTowardVelocity)
        {
            Vector3 v = _rb.linearVelocity;
            v.y = 0f;
            if (v.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(v), 0.2f);
        }
    }
    // --- NEW: begin casting window; target leaving range doesn't cancel ---
    void BeginCasting()
    {
        // grab casting time (defaults to 0 if no weapon)
        float cast = _rt.sourceWeapon ? Mathf.Max(0f, _rt.sourceWeapon.castingTime) : 0f;

        if (cast <= 0f)
        {
            // instant cast -> attack immediately
            DoAttack();
            return;
        }

        _castingTimer = cast;
        _rt.status = RuntimeStatus.Casting;

        // optional: stop horizontal drift while casting
        _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);

        // (debug)
        // Debug.Log($"[{name}] Begin CASTING ({cast:0.###}s) on {_target?.name ?? "(null)"}");
    }

    // --- DoAttack(): unchanged logic except it now follows casting ---
    void DoAttack()
    {
        if (_rt.sourceWeapon && _rt.sourceWeapon.attackPrefab)
        {
            Vector3 pos = initPoint
                ? initPoint.position
                : transform.position + Vector3.up * 1.0f + transform.forward * 0.5f;

            Quaternion rot = (_target
                ? Quaternion.LookRotation((_target.transform.position - pos).normalized, Vector3.up)
                : transform.rotation);

            var projGO = Instantiate(_rt.sourceWeapon.attackPrefab, pos, rot);

            // (optional) initialize projectile
            var pb = projGO.GetComponent<ProjectileBehaviour>();
            if (pb) pb.Init(_rt, _rt.sourceWeapon);
        }

        // enter cooldown AFTER attacking
        float cd = _rt.sourceWeapon ? Mathf.Max(0f, _rt.sourceWeapon.attackCooldown) : 1f;
        _cooldownTimer = cd;
        _rt.status = RuntimeStatus.Cooldown;

        // optional: zero horizontal on attack
        _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);

        Debug.Log($"[{name}] ATTACKED {_target?.name ?? "(null)"} — projectile spawned; cooldown {cd:0.###}s.");
    }

    CharacterRuntime FindNearestEnemy()
    {
        var all = FindObjectsOfType<CharacterRuntime>(includeInactive: false);
        CharacterRuntime best = null;
        float bestD2 = float.PositiveInfinity;

        foreach (var other in all)
        {
            if (!other || other == _rt) continue;
            if (other.status == RuntimeStatus.Dead) continue;
            if (other.team == Team.None || other.team == _rt.team) continue;

            float d2 = (other.transform.position - transform.position).sqrMagnitude;
            if (d2 < bestD2)
            {
                bestD2 = d2;
                best = other;
            }
        }
        return best;
    }
    float SpeedFactor()
    {
        // 0 SPD → 1.0x, 1000 SPD → 3.0x (clamped)
        float spd = Mathf.Max(0, _rt.currentStats?.spd ?? 0);
        return 1f + 2f * (spd / 1000f);
    }

}
