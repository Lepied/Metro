using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float wallJumpForce = 12f;
    public float gravityScale = 2f;
    public float wallSlideGravityScale = 0.5f;
    public float wallJumpControlLockTime = 0.2f;
    public float wallJumpTime = 0.2f; // 벽점프 후 벽을 무시하는 시간
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    private Rigidbody2D rb;
    private Animator anim;


    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private float moveInput;
    private int facingDirection = -1;
    private bool ignoreWallCheck = false; // 벽 검사 무시 상태

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isWallJumping)
        {
            moveInput = Input.GetAxisRaw("Horizontal");
            if (moveInput != 0)
            {
                facingDirection = (int)-Mathf.Sign(moveInput);
                transform.localScale = new Vector3(facingDirection, transform.localScale.y, transform.localScale.z);
            }
        }

        Move();
        Jump();
        if (!ignoreWallCheck) CheckSurroundings();
    }

    void Move()
    {
        if (!isWallJumping)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }
        if (rb.linearVelocity.normalized.x == 0)
        {
            anim.SetBool("isMove", false);
        }
        else
        {
            anim.SetBool("isMove", true);
        }
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetBool("isJump", true);
            if (isGrounded)
            {
                anim.SetBool("isFall", false);
                anim.SetBool("isLand", false);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (isWallSliding) // 벽 점프 처리
            {
                anim.SetBool("isFall", false);
                anim.SetBool("isLand", false);

                isWallJumping = true;
                isWallSliding = false; // 벽 점프 직후 벽 슬라이딩 방지

                facingDirection = -facingDirection;
                transform.localScale = new Vector3(facingDirection, transform.localScale.y, transform.localScale.z);

                ignoreWallCheck = true; // 벽 감지 무시
                StartCoroutine(ResetWallJump());

                // 사선 방향으로 점프 적용
                rb.linearVelocity = new Vector2(-facingDirection * wallJumpForce * 1f, jumpForce * 1f);
            }
        }
        anim.SetFloat("yVelo", rb.linearVelocity.y);
    }

    IEnumerator ResetWallJump()
    {
        rb.gravityScale = gravityScale * 0.7f; // 부드럽게 점프
        yield return new WaitForSeconds(wallJumpTime);
        ignoreWallCheck = false; //  벽 검사 재개

        yield return new WaitForSeconds(wallJumpControlLockTime);
        isWallJumping = false;
        rb.gravityScale = gravityScale; // 중력 복구
    }

    void CheckSurroundings()
    {
        Vector2 pos = transform.position;

        Debug.DrawRay(pos, Vector2.down * 1f, Color.green); // 바닥 감지
        Debug.DrawRay(pos, Vector2.right * -facingDirection * 0.5f, Color.red); // 벽 감지

        isGrounded = Physics2D.Raycast(pos, Vector2.down, 1f, groundLayer);
        isTouchingWall = Physics2D.Raycast(pos, Vector2.right * -facingDirection, 0.5f, wallLayer);
        isWallSliding = isTouchingWall && moveInput == -facingDirection && !isGrounded;

        // 벽에서 슬라이딩 효과 적용
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideGravityScale));
            anim.SetBool("isJump", false);
            anim.SetBool("isFall", false);
            anim.SetBool("isLand", false);
            anim.SetBool("isMove",false);
        }
        if(isGrounded && rb.linearVelocity.y<0) //착지 감지
        { 
            anim.SetBool("isJump", false);
            anim.SetBool("isFall", false);
            anim.SetBool("isLand", true);
        }

        // 공중에서 떨어질 때 상태 갱신
        if (rb.linearVelocity.y < 0 && !isGrounded && !isWallSliding)
        {
            anim.SetBool("isFall", true);
            anim.SetBool("isJump", false);
            anim.SetBool("isLand", false);
        }
    }
}
