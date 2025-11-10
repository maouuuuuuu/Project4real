// Assets/Scripts/Battle/ProjectileBehaviour.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ProjectileBehaviour : MonoBehaviour
{
    [Header("Runtime wiring")]
    public WeaponDefinition weapon;          // set at spawn
    public CharacterRuntime owner;           // set at spawn

    Rigidbody _rb;
    Collider _col;
    float _life;
    bool _spent;       // after first impact, no more damage
    bool _launched;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        // Make sure collision generates events:
        _col.isTrigger = false; // works with OnCollisionEnter; set true if you prefer triggers (see OnTriggerEnter)
    }

    void Start()
    {
        _life = weapon ? Mathf.Max(0f, weapon.projectileLifetime) : 2f;

        // Optional: auto-launch forward for ranged
        if (weapon && weapon.type == WeaponType.Ranged && weapon.projectileSpeed > 0f)
        {
            // give it some forward speed
            _rb.linearVelocity = transform.forward * weapon.projectileSpeed;
            _launched = true;
        }
    }

    void Update()
    {
        _life -= Time.deltaTime;
        if (_life <= 0f) Destroy(gameObject);
    }

    // ----- collisions -----

    void OnCollisionEnter(Collision c)
    {
        HandleHit(c.collider, c.GetContact(0).point, c.GetContact(0).normal);
    }

    void OnTriggerEnter(Collider other) // if you prefer triggers, switch collider to trigger and rely on this
    {
        // HandleHit(other, transform.position, transform.forward);
    }

    void HandleHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!weapon) { Destroy(gameObject); return; }

        // MAGIC: do magic thing later; destroy now
        if (weapon.type == WeaponType.Ranged && weapon.weaponClass == WeaponClass.Magic)
        {
            Debug.Log($"[Projectile] magic hit on '{other.name}' → do magic attack thing");
            Destroy(gameObject);
            return;
        }

        // PHYSICAL ranged (or melee projectiles if you use them)
        if (!_spent)
        {
            var target = other.GetComponentInParent<CharacterRuntime>();

            // Damage only if we struck a character on an opposing team
            if (weapon.type == WeaponType.Ranged && weapon.weaponClass == WeaponClass.Physical
                && target != null && owner != null && target.team != Team.None && owner.team != Team.None
                && target.team != owner.team && target.status != RuntimeStatus.Dead)
            {
                int dmg = Mathf.Max(0, owner.currentStats?.atk ?? 0);
                int before = target.currentHP;
                target.currentHP = Mathf.Max(0, target.currentHP - dmg);
                if (target.currentHP <= 0) target.status = RuntimeStatus.Dead;

                Debug.Log($"[Projectile] {owner.name} hit {target.name} for {dmg} dmg ({before}→{target.currentHP})");
            }

            // Go to "spent" state: flop on the ground until lifetime ends
            BecomeSpent();
        }
        // already spent → just bounce around until lifetime ends
    }

    void BecomeSpent()
    {
        _spent = true;

        // Make sure it can physically flop (no damage any more).
        // Leave collider solid so it bounces; if you want it to stop colliding with characters,
        // you could move it to a "SpentProjectile" layer instead.
        // Physics tuning: reduce drag so it tumbles naturally
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0.05f;
    }

    // ---- helper API for spawner/attacker to wire this up cleanly ----
    public void Init(CharacterRuntime ownerRt, WeaponDefinition wpn)
    {
        owner = ownerRt;
        weapon = wpn;
    }
}
