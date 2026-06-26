/*
 * ボスの攻撃パターンを制御するクラス
 */

using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class BossPattern : MonoBehaviour {
    [Header("Prefabs")]
    [SerializeField] private GameObject aoePrefab;
    [SerializeField] private GameObject shotgunEffectPrefab;
    [SerializeField] private GameObject sledEffectPrefab;
    [SerializeField] private GameObject snowPrefab;
    [SerializeField] private GameObject snowEffectAttackPrefab;
    [SerializeField] private GameObject giftBoxPrefab;
    [SerializeField] private GameObject boomBoxPrefab;
    [SerializeField] private GameObject santaBagPrefab;
    [SerializeField] private GameObject bigGiftBoxPrefab;

    [Header("Grid Reference")]
    [SerializeField] private Tilemap targetTilemap;

    [Header("AoE Grid Area (Cell Position)")]
    [SerializeField] private Vector3Int minCellBounds = new Vector3Int(-5, -3, 0);
    [SerializeField] private Vector3Int maxCellBounds = new Vector3Int(4, 0, 0);

    [Header("AoE Settings (AoESquareFill.cs와 동기화)")]
    [SerializeField] private float aoeSize = 1.0f;
    [SerializeField] private float aoeInitialDuration = 1.0f; // 警告の維持時間
    [SerializeField] private float aoeFadeDuration = 1.0f; // 消える時間

    private bool animationTriggeredThisCycle = false;
    private bool isSoundPlayedThisPattern = false;
    private AudioSource audioSource;
    [Header("Health Reference")]
    [SerializeField] private BossHealth bossHealth;

    [Header("Audio")]
    [Tooltip("Shotgun Effectが生成される時に再生するサウンド")]
    public AudioClip shotgunSoundClip;
    public AudioClip sledSoundClip;
    public AudioClip bellSoundClip;
    public AudioClip snowSoundClip;
    public AudioClip boxBellSoundClip;
    public AudioClip fallingSoundClip;

    private Animator animator;
    private bool isInvulnerable = false;
    // 現在メーンパターンコルーチンを追跡
    private Coroutine mainPatternCoroutine;
    // 現在実行中のパターンを追跡
    private Coroutine activeSubPatternCoroutine;

    // 現在パターンのフェーズの変数
    private int currentPhase = 1;
    private const float PHASE_TWO_THRESHOLD = 0.5f; // 体力50%感知のため

    public bool IsInvulnerable() {
        return isInvulnerable;
    }

    private void Start() {
        animator = GetComponent<Animator>();
        if (bossHealth == null) {
            bossHealth = GetComponent<BossHealth>();
        }
        if (bossHealth != null) {
            bossHealth.OnHealthChanged += HandleHealthChanged;
        }
        audioSource = GetComponent<AudioSource>();
        mainPatternCoroutine = StartCoroutine(MainPatternSequence(1));
    }
    private void OnDestroy() {
        if (bossHealth != null) {
            bossHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }
    
    // フェーズによりパターン進行
    public IEnumerator MainPatternSequence(int phase) {
        while (true) {
            if (phase == 1) {
                yield return new WaitForSeconds(1.0f);

                activeSubPatternCoroutine = StartCoroutine(PatternShoot());
                yield return activeSubPatternCoroutine;
                yield return new WaitForSeconds(1.0f);

                activeSubPatternCoroutine = StartCoroutine(PatternThrowTheBigBox());
                yield return activeSubPatternCoroutine;

                animator.SetBool("IsSledPattern", true);
                activeSubPatternCoroutine = StartCoroutine(PatternSled());
                yield return activeSubPatternCoroutine;
                animator.SetBool("IsSledPattern", false);
                yield return new WaitForSeconds(1.0f);

                activeSubPatternCoroutine = StartCoroutine(PatternSnow());
                yield return activeSubPatternCoroutine;
                yield return new WaitForSeconds(1.0f);
            } else if (phase == 2) {
                activeSubPatternCoroutine = StartCoroutine(PatternBoxBell());
                yield return activeSubPatternCoroutine;
                yield return new WaitForSeconds(1.0f);
                
                animator.SetBool("IsSledPattern", true);
                activeSubPatternCoroutine = StartCoroutine(PatternSled2());
                yield return activeSubPatternCoroutine;
                animator.SetBool("IsSledPattern", false);

                activeSubPatternCoroutine = StartCoroutine(PatternBagDrop());
                yield return activeSubPatternCoroutine;
                yield return new WaitForSeconds(1.0f);

                animator.SetBool("IsShooting", true);
                activeSubPatternCoroutine = StartCoroutine(PatternShoot(35));
                yield return activeSubPatternCoroutine;
                animator.SetBool("IsShooting", false);
                yield return new WaitForSeconds(1.0f);
            }
            
            // フェーズ転換時ループ終了
            if (phase != currentPhase) {
                Debug.Log($"フェーズが{phase}から{currentPhase}に転換されました。現在パターンのループ終了。");
                yield break;
            }

            yield return null; // 次のフレームまで待つ (フリーズを防ぐため)
        }
    }

    // 体力の変更を感知し、フェーズ転換
    private void HandleHealthChanged(float currentHealth, float maxHealth) {
        float healthPercentage = currentHealth / maxHealth;

        if (healthPercentage < PHASE_TWO_THRESHOLD && currentPhase == 1) {
            // 現在実行中のメーンのループ中止
            if (mainPatternCoroutine != null) {
                StopCoroutine(mainPatternCoroutine);
                mainPatternCoroutine = null;
            }

            // 現在実行中のパターン中止
            if (activeSubPatternCoroutine != null) {
                StopCoroutine(activeSubPatternCoroutine);
                activeSubPatternCoroutine = null;
            }

            // アニメーター整理
            animator.SetBool("IsShooting", false);
            animator.SetBool("IsSledPattern", false);
            animator.SetBool("IsSnowPattern", false);
            animator.SetBool("IsBagPattern", false);

            // 2フェーズ開始
            currentPhase = 2;
            StartCoroutine(MainPatternSequence(2));
        }
    }

    // aoeCountPerShotの数だけ、2秒おきに攻撃
    public IEnumerator PatternShoot(int aoeCountPerShot = 30) {
        const int repeatCount = 5;
        const float targetPatternPeriod = 2.0f;
        animator.SetBool("IsShooting", true);

        for (int i = 0; i < repeatCount; i++) {
            Debug.Log($"PatternShoot: {i + 1}/{repeatCount}目のAoE生成開始。(周期目標: {targetPatternPeriod}秒)");
            float actualPatternDuration = aoeInitialDuration + aoeFadeDuration;
            yield return StartCoroutine(ExecuteNonOverlappingAoEPattern(aoeCountPerShot));
            float delayBetweenShots = targetPatternPeriod - actualPatternDuration;

            if (delayBetweenShots > 0) {
                Debug.Log($"PatternShoot: 次の発射まで{delayBetweenShots:F2}秒追加で待つ。");
                yield return new WaitForSeconds(delayBetweenShots);
            }
        }

        animator.SetBool("IsShooting", false);
    }

    // 指定したグリッド領域にAoEを同時生成し、AoEの警告直後にShotgunEffectを生成するパターン
    public IEnumerator ExecuteNonOverlappingAoEPattern(int numberOfAoEs) {
        List<Vector3> spawnPositions = GetUniqueWorldPositions(numberOfAoEs);
        // Shotgun Effect が発動すべきタイミング = AoE警告の表示時間
        float effectTriggerDuration = aoeInitialDuration;
        animationTriggeredThisCycle = false;
        foreach (Vector3 worldPos in spawnPositions) {
            GameObject aoeInstance = Instantiate(aoePrefab, worldPos, Quaternion.identity);
            aoeInstance.transform.localScale = new Vector3(aoeSize, aoeSize, 1f);
            StartCoroutine(AoEFollowUp(worldPos, effectTriggerDuration));
        }

        float totalAoeDuration = aoeInitialDuration + aoeFadeDuration;
        yield return new WaitForSeconds(totalAoeDuration);
    }

    // AoE Initial Duration 終了後に Shotgun Effect を生成する個別コルーチン
    private IEnumerator AoEFollowUp(Vector3 spawnWorldPosition, float triggerDelay) {
        // AoEがInitial Durationの時間だけ出た後に待機終了→すぐにShotgun Effect生成
        yield return new WaitForSeconds(triggerDelay);

        SpawnShotgunEffect(spawnWorldPosition);

        if (animator != null && !animationTriggeredThisCycle) {
            animator.SetTrigger("ShootTrigger");

            // サウンド再生 (一度に一回のみ)
            if (audioSource != null && shotgunSoundClip != null) {
                audioSource.PlayOneShot(shotgunSoundClip);
            }

            animationTriggeredThisCycle = true;
        }
    }

    // 指定した個数分の重複しないランダムなCell座標を取得し、World座標のリストとして返す
    private List<Vector3> GetUniqueWorldPositions(int count) {

        HashSet<Vector3Int> selectedCells = new HashSet<Vector3Int>();
        List<Vector3> worldPositions = new List<Vector3>();

        int minX = minCellBounds.x;
        int maxX = maxCellBounds.x;
        int minY = minCellBounds.y;
        int maxY = maxCellBounds.y;
        int cellRange = (maxX - minX + 1) * (maxY - minY + 1);

        // 指定された数が全体の範囲より大きければ、全体の範囲の分だけ生成
        if (count > cellRange) {
            count = cellRange;
            Debug.LogWarning($"要求されたAoE個数({count})が全体グリッド範囲({cellRange})より大きいため最大値に調整します。");
        }

        // 重複しない位置が見つかるまで繰り返す
        while (selectedCells.Count < count) {
            int randomX = Random.Range(minX, maxX + 1);
            int randomY = Random.Range(minY, maxY + 1);
            Vector3Int randomCell = new Vector3Int(randomX, randomY, minCellBounds.z);

            if (!selectedCells.Contains(randomCell)) {
                selectedCells.Add(randomCell);
                worldPositions.Add(targetTilemap.GetCellCenterWorld(randomCell));
            }
        }

        return worldPositions;
    }

    // 指定したWorld座標にShotgunEffectプレハブを生成する
    private void SpawnShotgunEffect(Vector3 position) {
        if (shotgunEffectPrefab != null) {
            Instantiate(shotgunEffectPrefab, position, Quaternion.identity);
        }
    }

    public IEnumerator PatternSled() {
        const float delayBetweenSledShots = 2.0f;
        float totalAoeDuration = aoeInitialDuration + aoeFadeDuration;
        float actualSledDelay = delayBetweenSledShots - totalAoeDuration;

        actualSledDelay = Mathf.Max(0, actualSledDelay);

        StartCoroutine(ExecuteSledShot(new int[] { 0, 2, }, false));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(ExecuteSledShot(new int[] { 1, 3 }, true));
        yield return new WaitForSeconds(totalAoeDuration + actualSledDelay);

        StartCoroutine(ExecuteSledShot(new int[] { 1, 3 }, false));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(ExecuteSledShot(new int[] { 0, 2 }, true));
        yield return new WaitForSeconds(totalAoeDuration + actualSledDelay);

        StartCoroutine(ExecuteSledShot(new int[] { 0, 1, 2 }, false));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(ExecuteSledShot(new int[] { 3 }, true));
        yield return new WaitForSeconds(totalAoeDuration + actualSledDelay);

        StartCoroutine(ExecuteSledShot(new int[] { 1, 2, 3 }, false));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(ExecuteSledShot(new int[] { 0 }, true));
        yield return new WaitForSeconds(totalAoeDuration); // AoE完了待機
        Debug.Log("PatternSled: パターン終了。");
    }

    public IEnumerator PatternSled2() {
        const float delayBetweenSledShots = 2.0f;
        float totalAoeDuration = aoeInitialDuration + aoeFadeDuration;
        float actualSledDelay = delayBetweenSledShots - totalAoeDuration;

        actualSledDelay = Mathf.Max(0, actualSledDelay);

        StartCoroutine(ExecuteSledShot(new int[] { 0, }, false));
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(ExecuteSledShot(new int[] { 1, }, false));
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(ExecuteSledShot(new int[] { 2, }, false));
        yield return new WaitForSeconds(0.4f);
        StartCoroutine(ExecuteSledShot(new int[] { 3, }, false));
        yield return new WaitForSeconds(1.0f);
        StartCoroutine(ExecuteSledShot(new int[] { 3, }, true));
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(ExecuteSledShot(new int[] { 2, }, true));
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(ExecuteSledShot(new int[] { 1, }, true));
        yield return new WaitForSeconds(0.4f);
        StartCoroutine(ExecuteSledShot(new int[] { 0, }, true));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(ExecuteSledShot(new int[] { 0, 2 }, false));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(ExecuteSledShot(new int[] { 1, 3 }, true));
        yield return new WaitForSeconds(totalAoeDuration + actualSledDelay);
    }

    // Sledパターンの単発ショットロジック：指定したy軸の行にAoEを生成し、消えた後にSled Effectを生成します。
    private IEnumerator ExecuteSledShot(int[] relativeRows, bool reverseX, int aoeCountPerLine = 8) {
        List<Vector3> aoeSpawnPositions = new List<Vector3>();
        float effectTriggerDuration = aoeInitialDuration;

        if (animator != null) {
            animator.SetTrigger("BellTrigger");
        }
        if (audioSource != null && bellSoundClip != null) {
            audioSource.PlayOneShot(bellSoundClip);
        }
        foreach (int relativeY in relativeRows) {
            int yCell = minCellBounds.y + relativeY;

            int startX = minCellBounds.x;
            int endX = maxCellBounds.x;

            // minCellBounds.x から maxCellBounds.x まですべて含む
            for (int x = startX; x <= endX; x++) {
                Vector3Int cellPos = new Vector3Int(x, yCell, minCellBounds.z);
                aoeSpawnPositions.Add(targetTilemap.GetCellCenterWorld(cellPos));
            }

            // Sled Effect の開始位置を計算（境界の外側1マス）
            Vector3 sledSpawnWorldPos;
            if (reverseX) {
                // 左右反転（左方向に移動）
                Vector3Int cellPos = new Vector3Int(maxCellBounds.x + 1, yCell, minCellBounds.z);
                sledSpawnWorldPos = targetTilemap.GetCellCenterWorld(cellPos);
            } else {
                // 正方向（右方向に移動）
                Vector3Int cellPos = new Vector3Int(minCellBounds.x - 1, yCell, minCellBounds.z);
                sledSpawnWorldPos = targetTilemap.GetCellCenterWorld(cellPos);
            }

            StartCoroutine(AoEFollowUpWithSled(sledSpawnWorldPos, effectTriggerDuration, reverseX));
        }

        foreach (Vector3 worldPos in aoeSpawnPositions) {
            GameObject aoeInstance = Instantiate(aoePrefab, worldPos, Quaternion.identity);
            aoeInstance.transform.localScale = new Vector3(aoeSize, aoeSize, 1f);
        }

        yield break;
    }

    // AoE が Initial Duration 後に Sled Effect を生成する個別コルーチン
    private IEnumerator AoEFollowUpWithSled(Vector3 spawnWorldPosition, float triggerDelay, bool reverseX) {
        // AoE警告時間ほど待機
        yield return new WaitForSeconds(triggerDelay);
        SpawnSledEffect(spawnWorldPosition, reverseX);
    }

    // 指定した World 座標に SledEffect プレハブを生成し、左右反転を適用する
    private void SpawnSledEffect(Vector3 position, bool reverseX) {
        if (sledEffectPrefab != null) {
            GameObject sledInstance = Instantiate(sledEffectPrefab, position, Quaternion.identity);

            if (reverseX) {
                // X軸スケールを-1に設定して左右反転する
                sledInstance.transform.localScale = new Vector3(
                    -sledInstance.transform.localScale.x,
                    sledInstance.transform.localScale.y,
                    sledInstance.transform.localScale.z
                );
            }

            if (audioSource != null && sledSoundClip != null) {
                audioSource.PlayOneShot(sledSoundClip);
            }
        }
    }

    // SnowパターンのAoE召喚ロジック：四隅（4マス）を除くすべてのセルにAoEを生成
    private IEnumerator ExecuteSnowAoE() {
        List<Vector3> aoeSpawnPositions = new List<Vector3>();
        float effectTriggerDuration = aoeInitialDuration;

        // グリッド全体の範囲 (X: -5 ~ 4, Y: -3 ~ 0)
        int minX = minCellBounds.x;
        int maxX = maxCellBounds.x;
        int minY = minCellBounds.y;
        int maxY = maxCellBounds.y;

        // 四隅の座標
        // (minX, minY), (maxX, minY), (minX, maxY), (maxX, maxY)
        HashSet<Vector3Int> corners = new HashSet<Vector3Int> {
            new Vector3Int(minX, minY, 0),
            new Vector3Int(maxX, minY, 0),
            new Vector3Int(minX, maxY, 0),
            new Vector3Int(maxX, maxY, 0)
        };

        // 四隅を除くすべてのマスの位置を計算する
        for (int y = minY; y <= maxY; y++) {
            for (int x = minX; x <= maxX; x++) {
                Vector3Int cellPos = new Vector3Int(x, y, minCellBounds.z);

                if (!corners.Contains(cellPos)) {
                    aoeSpawnPositions.Add(targetTilemap.GetCellCenterWorld(cellPos));
                }
            }
        }

        // AoE 生成
        foreach (Vector3 worldPos in aoeSpawnPositions) {
            GameObject aoeInstance = Instantiate(aoePrefab, worldPos, Quaternion.identity);
            aoeInstance.transform.localScale = new Vector3(aoeSize, aoeSize, 1f);

            // Snow パターンでは AoEFollowUpWithSnow を使用し、AoE が消えた後にダメージを発生させる
            StartCoroutine(AoEFollowUpWithSnow(worldPos, effectTriggerDuration, 30));
        }

        float totalAoeDuration = aoeInitialDuration + aoeFadeDuration;
        yield return new WaitForSeconds(totalAoeDuration);
    }

    // AoE が Initial Duration 後に Snow パターンのダメージ効果を生成する個別コルーチン
    private IEnumerator AoEFollowUpWithSnow(Vector3 spawnWorldPosition, float triggerDelay, int damageAmount) {
        // AoE 警告時間だけ待機
        yield return new WaitForSeconds(triggerDelay);
        const float additionalDelay = 1.0f;
        yield return new WaitForSeconds(additionalDelay);
        if (!isSoundPlayedThisPattern) {
            if (audioSource != null && snowSoundClip != null) {
                audioSource.PlayOneShot(snowSoundClip);
                isSoundPlayedThisPattern = true;
            }
        }
        Instantiate(snowEffectAttackPrefab, spawnWorldPosition, Quaternion.identity);
    }

    // Snow パターンのコルーチン：四隅を除いて AoE を生成し、SnowPrefab を生成し、4秒待機後にサウンドと効果を終了する。
    public IEnumerator PatternSnow() {
        isSoundPlayedThisPattern = false;
        animator.SetBool("IsSnowPattern", true);

        // Snow プレハブを生成（移動はプレハブ側のスクリプトで処理）
        if (snowPrefab != null) {
            Instantiate(snowPrefab, transform.position, Quaternion.identity);
        }

        yield return StartCoroutine(ExecuteSnowAoE());

        animator.SetBool("IsSnowPattern", false);
        isSoundPlayedThisPattern = false;
    }

    // 4つのランダムのマスにAoE警告後プレゼント箱3つ, 爆発箱1つ生成する
    public IEnumerator PatternBoxBell() {
        const int totalBoxesToSpawn = 4;
        const int boomBoxCount = 1;

        if (animator != null) {
            animator.SetTrigger("BoxBellTrigger");
        }
        if (audioSource != null && boxBellSoundClip != null) {
            audioSource.PlayOneShot(boxBellSoundClip);
        }

        // AoE生成位置を4つ
        List<Vector3> spawnPositions = GetUniqueWorldPositions(totalBoxesToSpawn);

        // AoEパターンの実行時間を計算
        float totalAoeDuration = aoeInitialDuration + aoeFadeDuration;
        float effectTriggerDuration = aoeInitialDuration;

        for (int i = 0; i < totalBoxesToSpawn; i++) {
            Vector3 worldPos = spawnPositions[i];

            // AoE 生成
            GameObject aoeInstance = Instantiate(aoePrefab, worldPos, Quaternion.identity);
            aoeInstance.transform.localScale = new Vector3(aoeSize, aoeSize, 1f);

            // 生成する箱の種類を決定 (i=0のときは爆発箱, それ以外はプレゼント箱)
            GameObject boxPrefabToSpawn = (i < boomBoxCount) ? boomBoxPrefab : giftBoxPrefab;

            StartCoroutine(AoEFollowUpWithBox(worldPos, effectTriggerDuration, boxPrefabToSpawn));
        }

        // AoE 待機
        yield return new WaitForSeconds(totalAoeDuration);
    }

    // AoEの後に指定した箱プレハブを生成する個別コルーチン
    private IEnumerator AoEFollowUpWithBox(Vector3 spawnWorldPosition, float triggerDelay, GameObject boxPrefab) {
        // AoE 警告時間だけ待機
        yield return new WaitForSeconds(triggerDelay);

        // 箱プリハブ生成
        if (boxPrefab != null) {
            Instantiate(boxPrefab, spawnWorldPosition, Quaternion.identity);
        }
    }

    // Bag パターン
    public IEnumerator PatternBagDrop() {
        // アニメーターおよび無敵を活性化
        if (animator != null) {
            animator.SetBool("IsBagPattern", true);
        }
        isInvulnerable = true;

        float bagDropDelay = 0.5f;

        // 正方向 Bag（右端の列を除く）
        // AoE 召喚位置を取得 (minX ~ maxX-1)
        List<Vector3> aoePositions1 = GetPositionsExcludingColumns(minCellBounds.x, maxCellBounds.x - 1);

        StartCoroutine(ExecuteAoEWarning(aoePositions1));

        yield return new WaitForSeconds(aoeInitialDuration);
        if (audioSource != null && fallingSoundClip != null) {
            audioSource.PlayOneShot(fallingSoundClip);
        }
        yield return new WaitForSeconds(aoeFadeDuration+bagDropDelay);
        SpawnSantaBag(false);
        yield return new WaitForSeconds(1.0f);

        // 逆方向 Bag（左端の列を除く）
        // AoE 召喚位置を取得 (minX+1 ~ maxX)
        List<Vector3> aoePositions2 = GetPositionsExcludingColumns(minCellBounds.x + 1, maxCellBounds.x);

        StartCoroutine(ExecuteAoEWarning(aoePositions2));

        yield return new WaitForSeconds(aoeInitialDuration);
        if (audioSource != null && fallingSoundClip != null) {
            audioSource.PlayOneShot(fallingSoundClip);
        }
        yield return new WaitForSeconds(aoeFadeDuration + bagDropDelay);
        SpawnSantaBag(true);
        yield return new WaitForSeconds(1.0f);

        // アニメーターおよび無敵を解除
        if (animator != null) {
            animator.SetBool("IsBagPattern", false);
        }
        isInvulnerable = false;
    }

    // 指定したX軸の列範囲にAoE警告を表示
    private IEnumerator ExecuteAoEWarning(List<Vector3> spawnPositions) {
        float totalDuration = aoeInitialDuration + aoeFadeDuration;
        float effectTriggerDuration = aoeInitialDuration;

        foreach (Vector3 worldPos in spawnPositions) {
            GameObject aoeInstance = Instantiate(aoePrefab, worldPos, Quaternion.identity);
            aoeInstance.transform.localScale = new Vector3(aoeSize, aoeSize, 1f);
        }

        // AoE 待機
        yield return new WaitForSeconds(totalDuration);
    }

    // 指定したX軸範囲内のすべてのCellのWorld位置を返す
    private List<Vector3> GetPositionsExcludingColumns(int startX, int endX) {
        List<Vector3> worldPositions = new List<Vector3>();

        int minY = minCellBounds.y;
        int maxY = maxCellBounds.y;

        for (int y = minY; y <= maxY; y++) {
            for (int x = startX; x <= endX; x++) {
                Vector3Int cellPos = new Vector3Int(x, y, minCellBounds.z);
                worldPositions.Add(targetTilemap.GetCellCenterWorld(cellPos));
            }
        }
        return worldPositions;
    }

    // Santa Bagを生成して移動開始
    private void SpawnSantaBag(bool isXFlipped) {
        if (santaBagPrefab == null) return;

        Vector3 initialSpawnPosition = transform.position;
        initialSpawnPosition.y = 10f;

        GameObject bagInstance = Instantiate(santaBagPrefab, initialSpawnPosition, Quaternion.identity);
        SantaBagMovement bagMovement = bagInstance.GetComponent<SantaBagMovement>();
        

        if (bagMovement != null) {
            bagMovement.InitializeAndStart(initialSpawnPosition, isXFlipped);
        }
    }


    // 6番目のパターン：BigGiftBox を召喚し、アニメーションを再生したまま待機
    public IEnumerator PatternThrowTheBigBox() {
        const float patternDuration = 3.0f;

        if (animator != null) {
            animator.SetBool("IsSnowPattern", true);
        }

        Vector3 spawnPosition = transform.position;
        spawnPosition.y = 5.57f;

        Instantiate(bigGiftBoxPrefab, spawnPosition, Quaternion.identity);

        // BigGiftBox の全体パターン（スケールアップ、AoE 待機、移動、爆発）が終わるまで待機
        yield return new WaitForSeconds(patternDuration);

        // アニメーター終了
        if (animator != null) {
            animator.SetBool("IsSnowPattern", false);
        }
    }
}