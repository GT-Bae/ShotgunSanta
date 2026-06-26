/*
 * AoEの生成から消滅までを制御するクラス
 */

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class AoESquareFill : MonoBehaviour {
    [Header("Effect Settings")]
    [Tooltip("AoEが完全に不透明な状態を維持する時間")]
    public float initialDuration = 1.0f;
    [Tooltip("透明になって消えるまでにかかる時間（フェードアウト時間）")]
    public float fadeDuration = 1.0f;
    [Tooltip("初期透明度 (0.0f~1.0f)")]
    public float initialAlpha = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Color baseColor; // SpriteRendererの基本色(R, G, B)を保存つる変数

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();

        baseColor = spriteRenderer.color;

        Color startColor = baseColor;
        startColor.a = initialAlpha; // 初期透明度を適用
        spriteRenderer.color = startColor;

        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy() {
        // 初期不透明維持時間の待機 (50%の透明度維持する時間)
        yield return new WaitForSeconds(initialDuration);

        // フェードアウト開始 (50% → 0%)
        float elapsedTime = 0f;
        float startAlpha = spriteRenderer.color.a;

        while (elapsedTime < fadeDuration) {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, t);
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, newAlpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}