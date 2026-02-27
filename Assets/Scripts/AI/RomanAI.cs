using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // for Action

// AI controlled version of Roman. It chases a locked target and performs
// the same animations / skills as the player version when available.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class RomanAI : MonoBehaviour
{
    [Header("Target Lock")]
    [Tooltip("Target transform to chase. If null, will auto-find by tag")]
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
    [SerializeField] private float stopDistance = 1.0f;

    [Header("Dash Settings")]
    [SerializeField] private float dashingPower = 100f;
    [SerializeField] private float dashnigTime = 0.2f;
    [SerializeField] private float dashingCooldown = 1f;
    private bool canDash = true;
    private bool isDashing;

    [Header("Dash Collision")]
    [Tooltip("Layers that block dash movement (e.g., Walls, Ground Bounds)")]
    [SerializeField] private LayerMask dashBlockLayers;
    [SerializeField] private float dashSkin = 0.05f;
    [SerializeField] private bool dashUseBoxCast = true;

    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private Animator anim;

    private bool ground;

    [Header("Attack")]
    public GameObject AttackPoint;
    public float radius = 1f;
    public GameObject CrouchAttackPoint;
    public float crouchRadius = 1f;
    [Tooltip("Layers to damage when AI attacks (should include Player)")]
    public LayerMask Opponent;
    [Header("Damage Values")] public float damage = 100f; // legacy default
    public float normalAttackDamage = 70f;
    public float crouchAttackDamage = 180f;
    [Header("Attack Cooldowns")]
    [SerializeField] private float normalAttackCooldown = 2.0f;
    [SerializeField] private float crouchAttackCooldown = 6.0f;
    private float normalAttackTimer = 0f;
    private float crouchAttackTimer = 0f;
    [Tooltip("If true, AI can use crouch attack on proximity even if the target is not crouching.")]
    [SerializeField] private bool useCrouchOnProximity = true;
    private bool attack;

    [Header("Ultimate Skill")]
    [SerializeField] private float ultimateCooldown = 8f;
    [SerializeField] private float ultimateDamage = 500f;
    [SerializeField] private float ultimateHitRadiusMultiplier = 2f;
    private float ultimateTimer = 0f;
    private bool ultimateReady = false;
    private bool inUltimate = false;
    private bool preparingUltimate = false;
    [SerializeField] private bool logUltimateCooldown = true;
    private int ultimateLastLoggedSecond = -1;

    [Header("Ultimate Animation")]
    [SerializeField] private string ultimateTrigger = "ulti";

    [Header("Ultimate Warp (disabled)")]
    [SerializeField] private bool enableUltimateHitWarp = false;

    private bool ultimateDamageFired = false;

    [Header("Ultimate Damage Point")]
    public UltimateDamagePoint ultimateDamagePoint;

    [Header("Ultimate Box Area")]
    [SerializeField] private bool ultimateUseBox = false;
    [SerializeField] private Vector2 ultimateBoxSize = new Vector2(4f, 2f);
    [SerializeField] private float ultimateBoxAngle = 0f;
    [SerializeField] private Transform UltimateAttackPoint;

    [Header("Air Jump")]
    [SerializeField] private int extraJumpsMax = 1;
    private int extraJumps;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;
    [Header("Animator States")]
    [Tooltip("Animator state name to play when returning to idle after actions")]
    [SerializeField] private string idleState = "Roman-idle";

    // Movement lock during normal attack
    private bool movementLocked = false;

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
        ultimateLastLoggedSecond = -1;
        inUltimate = false;
        preparingUltimate = false;
        extraJumps = extraJumpsMax;

        if (tr)
        {
            tr.emitting = false;
            tr.enabled = false;
            tr.Clear();
        }
        if (target == null)
        {
            var t = GameObject.FindGameObjectWithTag(targetTag);
            if (t != null) target = t.transform;
        }
    }

    private void OnValidate()
    {
        if (normalAttackCooldown < 0.1f) normalAttackCooldown = 0.1f;
        if (crouchAttackCooldown < 0.1f) crouchAttackCooldown = 0.1f;
        if (radius <= 0f) radius = 0.1f;
        if (crouchRadius <= 0f) crouchRadius = 0.1f;
    }

    private void Update()
    {
        // 1. Cooldowns
        TickUltimateCooldown(Time.deltaTime);
        if (normalAttackTimer > 0f) normalAttackTimer -= Time.deltaTime;
        if (crouchAttackTimer > 0f) crouchAttackTimer -= Time.deltaTime;

        if (Timer.GateBlocked)
        {
            SetRun(false);
            return;
        }

        if (target == null)
        {
            var t = GameObject.FindGameObjectWithTag(targetTag);
            if (t != null) target = t.transform; else { SetRun(false); return; }
        }

        IsGrounded();

        // 2. Block Logic (กำลังอัลติ, พุ่ง, หรือโจมตีอยู่ ห้ามเดินเด็ดขาด)
        if (preparingUltimate) { SetRun(false); return; }
        if (inUltimate) { return; }
        if (isDashing) return;

        // 🔥 แก้บัคเดินสไลด์: ถ้ากำลังง้างโจมตี (attack=true) หรือถูกล็อคขา ห้ามเดินต่อ!
        if (attack || movementLocked)
        {
            body.velocity = new Vector2(0f, body.velocity.y);
            SetRun(false);
            UpdateAnimParams();
            return;
        }

        // 3. Auto use Ultimate
        if (ultimateReady && ground)
        {
            float distToTarget = Mathf.Abs(target.position.x - transform.position.x);
            if (distToTarget <= detectRange)
            {
                StartCoroutine(PrepareAndStartUltimate());
                return;
            }
        }

        // 4. Movement & Attack Logic
        Vector3 attackPos = AttackPoint != null ? AttackPoint.transform.position : transform.position;
        float dist = Vector2.Distance(attackPos, target.position);
        float dir = Mathf.Sign(target.position.x - transform.position.x);

        // หันหน้าเสมอ
        if (dir > 0.01f) transform.localScale = new Vector3(1, 1, 1);
        else if (dir < -0.01f) transform.localScale = new Vector3(-1, 1, 1);

        if (dist > attackRange)
        {
            // นอกระยะโจมตี -> วิ่งเข้าหา
            body.velocity = new Vector2(dir * speed, body.velocity.y);
            SetRun(true);

            // Dash ตามถ้าระยะห่างมาก
            if (canDash && dist > attackRange * 2.5f)
                StartCoroutine(Dash());
        }
        else
        {
            // อยู่ในระยะโจมตี -> เช็คว่าโจมตีได้ไหม
            bool inNormalRange = AttackPoint != null ? Vector2.Distance(AttackPoint.transform.position, target.position) <= radius : dist <= attackRange;
            bool inCrouchRange = CrouchAttackPoint != null ? Vector2.Distance(CrouchAttackPoint.transform.position, target.position) <= crouchRadius : dist <= attackRange;

            var playerComp = target.GetComponent<Player>();
            bool targetCrouching = playerComp != null && playerComp.isCrouching;

            bool started = TryAttack(inNormalRange, inCrouchRange, targetCrouching);

            if (started)
            {
                // โจมตีสำเร็จ -> เบรกทันที
                body.velocity = new Vector2(0f, body.velocity.y);
                SetRun(false);
            }
            else
            {
                // โจมตีไม่ได้ (ติดคูลดาวน์) -> รักษาระยะห่าง
                if (dist < stopDistance)
                {
                    // ชิดเกินไป เดินถอยหลัง
                    body.velocity = new Vector2(-dir * speed, body.velocity.y);
                    SetRun(true);
                }
                else
                {
                    // ระยะสวยงามแล้ว ยืนรอคูลดาวน์นิ่งๆ
                    body.velocity = new Vector2(0f, body.velocity.y);
                    SetRun(false);
                }
            }
        }

        UpdateAnimParams();
    }

    private void UpdateAnimParams()
    {
        if (anim != null)
        {
            anim.SetBool("ground", ground);
            anim.SetFloat("yVelocity", body.velocity.y);
        }
    }

    private bool TryAttack(bool inNormalRange, bool inCrouchRange, bool targetCrouching)
    {
        if ((targetCrouching || useCrouchOnProximity) && inCrouchRange && crouchAttackTimer <= 0f)
        {
            StartAttackAnimation(isCrouch: true);
            crouchAttackTimer = crouchAttackCooldown;
            attack = true;
            return true;
        }

        if (inNormalRange && normalAttackTimer <= 0f)
        {
            StartAttackAnimation(isCrouch: false);
            normalAttackTimer = normalAttackCooldown;
            attack = true;
            return true;
        }
        return false;
    }

    private void StartAttackAnimation(bool isCrouch)
    {
        if (anim == null) return;
        anim.SetBool("run", false);
        if (isCrouch) anim.SetBool("crouch", true);

        bool setBool = HasAnimatorParameter("atk", AnimatorControllerParameterType.Bool);
        bool hasAtkTrigger = HasAnimatorParameter("atk", AnimatorControllerParameterType.Trigger);
        bool hasAttackTrigger = HasAnimatorParameter("attack", AnimatorControllerParameterType.Trigger);

        if (setBool) anim.SetBool("atk", true);
        else if (hasAtkTrigger) anim.SetTrigger("atk");
        else if (hasAttackTrigger) anim.SetTrigger("attack");

        if (verboseLog) Debug.Log($"[AI ATK] StartAttackAnimation {(isCrouch ? "[CROUCH]" : "[NORMAL]")}");
    }

    private bool HasAnimatorParameter(string name, AnimatorControllerParameterType type)
    {
        if (anim == null) return false;
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }

    private void SetRun(bool r)
    {
        if (anim == null) return;
        if (HasAnimatorParameter("run", AnimatorControllerParameterType.Bool)) anim.SetBool("run", r);
        if (HasAnimatorParameter("Run", AnimatorControllerParameterType.Bool)) anim.SetBool("Run", r);
    }

    private bool IsGrounded()
    {
        if (boxCollider == null) return ground;
        ground = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        return ground;
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        // Reset attack states if interrupted
        attack = false;
        movementLocked = false;
        if (anim != null) anim.SetBool("atk", false);

        float originalGravity = body.gravityScale;
        body.gravityScale = 0f;

        Vector3 startPos = transform.position;
        float dir = transform.localScale.x >= 0 ? 1f : -1f;
        float desiredDistance = Mathf.Abs(dashingPower) * dashnigTime;
        float allowedDistance = desiredDistance;

        if (dashUseBoxCast && boxCollider != null)
        {
            Vector2 castSize = boxCollider.size;
            RaycastHit2D hit = Physics2D.BoxCast(startPos, castSize, 0f, new Vector2(dir, 0f), desiredDistance, dashBlockLayers);
            if (hit.collider != null)
            {
                allowedDistance = Mathf.Max(0f, hit.distance - dashSkin);
            }
        }

        body.velocity = Vector2.zero;
        Vector3 target = startPos + new Vector3(dir * allowedDistance, 0f, 0f);
        if (tr) tr.emitting = true;
        transform.position = target;

        yield return new WaitForSeconds(dashnigTime);

        if (tr) tr.emitting = false;
        body.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }

    public void Attack()
    {
        if (AttackPoint == null) return;
        float usedDamage = normalAttackDamage;
        var pos = AttackPoint.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius, Opponent);
        foreach (var h in hits)
        {
            var hpEnemy = h.GetComponent<HealthEnemy>();
            if (hpEnemy != null) { hpEnemy.TakeDamage(usedDamage); continue; }
            var hpPlayer = h.GetComponent<HealthCh>();
            if (hpPlayer != null) { hpPlayer.TakeDamage(usedDamage); }
        }
    }

    public void CrouchAttack()
    {
        if (CrouchAttackPoint == null) return;
        var pos = CrouchAttackPoint.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, crouchRadius, Opponent);
        foreach (var h in hits)
        {
            var hpEnemy = h.GetComponent<HealthEnemy>();
            if (hpEnemy != null) { hpEnemy.TakeDamage(crouchAttackDamage); continue; }
            var hpPlayer = h.GetComponent<HealthCh>();
            if (hpPlayer != null) { hpPlayer.TakeDamage(crouchAttackDamage); }
        }
    }

    // ===== Animation Event Receivers =====
    public void OnAttackStart()
    {
        if (anim != null) anim.SetBool("atk", true);
        attack = true;
    }

    public void OnAttackEnd()
    {
        StopAttackAnimation();
        movementLocked = false;

        if (anim != null && anim.GetBool("crouch"))
        {
            if (crouchAttackTimer < crouchAttackCooldown)
                crouchAttackTimer = crouchAttackCooldown;
        }
        else
        {
            if (normalAttackTimer < normalAttackCooldown)
                normalAttackTimer = normalAttackCooldown;
        }
    }

    public void StopAttackAnimation()
    {
        if (anim != null) anim.SetBool("atk", false);
        attack = false;
    }

    public void NormalAttackLockMovement()
    {
        movementLocked = true;
        if (body != null) body.velocity = new Vector2(0f, body.velocity.y);
    }

    public void NormalAttackUnlockMovement()
    {
        movementLocked = false;
    }

    private IEnumerator PrepareAndStartUltimate()
    {
        preparingUltimate = true;
        body.velocity = new Vector2(0f, body.velocity.y);
        SetRun(false);
        yield return null;
        StartUltimate();
    }

    private void StartUltimate()
    {
        inUltimate = true;
        preparingUltimate = false;
        ultimateReady = false;
        ultimateTimer = 0f;
        ultimateLastLoggedSecond = -1;
        ultimateDamageFired = false;

        body.velocity = Vector2.zero;
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (!string.IsNullOrEmpty(ultimateTrigger))
            anim.SetTrigger(ultimateTrigger);
        Time.timeScale = 0f;

        if (ultimateDamagePoint != null)
            ultimateDamagePoint.gameObject.SetActive(true);

        UltimateEventBus.RaiseStart(transform);
    }

    public void UltimateDamageEvent()
    {
        if (AttackPoint == null && UltimateAttackPoint == null) return;
        if (!ultimateDamageFired)
        {
            ultimateDamageFired = true;
            UltimateEventBus.RaiseDamage(transform);
        }
        Vector2 center = UltimateAttackPoint != null ? (Vector2)UltimateAttackPoint.position : (Vector2)AttackPoint.transform.position;

        int hitCount = 0;
        if (ultimateUseBox)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, ultimateBoxSize, ultimateBoxAngle, Opponent);
            foreach (var h in hits)
            {
                var he = h.GetComponent<HealthEnemy>();
                var hp = h.GetComponent<HealthCh>();
                if (he != null) { he.TakeDamage(ultimateDamage); hitCount++; }
                else if (hp != null) { hp.TakeDamage(ultimateDamage); hitCount++; }
            }
        }
        else
        {
            float hitRadius = radius * ultimateHitRadiusMultiplier;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius, Opponent);
            foreach (var h in hits)
            {
                var he = h.GetComponent<HealthEnemy>();
                var hp = h.GetComponent<HealthCh>();
                if (he != null) { he.TakeDamage(ultimateDamage); hitCount++; }
                else if (hp != null) { hp.TakeDamage(ultimateDamage); hitCount++; }
            }
        }
    }

    public void UltimateFinishEvent()
    {
        Time.timeScale = 1f;
        anim.updateMode = AnimatorUpdateMode.Normal;
        inUltimate = false;

        if (ultimateDamagePoint != null && ultimateDamagePoint.gameObject.activeSelf)
            ultimateDamagePoint.gameObject.SetActive(false);

        anim.ResetTrigger(ultimateTrigger);
        SetRun(false);
        anim.SetBool("atk", false);
        anim.SetBool("crouch", false);
        anim.SetFloat("yVelocity", 0f);

        PlayIdleState();

        UltimateEventBus.RaiseFinish(transform);
    }

    private void TickUltimateCooldown(float dt)
    {
        if (ultimateReady) return;
        ultimateTimer += Mathf.Max(0f, dt);
        float remain = Mathf.Max(0f, ultimateCooldown - ultimateTimer);

        if (ultimateTimer >= ultimateCooldown)
        {
            ultimateTimer = 0f;
            ultimateReady = true;
            ultimateLastLoggedSecond = -1;
        }
    }

    private void PlayIdleState()
    {
        if (anim == null) return;
        string[] candidates = new string[] { idleState, "idle", "Idle", "Roman-idle", "idle-K" };
        foreach (var s in candidates)
        {
            if (string.IsNullOrEmpty(s)) continue;
            int hash = Animator.StringToHash(s);
            if (anim.HasState(0, hash))
            {
                anim.Play(hash, 0, 0f);
                return;
            }
        }
    }
}