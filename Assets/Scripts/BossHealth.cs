using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class BossHealth : MonoBehaviour, ITakeDamage {
    public float maxHealth = 1000f;
    private float currentHealth;
    private BossPattern bossPattern;

    // (현재 체력, 최대 체력) 정보를 전달
    public event Action<float, float> OnHealthChanged;


    void Start() {
        currentHealth = maxHealth;
        bossPattern = GetComponent<BossPattern>();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // 총알 스크립트에서 호출될 피해 처리 함수
    public void TakeDamage(float damageAmount) {
        if (bossPattern != null && bossPattern.IsInvulnerable()) { // 무적
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