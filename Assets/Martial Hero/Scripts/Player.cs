using SupanthaPaul;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

    //now it's can't dash, but you can add it back if you want
    [Header("Dash Settings")]
    [SerializeField] private float dashPower = 5f;
    [SerializeField] private float dashTime = 0.1f;
    [SerializeField] private float dashCooldown = 2f;

    [Header("Sound")]
    [SerializeField] private AudioSource jumpSoundEffect;
    [SerializeField] private AudioSource dashSoundEffect;

    private Rigidbody2D body;
    public BoxCollider2D boxCollider;
    public Animator anim;

    //private bool canDash = true;
    //private bool isDashing = false;
    public bool isCrouching = false;
    private float horizontalInput;
    private bool ground;

    [Header("Attack")]
    public GameObject AttackPoint;
    public float radius;
    public LayerMask Enemy;
    public float damage;

    private bool attack;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        // Move left-right
        horizontalInput = Input.GetAxis("Horizontal");

        // Crouch (S)
        if (Input.GetKey(KeyCode.S) && IsGrounded())
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }

        // Attack (J)
        if (Input.GetKeyDown(KeyCode.J) && !isCrouching && ground)
        {
            // หยุดเดินทันที
            body.velocity = new Vector2(0, body.velocity.y);
            anim.SetBool("run", false);
            anim.SetBool("atk", true);
            Attack();
            attack = true;
            speed = 0; // Stop moving while attacking
        }
        else
        {
            // Prevent running if crouching
            if (isCrouching)
            {
                body.velocity = new Vector2(0, body.velocity.y);
            }
            else
            {
                speed = 20f; // Reset speed when not crouching or attacking
                body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
            }
        }

        // Flip player (A, D)
        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(10, 10, 1);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-10, 10, 1);

        // Jump (W)
        if (Input.GetKeyDown(KeyCode.W) && IsGrounded() && !isCrouching)
        {
            Jump();
        }

        // Animator update
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
        jumpSoundEffect?.Play();
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

    public void StopAttackAnimation()
    {
        anim.SetBool("atk", false);
        attack = false;
    }

    public void Attack()
    {
        Collider2D[] enemy = Physics2D.OverlapCircleAll(AttackPoint.transform.position, radius, Enemy);
        foreach (Collider2D enemyGameobject in enemy)
        {
            Debug.Log("Hit Enemy");
            enemyGameobject.GetComponent<HealthEnemy>().startingHealth -= damage; // Assuming Enemy script has TakeDamage method
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (AttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(AttackPoint.transform.position, radius);
        }
    }
}
