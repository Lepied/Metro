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

    // 공격 관련 변수
    [Header("공격변수")]
    public float attackCooldown = 0.5f;      // 공격 쿨다운
    public int maxCombo = 3;                 // 최대 콤보 수
    public float comboWindow = 0.8f;         // 콤보 입력 가능 시간
    public Transform attackPoint;            // 공격 판정 위치
    public float attackRadius = 0.8f;        // 공격 범위
    public LayerMask enemyLayer;             // 적 레이어
    public float attackDamage = 10f;         // 공격 데미지
    public float[] attackMultipliers = new float[] { 1f, 1.2f, 1.5f }; // 콤보별 데미지 배율

    // 공격 관련 상태
    private bool isAttacking = false;
    private bool canAttack = true;
    private int currentCombo = 0;
    private float lastAttackTime = -999f;
    private float comboResetTime = 0f;

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

        // 공격 판정 위치가 할당되지 않았다면 생성
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.parent = transform;
            attackPointObj.transform.localPosition = new Vector3(0.8f * -facingDirection, 0, 0);
            attackPoint = attackPointObj.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && isGrounded && !isSliding) // 아래 키 누르면 슬라이딩 시작
        {
            StartCoroutine(Slide());
        }

        // 공격 입력 처리
        if (Input.GetKeyDown(KeyCode.X) && canAttack && !isSliding && !isWallSliding)
        {
            if(!isGrounded)
            {
                JumpAttack();
            }
            else
            Attack();
        }

        // 콤보 시간 체크
        if (Time.time > comboResetTime && currentCombo > 0)
        {
            ResetCombo();
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
    void Attack()
    {
        // 콤보 시스템 처리
        if (currentCombo >= maxCombo)
        {
            currentCombo = 0;
        }

        // 공격 시작
        isAttacking = true;
        canAttack = false;

        // 공격 애니메이션 재생
        anim.SetBool("isAttack", true);
        anim.SetInteger("attackCombo", currentCombo);

        StartCoroutine(CreateAttackHitbox()); //나중에 이펙트로 따로 설정하기?

        // 콤보 카운터 증가
        currentCombo++;
        lastAttackTime = Time.time;
        comboResetTime = Time.time + comboWindow;

        // 공격 쿨다운
        StartCoroutine(AttackCooldown());
    }
    void JumpAttack()
    {
        if (!isGrounded && canAttack) // 공중에서 공격 가능할 때
        {
            isAttacking = true;
            canAttack = false;

            anim.SetBool("isJumpAttack", true); // 애니메이터에 새로운 파라미터 추가
            StartCoroutine(CreateAttackHitbox());

            StartCoroutine(AttackCooldown()); // 공격 쿨다운 적용
        }
    }
    // 공격 판정 생성
    IEnumerator CreateAttackHitbox()
    {
        // 공격 모션 시작 후 약간의 딜레이 (애니메이션과 맞춤)
        yield return new WaitForSeconds(0.1f);

        // 공격 판정 생성 및 데미지 계산
        float currentDamage = attackDamage * attackMultipliers[Mathf.Clamp(currentCombo - 1, 0, attackMultipliers.Length - 1)];

        // 디버그용 원 그리기
        Debug.DrawRay(attackPoint.position, Vector3.forward, Color.red, 0.2f);

        /*
        // 적 감지 및 데미지 적용
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            // 데미지 적용 (적 스크립트에 따라 적절히 수정 필요)
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(currentDamage);
            }

            // 데미지 디버그 출력
            Debug.Log($"Hit enemy: {enemy.name} - Damage: {currentDamage}");
        }
        */
        // 공격 판정 시각화를 위한 이펙트 생성 (필요시)
        CreateAttackEffect();

        // 공격 종료 처리
        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
        anim.SetBool("isAttack", false);
        anim.SetBool("isJumpAttack", false);
    }
    // 공격 이펙트 생성 (필요시 구현)
    void CreateAttackEffect()
    {
        // 공격 범위 시각화를 위한 이펙트 생성
        // 예: 파티클 시스템 재생 또는 스프라이트 애니메이션 재생
    }

    // 공격 쿨다운 처리
    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // 콤보 초기화
    void ResetCombo()
    {
        currentCombo = 0;
        anim.SetInteger("attackCombo", 0);
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
        while (timer < slideDuration || IsUnderObstacle()) // 슬라이딩 시간 or 좁은 공간 유지, 좁은공간에서 빠져나와도 살짝 더가게해야할듯
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
