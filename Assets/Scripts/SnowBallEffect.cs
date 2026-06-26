/*
 * 雪玉パターンのアニメーションを制御するクラス
 */

using UnityEngine;
using System.Collections;

public class SnowBallEffect : MonoBehaviour {
    [Header("Phase 1: スケールと座標を変更")]
    [Tooltip("Phase 1の目標 X スケール")]
    public float targetScaleX = 8.31f;
    [Tooltip("Phase 1の目標 Y スケール")]
    public float targetScaleY = 8.31f;
    [Tooltip("Phase 1の目標 Z スケール（通常は変更しない）")]
    public float targetScaleZ = 1.0f;

    [Tooltip("Phase 1の目標 Y ワールド座標")]
    public float targetPosY1 = 7.18f;
    [Tooltip("Phase 1の変形にかかる時間（例: 0.5 秒以上）")]
    public float phase1Duration = 3.0f;

    [Header("Phase 2: 最終 Y 位置へ移動")]
    [Tooltip("Phase 2の最終目標 Y ワールド座標（-1.03）")]
    public float targetPosY2 = -1.03f;
    [Tooltip("Phase 2の移動にかかる時間（1 秒固定）")]
    public float phase2Duration = 1.0f;

    private void Start() {
        StartCoroutine(ExecuteSnowBallPattern());
    }

    // スノーボールのサイズ・位置変形および最終移動を管理するコルーチン
    public IEnumerator ExecuteSnowBallPattern() {
        Vector3 initialScale = transform.localScale;
        Vector3 initialPosition = transform.position;

        // 目標スケールを設定
        Vector3 targetScale = new Vector3(targetScaleX, targetScaleY, targetScaleZ);

        // 目標位置を設定（X, Z は固定し、Y のみ変更）
        Vector3 targetPosition1 = new Vector3(initialPosition.x, targetPosY1, initialPosition.z);

        float timer = 0f;

        // サイズ変形および Y 軸位置を移動
        while (timer < phase1Duration) {
            float t = timer / phase1Duration;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            transform.position = Vector3.Lerp(initialPosition, targetPosition1, t);
            timer += Time.deltaTime;
            yield return null; // 次のフレームまで待機
        }

        transform.localScale = targetScale;
        transform.position = targetPosition1;

        // 最終 Y 軸位置へ移動（1 秒
        Vector3 startPosition2 = transform.position;
        // Phase 2 の目標位置を設定（X, Z は固定し、Y を targetPosY2 に変更）
        Vector3 targetPosition2 = new Vector3(startPosition2.x, targetPosY2, startPosition2.z);

        timer = 0f; // タイマーをリセット

        while (timer < phase2Duration) // phase2Duration = 1.0f
        {
            float t = timer / phase2Duration;

            // Y 軸位置のみを 1 秒かけて -1.03 まで移動
            transform.position = Vector3.Lerp(startPosition2, targetPosition2, t);

            timer += Time.deltaTime;
            yield return null; // 次のフレームまで待機
        }
        transform.position = targetPosition2;

        Destroy(gameObject);
    }
}