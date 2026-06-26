/*
 * プレイヤーのHPゲージを制御するクラス
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class HealthBarController : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private RectTransform healthGaugeRect;
    [SerializeField] private TextMeshProUGUI healthText;

    private float originalWidth;

    void Start() {
        originalWidth = healthGaugeRect.sizeDelta.x;
        if (GameManager.Instance != null) {
            GameManager.Instance.OnHealthChanged += UpdateHealthUI;
            GameManager.Instance.ChangeHealth(0);
        }
    }

    void OnDestroy() {
        if (GameManager.Instance != null) {
            GameManager.Instance.OnHealthChanged -= UpdateHealthUI;
        }
    }

    // HPゲージを更新
    private void UpdateHealthUI(float currentHealth, float maxHealth) {
        float healthRatio = currentHealth / maxHealth;
        healthGaugeRect.sizeDelta = new Vector2(originalWidth * healthRatio, healthGaugeRect.sizeDelta.y);
        healthText.text = $"{currentHealth:F0} / {maxHealth:F0}";
    }
}