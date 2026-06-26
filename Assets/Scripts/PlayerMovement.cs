using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    public float speed = 5f;
    public float dodgeForce = 15f;
    public float dodgeDuration = 0.15f;
    public float dodgeCooldown = 2f;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer playerSpriteRenderer;

    private float nextDodgeTime = 0f;
    private bool isDodging = false;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();

        if (GameManager.Instance != null) {
            GameManager.Instance.playerRigidbody = rb;
            GameManager.Instance.targetAnimator = animator;
        } else {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다. 게임 흐름 제어에 문제가 발생할 수 있습니다.");
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextDodgeTime) {
            StartDodge();
        }
    }

    void FixedUpdate() {
        if (isDodging) {
            return;
        }

        float xInput = Input.GetAxisRaw("Horizontal");
        float yInput = Input.GetAxisRaw("Vertical");

        Vector2 moveDirection = new Vector2(xInput, yInput);

        bool isMoving = moveDirection.magnitude > 0.01f;

        if (animator.GetBool("IsWalk") != isMoving) {
            animator.SetBool("IsWalk", isMoving);
        }

        if (isMoving) {
            rb.linearVelocity = moveDirection.normalized * speed;
        } else {
            // 멈춰 있을 때 속도를 0으로 설정하여 미끄러짐 방지
            rb.linearVelocity = Vector2.zero;
        }
        
        // 좌우 반전
        if (xInput < 0) {
            playerSpriteRenderer.flipX = true;
        } else if (xInput > 0) {
            playerSpriteRenderer.flipX = false;
        }
    }

    void StartDodge() {
        nextDodgeTime = Time.time + dodgeCooldown;
        isDodging = true;
        animator.SetTrigger("Dodge");

        float xInput = Input.GetAxisRaw("Horizontal");
        float yInput = Input.GetAxisRaw("Vertical");
        Vector2 dodgeDirection = new Vector2(xInput, yInput).normalized;

        // 입력이 없을 경우 마지막 속도 또는 스프라이트 방향으로 회피
        if (dodgeDirection == Vector2.zero) {
            if (rb.linearVelocity.sqrMagnitude > 0.01f) {
                dodgeDirection = rb.linearVelocity.normalized;
            } else {
                // flipX가 true면 왼쪽, false면 오른쪽
                dodgeDirection = playerSpriteRenderer.flipX ? Vector2.left : Vector2.right;
            }
        }
        rb.linearVelocity = dodgeDirection * dodgeForce;

        // 좌우 반전
        if (dodgeDirection.x != 0) {
            playerSpriteRenderer.flipX = dodgeDirection.x < 0;
        }

        Invoke("StopDodge", dodgeDuration);
    }

    void StopDodge() {
        isDodging = false;
        rb.linearVelocity = Vector2.zero;
    }

    // 데미지 처리
    public void TakeDamage(float damageAmount) {
        if (GameManager.Instance != null) {
            GameManager.Instance.ChangeHealth(-damageAmount);
        }
    }
}