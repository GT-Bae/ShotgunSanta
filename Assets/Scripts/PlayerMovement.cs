/*
 * プレイヤーの移動を制御するクラス
 */

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
            Debug.LogError("GameManager のインスタンスが見つかりません");
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
            // 停止中は速度を 0 に設定して滑りを防止
            rb.linearVelocity = Vector2.zero;
        }
        
        // 左右反転
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

        // 入力がない場合は、最後の速度またはスプライトの向きに基づいて回避
        if (dodgeDirection == Vector2.zero) {
            if (rb.linearVelocity.sqrMagnitude > 0.01f) {
                dodgeDirection = rb.linearVelocity.normalized;
            } else {
                // flipX が true の場合は左、false の場合は右
                dodgeDirection = playerSpriteRenderer.flipX ? Vector2.left : Vector2.right;
            }
        }
        rb.linearVelocity = dodgeDirection * dodgeForce;

        // 左右反転
        if (dodgeDirection.x != 0) {
            playerSpriteRenderer.flipX = dodgeDirection.x < 0;
        }

        Invoke("StopDodge", dodgeDuration);
    }

    void StopDodge() {
        isDodging = false;
        rb.linearVelocity = Vector2.zero;
    }

    // ダメージ処理
    public void TakeDamage(float damageAmount) {
        if (GameManager.Instance != null) {
            GameManager.Instance.ChangeHealth(-damageAmount);
        }
    }
}