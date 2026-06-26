/*
 * プレイヤーがダメージを受けた際のエフェクトを制御するクラス
 */

using UnityEngine;
using System.Collections;

public class PlayerVisuals : MonoBehaviour {
    private SpriteRenderer playerSpriteRenderer;
    private Color hitColor = Color.red;
    private float hitEffectDuration = 0.2f;

    private Color originalColor;

    void Awake() {
        if (playerSpriteRenderer == null) {
            playerSpriteRenderer = GetComponent<SpriteRenderer>();
            if (playerSpriteRenderer == null) {
                Debug.LogError("PlayerVisuals: SpriteRendererが見つかりません", this);
                enabled = false;
                return;
            }
        }
    }

    void Start() {
        originalColor = playerSpriteRenderer.color;
    }

    void OnEnable() {
        if (GameManager.Instance != null) {
            GameManager.Instance.OnPlayerDamaged += HandlePlayerDamaged;
        }
    }

    void OnDisable() {
        if (GameManager.Instance != null) {
            GameManager.Instance.OnPlayerDamaged -= HandlePlayerDamaged;
        }
    }

    private void HandlePlayerDamaged() {
        StopAllCoroutines();
        StartCoroutine(HitEffectCoroutine());
    }

    // 被弾時の点滅エフェクトを管理するコルーチン
    private IEnumerator HitEffectCoroutine() {
        playerSpriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitEffectDuration);
        playerSpriteRenderer.color = originalColor;
    }
}