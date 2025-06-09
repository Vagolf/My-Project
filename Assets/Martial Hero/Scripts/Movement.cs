using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

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

    private bool canDash = true;
    private bool isDashing = false;
    private bool isCrouching = false;
    private float horizontalInput;
    private bool ground;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isDashing) return;

        // Move left-right
        horizontalInput = Input.GetAxis("Horizontal");
        body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);

        // Flip player
        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(10, 10, 1);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-10, 10, 1);

        // Jump (W)
        if (Input.GetKeyDown(KeyCode.W) && IsGrounded() && !isCrouching)
        {
            Jump();
        }

        // Dash (L)
        if (Input.GetKeyDown(KeyCode.L) && canDash)
        {
            StartCoroutine(Dash());
        }

        // Crouch (S)
        if (Input.GetKey(KeyCode.S) && IsGrounded())
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
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


    private IEnumerator Dash()
    {
        dashSoundEffect?.Play();
        canDash = false;
        isDashing = true;

        float originalGravity = body.gravityScale;
        body.gravityScale = 0;
        tr.emitting = true;

        // Move instantly in facing direction
        Vector3 dashDirection = new Vector3(transform.localScale.x * dashPower, 0f, 0f);
        transform.position += dashDirection;

        yield return new WaitForSeconds(dashTime);

        tr.emitting = false;
        body.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
