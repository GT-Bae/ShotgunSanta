/*
 * 敵の体力を制御するクラス
 */

using UnityEngine;

public class EnemyHealth : MonoBehaviour, ITakeDamage {
    public float maxHealth = 100f;
    private float currentHealth;

    void Start() {
        currentHealth = maxHealth;
    }

    // 弾丸スクリプトから呼び出されるダメージ処理関数
    public void TakeDamage(float damageAmount) {
        // 受けたダメージ量分だけ体力を減少させる
        currentHealth -= damageAmount;
        if (currentHealth <= 0) {
            Die();
        }
    }

    private void Die() {
        Destroy(gameObject);
    }
}
