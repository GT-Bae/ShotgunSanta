using UnityEngine;

public class HandgunBulletController : MonoBehaviour {
    public float damage = 10f;
    private Rigidbody2D rb;
    private float currentSpeed;
    private Vector2 direction;

    [Tooltip("총알이 자동으로 파괴되기까지의 시간")]
    public float maxLifetime = 2f;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, maxLifetime);
    }

    // HandgunHandler에서 호출하여 총알을 발사
    public void Fire(Vector2 fireDirection, float speed) {
        direction = fireDirection;
        currentSpeed = speed;

        if (rb != null) {
            rb.linearVelocity = direction * currentSpeed;
        }
    }

    // 적과의 충돌 처리
    private void OnTriggerEnter2D(Collider2D other) {

        // 충돌한 객체에서 ITakeDamage 인터페이스를 가진 컴포넌트를 찾음
        ITakeDamage damageReceiver = other.gameObject.GetComponent<ITakeDamage>();

        if (damageReceiver != null) {
            damageReceiver.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}