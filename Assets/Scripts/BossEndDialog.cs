/*
 * ボスを倒せた後で進行されるエンディングを制御するクラス
 */

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

    //インスタンス
    private SpriteRenderer coalInstance;
    private SpriteRenderer paperInstance;
    private GameObject currentPaperInstance;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip bossDieSound;
    [SerializeField] private AudioClip disappearSound;
    [SerializeField] private AudioSource audioSource;

    // ボスが攻撃をくらった時のエフェクトに使う変数
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

        // プレハブを作って設定する
        if (coalPrefab != null) {
            GameObject coalObj = Instantiate(coalPrefab, transform.position, Quaternion.identity);
            coalInstance = coalObj.GetComponent<SpriteRenderer>();
        }
        if (paperPrefab != null) {
            GameObject paperObj = Instantiate(paperPrefab, transform.position, Quaternion.identity);
            paperInstance = paperObj.GetComponent<SpriteRenderer>();
        }

        // 初期状態の設定
        if (santaObject != null) santaObject.gameObject.SetActive(false);
        if (coalInstance != null) coalInstance.gameObject.SetActive(false);
        if (paperInstance != null) paperInstance.gameObject.SetActive(false);

        StartCoroutine(ControlledDialogueFlow());
    }


    private IEnumerator ControlledDialogueFlow() {
        dialogManager.isAutoAdvanceEnabled = false;
        dialogManager.StartDialog(); // 台詞 1
        yield return null;

        yield return WaitForUserClick();
        dialogManager.AdvanceDialog(); // 台詞 2
        yield return null;

        yield return WaitForUserClick();
        dialogManager.AdvanceDialog(); // 台詞 3
        yield return null;

        // サンタ登場
        audioSource.PlayOneShot(jumpSound);
        santaObject.position = new Vector3(10f, -1f, 0f);
        santaObject.gameObject.SetActive(true);
        Vector3 targetPosSanta = new Vector3(1.66f, -0.445f, 0f);
        yield return MoveParabolic(santaObject, targetPosSanta, 1.5f, 2.0f);

        dialogManager.AdvanceDialog(); // 台詞 4
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 台詞 5
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 台詞 6
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 台詞 7
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 台詞 8
        yield return null;
        yield return WaitForUserClick();

        // 石炭の投げて当てる
        if (coalInstance != null) {
            audioSource.PlayOneShot(hitSound);
            coalInstance.gameObject.SetActive(true);
            coalInstance.transform.position = santaObject.position;
            yield return ProjectileMove(coalInstance.transform, bossObject.position, 0.3f, 720f);
            yield return HandleBossHit();
            coalInstance.gameObject.SetActive(false);
        }

        dialogManager.AdvanceDialog(); // 台詞 9
        yield return null;
        yield return WaitForUserClick();

        dialogManager.AdvanceDialog(); // 台詞 10
        audioSource.PlayOneShot(bossDieSound);
        Vector3 bossLastPos = bossObject.position;

        // ボス消滅
        yield return BossDissolveEffect(bossObject, 2.0f);

        // 紙を出す
        if (paperInstance != null) {
            paperInstance.gameObject.SetActive(true);
            paperInstance.transform.position = bossLastPos;
            Vector3 targetPosPaper = new Vector3(0f, 0.69f, 0f);
            yield return ProjectileMove(paperInstance.transform, targetPosPaper, 1.0f, 360f);
        }

        dialogManager.AdvanceDialog(); // 台詞 11
        yield return null;
        yield return WaitForUserClick();
        yield return new WaitForSeconds(0.5f);

        // 紙UI表示
        if (paperShowPrefab != null) {
            currentPaperInstance = Instantiate(paperShowPrefab, Vector3.zero, Quaternion.identity);

            if (uiCanvasTransform != null) {
                currentPaperInstance.transform.SetParent(uiCanvasTransform, false);
                // UIが画面のまん中にくるように位置を直す
                currentPaperInstance.transform.localPosition = Vector3.zero;
            }

            yield return WaitForUserClick();

            Destroy(currentPaperInstance);
            currentPaperInstance = null;
        }

        dialogManager.AdvanceDialog(); // 台詞 12
        yield return null;

        // 紙を消滅
        if (paperInstance != null) {
            audioSource.PlayOneShot(disappearSound);
            yield return DissolveSprite(paperInstance, 0.5f);
            paperInstance.gameObject.SetActive(false);
        }

        yield return WaitForUserClick();

        // 台詞13番目から自動で進める
        dialogManager.isAutoAdvanceEnabled = true;
        dialogManager.AdvanceDialog();

        // 台詞が終わるまで待機
        while (dialogManager.IsDialogActive) {
            yield return null;
        }

        Debug.Log(">> シーケンス完了および終了！");
        EndUI.gameObject.SetActive(true);
        DialogManager.OnDialogFinished?.Invoke();
    }

    // クリックやスペースキーを待機
    private IEnumerator WaitForUserClick() {
        while (dialogManager.IsTyping) {
            yield return null;
        }
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));
    }

    // ボスが攻撃をくらった時のエフェクト
    private IEnumerator HandleBossHit() {
        if (bossSpriteRenderer != null) {
            Color tempOriginalColor = bossSpriteRenderer.color;
            bossSpriteRenderer.color = hitColor;
            yield return new WaitForSeconds(hitEffectDuration);
            bossSpriteRenderer.color = tempOriginalColor;
        }
    }

    // 物をstartPosからendPosまで放物線で動かす
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

    // 物をendPosまで回しながら動かす
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

    // 揺れながら透明になるエフェクト
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

    // 透明になるエフェクト
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
