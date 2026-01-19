using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

    private Rigidbody2D body;
    public BoxCollider2D boxCollider;
    public Animator anim;

    public bool isCrouching = false;
    private float horizontalInput;
    private bool ground;
                        
    [Header("Attack")]
    public GameObject AttackPoint;
    public float radius;
    public GameObject CrouchAttackPoint;
    public float crouchRadius;
    public LayerMask PlayerLayer;
    public float damage;
    private bool attack;

    private bool canDash = true;
    private bool isDashing = false;
    [SerializeField] private float dashPower = 5f;
    [SerializeField] private float dashTime = 0.1f;
    [SerializeField] private float dashCooldown = 2f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        if (isDashing)
            return;

        if (Input.GetKey(KeyCode.S) && IsGrounded())
            isCrouching = true;
        else
            isCrouching = false;

        if (Input.GetKeyDown(KeyCode.J) && ground && !isCrouching)
        {
            body.velocity = new Vector2(0, body.velocity.y);
            anim.SetBool("run", false);
            anim.SetBool("atk", true);
            attack = true;
            speed = 0;
            Attack();
        }
        else
        {
            if (isCrouching)
            {
                if (Input.GetKeyDown(KeyCode.J) && ground)
                {
                    body.velocity = new Vector2(0, body.velocity.y);
                    anim.SetBool("run", false);
                    anim.SetBool("atk", true);
                    attack = true;
                    speed = 0;
                    CrouchAttack();
                }
                else
                {
                    body.velocity = new Vector2(0, body.velocity.y);
                }
            }
            else
            {
                speed = 20f;
                body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            StartCoroutine(Dash());
        }

        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        if (Input.GetKeyDown(KeyCode.W) && IsGrounded() && !isCrouching)
        {
            Jump();
        }

        anim.SetBool("run", horizontalInput != 0 && ground && !isCrouching);
        anim.SetBool("ground", ground);
        anim.SetBool("crouch", isCrouching);
        anim.SetFloat("yVelocity", body.velocity.y);
    }

    private void Jump()
    {
        body.velocity = new Vector2(body.velocity.x, jumpForce);
        ground = false;
        anim.SetTrigger("jump");
        // Enemy jump sound can be added here if needed
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
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = body.gravityScale;
        body.gravityScale = 0;
        Vector3 dashPosition = transform.position + new Vector3(transform.localScale.x * dashPower, 0f, 0f);
        transform.position = dashPosition;
        tr.emitting = true;
        yield return new WaitForSeconds(dashTime);
        tr.emitting = false;
        body.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void StopAttackAnimation()
    {
        anim.SetBool("atk", false);
        attack = false;
    }

    public void Attack()
    {
        Collider2D[] players = Physics2D.OverlapCircleAll(AttackPoint.transform.position, radius, PlayerLayer);
        foreach (Collider2D player in players)
        {
            Debug.Log("Hit Player");
            player.GetComponent<HealthCh>()?.TakeDamage(damage);
        }
    }

    public void CrouchAttack()
    {
        Collider2D[] players = Physics2D.OverlapCircleAll(CrouchAttackPoint.transform.position, crouchRadius, PlayerLayer);
        foreach (Collider2D player in players)
        {
            Debug.Log("Crouch Hit Player");
            player.GetComponent<HealthCh>()?.TakeDamage(damage * 3);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (AttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(AttackPoint.transform.position, radius);
        }
        if (CrouchAttackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(CrouchAttackPoint.transform.position, crouchRadius);
        }
    }

    private void stopTakingDamage()
    {
        anim.SetBool("hurt", false);
        body.velocity = new Vector2(body.velocity.x, 0);
    }
}
