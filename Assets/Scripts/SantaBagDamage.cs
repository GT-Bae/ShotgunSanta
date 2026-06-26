using UnityEngine;
using System.Collections;

public class SantaBagDamage : MonoBehaviour {
    [Header("Damage Settings")]
    [Tooltip("플레이어에게 줄 피해량")]
    [SerializeField] private int damageAmount = 30;

    [Tooltip("최종 위치 도달 후 데미지 트리거 유지 시간")]
    [SerializeField] private float damageDuration = 0.5f;

    private Rigidbody2D rb;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    // SantaBagMovement.cs에서 사용
    public void ActivateDamageTrigger() {
        StartCoroutine(SelfDestructTimer());
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (GameManager.Instance == null) {
            Debug.LogError("GameManager.Instance is missing! Cannot apply damage.");
            return;
        }

        if (other.CompareTag("Player")) {
            GameManager.Instance.ChangeHealth(-damageAmount);
        }
    }

    private IEnumerator SelfDestructTimer() {
        yield return new WaitForSeconds(damageDuration);
        Destroy(gameObject);
    }
}