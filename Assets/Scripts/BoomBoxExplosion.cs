/*
 * 爆弾プレゼントボックスのパターンを制御するクラス
 */

using UnityEngine;
using System.Collections;

public class BoomBoxExplosion : MonoBehaviour {
    [Header("Explosion Settings")]
    [Tooltip("爆発までの待機時間 (秒)")]
    [SerializeField] private float timeToExplode = 3.0f;

    [Tooltip("爆発時の被害範囲 (ワールド単位サイズ)")]
    [SerializeField] private float explosionRadius = 2.0f;


    [Header("Movement Settings")]
    [Tooltip("プレイヤーを追跡する速度")]
    [SerializeField] private float moveSpeed = 3.0f;

    [Header("Visual Effects")]
    [Tooltip("AoE 予告視覚エフェクトのプレハブ")]
    [SerializeField] private GameObject aoeVisualPrefab; // AoE予告の視覚効果

    [Tooltip("AoE予告の視覚効果が表示される時間 (表示後に削除)")]
    [SerializeField] private float visualDuration = 1.0f; // AoE予告時間

    [Tooltip("ダメージを与えるBoomEffectプレハブ")]
    [SerializeField] private GameObject boomEffectDamagePrefab; // BoomEffect(ダメージトリガー)

    [Tooltip("ダメージを与えるBoomEffectが活性化される時間 (接触時にダメージ発生後削除)")]
    [SerializeField] private float damageActiveDuration = 0.2f; // BoomEffectが活性化せれる時間(0.2秒)
    [Tooltip("プレイヤーの距離がこの値以下になると追跡を停止します。")]
    [SerializeField] private float minFollowDistance = 1.0f;

    private Transform playerTarget;
    private bool hasTriggered = false;

    void Start() {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            playerTarget = player.transform;
        }

        StartCoroutine(ExplosionTimer());
    }

    void Update() {
        if (playerTarget != null && !hasTriggered ) {
            float sqrDistance = (playerTarget.position - transform.position).sqrMagnitude;
            float sqrMinDistance = minFollowDistance * minFollowDistance;

            // 最小距離の遠い場合のみプレイヤーに向かって移動
            if (sqrDistance > sqrMinDistance) {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    playerTarget.position,
                    moveSpeed * Time.deltaTime
                );
            }
        }
    }

    IEnumerator ExplosionTimer() {
        yield return new WaitForSeconds(timeToExplode);

        if (!hasTriggered) {
            StartCoroutine(ExplosionSequence());
        }
    }

    // AoE予告 -> ディレイ -> BoomEffectダメージ適用 -> 爆発
    IEnumerator ExplosionSequence() {
        hasTriggered = true;
        GameObject aoeVisual = null;
        GameObject boomDamageTrigger = null;

        float scaleFactor = explosionRadius * 2f;

        // AoE生成
        if (aoeVisualPrefab != null) {
            aoeVisual = Instantiate(aoeVisualPrefab, transform.position, Quaternion.identity);
        }
        yield return new WaitForSeconds(visualDuration);

        if (aoeVisual != null) {
            Destroy(aoeVisual);
        }

        // BoomEffect (ダメージトレガー) 生成
        if (boomEffectDamagePrefab != null) {
            boomDamageTrigger = Instantiate(boomEffectDamagePrefab, transform.position, Quaternion.identity);

            yield return new WaitForSeconds(damageActiveDuration);

            // BoomEffect削除
            Destroy(boomDamageTrigger);
        }

        // BoomBox本体削除
        Destroy(gameObject);
    }
}