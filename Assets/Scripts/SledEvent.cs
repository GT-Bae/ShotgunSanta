using UnityEngine;

// SledEffect 프리팹에 부착되어 X축 방향으로 빠르게 이동한 후 스스로 파괴
public class SledEvent : MonoBehaviour {

    [Header("Sled Settings")]
    [Tooltip("Sled의 이동 속도")]
    [SerializeField] private float moveSpeed = 15.0f;

    [Tooltip("Sled가 파괴될 때까지의 시간")]
    [SerializeField] private float lifeTime = 2.0f;

    [Header("Damage Settings")]
    [Tooltip("플레이어에게 입힐 데미지")]
    [SerializeField] private float damageAmount = 10f;

    private int direction = 1; // 1: 오른쪽, -1: 왼쪽

    private void Start() {
        if (transform.localScale.x < 0) {
            direction = -1;
        } else {
            direction = 1;
        }

        Destroy(gameObject, lifeTime);
    }

    private void Update() {
        transform.Translate(Vector3.right * moveSpeed * direction * Time.deltaTime, Space.World);
    }

    // 플레이어에게 데미지
    private void OnTriggerEnter2D(Collider2D other) {
        if (GameManager.Instance == null) {
            return;
        }

        if (other.CompareTag("Player")) {
            Debug.Log($"Sled Effect: Player hit! Damage: {damageAmount}");
            GameManager.Instance.ChangeHealth(-damageAmount);
        }
    }
}