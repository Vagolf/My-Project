using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class EvaAI : MonoBehaviour
{
    [Header("Target Lock")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("Movement Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float detectRange = 15f;

    [Tooltip("เข้าใกล้ถึงระยะนี้จะหยุดแล้วโจมตี")]
    [SerializeField] private float attackStopDistance = 5.5f;

    [SerializeField] private LayerMask groundLayer;

    [Header("Attack - Normal ยิงลมตัดสีฟ้า")]
    [SerializeField] private GameObject normalProjectilePrefab;
    [SerializeField] private Transform firePointNormal;
    [SerializeField] private float normalDamage = 75f;
    [SerializeField] private float normalProjectileSpeed = 18f;
    [SerializeField] private float normalFireInterval = 0.25f; // กันยิงรัว 60fps

    [Header("Attack - Crouch ยิงสตั๊น + ผลักถอย")]
    [SerializeField] private GameObject stunProjectilePrefab;
    [SerializeField] private Transform firePointStun;
    [SerializeField] private float stunDamage = 150f;
    [SerializeField] private float stunProjectileSpeed = 14f;
    [SerializeField] private float stunCooldown = 4f;
    [SerializeField] private float stunKnockbackForce = 10f;
    [SerializeField] private float stunDuration = 0.8f;

    [Header("Ultimate - พายุ 6 ครั้ง")]
    [SerializeField] private GameObject tornadoPrefab;
    [SerializeField] private Transform ultimatePoint;
    [SerializeField] private float ultimateCooldown = 10f;
    [SerializeField] private float ultimateHitDamage = 50f;
    [SerializeField] private int ultimateHits = 6;
    [SerializeField] private float ultimateTickInterval = 0.25f;
    [SerializeField] private float ultimateRadius = 2.2f;

    [Header("Opponent Layer")]
    [SerializeField] private LayerMask Opponent; // ใส่ Layer Player

    [Header("Animator Params")]
    [SerializeField] private string runBool = "run";
    [SerializeField] private string crouchBool = "crouch";
    [SerializeField] private string atkTrigger = "atk";
    [SerializeField] private string ultiTrigger = "ulti";

    [Header("Hurtbox (for receiving damage)")]
    [SerializeField] private bool addHurtTrigger = true;
    [SerializeField] private float hurtRadius = 0.6f;
    [SerializeField] private Vector2 hurtOffset = Vector2.zero;

    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private Animator anim;

    private bool ground;
    private CircleCollider2D hurtTrigger;

    private float nextNormalTime = 0f;
    private float nextStunReadyTime = 0f;
    private float nextUltiReadyTime = 0f;

    private bool actionLocked = false;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        FindTargetIfNeeded();

        // auto hurt trigger so player attacks can detect Eva by overlap
        if (addHurtTrigger)
        {
            hurtTrigger = GetComponent<CircleCollider2D>();
            if (hurtTrigger == null) hurtTrigger = gameObject.AddComponent<CircleCollider2D>();
            hurtTrigger.isTrigger = true;
            hurtTrigger.radius = Mathf.Max(0.05f, hurtRadius);
            hurtTrigger.offset = hurtOffset;
        }

        // กันอัลติใช้ทันทีตอนเริ่มเกม
        nextUltiReadyTime = Time.time + 1f;
    }

    private void Update()
    {
        // ถ้ามี Timer.GateBlocked ในเกมคุณ
        if (Timer.GateBlocked)
        {
            StopMove();
            SetRun(false);
            return;
        }

        if (!FindTargetIfNeeded()) return;

        IsGrounded();

        float dist = Vector2.Distance(transform.position, target.position);
        if (dist > detectRange)
        {
            StopMove();
            SetRun(false);
            UpdateAnim();
            return;
        }

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        Flip(dir);

        // ถ้ากำลังทำท่าพิเศษ → ไม่ให้เดิน
        if (actionLocked)
        {
            StopMove();
            SetRun(false);
            UpdateAnim();
            return;
        }

        // ✅ ถ้าเข้าใกล้พอแล้ว → หยุดและโจมตี
        if (dist <= attackStopDistance)
        {
            StopMove();
            SetRun(false);

            // Priority: Ultimate > Stun > Normal
            if (Time.time >= nextUltiReadyTime)
            {
                StartCoroutine(UltimateRoutine());
                UpdateAnim();
                return;
            }

            // Stun ยิงเมื่อคูลดาวน์พร้อม
            if (Time.time >= nextStunReadyTime)
            {
                StartCoroutine(StunShotRoutine());
                UpdateAnim();
                return;
            }

            // ยิงธรรมดาเรื่อย ๆ
            TryNormalShot(dir);

            UpdateAnim();
            return;
        }

        // ✅ ยังไม่ถึงระยะโจมตี → วิ่งไล่ตาม
        body.velocity = new Vector2(dir * speed, body.velocity.y);
        SetRun(true);
        UpdateAnim();
    }

    // =========================
    // Attack Skills
    // =========================
    private void TryNormalShot(float dir)
    {
        if (normalProjectilePrefab == null) return;
        if (Time.time < nextNormalTime) return;

        nextNormalTime = Time.time + Mathf.Max(0.05f, normalFireInterval);

        // animation trigger (ถ้ามี)
        TriggerIfExists(atkTrigger);

        Vector3 spawn = firePointNormal != null ? firePointNormal.position : transform.position;
        GameObject go = Instantiate(normalProjectilePrefab, spawn, Quaternion.identity);

        var proj = go.GetComponent<EvaProjectile>();
        if (proj != null)
        {
            proj.Init(new Vector2(dir, 0f), normalProjectileSpeed, normalDamage, Opponent, 0f, 0f);
        }
    }

    private IEnumerator StunShotRoutine()
    {
        if (stunProjectilePrefab == null) yield break;

        actionLocked = true;
        nextStunReadyTime = Time.time + stunCooldown;

        // crouch anim
        SetCrouch(true);
        TriggerIfExists(atkTrigger);

        yield return new WaitForSecondsRealtime(0.15f);

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        Vector3 spawn = firePointStun != null ? firePointStun.position : transform.position;

        GameObject go = Instantiate(stunProjectilePrefab, spawn, Quaternion.identity);
        var proj = go.GetComponent<EvaProjectile>();
        if (proj != null)
        {
            proj.Init(new Vector2(dir, 0f), stunProjectileSpeed, stunDamage, Opponent, stunKnockbackForce, stunDuration);
        }

        yield return new WaitForSecondsRealtime(0.25f);
        SetCrouch(false);
        actionLocked = false;
    }

    private IEnumerator UltimateRoutine()
    {
        if (tornadoPrefab == null) yield break;

        actionLocked = true;
        nextUltiReadyTime = Time.time + ultimateCooldown;

        TriggerIfExists(ultiTrigger);

        yield return new WaitForSecondsRealtime(0.25f);

        Vector3 spawn = ultimatePoint != null ? ultimatePoint.position : transform.position;
        GameObject go = Instantiate(tornadoPrefab, spawn, Quaternion.identity);

        var tornado = go.GetComponent<EvaTornadoUltimate>();
        if (tornado != null)
        {
            tornado.Init(target, Opponent, ultimateHitDamage, ultimateHits, ultimateTickInterval, ultimateRadius);
        }

        yield return new WaitForSecondsRealtime(0.6f);
        actionLocked = false;
    }

    // =========================
    // Helpers
    // =========================
    private void StopMove()
    {
        body.velocity = new Vector2(0f, body.velocity.y);
    }

    private void Flip(float dir)
    {
        if (dir > 0.01f) transform.localScale = new Vector3(1, 1, 1);
        else if (dir < -0.01f) transform.localScale = new Vector3(-1, 1, 1);
    }

    private void UpdateAnim()
    {
        if (anim == null) return;
        anim.SetBool("ground", ground);
        anim.SetFloat("yVelocity", body.velocity.y);
    }

    private void SetRun(bool r)
    {
        if (anim == null) return;
        if (HasAnimatorParameter("run", AnimatorControllerParameterType.Bool)) anim.SetBool("run", r);
        if (HasAnimatorParameter("Run", AnimatorControllerParameterType.Bool)) anim.SetBool("Run", r);
    }

    private void SetCrouch(bool c)
    {
        if (anim == null) return;
        if (HasAnimatorParameter("crouch", AnimatorControllerParameterType.Bool)) anim.SetBool("crouch", c);
    }

    private void TriggerIfExists(string trig)
    {
        if (anim == null || string.IsNullOrEmpty(trig)) return;
        if (HasAnimatorParameter(trig, AnimatorControllerParameterType.Trigger))
            anim.SetTrigger(trig);
    }

    private bool HasAnimatorParameter(string name, AnimatorControllerParameterType type)
    {
        if (anim == null) return false;
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }

    private bool IsGrounded()
    {
        if (boxCollider == null) return ground;
        ground = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        return ground;
    }

    private bool FindTargetIfNeeded()
    {
        if (target != null) return true;
        var t = GameObject.FindGameObjectWithTag(targetTag);
        if (t != null) { target = t.transform; return true; }
        return false;
    }
}
