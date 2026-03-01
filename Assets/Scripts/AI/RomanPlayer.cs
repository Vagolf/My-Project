using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RomanPlayer : MonoBehaviour
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

    private Rigidbody2D body;
    public BoxCollider2D boxCollider;
    public Animator anim;

    public bool isCrouching = false;
    private float horizontalInput;
    private bool ground;
    private bool movementLocked = false;

    [Header("Attack (ค่าดาเมจ)")]
    public GameObject AttackPoint;
    public float radius = 1f;
    public GameObject CrouchAttackPoint;
    public float crouchRadius = 1f;
    public LayerMask Enemy;
    public float normalAttackDamage = 70f;
    public float crouchAttackDamage = 180f;
    public float crouchAttackCooldown = 4f;
    private float crouchAttackTimer = 0f;
    private bool attack;
    private List<HealthEnemy> damagedEnemies = new List<HealthEnemy>(); // 🔥 เพิ่ม List นี้มาจำชื่อศัตรูที่โดนไปแล้ว

    // 🔥 เพิ่มส่วนนี้: ตั้งเวลาให้โค้ดสั่งหยุดตีเอง
    [Header("Attack Timing (ระบบนับเวลาในโค้ด)")]
    [Tooltip("เวลาทั้งหมดของแอนิเมชันตีปกติ (วินาที)")]
    [SerializeField] private float normalAttackDuration = 0.4f;
    [Tooltip("จังหวะที่ดาเมจจะออกหลังจากกดตี (วินาที)")]
    [SerializeField] private float normalAttackHitDelay = 0.15f;

    [Tooltip("เวลาทั้งหมดของแอนิเมชันนั่งตี (วินาที)")]
    [SerializeField] private float crouchAttackDuration = 0.5f;
    [Tooltip("จังหวะที่ดาเมจนั่งตีจะออก (วินาที)")]
    [SerializeField] private float crouchAttackHitDelay = 0.2f;

    [Header("Ultimate Skill (Roman)")]
    [SerializeField] private float ultimateCooldown = 8f;
    [SerializeField] private float ultimateDamage = 500f;
    [SerializeField] private float ultimateHitRadiusMultiplier = 2f;
    private float ultimateTimer = 0f;
    private bool ultimateReady = true;
    private bool inUltimate = false;
    private bool preparingUltimate = false;
    [SerializeField] private string ultimateTrigger = "ulti";

    [Header("Ultimate Area")]
    [SerializeField] private bool ultimateUseBox = false;
    [SerializeField] private Vector2 ultimateBoxSize = new Vector2(4f, 2f);
    [SerializeField] private float ultimateBoxAngle = 0f;
    [SerializeField] private Transform UltimateAttackPoint;
    
    public UltimateDamagePoint ultimateDamagePoint;
    private bool ultimateDamageFired = false;

    [Header("Air Jump")]
    [SerializeField] private int extraJumpsMax = 1;
    private int extraJumps;

    public static event Action<RomanPlayer> OnAnyUltimateStart;
    public static event Action<RomanPlayer> OnAnyUltimateFinish;

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
        extraJumps = extraJumpsMax;
        if (tr) tr.emitting = false;
    }

    private void Update()
    {
        if (!ultimateReady) TickUltimateCooldown(Time.deltaTime);

        if (Timer.GateBlocked) { if (anim) anim.SetBool("run", false); return; }

        if (crouchAttackTimer > 0f) crouchAttackTimer -= Time.deltaTime;

        if (preparingUltimate) { anim.SetBool("run", false); return; }
        if (inUltimate || isDashing) return;

        // กดใช้อัลติ
        if (Input.GetKeyDown(KeyCode.K) && ultimateReady && IsGrounded())
        {
            StartCoroutine(PrepareAndStartUltimate());
            return;
        }

        horizontalInput = Input.GetAxis("Horizontal");
        isCrouching = Input.GetKey(KeyCode.S) && IsGrounded();

        // 🔥 ระบบเช็คการกดปุ่มโจมตี (เปลี่ยนมาใช้ Coroutine คุมเวลาแทน)
        if (Input.GetKeyDown(KeyCode.J) && ground && !attack && !isDashing)
        {
            if (!isCrouching)
            {
                StartCoroutine(NormalAttackRoutine());
            }
            else if (crouchAttackTimer <= 0f)
            {
                StartCoroutine(CrouchAttackRoutine());
            }
        }
        else if (!attack && !isDashing) // ถ้าไม่ได้ตีหรือแดช ให้เดินได้ปกติ
        {
            if (!isCrouching)
            {
                speed = 20f;
                body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
            }
            else
            {
                body.velocity = new Vector2(0, body.velocity.y); // นั่งอยู่ให้หยุดเดิน
            }
        }

        if (Input.GetKeyDown(KeyCode.L) && canDash) StartCoroutine(Dash());

        if (horizontalInput > 0.01f) transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < -0.01f) transform.localScale = new Vector3(-1, 1, 1);

        if (Input.GetKeyDown(KeyCode.W) && !isCrouching)
        {
            if (IsGrounded()) Jump();
            else if (extraJumps > 0)
            {
                extraJumps--;
                anim.ResetTrigger("jump");
                Jump();
            }
        }

        anim.SetBool("run", (horizontalInput != 0 && ground && !isCrouching && !attack));
        anim.SetBool("ground", ground);
        anim.SetBool("crouch", isCrouching);
        anim.SetFloat("yVelocity", body.velocity.y);

        if (movementLocked)
        {
            body.velocity = new Vector2(0f, body.velocity.y);
            anim.SetBool("run", false);
        }

    }

    // ==========================================
    // 🔥 Coroutine ควบคุมการโจมตีปกติ
    // ==========================================
    private IEnumerator NormalAttackRoutine()
    {
        damagedEnemies.Clear(); // 👈 เพิ่มบรรทัดนี้: ล้างความจำทุกครั้งที่ง้างดาบใหม่!

        // 1. สั่งเริ่มแอนิเมชันตีและล็อคการขยับ
        anim.SetBool("run", false);
        anim.SetBool("atk", true);
        attack = true;
        movementLocked = true;
        speed = 0f;
        body.velocity = new Vector2(0, body.velocity.y);

        // 2. นับเวลารอจนถึงจังหวะที่ดาบฟันโดนศัตรู
        yield return new WaitForSeconds(normalAttackHitDelay);
        Attack(); // ยิงดาเมจ

        // 3. รอจนกว่าเวลาแอนิเมชันตีจะจบลง
        float remainingTime = Mathf.Max(0f, normalAttackDuration - normalAttackHitDelay);
        yield return new WaitForSeconds(remainingTime);

        // 4. สั่งจบการตี ปลดล็อคสถานะทั้งหมด
        StopAttackAnimation();
    }

    // ==========================================
    // 🔥 Coroutine ควบคุมการนั่งโจมตี
    // ==========================================
    private IEnumerator CrouchAttackRoutine()
    {
        damagedEnemies.Clear(); // 👈 เพิ่มบรรทัดนี้: ล้างความจำทุกครั้งที่ง้างดาบใหม่!

        anim.SetBool("run", false);
        anim.SetBool("atk", true);
        attack = true;
        movementLocked = true;
        speed = 0f;
        body.velocity = new Vector2(0, body.velocity.y);
        crouchAttackTimer = crouchAttackCooldown;

        yield return new WaitForSeconds(crouchAttackHitDelay);
        CrouchAttack(); // ยิงดาเมจ

        float remainingTime = Mathf.Max(0f, crouchAttackDuration - crouchAttackHitDelay);
        yield return new WaitForSeconds(remainingTime);

        StopAttackAnimation();
    }

    // ==========================================
    // ท่าไม้ตาย (Ultimate)
    // ==========================================
    private IEnumerator PrepareAndStartUltimate()
    {
        damagedEnemies.Clear();

        preparingUltimate = true;
        body.velocity = new Vector2(0f, body.velocity.y);
        anim.SetBool("run", false);

        // 🔥 แก้ตรงนี้: เปลี่ยนมาหยุดเฉพาะ Coroutine ตี ห้ามใช้ StopAllCoroutines() เด็ดขาด!
        StopCoroutine(nameof(NormalAttackRoutine));
        StopCoroutine(nameof(CrouchAttackRoutine));
        StopAttackAnimation();

        yield return null;

        inUltimate = true;
        preparingUltimate = false;
        ultimateReady = false;
        ultimateTimer = 0f;
        ultimateDamageFired = false;
        body.velocity = Vector2.zero;
        Time.timeScale = 0f;
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (!string.IsNullOrEmpty(ultimateTrigger)) anim.SetTrigger(ultimateTrigger);
        OnAnyUltimateStart?.Invoke(this);
        UltimateEventBus.RaiseStart(transform);
        if (ultimateDamagePoint != null) ultimateDamagePoint.gameObject.SetActive(true);
    }

    public void StopAttackAnimation()
    {
        if (anim != null) anim.SetBool("atk", false);
        attack = false;
        movementLocked = false;
        speed = 20f;
    }

    private void Jump()
    {
        ground = false;
        anim.SetBool("run", false);
        anim.SetBool("crouch", false);
        anim.SetBool("ground", false);
        anim.ResetTrigger("atk");

        // ถ้ายกเลิกตีด้วยการกระโดด ให้ยกเลิก Coroutine ตีด้วย
        StopAllCoroutines();
        StopAttackAnimation();

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
        if (collision.gameObject.CompareTag("Ground")) { ground = true; extraJumps = extraJumpsMax; }
    }

    private IEnumerator Dash()
    {
        canDash = false; isDashing = true;

        // ยกเลิกการโจมตีถ้าเผลอกด Dash แคนเซิลท่า
        StopCoroutine(nameof(NormalAttackRoutine));
        StopCoroutine(nameof(CrouchAttackRoutine));
        StopAttackAnimation();

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
        Collider2D[] enemy = Physics2D.OverlapCircleAll(AttackPoint.transform.position, radius, Enemy);
        foreach (Collider2D e in enemy)
        {
            if (e.gameObject == this.gameObject || e.transform.IsChildOf(this.transform)) continue;

            var hp = e.GetComponent<HealthEnemy>() ?? e.GetComponentInParent<HealthEnemy>();

            // 🔥 เช็คว่ามีสคริปต์เลือด และ "ยังไม่เคยโดนดาเมจในดาบนี้" ใช่ไหม?
            if (hp != null && !damagedEnemies.Contains(hp))
            {
                hp.TakeDamage(normalAttackDamage);
                damagedEnemies.Add(hp); // จำชื่อเข้า List ไว้ จะได้ไม่โดนเบิ้ลอีก
            }
        }
    }

    // ==========================================
    // นั่งโจมตี (อัปเกรด: กันจุดบอด + ปริ้นต์เช็คดาเมจ)
    // ==========================================
    public void CrouchAttack()
    {
        // 🔥 ถ้าไม่ได้ลากใส่ช่อง หรือจุดมันพัง ให้สลับไปใช้ AttackPoint ปกติแทน
        Transform hitPoint = transform; // ค่าเริ่มต้น (ตัวผู้เล่น)
        if (CrouchAttackPoint != null) hitPoint = CrouchAttackPoint.transform;
        else if (AttackPoint != null) hitPoint = AttackPoint.transform;

        Collider2D[] enemy = Physics2D.OverlapCircleAll(hitPoint.position, crouchRadius, Enemy);

        // (ดูใน Console) แจ้งเตือนว่าจุดตีอยู่ไหน และกวาดวงกลมไปโดนใครบ้างไหม
        Debug.Log($"[Crouch] ปล่อยดาเมจที่จุด: {hitPoint.name} เจอวัตถุในระยะ {enemy.Length} ชิ้น");

        foreach (Collider2D e in enemy)
        {
            if (e.gameObject == this.gameObject || e.transform.IsChildOf(this.transform)) continue;

            var hp = e.GetComponent<HealthEnemy>() ?? e.GetComponentInParent<HealthEnemy>();

            if (hp != null && !damagedEnemies.Contains(hp))
            {
                hp.TakeDamage(crouchAttackDamage);
                damagedEnemies.Add(hp);
                Debug.Log($"[Crouch] นั่งฟันโดน! ลดเลือดศัตรูไป {crouchAttackDamage}");
            }
        }
    }

    // ==========================================
    // ท่าไม้ตาย (อัปเกรด: ตีเข้าทุกสคริปต์เลือด + เช็คดาเมจ)
    // ==========================================
    public void UltimateDamageEvent()
    {
        if (AttackPoint == null && UltimateAttackPoint == null) return;
        if (!ultimateDamageFired)
        {
            ultimateDamageFired = true;
            UltimateEventBus.RaiseDamage(transform);
        }

        // หาจุดศูนย์กลางของอัลติ
        Vector2 center = UltimateAttackPoint != null ? (Vector2)UltimateAttackPoint.position : (Vector2)AttackPoint.transform.position;

        Collider2D[] enemies;
        if (ultimateUseBox)
        {
            enemies = Physics2D.OverlapBoxAll(center, ultimateBoxSize, ultimateBoxAngle, Enemy);
        }
        else
        {
            float hitRadius = radius * ultimateHitRadiusMultiplier;
            enemies = Physics2D.OverlapCircleAll(center, hitRadius, Enemy);
        }

        // 🔥 แจ้งเตือนใน Console ว่าจุดอันติอยู่ตรงไหน กวาดโดนวัตถุไหม?
        Debug.Log($"[Ultimate] อันติฟาดลงที่: {center} เจอเป้าหมายในระยะ {enemies.Length} ชิ้น");

        foreach (var e in enemies)
        {
            if (e.gameObject == this.gameObject || e.transform.IsChildOf(this.transform)) continue;

            // 1. ลองเช็คหา HealthEnemy (ถ้า Kaisa เป็น AI สมบูรณ์)
            var hpE = e.GetComponent<HealthEnemy>() ?? e.GetComponentInParent<HealthEnemy>();
            if (hpE != null && !damagedEnemies.Contains(hpE))
            {
                hpE.TakeDamage(ultimateDamage); // 🔥 เปลี่ยนมาใช้ TakeDamage ปกติชัวร์กว่า
                damagedEnemies.Add(hpE);
                Debug.Log($"[Ultimate] อันติเข้าเป้า! ลดเลือด HealthEnemy ไป {ultimateDamage}");
                continue; // โดนแล้วข้ามไปตัวอื่นเลย
            }

            // 2. ลองเช็คหา HealthCh (เผื่อ Kaisa ยังแอบใช้สคริปต์เลือดของผู้เล่นอยู่)
            var hpC = e.GetComponent<HealthCh>() ?? e.GetComponentInParent<HealthCh>();
            if (hpC != null)
            {
                hpC.TakeDamage(ultimateDamage); // 🔥 ใช้ TakeDamage ปกติ
                Debug.Log($"[Ultimate] อันติเข้าเป้า! ลดเลือด HealthCh ไป {ultimateDamage}");
            }
        }
    }

    public void UltimateFinishEvent()
    {
        Time.timeScale = 1f;
        anim.updateMode = AnimatorUpdateMode.Normal;

        // 🔥 สำคัญมาก: ปลดล็อคทุกสถานะเพื่อป้องกันตัวค้างเดินไม่ได้
        inUltimate = false;
        preparingUltimate = false;
        attack = false;
        movementLocked = false;
        isDashing = false;
        speed = 20f;

        if (ultimateDamagePoint != null && ultimateDamagePoint.gameObject.activeSelf)
            ultimateDamagePoint.gameObject.SetActive(false);

        anim.ResetTrigger(ultimateTrigger);
        anim.SetBool("run", false);
        anim.SetBool("atk", false);
        anim.SetBool("crouch", false);
        anim.SetFloat("yVelocity", 0f);

        anim.Play("Roman-idle", 0, 0f);
        OnAnyUltimateFinish?.Invoke(this);
        UltimateEventBus.RaiseFinish(transform);
    }

    private void TickUltimateCooldown(float dt)
    {
        ultimateTimer += Mathf.Max(0f, dt);
        if (ultimateTimer >= ultimateCooldown) { ultimateTimer = 0f; ultimateReady = true; }
    }
    // ==========================================
    // ตัวช่วยวาดเส้นวงกลมโจมตีในหน้าต่าง Scene (ให้เรากะระยะได้ง่ายๆ)
    // ==========================================
    private void OnDrawGizmosSelected()
    {
        // วาดวงกลมตีปกติ (สีแดง)
        if (AttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(AttackPoint.transform.position, radius);
        }

        // วาดวงกลมนั่งตี (สีน้ำเงิน)
        if (CrouchAttackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(CrouchAttackPoint.transform.position, crouchRadius);
        }

        // 🔥 วาดวงกลม/กล่อง อัลติ (สีเหลือง)
        Vector2 center = UltimateAttackPoint != null ? (Vector2)UltimateAttackPoint.position : (AttackPoint != null ? (Vector2)AttackPoint.transform.position : (Vector2)transform.position);
        Gizmos.color = Color.yellow;
        if (ultimateUseBox)
        {
            Gizmos.DrawWireCube(center, ultimateBoxSize);
        }
        else
        {
            Gizmos.DrawWireSphere(center, radius * ultimateHitRadiusMultiplier);
        }
    }

    // ==========================================
    // Animation Event Receivers (เพิ่มไว้เพื่อกัน Error แดงใน Console)
    // ==========================================
    public void OnAttackStart() { }
    public void OnAttackEnd() { }
    public void NormalAttackLockMovement() { }
    public void NormalAttackUnlockMovement() { }
    public void StartUltimate() { } // 👈 เพิ่มบรรทัดนี้เข้าไป ป้องกัน Error ตอนกด K
}