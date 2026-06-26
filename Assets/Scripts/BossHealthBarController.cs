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

    // 체력 게이지만 업데이트
    private void UpdateHealthGauge(float currentHealth, float maxHealth) {
        float healthRatio = currentHealth / maxHealth;
        healthGaugeRect.sizeDelta = new Vector2(originalWidth * healthRatio, healthGaugeRect.sizeDelta.y);
    }
}