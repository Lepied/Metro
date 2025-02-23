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
    public float wallJumpControlLockTime = 0.2f; //벽점프하고 못움직이게 일정시간동안
    public float wallJumpTime = 0.2f; // 벽점프 후 벽을 무시하는 시간
    public float slideSpeed = 8f;
    public float slideDuration = 0.5f;
    public float slideCollider = 0.6f;

    public LayerMask caveLayer; //슬라이딩해야 지나갈 수 있는 곳
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private BoxCollider2D coll;


    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private bool isSliding = false; 
    private bool isInCave = false; //좁은 곳인지 아닌지

    private float CollHeight;
    private Vector2 CollOffset;

    private float moveInput;
    private int facingDirection = -1;
    private bool ignoreWallCheck = false; // 벽 검사 무시 상태

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
        anim = GetComponent<Animator>();
        coll = GetComponent<BoxCollider2D>();
        CollHeight = coll.size.y;
        CollOffset = coll.offset;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && isGrounded && !isSliding) // 아래 키 누르면 슬라이딩 시작
        {
            StartCoroutine(Slide());
        }

        Move();
        Jump();
        if (!ignoreWallCheck) CheckSurroundings();
    }

    void Move()
    {
        if (isSliding) { return; };
        //if (!isWallJumping! && !isSliding)
        if (!isWallJumping)
        {
            moveInput = Input.GetAxisRaw("Horizontal");
            if (moveInput != 0)
            {
                facingDirection = (int)-Mathf.Sign(moveInput);
                transform.localScale = new Vector3(facingDirection, transform.localScale.y, transform.localScale.z);
            }
        }
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
            if (isSliding)
            {
                StopSliding(); // 슬라이딩 중 점프하면 슬라이딩 멈춤
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // X 속도 초기화
            }

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

    IEnumerator Slide()
    {

        isSliding = true;
        anim.SetBool("isSlide", true);

        // 히트박스 줄이기
        coll.size = new Vector2(coll.size.x, slideCollider);
        coll.offset = new Vector2(coll.offset.x, coll.offset.y - (CollHeight - slideCollider) / 2);
  

        float timer = 0f;
        while (timer < slideDuration || IsUnderObstacle()) // 슬라이딩 시간 or 좁은 공간 유지
        {
            rb.linearVelocity = new Vector2(-facingDirection * slideSpeed, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        StopSliding();
    }

    void StopSliding()
    {
        Debug.Log("isSlide -> false");
        Debug.Log("Slide 종료");
        isSliding = false;

        // 히트박스 원래 크기로 복구
        coll.size = new Vector2(coll.size.x, CollHeight);
        coll.offset = CollOffset;

        anim.SetBool("isSlide", false);


    }

    bool IsUnderObstacle()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * (CollHeight / 2);
        return Physics2D.Raycast(origin, Vector2.up, CollHeight - slideCollider, caveLayer);
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

        // 바닥 감지용
        float castWidth = coll.size.x * 0.8f; // 감지범위
        float castHeight = 0.1f; // 감지 두께

        Vector2 groundCheckPos = new Vector2(pos.x, pos.y - CollHeight / 2); // 플레이어 발 위치

        Debug.DrawRay(groundCheckPos, Vector2.down * castHeight, Color.green); // 바닥 감지용 디버그 레이
        Debug.DrawRay(pos, Vector2.right * -facingDirection * 0.5f, Color.red); // 벽 감지

        isGrounded = Physics2D.BoxCast(groundCheckPos, new Vector2(castWidth, castHeight), 0f, Vector2.down, 0.1f, groundLayer);
        //isGrounded = Physics2D.Raycast(pos, Vector2.down, 1f, groundLayer);
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
