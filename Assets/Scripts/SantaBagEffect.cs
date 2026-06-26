/*
 * Santa Bag パターンのアニメーションを制御するクラス
 */

using UnityEngine;
using System.Collections;

public class SantaBagMovement : MonoBehaviour {
    private Vector3 targetPosition;

    [SerializeField] private float moveDuration = 0.5f;

    private SantaBagDamage damageScript;
    private SpriteRenderer spriteRenderer;
    private bool isInitialized = false;
    [Header("Audio Settings")]
    [Tooltip("サウンドを再生する AudioSource コンポーネント")]
    private AudioSource audioSource;
    [Tooltip("移動完了時に再生するサウンド")]
    [SerializeField] private AudioClip arrivalSoundClip;

    private void Awake() {
        damageScript = GetComponent<SantaBagDamage>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (damageScript == null) {
            Debug.LogError("SantaBagDamage.csが見つかりません。");
            enabled = false;
        }
    }

    // BossPattern.csで使用。最終目標位置を設定し、移動を開始
    public void InitializeAndStart(Vector3 initialPos, bool isXFlipped) {
        if (isInitialized) return; // すでに初期化済みの場合は無視

        transform.position = initialPos; // 初期位置（Y=10）を設定

        if (spriteRenderer != null) {
            spriteRenderer.flipX = isXFlipped;
        }

        // X 反転の有無に応じて最終目標位置を設定
        if (isXFlipped) { // 逆方向(X Flip)
            targetPosition = new Vector3(0.48f, -0.96f, initialPos.z);
        } else { // 正方向
            targetPosition = new Vector3(-0.49f, -0.96f, initialPos.z);
        }

        isInitialized = true;
        // 移動コルーチンを開始
        StartCoroutine(MoveToTarget());
    }

    private IEnumerator MoveToTarget() {
        // 初期化されていない場合は待機
        while (!isInitialized) {
            yield return null;
        }

        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < moveDuration) {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 最終位置に正確に到達
        transform.position = targetPosition;
        if (audioSource != null && arrivalSoundClip != null) {
            audioSource.PlayOneShot(arrivalSoundClip);
        }
        // 移動完了後にダメージトリガーを有効化
        if (damageScript != null) {
            damageScript.ActivateDamageTrigger();
        }
    }
}