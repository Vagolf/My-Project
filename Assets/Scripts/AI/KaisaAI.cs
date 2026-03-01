using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class KaisaAI : MonoBehaviour
{
    [Header("Target Lock")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

    [Header("Ranges")]
    [SerializeField] private float detectRange = 12f;
    [SerializeField] private float attackRange = 1.9f;

    // 🔥 เพิ่ม 2 ตัวแปรนี้เพื่อคุมระยะการเข้าหา
    [Tooltip("ระยะไกลสุดที่จะหยุดเดิน (ถอยหลังถ้าใกล้กว่านี้)")]
    [SerializeField] private float stopDistance = 1.0f;
    [Tooltip("ระยะใกล้สุดที่จะหยุดเดินเข้าหา (ห้ามเข้าใกล้กว่านี้)")]
    [SerializeField] private float minStopDistance = 1.5f;

    [Header("Dash Settings")]
    [SerializeField] private float dashingPower = 100f;
    [SerializeField] private float dashnigTime = 0.2f;
    [SerializeField] private float dashingCooldown = 1f;
    private bool canDash = true;
    private bool isDashing;

    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private Animator anim;
    private bool ground;
    private bool movementLocked = false;

    [Header("Attack")]
    public GameObject AttackPoint;
    public float radius = 1f;
    public GameObject CrouchAttackPoint;
    public float crouchRadius = 1f;
    public LayerMask Opponent;
    public float normalAttackDamage = 70f;
    public float crouchAttackDamage = 180f;
    [SerializeField] private float normalAttackCooldown = 2.0f;
    [SerializeField] private float crouchAttackCooldown = 6.0f;
    private float normalAttackTimer = 0f;
    private float crouchAttackTimer = 0f;
    private bool attack;

    // 🔥 เพิ่ม List สำหรับกันดาเมจเบิ้ล (Multi-hit prevention)
    private List<HealthCh> damagedPlayers = new List<HealthCh>();

    [Header("Ultimate Skill (Kaisa)")]
    [SerializeField] private float ultimateCooldown = 8f;
    [SerializeField] private float ultimateDamage = 500f;
    [SerializeField] private float ultimateHitRadiusMultiplier = 2f;
    private float ultimateTimer = 0f;
    private bool ultimateReady = false;
    private bool inUltimate = false;
    private bool preparingUltimate = false;
    [SerializeField] private string ultimateTrigger = "ulti";
    public UltimateDamagePoint ultimateDamagePoint;

    [Header("Ultimate Area")]
    [SerializeField] private bool ultimateUseBox = false; // รองรับ Box
    [SerializeField] private Vector2 ultimateBoxSize = new Vector2(4f, 2f);
    [SerializeField] private float ultimateBoxAngle = 0f;
    [SerializeField] private Transform UltimateAttackPoint;
    private bool ultimateDamageFired = false;

    // ----- ตัวแปร Warp ของ Kaisa ที่นำกลับมา -----
    [Header("Ultimate Warp On Hit")]
    [SerializeField] private bool enableUltimateHitWarp = true;
    [SerializeField] private float ultimateHitWarpDistance = 5f;
    [SerializeField] private bool ultimateHitWarpUseTrail = true;
    [SerializeField] private bool ultimateWarpOnActivate = false;
    [SerializeField] private bool ultimateWarpUseBoxCast = true;
    private bool ultimateHitWarpDone = false;

    [Header("Ultimate Warp Collision")]
    [SerializeField] private LayerMask ultimateWarpBlockLayers;
    [SerializeField] private float ultimateWarpSkin = 0.1f;
    [SerializeField] private bool ultimateWarpUseColliderBounds = true;

    [Header("Ultimate Warp Effects")]
    [SerializeField] private GameObject ultimateWarpStartEffect;
    [SerializeField] private GameObject ultimateWarpEndEffect;
    [SerializeField] private bool spawnEffectsInUnscaledTime = true;
    [SerializeField] private Vector2 effectOffset = Vector2.zero;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        if (tr == null) tr = GetComponent<TrailRenderer>();
    }

    private void Start()
    {
        ultimateReady = false;
        ultimateTimer = 0f;
        inUltimate = false;
        preparingUltimate = false;
        if (tr) { tr.emitting = false; tr.enabled = false; tr.Clear(); }
        if (target == null)
        {
            var t = GameObject.FindGameObjectWithTag(targetTag);
            if (t != null) target = t.transform;
        }
    }

    private void Update()
    {
        if (ultimateReady == false) { ultimateTimer += Time.deltaTime; if (ultimateTimer >= ultimateCooldown) { ultimateReady = true; ultimateTimer = 0; } }
        if (normalAttackTimer > 0f) normalAttackTimer -= Time.deltaTime;
        if (crouchAttackTimer > 0f) crouchAttackTimer -= Time.deltaTime;

        if (Timer.GateBlocked || target == null)
        {
            if (anim) anim.SetBool("run", false);
            return;
        }

        IsGrounded();
        if (preparingUltimate || inUltimate || isDashing) return;

        if (attack || movementLocked)
        {
            body.velocity = new Vector2(0f, body.velocity.y);
            anim.SetBool("run", false);
            return;
        }

        if (ultimateReady && ground)
        {
            float distToTarget = Mathf.Abs(target.position.x - transform.position.x);
            if (distToTarget <= detectRange)
            {
                StartCoroutine(PrepareAndStartUltimate());
                return;
            }
        }

        float dist = Vector2.Distance(transform.position, target.position);
        float dir = Mathf.Sign(target.position.x - transform.position.x);

        // หันหน้าหาเป้าหมาย
        if (dir > 0.01f) transform.localScale = new Vector3(1, 1, 1);
        else if (dir < -0.01f) transform.localScale = new Vector3(-1, 1, 1);

        // 🔥 Logic ใหม่: ตัดสินใจโจมตีก่อนเดิน
        bool inNormalRange = dist <= attackRange;
        var playerComp = target.GetComponent<RomanPlayer>();
        bool targetCrouching = playerComp != null && playerComp.isCrouching;

        if (inNormalRange)
        {
            body.velocity = new Vector2(0f, body.velocity.y); // อยู่ในระยะแล้ว หยุดเดินก่อน
            anim.SetBool("run", false);

            if (targetCrouching && crouchAttackTimer <= 0f)
            {
                anim.SetBool("crouch", true); anim.SetBool("atk", true);
                crouchAttackTimer = crouchAttackCooldown; attack = true;
            }
            else if (normalAttackTimer <= 0f) // ถ้าผู้เล่นไม่นั่ง หรือ นั่งตีคูลดาวน์อยู่ ให้ตีธรรมดา
            {
                anim.SetBool("crouch", false); anim.SetBool("atk", true);
                normalAttackTimer = normalAttackCooldown; attack = true;
            }
            else // ถ้ารอคูลดาวน์ตีอยู่
            {
                if (dist < stopDistance) // ถ้าใกล้ไป ค่อยถอย
                {
                    body.velocity = new Vector2(-dir * speed, body.velocity.y);
                    anim.SetBool("run", true);
                }
            }
        }
        else // ถ้านอกระยะโจมตี ให้วิ่งเข้าหา
        {
            body.velocity = new Vector2(dir * speed, body.velocity.y);
            anim.SetBool("run", true);
            if (canDash && dist > attackRange * 2.5f) StartCoroutine(Dash());
        }

        if (anim != null) { anim.SetBool("ground", ground); anim.SetFloat("yVelocity", body.velocity.y); }
    }

    private bool IsGrounded()
    {
        ground = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        return ground;
    }

    private IEnumerator Dash()
    {
        canDash = false; isDashing = true; attack = false; movementLocked = false;
        if (anim != null) anim.SetBool("atk", false);
        float originalGravity = body.gravityScale;
        body.gravityScale = 0f;
        body.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        if (tr) tr.emitting = true;
        yield return new WaitForSeconds(dashnigTime);
        if (tr) tr.emitting = false;
        body.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }

    // ==========================================
    // โจมตีปกติ (อัปเกรด: กันดาเมจเบิ้ล)
    // ==========================================
    public void Attack()
    {
        if (AttackPoint == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(AttackPoint.transform.position, radius, Opponent);
        foreach (var h in hits)
        {
            if (h.gameObject == this.gameObject || h.transform.IsChildOf(this.transform)) continue;

            var hpPlayer = h.GetComponent<HealthCh>() ?? h.GetComponentInParent<HealthCh>();
            if (hpPlayer != null && !damagedPlayers.Contains(hpPlayer))
            {
                hpPlayer.TakeDamage(normalAttackDamage);
                damagedPlayers.Add(hpPlayer);
            }
        }
    }

    // ==========================================
    // นั่งโจมตี (อัปเกรด: กันดาเมจเบิ้ล)
    // ==========================================
    public void CrouchAttack()
    {
        if (CrouchAttackPoint == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(CrouchAttackPoint.transform.position, crouchRadius, Opponent);
        foreach (var h in hits)
        {
            if (h.gameObject == this.gameObject || h.transform.IsChildOf(this.transform)) continue;

            var hpPlayer = h.GetComponent<HealthCh>() ?? h.GetComponentInParent<HealthCh>();
            if (hpPlayer != null && !damagedPlayers.Contains(hpPlayer))
            {
                hpPlayer.TakeDamage(crouchAttackDamage);
                damagedPlayers.Add(hpPlayer);
            }
        }
    }

    // เคลียร์ค่าจำดาเมจทุกครั้งที่เริ่มง้างตี
    public void OnAttackStart()
    {
        damagedPlayers.Clear(); // 🔥 ล้างความจำกันดาเมจหาย
        if (anim != null) anim.SetBool("atk", true);
        attack = true;
    }

    public void OnAttackEnd()
    {
        if (anim != null) anim.SetBool("atk", false);
        attack = false;
        movementLocked = false;
        damagedPlayers.Clear(); // ล้างความจำตอนจบ
    }

    public void NormalAttackLockMovement() { movementLocked = true; if (body != null) body.velocity = new Vector2(0f, body.velocity.y); }
    public void NormalAttackUnlockMovement() { movementLocked = false; }

    // ================== ระบบ Ultimate ของ Kaisa (มีวาร์ป) ==================
    private IEnumerator PrepareAndStartUltimate()
    {
        damagedPlayers.Clear(); // 🔥 ล้างความจำก่อนอันติ

        preparingUltimate = true;
        body.velocity = new Vector2(0f, body.velocity.y);
        anim.SetBool("run", false);
        yield return null;
        inUltimate = true;
        preparingUltimate = false;
        ultimateReady = false;
        ultimateTimer = 0f;
        ultimateDamageFired = false;
        ultimateHitWarpDone = false;

        body.velocity = Vector2.zero;
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (!string.IsNullOrEmpty(ultimateTrigger)) anim.SetTrigger(ultimateTrigger);
        Time.timeScale = 0f;

        if (ultimateDamagePoint != null) ultimateDamagePoint.gameObject.SetActive(true);
        UltimateEventBus.RaiseStart(transform);

        if (enableUltimateHitWarp && ultimateWarpOnActivate) PerformUltimateWarp();
    }

    public void UltimateWarpEvent()
    {
        if (!enableUltimateHitWarp || ultimateHitWarpDone) return;
        PerformUltimateWarp();
    }

    private void PerformUltimateWarp()
    {
        ultimateHitWarpDone = true;
        Vector3 startPos = transform.position;
        SpawnWarpEffect(ultimateWarpStartEffect, startPos + (Vector3)effectOffset, "start");

        float direction = transform.localScale.x >= 0 ? 1f : -1f;
        float halfWidthExtra = (ultimateWarpUseColliderBounds && boxCollider != null) ? boxCollider.bounds.extents.x : 0f;
        Vector2 dir = new Vector2(direction, 0f);
        float allowedDistance = ultimateHitWarpDistance;

        if (ultimateWarpUseBoxCast && boxCollider != null)
        {
            RaycastHit2D hit = Physics2D.BoxCast(startPos, boxCollider.size, 0f, dir, ultimateHitWarpDistance + halfWidthExtra, ultimateWarpBlockLayers);
            if (hit.collider != null && hit.collider != boxCollider)
            {
                allowedDistance = hit.distance - ultimateWarpSkin;
                if (allowedDistance < 0f) allowedDistance = 0f;
            }
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, ultimateHitWarpDistance + halfWidthExtra, ultimateWarpBlockLayers);
            if (hit.collider != null)
            {
                allowedDistance = hit.distance - ultimateWarpSkin - halfWidthExtra;
                if (allowedDistance < 0f) allowedDistance = 0f;
            }
        }

        Vector3 target = startPos + new Vector3(direction * allowedDistance, 0f, 0f);

        if (ultimateHitWarpUseTrail && tr != null)
        {
            bool prev = tr.emitting; tr.emitting = true; tr.Clear();
            transform.position = target;
            tr.emitting = prev;
        }
        else { transform.position = target; }

        SpawnWarpEffect(ultimateWarpEndEffect, target + (Vector3)effectOffset, "end");
    }

    private void SpawnWarpEffect(GameObject prefab, Vector3 position, string phase)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, position, Quaternion.identity);
        if (spawnEffectsInUnscaledTime)
        {
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.useUnscaledTime = true;
            }
        }
    }

    // ==========================================
    // ท่าไม้ตาย (อัปเกรด: กันดาเมจเบิ้ล + รองรับ Box)
    // ==========================================
    public void UltimateDamageEvent()
    {
        if (!ultimateDamageFired)
        {
            ultimateDamageFired = true;
            UltimateEventBus.RaiseDamage(transform);
        }

        Vector2 center = UltimateAttackPoint != null ? (Vector2)UltimateAttackPoint.position : (AttackPoint != null ? (Vector2)AttackPoint.transform.position : (Vector2)transform.position);

        Collider2D[] hits;
        if (ultimateUseBox)
        {
            hits = Physics2D.OverlapBoxAll(center, ultimateBoxSize, ultimateBoxAngle, Opponent);
        }
        else
        {
            float hitRadius = radius * ultimateHitRadiusMultiplier;
            hits = Physics2D.OverlapCircleAll(center, hitRadius, Opponent);
        }

        foreach (var h in hits)
        {
            if (h.gameObject == this.gameObject || h.transform.IsChildOf(this.transform)) continue;

            var hp = h.GetComponent<HealthCh>() ?? h.GetComponentInParent<HealthCh>();
            if (hp != null && !damagedPlayers.Contains(hp))
            {
                hp.TakeDamage(ultimateDamage);
                damagedPlayers.Add(hp);
            }
        }
    }

    public void UltimateFinishEvent()
    {
        Time.timeScale = 1f;
        anim.updateMode = AnimatorUpdateMode.Normal;
        inUltimate = false;
        preparingUltimate = false; // เคลียร์เพื่อกันบั๊กเดินไม่ได้
        attack = false;
        movementLocked = false;

        if (ultimateDamagePoint != null && ultimateDamagePoint.gameObject.activeSelf) ultimateDamagePoint.gameObject.SetActive(false);
        anim.ResetTrigger(ultimateTrigger);
        anim.SetBool("run", false); anim.SetBool("atk", false); anim.SetBool("crouch", false);
        anim.Play("idle-K", 0, 0f);
        UltimateEventBus.RaiseFinish(transform);
    }

    // ==========================================
    // ตัวช่วยวาดเส้นและสีพื้นที่ในหน้าต่าง Scene (อัปเกรดสีสันให้ดูง่ายขึ้น)
    // ==========================================
    private void OnDrawGizmosSelected()
    {
        // 1. พื้นที่โจมตีปกติ (สีแดงโปร่งใส)
        if (AttackPoint != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // แดงโปร่งใส (Alpha 0.3)
            Gizmos.DrawSphere(AttackPoint.transform.position, radius);
            Gizmos.color = Color.red; // ขอบแดงเข้ม
            Gizmos.DrawWireSphere(AttackPoint.transform.position, radius);
        }

        // 2. พื้นที่นั่งตี (สีน้ำเงินโปร่งใส)
        if (CrouchAttackPoint != null)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.3f); // น้ำเงินโปร่งใส
            Gizmos.DrawSphere(CrouchAttackPoint.transform.position, crouchRadius);
            Gizmos.color = Color.blue; // ขอบน้ำเงินเข้ม
            Gizmos.DrawWireSphere(CrouchAttackPoint.transform.position, crouchRadius);
        }

        // 3. พื้นที่ท่าไม้ตาย (สีเหลืองโปร่งใส)
        Vector2 center = UltimateAttackPoint != null ? (Vector2)UltimateAttackPoint.position : (AttackPoint != null ? (Vector2)AttackPoint.transform.position : (Vector2)transform.position);
        if (ultimateUseBox)
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f); // เหลืองโปร่งใส
            Gizmos.DrawCube(center, ultimateBoxSize);
            Gizmos.color = Color.yellow; // ขอบเหลืองเข้ม
            Gizmos.DrawWireCube(center, ultimateBoxSize);
        }
        else
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f);
            Gizmos.DrawSphere(center, radius * ultimateHitRadiusMultiplier);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, radius * ultimateHitRadiusMultiplier);
        }

        // ==========================================
        // 🔥 เพิ่มพิเศษ: วงแหวนบอกระยะการเดินของ AI (ช่วยให้ตั้งค่าตรง Ranges ง่ายขึ้น)
        // ==========================================

        // เส้นสีขาว: ระยะมองเห็น (Detect Range) AI จะเริ่มวิ่งหาเมื่อเราเข้าวงนี้
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // เส้นสีม่วง: ระยะหวงตัว (Min Stop Distance) AI จะเบรกทันทีที่แตะเส้นนี้
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, minStopDistance);
    }

    // ==========================================
    // Animation Event Receivers (เพิ่มไว้เพื่อกัน Error แดงใน Console)
    // ==========================================
    public void StartUltimate() { }
}