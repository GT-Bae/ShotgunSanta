/*
 * Santa Bag パターンのダメージ効果を制御するクラス
 */

using UnityEngine;
using System.Collections;

public class SantaBagDamage : MonoBehaviour {
    [Header("Damage Settings")]
    [Tooltip("プレイヤーに与えるダメージ量")]
    [SerializeField] private int damageAmount = 30;

    [Tooltip("最終位置到達後にダメージトリガーを維持する時間")]
    [SerializeField] private float damageDuration = 0.5f;

    private Rigidbody2D rb;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    // SantaBagMovement.csで使う
    public void ActivateDamageTrigger() {
        StartCoroutine(SelfDestructTimer());
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (GameManager.Instance == null) {
            Debug.LogError("GameManager.Instanceが見つかりません");
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