/*
 * 雪玉パターンのダメージを管理するクラス
 */ 

using UnityEngine;

// 雪玉パターンで AoE 位置に召喚され、プレイヤーにダメージを与える
public class SnowEffectAttack : MonoBehaviour {

    [Header("Attack Settings")]
    [Tooltip("プレイヤーに与えるダメージ量")]
    public int damageAmount = 30;
    [Tooltip("このエフェクトが一定時間持続した後、自動的に破棄されるまでの時間")]
    public float lifetime = 0.5f;

    private void Start() {
        Destroy(gameObject, lifetime);
    }

    // プレイヤーに与えるダメージ
    private void OnTriggerEnter2D(Collider2D other) {
        if (GameManager.Instance == null) {
            Debug.LogError("GameManager.Instanceが見つかりません");
            return;
        }

        if (other.CompareTag("Player")) {
            GameManager.Instance.ChangeHealth(-damageAmount);
        }
    }
}