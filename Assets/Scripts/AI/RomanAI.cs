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
    private bool ultimateReady = false; // start on cooldown like Roman
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
    [SerializeField] private int extraJumpsMax = 1; // 1 = double jump
    private int extraJumps;

    [Header("Debug")] 
    [SerializeField] private bool verboseLog = false;
    [Header("Animator States")]
    [Tooltip("Animator state name to play when returning to idle after actions (e.g., 'idle' or 'idle-K')")]
    [SerializeField] private string idleState = "Roman-idle";

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        if (tr == null) tr = GetComponent<TrailRenderer>();
    }

    private void Start()
    {
        // Start with ultimate on cooldown to mirror Roman
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
        // Ensure cooldowns and radii are never zero/negative due to inspector values
        if (normalAttackCooldown < 0.1f) normalAttackCooldown = 0.1f;
        if (crouchAttackCooldown < 0.1f) crouchAttackCooldown = 0.1f;
        if (radius <= 0f) radius = 0.1f;
        if (crouchRadius <= 0f) crouchRadius = 0.1f;
    }

    private void Update()
    {
        // cooldowns
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

        // no safety timeout; rely on animation events for timing

        if (preparingUltimate) { SetRun(false); return; }
        if (inUltimate) { return; }
        if (isDashing) return;
        if (movementLocked)
        {
            // freeze horizontal motion but keep gravity
            body.velocity = new Vector2(0f, body.velocity.y);
            SetRun(false);
            return;
        }

        // Auto use Ultimate when ready and close enough
        if (ultimateReady && ground)
        {
            float distToTarget = Mathf.Abs(target.position.x - transform.position.x);
            if (distToTarget <= detectRange)
            {
                StartCoroutine(PrepareAndStartUltimate());
                return;
            }
        }

        // Movement towards target
        Vector3 attackPos = AttackPoint != null ? AttackPoint.transform.position : transform.position;
        float dist = Vector2.Distance(attackPos, target.position);
        float dir = Mathf.Sign(target.position.x - transform.position.x);

        // flip
        if (dir > 0.01f) transform.localScale = new Vector3(1, 1, 1);
        else if (dir < -0.01f) transform.localScale = new Vector3(-1, 1, 1);

        if (dist > attackRange)
        {
            body.velocity = new Vector2(dir * speed, body.velocity.y);
            SetRun(true);
            // optionally dash to close gap
            if (canDash && dist > attackRange * 2.5f)
                StartCoroutine(Dash());
        }
        else
        {
            // compute in-range flags for normal & crouch
            bool inNormalRange;
            if (AttackPoint != null)
                inNormalRange = Vector2.Distance(AttackPoint.transform.position, target.position) <= radius;
            else
                inNormalRange = Vector2.Distance(transform.position, target.position) <= attackRange;
            bool inCrouchRange;
            if (CrouchAttackPoint != null)
                inCrouchRange = Vector2.Distance(CrouchAttackPoint.transform.position, target.position) <= crouchRadius;
            else
                inCrouchRange = Vector2.Distance(transform.position, target.position) <= attackRange;

            // prefer crouch attack if target is crouching and we are in range & off cooldown
            var playerComp = target.GetComponent<Player>();
            bool targetCrouching = playerComp != null && playerComp.isCrouching;
            bool started = TryAttack(inNormalRange, inCrouchRange, targetCrouching);

            // If we couldn't attack (cooldowns or out of sub-range), keep spacing so AI doesn't stand still forever
            if (!started)
            {
                float distNow = Vector2.Distance(attackPos, target.position);
                // If too close, step back; otherwise keep approaching even while on cooldown
                if (distNow < stopDistance)
                    body.velocity = new Vector2(-dir * speed, body.velocity.y);
                else
                    body.velocity = new Vector2(dir * speed, body.velocity.y);
                SetRun(true);
            }
            else
            {
                // We are going to attack now, stop to play animation cleanly
                body.velocity = new Vector2(0f, body.velocity.y);
                SetRun(false);
            }

            // If not attacking and not explicitly set run false, ensure movement carries on
            if (!attack && !movementLocked)
            {
                float distBand = Vector2.Distance(attackPos, target.position);
                if (distBand > attackRange)
                    body.velocity = new Vector2(dir * speed, body.velocity.y);
                else if (distBand < stopDistance)
                    body.velocity = new Vector2(-dir * speed, body.velocity.y);
                SetRun(true);
            }
        }

        // animator params
        anim.SetBool("ground", ground);
        anim.SetFloat("yVelocity", body.velocity.y);
    }

    private bool TryAttack(bool inNormalRange, bool inCrouchRange, bool targetCrouching)
    {
        // Try crouch attack first if appropriate
        if (((targetCrouching) || (useCrouchOnProximity)) && inCrouchRange && crouchAttackTimer <= 0f)
        {
            StartAttackAnimation(isCrouch:true);
            crouchAttackTimer = crouchAttackCooldown;
            attack = true;
            // rely on animation events to apply damage and end attack
            return true;
        }

        // Fallback to normal attack
        if (inNormalRange && normalAttackTimer <= 0f)
        {
            StartAttackAnimation(isCrouch:false);
            normalAttackTimer = normalAttackCooldown;
            attack = true;
            // rely on animation events to apply damage and end attack
            return true;
        }
        return false;
    }

    // Start attack animation similar to Player: set run false, set crouch if needed, then set atk bool (or trigger if only trigger exists)
    private void StartAttackAnimation(bool isCrouch)
    {
        if (anim == null) return;
        // ensure run is off
        anim.SetBool("run", false);
        if (isCrouch) anim.SetBool("crouch", true);

        bool setBool = HasAnimatorParameter("atk", AnimatorControllerParameterType.Bool);
        bool hasAtkTrigger = HasAnimatorParameter("atk", AnimatorControllerParameterType.Trigger);
        bool hasAttackTrigger = HasAnimatorParameter("attack", AnimatorControllerParameterType.Trigger);
        if (setBool)
        {
            anim.SetBool("atk", true);
        }
        else if (hasAtkTrigger)
        {
            anim.SetTrigger("atk");
        }
        else if (hasAttackTrigger)
        {
            anim.SetTrigger("attack");
        }
        if (verboseLog) Debug.Log($"[AI ATK] StartAttackAnimation {(isCrouch ? "[CROUCH]" : "[NORMAL]")}: setBool={setBool}, atkTrig={hasAtkTrigger}, attackTrig={hasAttackTrigger}");
    }

    private bool HasAnimatorParameter(string name, AnimatorControllerParameterType type)
    {
        if (anim == null) return false;
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }

    private bool HasAnimatorState(string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName)) return false;
        int hash = Animator.StringToHash(stateName);
        return anim.HasState(0, hash);
    }

    private void ClearAttackFlag()
    {
        if (anim != null)
        {
            anim.SetBool("atk", false);
            anim.SetBool("crouch", false);
            Debug.Log($"[AI ATK] ClearAttackFlag. anim.atk={anim.GetBool("atk")}, crouch={anim.GetBool("crouch")}");
        }
        attack = false;
        // Resume chasing if not in attack wind-up
        if (target != null)
        {
            Vector3 attackPos = AttackPoint != null ? AttackPoint.transform.position : transform.position;
            float dist = Vector2.Distance(attackPos, target.position);
            float dir = Mathf.Sign(target.position.x - transform.position.x);
            if (dist > attackRange)
                body.velocity = new Vector2(dir * speed, body.velocity.y);
            else if (dist < stopDistance)
                body.velocity = new Vector2(-dir * speed, body.velocity.y);
            else
                body.velocity = new Vector2(dir * 0.2f * speed, body.velocity.y); // micro adjust while in band
            SetRun(true);
        }
    }

    private void SetRun(bool r)
    {
        if (anim == null) return;
        // Support both 'run' and 'Run' parameter names
        if (HasAnimatorParameter("run", AnimatorControllerParameterType.Bool)) anim.SetBool("run", r);
        if (HasAnimatorParameter("Run", AnimatorControllerParameterType.Bool)) anim.SetBool("Run", r);
    }

    private void Jump()
    {
        ground = false;
        SetRun(false);
        anim.SetBool("crouch", false);
        anim.SetBool("ground", false);
        anim.ResetTrigger("atk");
        body.velocity = new Vector2(body.velocity.x, jumpForce);
        anim.SetTrigger("jump");
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
        float originalGravity = body.gravityScale;
        body.gravityScale = 0f;
        // compute allowed dash distance using casts to avoid passing through walls
        Vector3 startPos = transform.position;
        float dir = transform.localScale.x >= 0 ? 1f : -1f;
        float desiredDistance = Mathf.Abs(dashingPower) * dashnigTime; // approx distance = speed * time
        float allowedDistance = desiredDistance;
        if (dashUseBoxCast && boxCollider != null)
        {
            Vector2 castSize = boxCollider.size;
            RaycastHit2D hit = Physics2D.BoxCast(startPos, castSize, 0f, new Vector2(dir, 0f), desiredDistance, dashBlockLayers);
            if (hit.collider != null)
            {
                allowedDistance = Mathf.Max(0f, hit.distance - dashSkin);
                if (verboseLog) Debug.Log($"[AI DASH] Blocked by {hit.collider.name} -> allow = {allowedDistance:F2}/{desiredDistance:F2}");
            }
        }
        // stop current velocity and warp within allowed distance to avoid tunneling
        body.velocity = Vector2.zero;
        Vector3 target = startPos + new Vector3(dir * allowedDistance, 0f, 0f);
        if (tr) tr.emitting = true;
        transform.position = target;
        if (tr) tr.emitting = true;
        yield return new WaitForSeconds(dashnigTime);
        if (tr) tr.emitting = false;
        body.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }

    // Called by animation event or fallback in TryAttack
    public void Attack()
    {
        if (AttackPoint == null) return;
        float usedDamage = normalAttackDamage;
        var pos = AttackPoint.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius, Opponent);
        foreach (var h in hits)
        {
            var hpEnemy = h.GetComponent<HealthEnemy>();
            if (hpEnemy != null) { hpEnemy.TakeDamage(usedDamage); Debug.Log($"[AI ATK] Normal dealt {usedDamage} to {hpEnemy.name} at {pos}"); continue; }
            var hpPlayer = h.GetComponent<HealthCh>();
            if (hpPlayer != null) { hpPlayer.TakeDamage(usedDamage); Debug.Log($"[AI ATK] Normal dealt {usedDamage} to Player {hpPlayer.name} at {pos}"); }
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
            if (hpEnemy != null) { hpEnemy.TakeDamage(crouchAttackDamage); Debug.Log($"[AI ATK] Crouch dealt {crouchAttackDamage} to {hpEnemy.name} at {pos}"); continue; }
            var hpPlayer = h.GetComponent<HealthCh>();
            if (hpPlayer != null) { hpPlayer.TakeDamage(crouchAttackDamage); Debug.Log($"[AI ATK] Crouch dealt {crouchAttackDamage} to Player {hpPlayer.name} at {pos}"); }
        }
    }

    // ===== Animation Event Receivers (match Roman) =====
    public void OnAttackStart()
    {
        if (anim != null) anim.SetBool("atk", true);
        attack = true;
    }

    public void OnAttackEnd()
    {
        StopAttackAnimation();
        // Safety: force unlock in case animation event NormalAttackUnlockMovement is missing
        movementLocked = false;
        if (verboseLog) Debug.Log("[AI ATK] OnAttackEnd -> force unlock movement");
        // Begin appropriate cooldown after the animation completes
        // Use crouch cooldown if we were crouching during this attack, otherwise normal
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

    // Movement lock during normal attack (optional but keeps parity with Roman)
    private bool movementLocked = false;

    public void NormalAttackLockMovement()
    {
        movementLocked = true;
        if (body != null) body.velocity = new Vector2(0f, body.velocity.y);
    }

    public void NormalAttackUnlockMovement()
    {
        movementLocked = false;
    }

    // Gizmo drawing removed per request

    private IEnumerator PrepareAndStartUltimate()
    {
        preparingUltimate = true;
        body.velocity = new Vector2(0f, body.velocity.y);
        SetRun(false);
        yield return null; // wait 1 frame to settle animator
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
        if (verboseLog) Debug.Log($"[AI ULTI] Damage hits: {hitCount}");
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

        // Force transition back to idle to avoid sticking on the last frame
        PlayIdleState();

        UltimateEventBus.RaiseFinish(transform);
    }

    private void TickUltimateCooldown(float dt)
    {
        if (ultimateReady) return;
        ultimateTimer += Mathf.Max(0f, dt);
        float remain = Mathf.Max(0f, ultimateCooldown - ultimateTimer);
        if (logUltimateCooldown)
        {
            int sec = Mathf.CeilToInt(remain);
            if (sec != ultimateLastLoggedSecond)
            {
                Debug.Log($"[AI ULTI] Cooldown remaining: {remain:F2}s");
                ultimateLastLoggedSecond = sec;
            }
        }
        if (ultimateTimer >= ultimateCooldown)
        {
            ultimateTimer = 0f;
            ultimateReady = true;
            ultimateLastLoggedSecond = -1;
            if (logUltimateCooldown)
                Debug.Log("[AI ULTI] Ready!");
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
                if (verboseLog) Debug.Log($"[AI ULTI] PlayIdleState -> {s}");
                return;
            }
        }
        if (verboseLog) Debug.Log("[AI ULTI] PlayIdleState -> no matching idle state found");
    }
}
