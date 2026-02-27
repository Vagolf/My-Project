using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HealthEnemy))]
public class DusanAI : MonoBehaviour
{
    [Header("Target Lock")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("Movement Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float detectRange = 15f;
    [SerializeField] private float attackStopDistance = 5.5f;
    [SerializeField] private float minDistance = 2.5f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Dash Settings (หนีเมื่อหลังชนกำแพง)")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private float checkWallDistance = 1.5f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Skill 1: Normal Attack (ยิงปกติ)")]
    [SerializeField] private GameObject normalProjectilePrefab;
    [SerializeField] private Transform firePointNormal;
    [SerializeField] private float normalDamage = 75f;
    [SerializeField] private float normalProjectileSpeed = 18f;
    [SerializeField] private float normalCooldown = 2f;

    // ==========================================
    // 🔥 เพิ่มส่วนนี้: ตั้งค่าสกิลเสกกระสุนตกจากฟ้า
    // ==========================================
    [Header("Skill 2: Air Strike (ยิงจากฟ้าลงหัว)")]
    [SerializeField] private GameObject airStrikePrefab;
    [Tooltip("ความสูงจากหัว Player ที่กระสุนจะเสกออกมา")]
    [SerializeField] private float airStrikeHeight = 12f;
    [SerializeField] private float airStrikeDamage = 100f;
    [SerializeField] private float airStrikeSpeed = 15f;
    [SerializeField] private float airStrikeCooldown = 6f; // คูลดาวน์สกิลนี้

    [Header("General Attack Params")]
    [SerializeField] private LayerMask Opponent;
    [SerializeField] private string atkTrigger = "atk"; // ใช้แอนิเมชันโจมตีเดียวกันได้

    [Header("Hurtbox (for receiving damage)")]
    [SerializeField] private bool addHurtTrigger = true;
    [SerializeField] private float hurtRadius = 0.6f;
    [SerializeField] private Vector2 hurtOffset = Vector2.zero;

    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private Animator anim;

    private bool ground;
    private CircleCollider2D hurtTrigger;

    private float nextNormalTime = 0.5f;
    private float nextAirStrikeTime = 3f; // เริ่มเกมมาให้รอ 3 วิ ค่อยใช้ท่าฟ้านี้
    private float nextDashTime = 0f;
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

        if (addHurtTrigger)
        {
            hurtTrigger = GetComponent<CircleCollider2D>();
            if (hurtTrigger == null) hurtTrigger = gameObject.AddComponent<CircleCollider2D>();
            hurtTrigger.isTrigger = true;
            hurtTrigger.radius = Mathf.Max(0.05f, hurtRadius);
            hurtTrigger.offset = hurtOffset;
        }
    }

    private void Update()
    {
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

        if (actionLocked)
        {
            UpdateAnim();
            return;
        }

        // ==========================================
        // 🔥 Priority 1: เช็คสกิลตกจากฟ้า (Air Strike)
        // (สกิลนี้โกงตรงที่ไม่ต้องรอเข้าใกล้ ขอแค่อยู่ในระยะสายตาก็ใช้ได้เลย)
        // ==========================================
        if (Time.time >= nextAirStrikeTime)
        {
            StopMove();
            SetRun(false);
            StartCoroutine(AirStrikeRoutine());
            UpdateAnim();
            return;
        }

        // Priority 2: สกิลยิงปกติ
        if (dist <= attackStopDistance && Time.time >= nextNormalTime)
        {
            StopMove();
            SetRun(false);
            StartCoroutine(NormalShotRoutine());
            UpdateAnim();
            return;
        }

        // ระบบเดินหน้า - ถอยหลัง - Dash หนี
        if (dist > minDistance + 0.1f)
        {
            body.velocity = new Vector2(dir * speed, body.velocity.y);
            SetRun(true);
        }
        else if (dist < minDistance - 0.1f)
        {
            if (IsWallBehind(dir) && Time.time >= nextDashTime)
            {
                StartCoroutine(DashForwardRoutine(dir));
                return;
            }
            else
            {
                body.velocity = new Vector2(-dir * speed, body.velocity.y);
                SetRun(true);
            }
        }
        else
        {
            StopMove();
            SetRun(false);
        }

        UpdateAnim();
    }

    private bool IsWallBehind(float currentDir)
    {
        if (boxCollider == null) return false;
        Vector2 checkDirection = new Vector2(-currentDir, 0);
        RaycastHit2D hit = Physics2D.Raycast(boxCollider.bounds.center, checkDirection, checkWallDistance, wallLayer);
        return hit.collider != null;
    }

    private IEnumerator DashForwardRoutine(float dir)
    {
        actionLocked = true;
        nextDashTime = Time.time + dashCooldown;
        body.velocity = new Vector2(dir * dashSpeed, body.velocity.y);
        yield return new WaitForSeconds(dashDuration);
        StopMove();
        actionLocked = false;
    }

    // ==========================================
    // 🔥 ท่าโจมตี 2: เสกตกจากฟ้า (Air Strike)
    // ==========================================
    private IEnumerator AirStrikeRoutine()
    {
        actionLocked = true;
        nextAirStrikeTime = Time.time + airStrikeCooldown; // รีเซ็ตคูลดาวน์

        SetRun(false);
        if (anim != null && !string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, true);

        // จังหวะที่บอสชูมือขึ้นฟ้า (ปรับเวลาให้ตรงกับแอนิเมชัน)
        yield return new WaitForSecondsRealtime(0.2f);

        if (airStrikePrefab != null && target != null)
        {
            // 📍 คำนวณจุดเกิด: เอาแกน X ของ Player ปัจจุบัน และแกน Y สูงขึ้นไปบนฟ้า
            Vector3 dropPosition = new Vector3(target.position.x, target.position.y + airStrikeHeight, transform.position.z);

            // สร้างกระสุนบนฟ้า
            GameObject go = Instantiate(airStrikePrefab, dropPosition, Quaternion.identity);

            ProjectileBehaviour proj = go.GetComponent<ProjectileBehaviour>();
            if (proj != null)
            {
                // สั่งให้พุ่งลงมาตรงๆ (Vector2.down คือ (0, -1) พุ่งลงพื้น)
                proj.Init(Vector2.down, airStrikeSpeed, airStrikeDamage, Opponent, 0f, 0f);
            }
        }

        yield return new WaitForSecondsRealtime(0.3f);
        if (anim != null && !string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, false);

        actionLocked = false;
    }

    private IEnumerator NormalShotRoutine()
    {
        actionLocked = true;
        nextNormalTime = Time.time + normalCooldown;

        SetRun(false);
        if (anim != null && !string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, true);

        yield return new WaitForSecondsRealtime(0.15f);

        if (normalProjectilePrefab != null && firePointNormal != null)
        {
            float direction = target != null ? Mathf.Sign(target.position.x - transform.position.x) : Mathf.Sign(transform.localScale.x);
            GameObject go = Instantiate(normalProjectilePrefab, firePointNormal.position, Quaternion.identity);

            ProjectileBehaviour proj = go.GetComponent<ProjectileBehaviour>();
            if (proj != null)
            {
                proj.Init(new Vector2(direction, 0f), normalProjectileSpeed, normalDamage, Opponent, 0f, 0f);
            }
        }

        yield return new WaitForSecondsRealtime(0.35f);
        if (anim != null && !string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, false);

        actionLocked = false;
    }

    public void ResetCooldowns()
    {
        StopAllCoroutines();
        actionLocked = false;
        nextNormalTime = Time.time + normalCooldown;
        nextAirStrikeTime = Time.time + airStrikeCooldown; // รีเซ็ตคูลดาวน์ฟ้าด้วย
        nextDashTime = 0f;
        if (anim != null)
        {
            if (!string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, false);
            SetRun(false);
        }
    }

    private void StopMove() { body.velocity = new Vector2(0f, body.velocity.y); }
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
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackStopDistance);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, minDistance);
        if (firePointNormal != null) { Gizmos.color = Color.green; Gizmos.DrawSphere(firePointNormal.position, 0.15f); }
        if (target != null) { Gizmos.color = Color.white; Gizmos.DrawLine(transform.position, target.position); }
        if (boxCollider != null)
        {
            Gizmos.color = Color.magenta;
            float dirGizmo = target != null ? Mathf.Sign(target.position.x - transform.position.x) : Mathf.Sign(transform.localScale.x);
            Vector3 backDir = new Vector3(-dirGizmo, 0, 0);
            Gizmos.DrawLine(boxCollider.bounds.center, boxCollider.bounds.center + (backDir * checkWallDistance));
        }
    }
}