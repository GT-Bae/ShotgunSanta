using UnityEngine;

public class ShotgunEffectHandler : MonoBehaviour {
    public int damageAmount = 10;

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            if (GameManager.Instance != null) {
                GameManager.Instance.ChangeHealth(-damageAmount);
            }
        }
    }

    public void DestroyEffect() {
        Destroy(gameObject);
    }
}