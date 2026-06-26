using UnityEngine;

public class BoomEffectDamage : MonoBehaviour {
    private int damageAmount;
    
    // BoomBoxExplosion.csで呼び出し
    public void Initialize(int damage) {
        damageAmount = damage;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            GameManager.Instance.ChangeHealth(-damageAmount);
        }
    }
}