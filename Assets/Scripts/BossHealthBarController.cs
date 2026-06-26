/*
 * ボスの体力バーを制御するクラス
 */

using UnityEngine;
using UnityEngine.UI;
using System;

public class BossHealthBarController : MonoBehaviour {

    [Header("UI References")]
    [Header("Boss Reference")]
    [SerializeField] private RectTransform healthGaugeRect;
    [Header("Boss Reference")]
    [SerializeField] private BossHealth targetBoss;

    private float originalWidth;

    void Start() {
        originalWidth = healthGaugeRect.sizeDelta.x;
        if (targetBoss != null) {
            targetBoss.OnHealthChanged += UpdateHealthGauge;
        }
    }

    void OnDestroy() {
        if (targetBoss != null) {
            targetBoss.OnHealthChanged -= UpdateHealthGauge;
        }
    }

    // 体力のゲージだけを更新する
    private void UpdateHealthGauge(float currentHealth, float maxHealth) {
        float healthRatio = currentHealth / maxHealth;
        healthGaugeRect.sizeDelta = new Vector2(originalWidth * healthRatio, healthGaugeRect.sizeDelta.y);
    }
}