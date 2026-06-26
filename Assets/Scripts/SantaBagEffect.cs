using UnityEngine;
using System.Collections;

public class SantaBagMovement : MonoBehaviour {
    private Vector3 targetPosition;

    [SerializeField] private float moveDuration = 0.5f;

    private SantaBagDamage damageScript;
    private SpriteRenderer spriteRenderer;
    private bool isInitialized = false;
    [Header("Audio Settings")]
    [Tooltip("사운드를 재생할 AudioSource 컴포넌트")]
    private AudioSource audioSource;
    [Tooltip("이동 완료 시 재생할 사운드 클립")]
    [SerializeField] private AudioClip arrivalSoundClip;

    private void Awake() {
        damageScript = GetComponent<SantaBagDamage>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (damageScript == null) {
            Debug.LogError("SantaBagDamage.cs를 찾을 수 없습니다. santaBag 프리팹에 추가해주세요.");
            enabled = false;
        }
    }

    // BossPattern.cs에서 사용, 최종 목표 위치를 설정하고 이동을 시작
    public void InitializeAndStart(Vector3 initialPos, bool isXFlipped) {
        if (isInitialized) return; // 이미 초기화된 경우 무시

        transform.position = initialPos; // 초기 위치 (Y=10) 설정

        if (spriteRenderer != null) {
            spriteRenderer.flipX = isXFlipped;
        }

        // X 반전 여부에 따라 최종 목표 위치 설정
        if (isXFlipped) { // 역방향 (X Flip)
            targetPosition = new Vector3(0.48f, -0.96f, initialPos.z);
        } else { // 정방향
            targetPosition = new Vector3(-0.49f, -0.96f, initialPos.z);
        }

        isInitialized = true;
        // 이동 코루틴 시작
        StartCoroutine(MoveToTarget());
    }

    private IEnumerator MoveToTarget() {
        // 초기화가 되지 않았다면 대기
        while (!isInitialized) {
            yield return null;
        }

        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < moveDuration) {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 최종 위치에 정확히 도달
        transform.position = targetPosition;
        if (audioSource != null && arrivalSoundClip != null) {
            audioSource.PlayOneShot(arrivalSoundClip);
        }
        // 이동 완료 후 데미지 트리거 활성화
        if (damageScript != null) {
            damageScript.ActivateDamageTrigger();
        }
    }
}