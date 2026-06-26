using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnPlayerDamaged;

    [Header("Player Health")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Invincibility Settings")]
    [SerializeField] private float invincibilityDuration = 0.5f;
    private bool isInvincible = false;

    [Header("Activation Targets")]
    [SerializeField] private HandgunHandler handgunScript;
    [SerializeField] private BossPattern bossPatternScript;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Audio")]
    [SerializeField] private AudioClip gameoverSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverUI;
    private bool isGameOver = false;

    [Header("Player Components")]
    public Animator targetAnimator;
    public Rigidbody2D playerRigidbody;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(gameObject);
            return;
        }

        currentHealth = maxHealth;

        if (gameOverUI != null) {
            gameOverUI.SetActive(false);
        }

        // 초기 체력 이벤트를 호출하여 UI를 초기화합니다.
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Start() {
        if (audioSource == null) {
            audioSource = GetComponent<AudioSource>();
        }

        DialogManager dialogManager = FindAnyObjectByType<DialogManager>();
        if (dialogManager != null) {
            dialogManager.StartDialog();
        } else {
            ActivateGameScripts();
        }

        isGameOver = false;
    }

    private void OnEnable() {
        DialogManager.OnDialogFinished += ActivateGameScripts;
    }

    private void OnDisable() {
        DialogManager.OnDialogFinished -= ActivateGameScripts;
    }

    private void ActivateGameScripts() {
        if (handgunScript != null) handgunScript.enabled = true;
        if (bossPatternScript != null) bossPatternScript.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
    }

    void Update() {
        if (isGameOver && Input.GetKeyDown(KeyCode.R)) {
            RestartGameFlow();
        }
    }

    /// <summary>
    /// 플레이어의 체력을 변경하고 관련 로직(무적, 사망)을 처리합니다.
    /// 이 메서드는 GameManager에 중앙 집중화되어 있습니다.
    /// </summary>
    /// <param name="amount">변경할 체력 양 (음수: 데미지, 양수: 회복)</param>
    public void ChangeHealth(float amount) {
        // isGameOver 상태 및 무적 상태 체크
        if (amount < 0 && (isInvincible || isGameOver)) {
            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (amount < 0 && currentHealth > 0) {
            OnPlayerDamaged?.Invoke();
            StartCoroutine(InvincibilityCoroutine());
        }

        // 체력 변경 이벤트를 호출하여 UI를 업데이트합니다.
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0 && !isGameOver) {
            ShowGameOverScreen();
        }
    }

    private IEnumerator InvincibilityCoroutine() {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    public void ShowGameOverScreen() {
        isGameOver = true;

        if (gameOverUI != null) {
            gameOverUI.SetActive(true);
        }

        // 플레이어 및 핵심 스크립트 비활성화
        if (playerMovement != null && playerRigidbody != null) {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerMovement.enabled = false;
        }

        if (bossPatternScript != null) bossPatternScript.enabled = false;
        if (handgunScript != null) handgunScript.enabled = false;

        // 애니메이션 및 사운드 재생
        if (targetAnimator != null) {
            targetAnimator.SetTrigger("IsDie");
        }

        if (audioSource != null && gameoverSound != null) {
            audioSource.PlayOneShot(gameoverSound);
        }
    }

    public void RestartGameFlow() {
        if (gameOverUI != null) {
            gameOverUI.SetActive(false);
        }
        GameOverSceneReload();
    }

    private void GameOverSceneReload() {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public float GetMaxHealth() { return maxHealth; }
    public float GetCurrentHealth() { return currentHealth; }
}