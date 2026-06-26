using UnityEngine;
using UnityEngine.Audio;

public class HandgunHandler : MonoBehaviour {
    private Transform gunPivot;
    private SpriteRenderer playerSpriteRenderer;
    private Vector2 currentAimDirection = Vector2.right;
    private float nextFireTime = 0f;

    // 탄창 및 재장전 관련 상태 변수
    private int currentAmmo;
    private bool isReloading = false; // 재장전 중인지 확인하는 플래그
    private float reloadStartTime = 0f;

    private AudioSource audioSource;
    [Header("Audio Settings")]
    [Tooltip("총 발사 시 재생할 오디오 클립을 할당합니다.")]
    public AudioClip shootSoundClip;
    [Tooltip("재장전 시작 시 재생할 오디오 클립을 할당합니다.")]
    public AudioClip reloadSoundClip;

    [Header("Visual Settings")]
    [Tooltip("총의 SpriteRenderer를 할당합니다. (선택 사항)")]
    public SpriteRenderer gunSpriteRenderer;

    [Header("Shooting Settings")]
    [Tooltip("발사할 총알 프리팹을 할당합니다.")]
    public GameObject handgunBulletPrefab;
    public float fireRate = 0.5f;

    [Header("Ammo Settings")]
    [Tooltip("탄창의 최대 탄약 수")]
    public int maxClipAmmo = 15;
    [Tooltip("재장전이 완료되는 데 걸리는 시간")]
    public float reloadTime = 1.5f;

    [Header("Bullet Settings")]
    public float gunOffsetLength = 0.3f; // 권총 길이만큼의 오프셋
    public float bulletSpeed = 15f;

    public int CurrentAmmo {
        get { return currentAmmo; }
    }

    // 재장전 중인지 여부를 외부에서 읽기 전용으로 제공
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

        // 재장전 상태 확인
        if (isReloading) {
            HandleReloading();
            return;
        }

        // 발사 입력 처리
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime) {
            Shoot();
        }

        // 재장전 입력 처리 (R 키)
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxClipAmmo && !isReloading) {
            StartReload();
        }
    }

    // 재장전 로직을 처리하는 메서드
    private void HandleReloading() {
        if (Time.time >= reloadStartTime + reloadTime) {
            currentAmmo = maxClipAmmo;
            isReloading = false;
        }
    }

    // 재장전 시작을 처리하는 메서드
    private void StartReload() {
        if (reloadSoundClip != null && audioSource != null) {
            audioSource.PlayOneShot(reloadSoundClip);
        }
        isReloading = true;
        reloadStartTime = Time.time;
    }

    private void HandleRotation() {
        // 마우스의 스크린 좌표를 월드 좌표로 변환
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        // 방향 벡터 계산 및 저장 (Shoot()에서 재사용)
        Vector2 direction = worldPos - gunPivot.position;
        currentAimDirection = direction.normalized;

        // 각도 계산 및 회전 적용
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        gunPivot.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        // 총의 Y축 스케일 조정 (총이 뒤집히지 않도록)
        if (angle > 90f || angle < -90f) {
            gunPivot.localScale = new Vector3(1, -1, 1);
        } else {
            gunPivot.localScale = new Vector3(1, 1, 1);
        }
    }

    private void Shoot() {
        // 탄약 확인: 탄약이 없으면 자동 재장전
        if (currentAmmo <= 0) {
            StartReload();
            nextFireTime = Time.time + fireRate; // 연사 속도 유지
            return;
        }

        // 발사 로직
        currentAmmo--; // 탄약 1발 감소
        nextFireTime = Time.time + fireRate; // 다음 발사 가능 시간 업데이트

        if (shootSoundClip != null && audioSource != null) {
            audioSource.PlayOneShot(shootSoundClip);
        }

        if (handgunBulletPrefab == null) {
            Debug.LogWarning("Handgun Bullet 프리팹이 할당되지 않았습니다.");
            return;
        }

        Vector3 gunCenterPosition = gunPivot.position;
        // 총구 위치 계산 (총알 발사 시작점)
        Vector3 startPoint = gunCenterPosition + (Vector3)currentAimDirection * gunOffsetLength;

        // 총알 인스턴스화
        Quaternion bulletRotation = Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2(currentAimDirection.y, currentAimDirection.x) * Mathf.Rad2Deg));
        GameObject bulletObject = Instantiate(handgunBulletPrefab, startPoint, bulletRotation);

        // 총알 컨트롤러를 찾아 초기화 및 발사
        HandgunBulletController bulletHandler = bulletObject.GetComponent<HandgunBulletController>();

        if (bulletHandler != null) {
            // 총알에 방향과 속도를 전달하여 발사
            bulletHandler.Fire(currentAimDirection, bulletSpeed);
        } else {
            // Rigidbody를 사용한 간단한 발사 로직
            Rigidbody2D rb = bulletObject.GetComponent<Rigidbody2D>();
            if (rb != null) {
                // rb.linearVelocity -> rb.velocity 로 수정 필요 (이전 대화에서 수정하기로 함)
                rb.linearVelocity = currentAimDirection * bulletSpeed;
            } else {
                Debug.LogError("HandgunBulletController 또는 Rigidbody2D 컴포넌트를 찾을 수 없습니다. 프리팹에 추가했는지 확인하세요.");
            }
        }
    }
}