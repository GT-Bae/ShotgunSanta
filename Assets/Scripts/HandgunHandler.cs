/*
 * ハンドガンの発射およびリロードを制御するクラス
 */

using UnityEngine;
using UnityEngine.Audio;

public class HandgunHandler : MonoBehaviour {
    private Transform gunPivot;
    private SpriteRenderer playerSpriteRenderer;
    private Vector2 currentAimDirection = Vector2.right;
    private float nextFireTime = 0f;

    // マガジンおよびリロードに関する状態変数
    private int currentAmmo;
    private bool isReloading = false; // 現在リロード中かどうかを示すフラグ
    private float reloadStartTime = 0f;

    private AudioSource audioSource;
    [Header("Audio Settings")]
    [Tooltip("発射時に再生する AudioClip を割り当てます")]
    public AudioClip shootSoundClip;
    [Tooltip("リロード開始時に再生する AudioClip を割り当てます")]
    public AudioClip reloadSoundClip;

    [Header("Visual Settings")]
    [Tooltip("銃の SpriteRenderer を割り当てます")]
    public SpriteRenderer gunSpriteRenderer;

    [Header("Shooting Settings")]
    [Tooltip("発射する弾丸プレハブを割り当てます")]
    public GameObject handgunBulletPrefab;
    public float fireRate = 0.5f;

    [Header("Ammo Settings")]
    [Tooltip("マガジンの最大装弾数")]
    public int maxClipAmmo = 15;
    [Tooltip("リロード完了までにかかる時間")]
    public float reloadTime = 1.5f;

    [Header("弾丸の設定")]
    public float gunOffsetLength = 0.3f;
    public float bulletSpeed = 15f;

    public int CurrentAmmo {
        get { return currentAmmo; }
    }

    // 現在リロード中かどうかを外部から読み取り専用で参照可能にする
    public bool IsReloading {
        get { return isReloading; }
    }

    void Start() {
        audioSource = GetComponent<AudioSource>();
        gunPivot = transform;
        currentAmmo = maxClipAmmo;
    }

    void Update() {
        HandleRotation();

        // リロード状態を確認
        if (isReloading) {
            HandleReloading();
            return;
        }

        // 発射入力処理
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime) {
            Shoot();
        }

        // リロード入力を処理 (Rキー)
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxClipAmmo && !isReloading) {
            StartReload();
        }
    }

    // リロード処理を実行するメソッド
    private void HandleReloading() {
        if (Time.time >= reloadStartTime + reloadTime) {
            currentAmmo = maxClipAmmo;
            isReloading = false;
        }
    }

    // リロード開始を処理するメソッド
    private void StartReload() {
        if (reloadSoundClip != null && audioSource != null) {
            audioSource.PlayOneShot(reloadSoundClip);
        }
        isReloading = true;
        reloadStartTime = Time.time;
    }

    private void HandleRotation() {
        // マウスのスクリーン座標をワールド座標に変換
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        // 方向ベクトルを計算して保持（Shoot() で再利用
        Vector2 direction = worldPos - gunPivot.position;
        currentAimDirection = direction.normalized;

        // 角度を計算して回転を適用
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        gunPivot.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        // 銃が上下反転しないようにY軸スケールを調整
        if (angle > 90f || angle < -90f) {
            gunPivot.localScale = new Vector3(1, -1, 1);
        } else {
            gunPivot.localScale = new Vector3(1, 1, 1);
        }
    }

    private void Shoot() {
        // 弾薬を確認し、残弾がない場合は自動でリロード
        if (currentAmmo <= 0) {
            StartReload();
            nextFireTime = Time.time + fireRate; // 連射間隔を維持
            return;
        }

        // 発射処理
        currentAmmo--; // 弾薬を 1 発消費
        nextFireTime = Time.time + fireRate; // 次回発射可能時刻を更新

        if (shootSoundClip != null && audioSource != null) {
            audioSource.PlayOneShot(shootSoundClip);
        }

        if (handgunBulletPrefab == null) {
            Debug.LogWarning("Handgun Bullet プレハブが割り当てられていません");
            return;
        }

        Vector3 gunCenterPosition = gunPivot.position;
        // 銃口の位置を計算（弾丸の発射開始位置）
        Vector3 startPoint = gunCenterPosition + (Vector3)currentAimDirection * gunOffsetLength;

        // 弾丸をインスタンス化
        Quaternion bulletRotation = Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2(currentAimDirection.y, currentAimDirection.x) * Mathf.Rad2Deg));
        GameObject bulletObject = Instantiate(handgunBulletPrefab, startPoint, bulletRotation);

        // 弾丸コントローラーを取得して初期化・発射
        HandgunBulletController bulletHandler = bulletObject.GetComponent<HandgunBulletController>();

        if (bulletHandler != null) {
            // 弾丸に方向と速度を渡して発射
            bulletHandler.Fire(currentAimDirection, bulletSpeed);
        } else {
            // Rigidbodyを使用したシンプルな発射処理
            Rigidbody2D rb = bulletObject.GetComponent<Rigidbody2D>();
            if (rb != null) {
                rb.linearVelocity = currentAimDirection * bulletSpeed;
            } else {
                Debug.LogError("HandgunBulletControllerまたは Rigidbody2D コンポーネントが見つかりません");
            }
        }
    }
}