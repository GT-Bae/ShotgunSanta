/*
 * 大きなプレゼントボックスのパターンを制御するクラス
 */


using UnityEngine;
using System.Collections;
public class BigGiftBoxPattern : MonoBehaviour {
    [Header("Dependencies")]
    [Tooltip("5x5 AoE 警告プリハブ (プレイヤー追跡用)")]
    [SerializeField] private GameObject aoeWarningPrefab;
    [Tooltip("爆発後に生成せれる後続エフェクトプリハブ")]
    [SerializeField] private GameObject giftBoxAttackPrefab;

    [SerializeField] private Transform playerTarget;

    [Header("Pattern Settings")]
    [Tooltip("最終的なスケール (5.0f)")]
    [SerializeField] private float targetScale = 5.0f;
    [Tooltip("スケールアップにかかる時間")]
    [SerializeField] private float scaleUpDuration = 1.0f;
    [Tooltip("AoE 固定後、爆発までかかる時間")]
    [SerializeField] private float aoeHoldDuration = 1.0f;
    [Tooltip("AoE 固定位置から爆発地点まで移動する時間")]
    [SerializeField] private float travelDuration = 0.3f;

    private GameObject currentAoe;
    private Coroutine currentFollowCoroutine;
    void Start() {
        playerTarget = GameObject.FindWithTag("Player").transform;
        transform.localScale = Vector3.one; // 初期スケール (1.0f)
        StartCoroutine(ExecutePattern()); // パターン開始
    }

    private IEnumerator ExecutePattern() {
        // AoE生成および追跡開始
        currentAoe = Instantiate(aoeWarningPrefab, playerTarget.position, Quaternion.identity);
        currentFollowCoroutine = StartCoroutine(FollowTarget(currentAoe.transform, playerTarget));

        // BigGiftBoxのスケールを拡大
        yield return StartCoroutine(ScaleOverTime(Vector3.one * targetScale, scaleUpDuration));

        // AoEの追跡を中止し、固定位置を保存
        Vector3 aoeHoldPosition = currentAoe.transform.position;
        if (currentFollowCoroutine != null) {
            StopCoroutine(currentFollowCoroutine);
            currentFollowCoroutine = null;
            Debug.Log("Big Gift Box拡大完了、 AoE追跡中止および固定。");
        }

        // AoE固定状態で待機
        Debug.Log("AoE固定位置: " + aoeHoldPosition + ", 待機開始。");
        yield return new WaitForSeconds(aoeHoldDuration);

        // BigGiftBoxがAoE固定位置へ飛んでいく
        Vector3 startPosition = transform.position;
        yield return StartCoroutine(MoveToPosition(aoeHoldPosition, travelDuration, startPosition));

        // 着地
        AttackAndDestroy(aoeHoldPosition);

        // AoEの破棄
        if (currentAoe != null) {
            Destroy(currentAoe);
        }
    }

    // Transform追跡
    private IEnumerator FollowTarget(Transform follower, Transform target) {
        while (true) {
            follower.position = target.position;
            yield return null;
        }
    }

    // objectをtargetScaleまでスケール拡大
    private IEnumerator ScaleOverTime(Vector3 targetScale, float duration) {
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < duration) {
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
    }

    // objectをtargetPosまで移動
    private IEnumerator MoveToPosition(Vector3 targetPos, float duration, Vector3 startPos) {
        float elapsedTime = 0f;

        while (elapsedTime < duration) {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
    }

    // 攻撃および破棄
    private void AttackAndDestroy(Vector3 explosionPosition) {
        if (giftBoxAttackPrefab != null) {
            GameObject attackInstance = Instantiate(giftBoxAttackPrefab, explosionPosition, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}