/*
 * プレイヤーの体力制御およびゲームオーバー制御
 */

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

        // 初期体力イベントを呼び出して UI を初期化します。
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
    /// プレイヤーの体力を変更し、関連するロジック（無敵、死亡）を処理します。
    /// </summary>
    /// <param name="amount">変更する体力量（負数: ダメージ、正数: 回復）</param>
    public void ChangeHealth(float amount) {
        // isGameOver 状態および無敵状態をチェック
        if (amount < 0 && (isInvincible || isGameOver)) {
            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (amount < 0 && currentHealth > 0) {
            OnPlayerDamaged?.Invoke();
            StartCoroutine(InvincibilityCoroutine());
        }

        // 体力変更イベントを呼び出して UI を更新します
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

        // プレイヤーおよび主要スクリプトを無効化
        if (playerMovement != null && playerRigidbody != null) {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerMovement.enabled = false;
        }

        if (bossPatternScript != null) bossPatternScript.enabled = false;
        if (handgunScript != null) handgunScript.enabled = false;

        // アニメーターおよびサウンド再生
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