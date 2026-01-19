using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System; // added for Action

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

    [Header("Dash Settings")]
    [SerializeField] private float dashingPower = 100f;
    [SerializeField] private float dashnigTime = 0.2f;
    [SerializeField] private float dashingCooldown = 1f;
    private bool canDash = true;
    private bool isDashing;

    [Header("Sound")] 
    [SerializeField] private AudioSource jumpSoundEffect;
    [SerializeField] private AudioSource dashSoundEffect;

    private Rigidbody2D body;
    public BoxCollider2D boxCollider;
    public Animator anim;

    public bool isCrouching = false;
    private float horizontalInput;
    private bool ground;

    [Header("Attack")]
    public GameObject AttackPoint;
    public float radius = 1f;
    public GameObject CrouchAttackPoint;
    public float crouchRadius = 1f;
    public LayerMask Enemy;
    [Header("Damage Values")] public float damage = 100f; // legacy default
    public float normalAttackDamage = 70f;
    public float crouchAttackDamage = 180f;
    public float crouchAttackCooldown = 4f;
    private float crouchAttackTimer = 0f;
    private bool attack;

    [Header("Ultimate Skill")]
    [SerializeField] private float ultimateCooldown = 8f;
    [SerializeField] private float ultimateDamage = 500f;
    [SerializeField] private float ultimateHitRadiusMultiplier = 2f;
    private float ultimateTimer = 0f;
    private bool ultimateReady = true;
    private bool inUltimate = false;
    private bool preparingUltimate = false; // สถานะใหม่สำหรับเฟสเตรียมก่อนเริ่ม Ultimate
    [SerializeField] private bool logUltimateCooldown = true;
    private int ultimateLastLoggedSecond = -1;

    [Header("Ultimate Animation")]
    [SerializeField] private string ultimateTrigger = " ulti ";

    [Header("Ultimate Warp On Hit")] // วาร์ปตอนเฟรมดาเมจ
    [SerializeField] private bool enableUltimateHitWarp = true;
    [SerializeField] private float ultimateHitWarpDistance = 5f;
    [SerializeField] private bool ultimateHitWarpUseTrail = true;
    [SerializeField] private bool ultimateWarpOnActivate = false; // วาร์ปทันทีตอนกดอัลติ (ไม่รอ damage event)
    [SerializeField] private bool ultimateWarpUseBoxCast = true; // NEW: ใช้ BoxCast กันทะลุกำแพง (แทน Raycast)
    private bool ultimateHitWarpDone = false;
    private bool ultimateDamageFired = false; // added missing declaration

    [Header("Ultimate Warp Collision")]
    [Tooltip("Layers ที่กันไม่ให้วาร์ปทะลุ")] [SerializeField] private LayerMask ultimateWarpBlockLayers;
    [Tooltip("ลดระยะก่อนชนกำแพง (offset)")] [SerializeField] private float ultimateWarpSkin = 0.1f;
    [Tooltip("ใช้ขอบ hitbox ของ player (ครึ่งกว้าง) ป้องกันฝังผนัง")] [SerializeField] private bool ultimateWarpUseColliderBounds = true;

    [Header("Ultimate Warp Effects")]
    [SerializeField] private GameObject ultimateWarpStartEffect;
    [SerializeField] private GameObject ultimateWarpEndEffect;
    [SerializeField] private bool spawnEffectsInUnscaledTime = true;
    [SerializeField] private Vector2 effectOffset = Vector2.zero;

    [Header("Debug")]
    [SerializeField] private bool verboseUltimateLog = true; // toggle log รายละเอียด
    private bool lastRunAnimValue = false; // ตรวจจับการเปลี่ยนค่า run

    // Events for camera / other systems
    public static event Action<Player> OnAnyUltimateStart; // fired when any player starts ultimate
    public static event Action<Player> OnAnyUltimateFinish; // fired when any player finishes ultimate
    public static event Action<Player> OnAnyUltimateDamage; // fired first time ultimate damage event happens (for camera revert)

    [Header("Ultimate Damage Point")]
    [Tooltip("อ้างอิง UltimateDamagePoint (trigger) ถ้ามี เพื่อเปิดตอนอัลติ")] public UltimateDamagePoint ultimateDamagePoint;

    private Vector2 _origColSize; // original size cache
    private Vector2 _origColOffset; // original offset cache
    private bool _origColIsTrigger; // original trigger state
    private bool _colliderModifiedViaEvent; // flag to track modification state

    [Header("Ultimate Box Area")] 
    [SerializeField] private bool ultimateUseBox = false;
    [SerializeField] private Vector2 ultimateBoxSize = new Vector2(4f, 2f);
    [SerializeField] private float ultimateBoxAngle = 0f;

    [Header("Ultimate Attack Point")]
    [SerializeField] private Transform UltimateAttackPoint; // optional separate point for ultimate AOE

    [Header("Air Jump")]
    [SerializeField] private int extraJumpsMax = 1; // 1 = double jump
    private int extraJumps;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        ultimateReady = true;
        inUltimate = false;
        preparingUltimate = false;
        if (verboseUltimateLog) Debug.Log("[ULTI] Init complete");
        extraJumps = extraJumpsMax; // init extra jumps
        if (tr) tr.emitting = false; // disable trail by default
    }

    private void Update()
    {
        // Always tick ultimate cooldown even during gate
        if (!ultimateReady)
            TickUltimateCooldown(Time.deltaTime);
        // Block all player logic while global gate is active (e.g., countdown running)
        if (Timer.GateBlocked)
        {
            if (anim) anim.SetBool("run", false);
            return;
        }
        // cooldown update for crouch attack
        if (crouchAttackTimer > 0f)
            crouchAttackTimer -= Time.deltaTime;

        if (preparingUltimate)
        {
            // ระหว่างเตรียม Ultimate บังคับ run = false และไม่รับ input
            if (verboseUltimateLog) Debug.Log("[ULTI] Preparing phase... holding state");
            anim.SetBool("run", false);
            return;
        }
        // ระหว่าง Ultimate หรือกำลัง Dash ไม่รับ input อื่น
        if (inUltimate)
        {
            if (verboseUltimateLog) Debug.Log("[ULTI] In ultimate - input blocked");
            return;
        }
        if (isDashing) return;

        // Ultimate Input (กด K + อยู่บนพื้น + cooldown เสร็จ)
        if (Input.GetKeyDown(KeyCode.K) && ultimateReady && IsGrounded())
        {
            if (verboseUltimateLog) Debug.Log($"[ULTI] K pressed. run={anim.GetBool("run")}, velX={body.velocity.x:F3}, grounded={ground}");
            StartCoroutine(PrepareAndStartUltimate());
            return;
        }

        // Move left-right
        horizontalInput = Input.GetAxis("Horizontal");

        // Crouch
        isCrouching = Input.GetKey(KeyCode.S) && IsGrounded();

        // Attack (J)
        if (Input.GetKeyDown(KeyCode.J) && ground && !isCrouching && !attack && !isDashing)
        {
            body.velocity = new Vector2(0, body.velocity.y);
            anim.SetBool("run", false);
            anim.SetBool("atk", true);
            attack = true;
            speed = 0;
        }
        else
        {
            if (isCrouching)
            {
                if (Input.GetKeyDown(KeyCode.J) && ground && !attack && !isDashing)
                {
                    if (crouchAttackTimer <= 0f)
                    {
                        body.velocity = new Vector2(0, body.velocity.y);
                        anim.SetBool("run", false);
                        anim.SetBool("atk", true);
                        attack = true;
                        speed = 0;
                        crouchAttackTimer = crouchAttackCooldown;
                    }
                }
                else if (!isDashing)
                {
                    body.velocity = new Vector2(0, body.velocity.y);
                }
            }
            else if (!isDashing)
            {
                speed = 20f;
                body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
            }
        }

        // Dash (L)
        if (Input.GetKeyDown(KeyCode.L) && canDash)
            StartCoroutine(Dash());

        // Flip
        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        // Simple ground jump only
        if (Input.GetKeyDown(KeyCode.W) && !isCrouching)
        {
            if (IsGrounded())
            {
                Jump();
            }
            else if (extraJumps > 0)
            {
                extraJumps--;
                // ensure animation can re-trigger
                anim.ResetTrigger("jump");
                // reuse Jump() to keep animation/params consistent
                Jump();
            }
        }

        // Animator update
        bool desiredRun = (horizontalInput != 0 && ground && !isCrouching);
        anim.SetBool("run", desiredRun);
        if (desiredRun != lastRunAnimValue)
        {
            if (verboseUltimateLog) Debug.Log($"[ANIM] run set -> {desiredRun} (velX={body.velocity.x:F2})");
            lastRunAnimValue = desiredRun;
        }
        anim.SetBool("ground", ground);
        anim.SetBool("crouch", isCrouching);
        anim.SetFloat("yVelocity", body.velocity.y);

        // cooldown already processed above

        // Block all player control while movement is locked by normal attack
        if (movementLocked)
        {
            // freeze horizontal speed but keep gravity
            body.velocity = new Vector2(0f, body.velocity.y);
            anim.SetBool("run", false);
            // ignore further input this frame
            return;
        }
    }

    private void StartUltimate()
    {
        if (verboseUltimateLog) Debug.Log("[ULTI] StartUltimate called");
        inUltimate = true;
        preparingUltimate = false;
        ultimateReady = false;
        ultimateTimer = 0f;
        ultimateLastLoggedSecond = -1;
        ultimateHitWarpDone = false; // reset warp flag
        ultimateDamageFired = false; // reset damage flag
        if (logUltimateCooldown) Debug.Log("[ULTI] Activated - entering time stop");

        body.velocity = Vector2.zero;
        Time.timeScale = 0f;

        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (!string.IsNullOrEmpty(ultimateTrigger))
            anim.SetTrigger(ultimateTrigger);

        // Fire global event
        OnAnyUltimateStart?.Invoke(this);
        UltimateEventBus.RaiseStart(transform);

        if (ultimateDamagePoint != null)
            ultimateDamagePoint.gameObject.SetActive(true);

        // NEW: warp immediately if option enabled
        if (enableUltimateHitWarp && ultimateWarpOnActivate)
        {
            if (verboseUltimateLog) Debug.Log("[ULTI] Immediate warp on activate");
            PerformUltimateWarp();
        }
    }

    public void UltimateDamageEvent()
    {
        if (AttackPoint == null && UltimateAttackPoint == null) return;

        if (!ultimateDamageFired)
        {
            ultimateDamageFired = true;
            OnAnyUltimateDamage?.Invoke(this); // notify camera to restore
            UltimateEventBus.RaiseDamage(transform);
            if (verboseUltimateLog) Debug.Log("[ULTI] OnAnyUltimateDamage invoked");
        }

        // center of ultimate hit
        Vector2 center = UltimateAttackPoint != null ? (Vector2)UltimateAttackPoint.position : (Vector2)AttackPoint.transform.position;

        int hitCount = 0;
        if (ultimateUseBox)
        {
            // Rectangular AOE
            Collider2D[] enemies = Physics2D.OverlapBoxAll(center, ultimateBoxSize, ultimateBoxAngle, Enemy);
            foreach (var e in enemies)
            {
                var hp = e.GetComponent<HealthEnemy>();
                if (hp == null) hp = e.GetComponentInParent<HealthEnemy>();
                if (hp != null)
                {
                    hp.TakeDamageUltimate(ultimateDamage);
                    hitCount++;
                }
            }
        }
        else
        {
            // Circular AOE
            float hitRadius = radius * ultimateHitRadiusMultiplier;
            Collider2D[] enemies = Physics2D.OverlapCircleAll(center, hitRadius, Enemy);
            foreach (var e in enemies)
            {
                var hp = e.GetComponent<HealthEnemy>();
                if (hp == null) hp = e.GetComponentInParent<HealthEnemy>();
                if (hp != null)
                {
                    hp.TakeDamageUltimate(ultimateDamage);
                    hitCount++;
                }
            }
        }
        if (logUltimateCooldown) Debug.Log($"[ULTI] Damage Event executed. Hits: {hitCount}");
    }

    // Animation Event: perform warp separately (call before damage event if needed)
    public void UltimateWarpEvent()
    {
        if (!enableUltimateHitWarp) return;
        if (ultimateHitWarpDone)
        {
            if (verboseUltimateLog) Debug.Log("[ULTI] UltimateWarpEvent skipped (already warped)");
            return;
        }
        PerformUltimateWarp();
    }

    private void PerformUltimateWarp()
    {
        ultimateHitWarpDone = true;
        if (!enableUltimateHitWarp)
            return;

        Vector3 startPos = transform.position;
        SpawnWarpEffect(ultimateWarpStartEffect, startPos + (Vector3)effectOffset, "start");

        float direction = transform.localScale.x >= 0 ? 1f : -1f;
        float halfWidthExtra = 0.0f;
        if (ultimateWarpUseColliderBounds && boxCollider != null)
            halfWidthExtra = boxCollider.bounds.extents.x;

        Vector2 dir = new Vector2(direction, 0f);
        float desiredDistance = ultimateHitWarpDistance;
        float allowedDistance = desiredDistance; // will adjust by cast

        if (ultimateWarpUseBoxCast && boxCollider != null)
        {
            // BoxCast using local collider size to detect obstacles
            Vector2 castSize = boxCollider.size; // size in local space (BoxCast uses size, not bounds)
            RaycastHit2D hit = Physics2D.BoxCast(startPos, castSize, 0f, dir, desiredDistance + halfWidthExtra, ultimateWarpBlockLayers);
            if (hit.collider != null && hit.collider != boxCollider)
            {
                allowedDistance = hit.distance - ultimateWarpSkin;
                if (allowedDistance < 0f) allowedDistance = 0f;
                if (verboseUltimateLog) Debug.Log($"[ULTI] BoxCast blocked by {hit.collider.name} dist={hit.distance:F2} -> allow={allowedDistance:F2}");
            }
            else if (verboseUltimateLog)
            {
                Debug.Log($"[ULTI] BoxCast path clear dist={desiredDistance:F2}");
            }
        }
        else
        {
            // fallback Raycast (original logic refined)
            float maxDistance = desiredDistance + halfWidthExtra;
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, maxDistance, ultimateWarpBlockLayers);
            if (hit.collider != null)
            {
                allowedDistance = hit.distance - ultimateWarpSkin - halfWidthExtra;
                if (allowedDistance < 0f) allowedDistance = 0f;
                if (verboseUltimateLog) Debug.Log($"[ULTI] Raycast blocked by {hit.collider.name} -> allow={allowedDistance:F2}");
            }
            else if (verboseUltimateLog) Debug.Log($"[ULTI] Raycast free path dist={desiredDistance:F2}");
        }

        Vector3 target = startPos + new Vector3(direction * allowedDistance, 0f, 0f);

        if (ultimateHitWarpUseTrail && tr != null)
        {
            bool prev = tr.emitting;
            tr.emitting = true;
            tr.Clear();
            transform.position = target;
            tr.emitting = prev;
        }
        else
        {
            transform.position = target;
        }

        SpawnWarpEffect(ultimateWarpEndEffect, target + (Vector3)effectOffset, "end");
        if (verboseUltimateLog) Debug.Log($"[ULTI] Warp executed to {target} (allowedDist={allowedDistance:F2})");
    }

    private void SpawnWarpEffect(GameObject prefab, Vector3 position, string phase)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, position, Quaternion.identity);
        if (spawnEffectsInUnscaledTime)
        {
            // ถ้า effect มี ParticleSystem ให้ใช้ unscaledTime
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.useUnscaledTime = true;
            }
        }
        if (verboseUltimateLog) Debug.Log($"[ULTI] Spawn effect {prefab.name} ({phase})");
    }

    public void UltimateFinishEvent()
    {
        if (verboseUltimateLog) Debug.Log("[ULTI] UltimateFinishEvent called");
        Time.timeScale = 1f;
        anim.updateMode = AnimatorUpdateMode.Normal;
        inUltimate = false;

        anim.ResetTrigger(ultimateTrigger);
        anim.SetBool("run", false);
        anim.SetBool("atk", false);
        anim.SetBool("crouch", false);
        anim.SetFloat("yVelocity", 0f);
        anim.Play("idle-K", 0, 0f);
        if (logUltimateCooldown) Debug.Log("[ULTI] Finished and returned to idle");

        OnAnyUltimateFinish?.Invoke(this);
        UltimateEventBus.RaiseFinish(transform);

    }


    private void Jump()
    {
        // force leave idle/run immediately
        ground = false;
        anim.SetBool("run", false);
        anim.SetBool("crouch", false);
        anim.SetBool("ground", false);
        anim.ResetTrigger("atk");

        // apply jump
        body.velocity = new Vector2(body.velocity.x, jumpForce);
        anim.SetTrigger("jump");
    }

    private bool IsGrounded()
    {
        ground = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        return ground;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            ground = true;
            extraJumps = extraJumpsMax; // reset double jump on landing
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
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

    public void OnAttackStart() { anim.SetBool("atk", true); attack = true; }
    public void OnAttackEnd() { StopAttackAnimation(); }
    public void StopAttackAnimation() { anim.SetBool("atk", false); attack = false; speed = 20f; }

    public void Attack()
    {
        if (AttackPoint == null) return;
        float usedDamage = normalAttackDamage; // normal standing
        Collider2D[] enemy = Physics2D.OverlapCircleAll(AttackPoint.transform.position, radius, Enemy);
        foreach (Collider2D enemyGameobject in enemy)
        {
            var hp = enemyGameobject.GetComponent<HealthEnemy>() ?? enemyGameobject.GetComponentInParent<HealthEnemy>();
            if (hp != null) hp.TakeDamage(usedDamage);
        }
    }

    public void CrouchAttack()
    {
        if (CrouchAttackPoint == null) return;
        Collider2D[] enemy = Physics2D.OverlapCircleAll(CrouchAttackPoint.transform.position, crouchRadius, Enemy);
        foreach (Collider2D enemyGameobject in enemy)
        {
            var hp = enemyGameobject.GetComponent<HealthEnemy>() ?? enemyGameobject.GetComponentInParent<HealthEnemy>();
            if (hp != null) hp.TakeDamage(crouchAttackDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // base attack point (red)
        if (AttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(AttackPoint.transform.position, radius);
        }

        // ultimate point center
        Vector3 ultiCenter = (UltimateAttackPoint != null)
            ? UltimateAttackPoint.position
            : (AttackPoint != null ? AttackPoint.transform.position : transform.position);

        if (ultimateUseBox)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(ultiCenter, ultimateBoxSize);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ultiCenter, radius * ultimateHitRadiusMultiplier);
        }

        if (CrouchAttackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(CrouchAttackPoint.transform.position, crouchRadius);
        }
    }

    public void DebugAnimatorParameters()
    {
#if UNITY_EDITOR
        if (anim == null || anim.runtimeAnimatorController == null) return;
        foreach (var p in anim.parameters)
            Debug.Log($"Param: {p.name} ({p.type})");
#endif
    }

    public bool isUltimating { get { return inUltimate; } }

    private IEnumerator PrepareAndStartUltimate()
    {
        if (verboseUltimateLog) Debug.Log("[ULTI] PrepareAndStartUltimate() entered");
        preparingUltimate = true;
        body.velocity = new Vector2(0f, body.velocity.y);
        anim.SetBool("run", false);
        // Log state snapshot
        if (verboseUltimateLog) Debug.Log($"[ULTI] Pre-Yield State: velX={body.velocity.x:F3}, grounded={ground}, runParam={anim.GetBool("run")}");
        // รอ 1 เฟรมให้ Animator เคลียร์ transition วิ่ง
        yield return null;
        if (verboseUltimateLog) Debug.Log($"[ULTI] Post-Yield State: runParam={anim.GetBool("run")}, yVel={body.velocity.y:F3}");
        StartUltimate();
    }

    [Header("Movement Lock (Normal Attack)")]
    [SerializeField] private bool logMovementLock = true;
    private bool movementLocked = false;

    // Animation Event: lock movement during normal attack animation
    public void NormalAttackLockMovement()
    {
        movementLocked = true;
        // stop horizontal motion immediately
        if (body != null) body.velocity = new Vector2(0f, body.velocity.y);
        if (logMovementLock) Debug.Log("[ATK] Movement locked");
    }

    // Animation Event: unlock movement after normal attack animation
    public void NormalAttackUnlockMovement()
    {
        movementLocked = false;
        if (logMovementLock) Debug.Log("[ATK] Movement unlocked");
    }

    private void TickUltimateCooldown(float dt)
    {
        ultimateTimer += Mathf.Max(0f, dt);
        float remain = Mathf.Max(0f, ultimateCooldown - ultimateTimer);
        if (logUltimateCooldown)
        {
            int sec = Mathf.CeilToInt(remain);
            if (sec != ultimateLastLoggedSecond)
            {
                Debug.Log($"[ULTI] Cooldown remaining: {remain:F2}s");
                ultimateLastLoggedSecond = sec;
            }
        }
        if (ultimateTimer >= ultimateCooldown)
        {
            ultimateTimer = 0f;
            ultimateReady = true;
            ultimateLastLoggedSecond = -1;
            if (logUltimateCooldown)
                Debug.Log("[ULTI] Ready!");
        }
    }
    

}