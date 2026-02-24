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
    [SerializeField] private float atk2StopDistance = 5.5f;
    [SerializeField] private float ultmateStopDistance = 5.5f;

    [SerializeField] private LayerMask groundLayer;

    [Header("Attack - Normal ยิงลมตัดสีฟ้า")]
    [SerializeField] private GameObject normalProjectilePrefab;
    [SerializeField] private Transform firePointNormal;
    [SerializeField] private float normalDamage = 75f;
    [SerializeField] private float normalProjectileSpeed = 18f;
    [SerializeField] private float normalCooldown = 2f; // ✅ เพิ่มตัวแปรคูลดาวน์ยิงปกติ

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

    private float nextNormalTime = 0.5f;
    private float nextStunReadyTime = 0f;
    private float nextUltiReadyTime = 60f;

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

        // ✅ ตั้งเวลาคูลดาวน์เริ่มต้น ไม่ให้บอสสแปมท่าใหญ่ทันทีที่เริ่มเกม
        nextUltiReadyTime = Time.time + ultimateCooldown;
        nextStunReadyTime = Time.time + stunCooldown; // <-- เพิ่มบรรทัดนี้ครับ!
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

            // Ultimate
            //if (Time.time >= nextUltiReadyTime)
            //{
            //    Debug.Log("Ultimate Start");
            //    StartCoroutine(UltimateRoutine());
            //    UpdateAnim();
            //    return;
            //}

            // Atk2 - Stun Shot
            if (Time.time >= nextStunReadyTime)
            {
                Debug.Log("Stun Shot Start");
                StartCoroutine(StunShotRoutine());
                UpdateAnim();
                return;
            }

            // ✅ Normal Attack: ตรวจสอบคูลดาวน์ก่อน
            if (Time.time >= nextNormalTime)
            {
                Debug.Log("Normal Shot Start");
                StartCoroutine(NormalShotRoutine());
            }

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

    // ✅ เปลี่ยนมายิงผ่าน Coroutine เพื่อคุมจังหวะและ Cooldown ได้สมบูรณ์
    private IEnumerator NormalShotRoutine()
    {
        actionLocked = true; // ล็อคไม่ให้ขยับ
        nextNormalTime = Time.time + normalCooldown; // ตั้งเวลาคูลดาวน์ครั้งถัดไป

        if (anim != null && !string.IsNullOrEmpty(atkTrigger))
        {
            anim.SetBool(atkTrigger, true); // สั่งเล่นแอนิเมชันโจมตี
        }

        // รอจังหวะแอนิเมชันให้มือสะบัดก่อนเสกกระสุน (ปรับเวลา 0.15f ได้ตามแอนิเมชันของคุณ)
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

        // รอให้แอนิเมชันโจมตีเล่นจนจบ
        yield return new WaitForSecondsRealtime(0.35f);

        if (anim != null && !string.IsNullOrEmpty(atkTrigger))
        {
            anim.SetBool(atkTrigger, false); // สั่งปิดแอนิเมชัน เพื่อกลับไป Idle/Walk
        }

        actionLocked = false; // ปลดล็อคให้เดินหรือโจมตีท่าอื่นต่อได้
    }

    private IEnumerator StunShotRoutine()
    {
        if (stunProjectilePrefab == null) yield break;

        actionLocked = true;
        nextStunReadyTime = Time.time + stunCooldown;

        // ==========================================
        // 1. สั่งให้เข้าท่าก้ม (Eva-defent) ก่อน
        // ==========================================
        SetRun(false);
        // 🚨 บังคับ atk ให้เป็น false ก่อน เพื่อให้ผ่านด่านลูกศรไปท่าก้มได้!
        if (anim != null && !string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, false);
        SetCrouch(true);

        // รอเสี้ยววินาที (0.05วิ) ให้ Animator เดินทางไปถึงท่า Eva-defent 
        yield return new WaitForSecondsRealtime(0.05f);

        // ==========================================
        // 2. พอตัวก้มแล้ว สั่งโจมตีได้ (วิ่งไป Eva-atk2)
        // ==========================================
        if (anim != null && !string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, true);

        // รอแอนิเมชันง้างยิง (อาจจะปรับเพิ่ม/ลดได้ตามความสวยงามของท่า)
        yield return new WaitForSecondsRealtime(0.15f);

        // ==========================================
        // 3. สร้างกระสุน
        // ==========================================
        float dir = Mathf.Sign(target.position.x - transform.position.x);
        Vector3 spawn = firePointStun != null ? firePointStun.position : transform.position;

        GameObject go = Instantiate(stunProjectilePrefab, spawn, Quaternion.identity);
        ProjectileAtk2Behaviour proj = go.GetComponent<ProjectileAtk2Behaviour>();
        if (proj != null)
        {
            proj.Init(new Vector2(dir, 0f), stunProjectileSpeed, stunDamage, Opponent, stunKnockbackForce, stunDuration);
        }

        // รอจนท่าทางยิงเล่นจบ
        yield return new WaitForSecondsRealtime(0.4f);

        // ==========================================
        // 4. รีเซ็ตกลับท่ายืนปกติ
        // ==========================================
        SetCrouch(false);
        if (anim != null && !string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, false);

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackStopDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, atk2StopDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ultmateStopDistance);

        if (target != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }

    // =========================
    // ✅ เพิ่มฟังก์ชันนี้ สำหรับถูกเรียกตอนเริ่มรอบใหม่
    // =========================
    public void ResetCooldowns()
    {
        // 1. หยุดการทำงานของท่าโจมตีที่อาจจะค้างอยู่ตอนตาย
        StopAllCoroutines();
        actionLocked = false;

        // 2. รีเซ็ตเวลาคูลดาวน์ใหม่ทั้งหมด (เหมือนตอนเริ่มเกม)
        nextNormalTime = Time.time + normalCooldown;
        nextStunReadyTime = Time.time + stunCooldown;
        nextUltiReadyTime = Time.time + ultimateCooldown;

        // 3. ปิดแอนิเมชันโจมตีที่อาจจะค้างอยู่
        if (anim != null)
        {
            if (!string.IsNullOrEmpty(atkTrigger)) anim.SetBool(atkTrigger, false);
            SetCrouch(false);
            SetRun(false);
        }
    }
}