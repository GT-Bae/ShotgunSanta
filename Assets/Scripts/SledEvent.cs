/*
 * Sled パターンのアニメーション制御およびダメージ処理を行うクラス
 */

using UnityEngine;

// SledEffect プレハブにアタッチされ、X 軸方向に高速移動した後に自動で破棄される
public class SledEvent : MonoBehaviour {

    [Header("Sled Settings")]
    [Tooltip("Sledの移動速度")]
    [SerializeField] private float moveSpeed = 15.0f;

    [Tooltip("Sledが破壊されるまでの時間")]
    [SerializeField] private float lifeTime = 2.0f;

    [Header("Damage Settings")]
    [Tooltip("プレイヤーに与えるダメージ")]
    [SerializeField] private float damageAmount = 10f;

    private int direction = 1; // 1: 右、-1: 左

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

    // プレイヤーにダメージ
    private void OnTriggerEnter2D(Collider2D other) {
        if (GameManager.Instance == null) {
            return;
        }

        if (other.CompareTag("Player")) {
            Debug.Log($"Sled Effect: ヒット Damage: {damageAmount}");
            GameManager.Instance.ChangeHealth(-damageAmount);
        }
    }
}