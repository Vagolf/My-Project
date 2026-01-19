using UnityEngine;

// Simple AI for Dusan: only idle/run to follow a target.
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
    [SerializeField] private float stopDistance = 1.0f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Hurtbox (for receiving damage)")]
    [Tooltip("Auto add a trigger collider so Dusan can be detected by overlap attacks")] 
    [SerializeField] private bool addHurtTrigger = true;
    [SerializeField] private float hurtRadius = 0.6f;
    [SerializeField] private Vector2 hurtOffset = Vector2.zero;

    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private Animator anim;

    private bool ground;
    private CircleCollider2D hurtTrigger;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        if (target == null)
        {
            var t = GameObject.FindGameObjectWithTag(targetTag);
            if (t != null) target = t.transform;
        }

        // Ensure a trigger collider exists for damage detection (OverlapCircle/Box in attackers)
        if (addHurtTrigger)
        {
            hurtTrigger = GetComponent<CircleCollider2D>();
            if (hurtTrigger == null)
            {
                hurtTrigger = gameObject.AddComponent<CircleCollider2D>();
            }
            hurtTrigger.isTrigger = true;
            hurtTrigger.radius = Mathf.Max(0.05f, hurtRadius);
            hurtTrigger.offset = hurtOffset;
        }
    }

    private void Update()
    {
        // optionally block by global gate (reuse from Timer if present)
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

        // Move towards target and flip sprite
        float dir = Mathf.Sign(target.position.x - transform.position.x);
        if (dir > 0.01f) transform.localScale = new Vector3(1, 1, 1);
        else if (dir < -0.01f) transform.localScale = new Vector3(-1, 1, 1);

        float dist = Vector2.Distance(transform.position, target.position);
        if (dist > stopDistance)
        {
            body.velocity = new Vector2(dir * speed, body.velocity.y);
            SetRun(true);
        }
        else
        {
            body.velocity = new Vector2(0f, body.velocity.y);
            SetRun(false);
        }

        // animator params
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
}
