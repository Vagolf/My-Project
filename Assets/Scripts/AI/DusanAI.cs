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
    [Tooltip("Target transform to chase. If null, will auto-find by tag")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("Movement Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float detectRange = 15f;
    [SerializeField] private float attackStopDistance = 5.5f;

    [Tooltip("ถ้าระยะห่างน้อยกว่าค่านี้ จะเดินถอยหลังเพื่อรักษาระยะ")]
    [SerializeField] private float minDistance = 2.5f;

    [SerializeField] private LayerMask groundLayer;

    [Header("Dash Settings (หนีเมื่อหลังชนกำแพง)")]
    [SerializeField] private float dashSpeed = 25f; // ความเร็วตอนพุ่ง
    [SerializeField] private float dashDuration = 0.25f; // เวลาที่ใช้พุ่ง (วินาที)
    [SerializeField] private float dashCooldown = 3f; // พุ่งแล้วต้องรอคูลดาวน์กี่วิ
    [SerializeField] private float checkWallDistance = 1.5f; // ระยะเซ็นเซอร์เช็คกำแพงด้านหลัง (เส้นสีชมพู)
    [SerializeField] private LayerMask wallLayer; // เลเยอร์ของกำแพง (มักจะใช้อันเดียวกับ Ground)

    [Header("Attack Settings")]
    [SerializeField] private GameObject normalProjectilePrefab;
    [SerializeField] private Transform firePointNormal;
    [SerializeField] private float normalDamage = 75f;
    [SerializeField] private float normalProjectileSpeed = 18f;
    [SerializeField] private float normalCooldown = 2f;
    [SerializeField] private LayerMask Opponent;
    [SerializeField] private string atkTrigger = "atk";

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
    private float nextDashTime = 0f; // ตัวนับคูลดาวน์ Dash
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

        // 1. นอกระยะสายตา -> ยืนนิ่ง
        if (dist > detectRange)
        {
            StopMove();
            SetRun(false);
            UpdateAnim();
            return;
        }

        // 2. หันหน้าหาผู้เล่นเสมอ
        float dir = Mathf.Sign(target.position.x - transform.position.x);
        Flip(dir);

        // 3. ถ้ากำลังเล่นท่าโจมตี หรือ กำลัง Dash อยู่ -> ห้ามเดินปกติ
        if (actionLocked)
        {
            UpdateAnim();
            return;
        }

        // 4. ลำดับความสำคัญ: สกิลพร้อม + อยู่ในวงแดง -> ต้องยิงก่อน!
        if (dist <= attackStopDistance && Time.time >= nextNormalTime)
        {
            StopMove();
            SetRun(false);
            StartCoroutine(NormalShotRoutine());
            UpdateAnim();
            return;
        }

        // ==========================================
        // 5. ระบบขยับตัว (รวมเช็คกำแพง + Dash หนี)
        // ==========================================
        if (dist > minDistance + 0.1f)
        {
            // ระยะห่างมากพอ -> เดินหน้าเข้าหา
            body.velocity = new Vector2(dir * speed, body.velocity.y);
            SetRun(true);
        }
        else if (dist < minDistance - 0.1f)
        {
            // 🔥 Player เข้ามาใกล้ -> เตรียมเดินถอยหลัง
            // แต่ก่อนจะถอย ต้องเช็คก่อนว่า "หลังชนกำแพงไหม?" และ "Dash พร้อมไหม?"
            if (IsWallBehind(dir) && Time.time >= nextDashTime)
            {
                // ถ้าหลังชนกำแพงแล้ว -> ให้พุ่ง (Dash) สวนไปข้างหน้า!
                StartCoroutine(DashForwardRoutine(dir));
                return;
            }
            else
            {
                // ถ้าข้างหลังโล่ง หรือ Dash ติดคูลดาวน์ -> เดินถอยหลังหนีปกติ
                body.velocity = new Vector2(-dir * speed, body.velocity.y);
                SetRun(true);
            }
        }
        else
        {
            // อยู่ตรงเส้นพอดีเป๊ะๆ -> ยืนรอยิง
            StopMove();
            SetRun(false);
        }

        UpdateAnim();
    }

    // =========================
    // ระบบ Dash ทะลวงกำแพง
    // =========================
    private bool IsWallBehind(float currentDir)
    {
        if (boxCollider == null) return false;

        // ถ้ายิงแสงเลเซอร์ไปทางด้านหลัง (-currentDir)
        Vector2 checkDirection = new Vector2(-currentDir, 0);

        // ยิงเรย์คาสต์จากกลางตัว ไปข้างหลัง เพื่อดูว่าชนกำแพงไหม
        RaycastHit2D hit = Physics2D.Raycast(boxCollider.bounds.center, checkDirection, checkWallDistance, wallLayer);

        return hit.collider != null; // ถ้ามี collider มาขวางแปลว่าหลังติดกำแพงแล้ว
    }

    private IEnumerator DashForwardRoutine(float dir)
    {
        actionLocked = true; // ล็อกไม่ให้ทำอย่างอื่น
        nextDashTime = Time.time + dashCooldown; // รีเซ็ตคูลดาวน์พุ่ง

        // ถ้าคุณมี Animation Dash สามารถเปิดใช้ได้ตรงนี้
        // if (anim != null && HasAnimatorParameter("dash", AnimatorControllerParameterType.Trigger)) anim.SetTrigger("dash");

        // ให้ความเร็วพุ่งไปข้างหน้า (เข้าหา Player) อย่างรวดเร็ว
        body.velocity = new Vector2(dir * dashSpeed, body.velocity.y);

        // รอเวลาให้มันพุ่งจนจบ (เช่น 0.25 วินาที)
        yield return new WaitForSeconds(dashDuration);

        // พอพุ่งเสร็จปุ๊บ สั่งเบรกตัวโก่ง
        StopMove();
        actionLocked = false;
    }

    // =========================
    // Attack Routine
    // =========================
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

    // =========================
    // Helpers
    // =========================
    public void ResetCooldowns()
    {
        StopAllCoroutines();
        actionLocked = false;
        nextNormalTime = Time.time + normalCooldown;
        nextDashTime = 0f; // รีเซ็ตพุ่งด้วย
        if (anim != null)
        {
            if (!string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, false);
            SetRun(false);
        }
    }

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

    // =========================
    // Gizmos (เส้นไกด์ในหน้า Scene)
    // =========================
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackStopDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        if (firePointNormal != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(firePointNormal.position, 0.15f);
        }

        if (target != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, target.position);
        }

        // 🔥 เพิ่มวาดเส้นเซ็นเซอร์เช็คกำแพงด้านหลัง (สีชมพู)
        if (boxCollider != null)
        {
            Gizmos.color = Color.magenta;
            float dirGizmo = target != null ? Mathf.Sign(target.position.x - transform.position.x) : Mathf.Sign(transform.localScale.x);
            Vector3 backDir = new Vector3(-dirGizmo, 0, 0);
            Gizmos.DrawLine(boxCollider.bounds.center, boxCollider.bounds.center + (backDir * checkWallDistance));
        }
    }
}