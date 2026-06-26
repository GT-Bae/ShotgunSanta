/*
 * プレイヤーが発射した弾丸を制御するクラス
 */

using UnityEngine;

public class HandgunBulletController : MonoBehaviour {
    public float damage = 10f;
    private Rigidbody2D rb;
    private float currentSpeed;
    private Vector2 direction;

    [Tooltip("弾丸が破壊されるまでの時間")]
    public float maxLifetime = 2f;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, maxLifetime);
    }

    // HandgunHandlerから呼び出されて弾丸を発射
    public void Fire(Vector2 fireDirection, float speed) {
        direction = fireDirection;
        currentSpeed = speed;

        if (rb != null) {
            rb.linearVelocity = direction * currentSpeed;
        }
    }

    // 敵との衝突処理
    private void OnTriggerEnter2D(Collider2D other) {

        // 衝突したオブジェクトから ITakeDamage インターフェースを実装したコンポーネントを取得
        ITakeDamage damageReceiver = other.gameObject.GetComponent<ITakeDamage>();

        if (damageReceiver != null) {
            damageReceiver.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}