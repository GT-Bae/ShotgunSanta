using UnityEngine;
using System.Collections;

public class SnowBallEffect : MonoBehaviour {
    [Header("Phase 1: Scale & Y Position Change")]
    [Tooltip("Phase 1의 목표 X 스케일.")]
    public float targetScaleX = 8.31f;
    [Tooltip("Phase 1의 목표 Y 스케일.")]
    public float targetScaleY = 8.31f;
    [Tooltip("Phase 1의 목표 Z 스케일 (일반적으로 변경하지 않음).")]
    public float targetScaleZ = 1.0f;

    [Tooltip("Phase 1의 목표 Y 월드 위치.")]
    public float targetPosY1 = 7.18f;
    [Tooltip("Phase 1의 변형에 걸리는 시간 (예시: 0.5초 또는 그 이상).")]
    public float phase1Duration = 3.0f;

    [Header("Phase 2: Final Y Position Movement")]
    [Tooltip("Phase 2의 최종 목표 Y 월드 위치 (-1.03).")]
    public float targetPosY2 = -1.03f;
    [Tooltip("Phase 2 이동에 걸리는 시간 (1초 고정).")]
    public float phase2Duration = 1.0f;

    private void Start() {
        StartCoroutine(ExecuteSnowBallPattern());
    }

    // 스노우볼의 크기/위치 변형 및 최종 이동을 관리하는 코루틴
    public IEnumerator ExecuteSnowBallPattern() {
        Vector3 initialScale = transform.localScale;
        Vector3 initialPosition = transform.position;

        //목표 스케일 설정
        Vector3 targetScale = new Vector3(targetScaleX, targetScaleY, targetScaleZ);

        // 목표 위치 설정 (X, Z는 고정, Y만 변경)
        Vector3 targetPosition1 = new Vector3(initialPosition.x, targetPosY1, initialPosition.z);

        float timer = 0f;

        // 크기 변형 및 Y축 위치 이동
        while (timer < phase1Duration) {
            float t = timer / phase1Duration;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            transform.position = Vector3.Lerp(initialPosition, targetPosition1, t);
            timer += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        transform.localScale = targetScale;
        transform.position = targetPosition1;

        // 최종 Y축 위치 이동 (1초)
        Vector3 startPosition2 = transform.position;
        // Phase 2 목표 위치 설정 (X, Z는 고정, Y를 targetPosY2로 변경)
        Vector3 targetPosition2 = new Vector3(startPosition2.x, targetPosY2, startPosition2.z);

        timer = 0f; // 타이머 재설정

        while (timer < phase2Duration) // phase2Duration = 1.0f
        {
            float t = timer / phase2Duration;

            // Y축 위치만 1초 동안 -1.03까지 이동
            transform.position = Vector3.Lerp(startPosition2, targetPosition2, t);

            timer += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }
        transform.position = targetPosition2;

        Destroy(gameObject);
    }
}