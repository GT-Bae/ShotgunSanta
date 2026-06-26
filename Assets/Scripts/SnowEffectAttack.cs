using UnityEngine;

// Snow 패턴에서 AoE 위치에 소환되어 플레이어에게 데미지
public class SnowEffectAttack : MonoBehaviour {

    [Header("Attack Settings")]
    [Tooltip("플레이어에게 줄 데미지 양")]
    public int damageAmount = 30;
    [Tooltip("이 이펙트가 지속된 후 자동으로 파괴되는 시간")]
    public float lifetime = 0.5f;

    private void Start() {
        Destroy(gameObject, lifetime);
    }

    // 플레이어 데미지
    private void OnTriggerEnter2D(Collider2D other) {
        if (GameManager.Instance == null) {
            Debug.LogError("GameManager.Instance is missing! Cannot apply damage.");
            return;
        }

        if (other.CompareTag("Player")) {
            GameManager.Instance.ChangeHealth(-damageAmount);
        }
    }
}