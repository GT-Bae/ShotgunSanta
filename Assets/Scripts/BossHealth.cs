/*
 * ボスの体力を制御するクラス
 */

using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class BossHealth : MonoBehaviour, ITakeDamage {
    public float maxHealth = 1000f;
    private float currentHealth;
    private BossPattern bossPattern;

    // (現在体力、最大体力) 情報を渡す
    public event Action<float, float> OnHealthChanged;

    void Start() {
        currentHealth = maxHealth;
        bossPattern = GetComponent<BossPattern>();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // 弾丸のスクリプトから呼ばれる、ダメージの処理
    public void TakeDamage(float damageAmount) {
        if (bossPattern != null && bossPattern.IsInvulnerable()) { // 無敵
            return;
        }
        currentHealth -= damageAmount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0) {
            Die();
        }
    }

    private void Die() {
        SceneManager.LoadScene("End");
    }
}