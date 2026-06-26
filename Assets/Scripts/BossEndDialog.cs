using UnityEngine;
using System.Collections;

public class BossEndDIalog : MonoBehaviour {
    [Header("Core Managers")]
    public DialogManager dialogManager;
    
    [Header("Actors & Props")]
    public Transform santaObject;
    public Transform bossObject;

    [Header("Prefabs for Instantiation")]
    public GameObject coalPrefab;
    public GameObject paperPrefab;
    public GameObject paperShowPrefab;

    //인스턴스
    private SpriteRenderer coalInstance;
    private SpriteRenderer paperInstance;
    private GameObject currentPaperInstance;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip bossDieSound;
    [SerializeField] private AudioClip disappearSound;
    [SerializeField] private AudioSource audioSource;

    // 보스 피격 효과에 필요한 변수
    private SpriteRenderer bossSpriteRenderer;
    private Color bossOriginalColor;
    private Color hitColor = Color.red;
    private float hitEffectDuration = 0.2f;

    public GameObject EndUI;
    [Header("UI Reference")]
    [SerializeField] private Transform uiCanvasTransform;

    void Start() {
        if (audioSource == null) {
            audioSource = GetComponent<AudioSource>();
        }

        bossSpriteRenderer = bossObject.GetComponent<SpriteRenderer>();
        if (bossSpriteRenderer != null) {
            bossOriginalColor = bossSpriteRenderer.color;
        }

        // 프리팹 인스턴스화 및 할당
        if (coalPrefab != null) {
            GameObject coalObj = Instantiate(coalPrefab, transform.position, Quaternion.identity);
            coalInstance = coalObj.GetComponent<SpriteRenderer>();
        }
        if (paperPrefab != null) {
            GameObject paperObj = Instantiate(paperPrefab, transform.position, Quaternion.identity);
            paperInstance = paperObj.GetComponent<SpriteRenderer>();
        }

        // 초기 상태 설정
        if (santaObject != null) santaObject.gameObject.SetActive(false);
        if (coalInstance != null) coalInstance.gameObject.SetActive(false);
        if (paperInstance != null) paperInstance.gameObject.SetActive(false);

        StartCoroutine(ControlledDialogueFlow());
    }


    private IEnumerator ControlledDialogueFlow() {
        dialogManager.isAutoAdvanceEnabled = false;
        dialogManager.StartDialog(); // 대사 1
        yield return null;

        yield return WaitForUserClick();
        dialogManager.AdvanceDialog(); // 대사 2
        yield return null;

        yield return WaitForUserClick();
        dialogManager.AdvanceDialog(); // 대사 3
        yield return null;

        // 산타 등장
        audioSource.PlayOneShot(jumpSound);
        santaObject.position = new Vector3(10f, -1f, 0f);
        santaObject.gameObject.SetActive(true);
        Vector3 targetPosSanta = new Vector3(1.66f, -0.445f, 0f);
        yield return MoveParabolic(santaObject, targetPosSanta, 1.5f, 2.0f);

        dialogManager.AdvanceDialog(); // 대사 4
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 대사 5
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 대사 6
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 대사 7
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 대사 8
        yield return null;
        yield return WaitForUserClick();

        // 석탄 투척 및 타격
        if (coalInstance != null) {
            audioSource.PlayOneShot(hitSound);
            coalInstance.gameObject.SetActive(true);
            coalInstance.transform.position = santaObject.position;
            yield return ProjectileMove(coalInstance.transform, bossObject.position, 0.3f, 720f);
            yield return HandleBossHit();
            coalInstance.gameObject.SetActive(false);
        }

        dialogManager.AdvanceDialog(); // 대사 9
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 대사 10
        audioSource.PlayOneShot(bossDieSound);
        Vector3 bossLastPos = bossObject.position;

        // 보스 소멸
        yield return BossDissolveEffect(bossObject, 2.0f);

        // 종이 소환
        if (paperInstance != null) {
            paperInstance.gameObject.SetActive(true);
            paperInstance.transform.position = bossLastPos;
            Vector3 targetPosPaper = new Vector3(0f, 0.69f, 0f);
            yield return ProjectileMove(paperInstance.transform, targetPosPaper, 1.0f, 360f);
        }

        dialogManager.AdvanceDialog(); // 대사 11
        yield return null;
        yield return WaitForUserClick();
        yield return new WaitForSeconds(0.5f);

        // 종이 UI 표시
        if (paperShowPrefab != null) {
            currentPaperInstance = Instantiate(paperShowPrefab, Vector3.zero, Quaternion.identity);

            if (uiCanvasTransform != null) {
                // 선택된 canvas를 부모로 설정
                currentPaperInstance.transform.SetParent(uiCanvasTransform, false);
                // UI가 화면 중앙에 오도록 위치 재설정
                currentPaperInstance.transform.localPosition = Vector3.zero;
            }

            yield return WaitForUserClick();

            Destroy(currentPaperInstance);
            currentPaperInstance = null;
        }

        dialogManager.AdvanceDialog(); // 대화 12
        yield return null;

        // 종이 소멸
        if (paperInstance != null) {
            audioSource.PlayOneShot(disappearSound);
            yield return DissolveSprite(paperInstance, 0.5f);
            paperInstance.gameObject.SetActive(false);
        }

        yield return WaitForUserClick();

        // 대화 13번부터 자동 진행
        dialogManager.isAutoAdvanceEnabled = true;
        dialogManager.AdvanceDialog();

        // 대화 끝까지 자동 진행 대기
        while (dialogManager.IsDialogActive) {
            yield return null;
        }

        Debug.Log(">> 시퀀스 완료 및 종료!");
        EndUI.gameObject.SetActive(true);
        DialogManager.OnDialogFinished?.Invoke();
    }

    // 클릭 / space 대기
    private IEnumerator WaitForUserClick() {
        while (dialogManager.IsTyping) {
            yield return null;
        }
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));
    }

    // 보스 피격효과
    private IEnumerator HandleBossHit() {
        if (bossSpriteRenderer != null) {
            Color tempOriginalColor = bossSpriteRenderer.color;
            bossSpriteRenderer.color = hitColor;
            yield return new WaitForSeconds(hitEffectDuration);
            bossSpriteRenderer.color = tempOriginalColor;
        }
    }

    // 물체를 startPos → endPos 까지 포물선형태로 움직임
    private IEnumerator MoveParabolic(Transform target, Vector3 endPos, float duration, float heightFactor) {
        Vector3 startPos = target.position;
        float elapsed = 0f;

        while (elapsed < duration) {
            float t = elapsed / duration;
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            float yOffset = heightFactor * (t - t * t);
            currentPos.y += yOffset;

            target.position = currentPos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.position = endPos;
    }

    // 물체를 endPos까지 움직이면서 회전
    private IEnumerator ProjectileMove(Transform target, Vector3 endPos, float duration, float rotationSpeedDegreesPerSec) {
        Vector3 startPos = target.position;
        float elapsed = 0f;

        while (elapsed < duration) {
            float t = elapsed / duration;
            target.position = Vector3.Lerp(startPos, endPos, t);
            target.Rotate(0, 0, rotationSpeedDegreesPerSec * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.position = endPos;
    }

    // 떨림과 함께 투명해지는 효과
    private IEnumerator BossDissolveEffect(Transform target, float duration) {
        float elapsed = 0f;
        Vector3 originalPos = target.position;
        if (bossSpriteRenderer == null) yield break;

        while (elapsed < duration) {
            float t = elapsed / duration;
            float shakeIntensity = 0.1f * (1f - t);
            target.position = originalPos + new Vector3(
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                0f
            );

            Color currentColor = bossSpriteRenderer.color;
            currentColor.a = Mathf.Lerp(1f, 0f, t);
            bossSpriteRenderer.color = currentColor;
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.position = originalPos;
        bossSpriteRenderer.color = new Color(bossSpriteRenderer.color.r, bossSpriteRenderer.color.g, bossSpriteRenderer.color.b, 0f);
        target.gameObject.SetActive(false);
    }

    // 투명해지는 효과
    private IEnumerator DissolveSprite(SpriteRenderer sprite, float duration) {
        float elapsed = 0f;
        Color startColor = sprite.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < duration) {
            float t = elapsed / duration;
            sprite.color = Color.Lerp(startColor, endColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        sprite.color = endColor;
    }
}
