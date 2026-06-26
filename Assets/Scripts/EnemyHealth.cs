using UnityEngine;

public class EnemyHealth : MonoBehaviour, ITakeDamage {
    public float maxHealth = 100f;
    private float currentHealth;

    void Start() {
        currentHealth = maxHealth;
    }

    // 총알 스크립트에서 호출될 피해 처리 함수
    public void TakeDamage(float damageAmount) {
        // 받은 피해량만큼 체력을 감소
        currentHealth -= damageAmount;
        if (currentHealth <= 0) {
            Die();
        }
    }

    private void Die() {
        Destroy(gameObject);
    }
}
