using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
    [SerializeField] private float aoeInitialDuration = 1.0f; // 경고 유지 시간
    [SerializeField] private float aoeFadeDuration = 1.0f; // 사라지는 시간

    private bool animationTriggeredThisCycle = false;
    private bool isSoundPlayedThisPattern = false;
    private AudioSource audioSource;
    [Header("Health Reference")]
    [SerializeField] private BossHealth bossHealth;

    [Header("Audio")]
    [Tooltip("Shotgun Effect가 생성될 때 재생할 사운드 클립")]
    public AudioClip shotgunSoundClip;
    public AudioClip sledSoundClip;
    public AudioClip bellSoundClip;
    public AudioClip snowSoundClip;
    public AudioClip boxBellSoundClip;
    public AudioClip fallingSoundClip;

    private Animator animator;
    private bool isInvulnerable = false;
    // 현재 메인 패턴 코루틴을 추적
    private Coroutine mainPatternCoroutine;
    //현재 실행 중인 개별 패턴을 추적
    private Coroutine activeSubPatternCoroutine;

    // 현재 패턴 페이즈를 나타내는 변수 
    private int currentPhase = 1;
    private const float PHASE_TWO_THRESHOLD = 0.5f; // 체력 50% 임계값

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
    
    // Phase에 따라 패턴 진행
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
            
            // phase 전환시 루프 종료
            if (phase != currentPhase) {
                Debug.Log($"페이즈가 {phase}에서 {currentPhase}로 전환됨. 현재 패턴 루프 종료.");
                yield break;
            }

            yield return null; // 다음 프레임까지 대기 (무한 루프 보호)
        }
    }

    // 체력변화 감지하여 페이즈 전환
    private void HandleHealthChanged(float currentHealth, float maxHealth) {
        float healthPercentage = currentHealth / maxHealth;

        if (healthPercentage < PHASE_TWO_THRESHOLD && currentPhase == 1) {
            // 현재 실행 중인 메인 패턴 루프 중지
            if (mainPatternCoroutine != null) {
                StopCoroutine(mainPatternCoroutine);
                mainPatternCoroutine = null;
            }

            // 현재 실행 중이던 개별 패턴 코루틴이 있다면 강제 종료
            if (activeSubPatternCoroutine != null) {
                StopCoroutine(activeSubPatternCoroutine);
                activeSubPatternCoroutine = null;
            }

            // 애니메이터 정리
            animator.SetBool("IsShooting", false);
            animator.SetBool("IsSledPattern", false);
            animator.SetBool("IsSnowPattern", false);
            animator.SetBool("IsBagPattern", false);

            // 2페이즈 시작
            currentPhase = 2;
            StartCoroutine(MainPatternSequence(2));
        }
    }

    // aoeCountPerShot만큼 2초 간격으로 공격
    public IEnumerator PatternShoot(int aoeCountPerShot = 30) {
        const int repeatCount = 5;
        const float targetPatternPeriod = 2.0f;
        animator.SetBool("IsShooting", true);

        for (int i = 0; i < repeatCount; i++) {
            Debug.Log($"PatternShoot: {i + 1}/{repeatCount}회차 AoE 소환 시작. (주기 목표: {targetPatternPeriod}s)");
            float actualPatternDuration = aoeInitialDuration + aoeFadeDuration;
            yield return StartCoroutine(ExecuteNonOverlappingAoEPattern(aoeCountPerShot));
            float delayBetweenShots = targetPatternPeriod - actualPatternDuration;

            if (delayBetweenShots > 0) {
                Debug.Log($"PatternShoot: 다음 발사까지 {delayBetweenShots:F2}s 추가 대기.");
                yield return new WaitForSeconds(delayBetweenShots);
            }
        }

        animator.SetBool("IsShooting", false);
    }

    // 지정된 그리드 영역에 AoE를 동시에 생성하고, AoE 경고 직후 ShotgunEffect를 생성하는 패턴
    public IEnumerator ExecuteNonOverlappingAoEPattern(int numberOfAoEs) {
        List<Vector3> spawnPositions = GetUniqueWorldPositions(numberOfAoEs);
        // Shotgun Effect가 터져야 할 시간 = AoE 경고 유지 시간
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

    // AoE Initial Duration이 끝난 후 Shotgun Effect를 생성하는 개별 코루틴
   private IEnumerator AoEFollowUp(Vector3 spawnWorldPosition, float triggerDelay) {
        // AoE가 Initial Duration만큼 지속된 후 대기 종료 → 즉시 Shotgun Effect 생성
        yield return new WaitForSeconds(triggerDelay);

        SpawnShotgunEffect(spawnWorldPosition);

        if (animator != null && !animationTriggeredThisCycle) {
            animator.SetTrigger("ShootTrigger");

            // 사운드 재생 (한 패턴에 한 번만)
            if (audioSource != null && shotgunSoundClip != null) {
                audioSource.PlayOneShot(shotgunSoundClip);
            }

            animationTriggeredThisCycle = true;
        }
    }

    // 지정된 개수만큼 겹치지 않는 랜덤 Cell 좌표를 가져와 World 좌표 리스트로 반환
    private List<Vector3> GetUniqueWorldPositions(int count) {

        HashSet<Vector3Int> selectedCells = new HashSet<Vector3Int>();
        List<Vector3> worldPositions = new List<Vector3>();

        int minX = minCellBounds.x;
        int maxX = maxCellBounds.x;
        int minY = minCellBounds.y;
        int maxY = maxCellBounds.y;
        int cellRange = (maxX - minX + 1) * (maxY - minY + 1);

        // 요청한 개수가 전체 범위보다 크면 전체 범위까지만 소환
        if (count > cellRange) {
            count = cellRange;
            Debug.LogWarning($"요청된 AoE 개수({count})가 전체 그리드 영역({cellRange})보다 커서 최대치로 조정합니다.");
        }

        // 중복되지 않는 위치를 찾을 때까지 반복
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

    // 지정된 월드 좌표에 ShotgunEffect 프리팹을 생성
    private void SpawnShotgunEffect(Vector3 position) {
        if (shotgunEffectPrefab != null) {
            Instantiate(shotgunEffectPrefab, position, Quaternion.identity);
        }
    }

    public IEnumerator PatternSled() {
        const float delayBetweenSledShots = 2.0f;
        const float shortDelay = 1.0f;

        float totalAoeDuration = aoeInitialDuration + aoeFadeDuration;
        float actualSledDelay = delayBetweenSledShots - totalAoeDuration;
        float actualShortDelay = shortDelay - totalAoeDuration;

        // 대기 시간이 0 이하인 경우 최소값(0) 보장
        actualSledDelay = Mathf.Max(0, actualSledDelay);
        actualShortDelay = Mathf.Max(0, actualShortDelay);

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
        yield return new WaitForSeconds(totalAoeDuration); // AoE 완료 대기
        Debug.Log("PatternSled: 패턴 종료.");
    }

    public IEnumerator PatternSled2() {
        const float delayBetweenSledShots = 2.0f;
        const float shortDelay = 1.0f;

        float totalAoeDuration = aoeInitialDuration + aoeFadeDuration;
        float actualSledDelay = delayBetweenSledShots - totalAoeDuration;
        float actualShortDelay = shortDelay - totalAoeDuration;

        // 대기 시간이 0 이하인 경우 최소값(0) 보장
        actualSledDelay = Mathf.Max(0, actualSledDelay);
        actualShortDelay = Mathf.Max(0, actualShortDelay);

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

    // Sled 패턴의 단일 샷 로직: 지정된 y축 행에 AoE를 생성하고, 사라진 후 Sled Effect를 생성합니다.
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

            // minCellBounds.x 부터 maxCellBounds.x 까지 모두 포함
            for (int x = startX; x <= endX; x++) {
                Vector3Int cellPos = new Vector3Int(x, yCell, minCellBounds.z);
                aoeSpawnPositions.Add(targetTilemap.GetCellCenterWorld(cellPos));
            }

            // 2Sled Effect 시작 위치 계산 (경계 밖 한 칸)
            Vector3 sledSpawnWorldPos;
            if (reverseX) {
                // 좌우 반전 (왼쪽으로 이동)
                Vector3Int cellPos = new Vector3Int(maxCellBounds.x + 1, yCell, minCellBounds.z);
                sledSpawnWorldPos = targetTilemap.GetCellCenterWorld(cellPos);
            } else {
                // 정방향 (오른쪽으로 이동)
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

    // AoE가 Initial Duration 이후 Sled Effect를 생성하는 개별 코루틴
    private IEnumerator AoEFollowUpWithSled(Vector3 spawnWorldPosition, float triggerDelay, bool reverseX) {
        // AoE 경고 시간만큼 대기
        yield return new WaitForSeconds(triggerDelay);
        SpawnSledEffect(spawnWorldPosition, reverseX);
    }

    // 지정된 월드 좌표에 SledEffect 프리팹을 생성하고 좌우 반전을 적용
    private void SpawnSledEffect(Vector3 position, bool reverseX) {
        if (sledEffectPrefab != null) {
            GameObject sledInstance = Instantiate(sledEffectPrefab, position, Quaternion.identity);

            if (reverseX) {
                // X축 스케일을 -1로 설정하여 좌우 반전
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

    // Snow 패턴의 AoE 소환 로직: 꼭짓점(4칸)을 제외한 모든 셀에 AoE를 생성
    private IEnumerator ExecuteSnowAoE() {
        List<Vector3> aoeSpawnPositions = new List<Vector3>();
        float effectTriggerDuration = aoeInitialDuration;

        // 전체 그리드 범위 (X: -5 ~ 4, Y: -3 ~ 0)
        int minX = minCellBounds.x;
        int maxX = maxCellBounds.x;
        int minY = minCellBounds.y;
        int maxY = maxCellBounds.y;

        // 꼭짓점 좌표
        // (minX, minY), (maxX, minY), (minX, maxY), (maxX, maxY)
        HashSet<Vector3Int> corners = new HashSet<Vector3Int> {
            new Vector3Int(minX, minY, 0),
            new Vector3Int(maxX, minY, 0),
            new Vector3Int(minX, maxY, 0),
            new Vector3Int(maxX, maxY, 0)
        };

        // 꼭짓점을 제외한 모든 칸의 위치를 계산
        for (int y = minY; y <= maxY; y++) {
            for (int x = minX; x <= maxX; x++) {
                Vector3Int cellPos = new Vector3Int(x, y, minCellBounds.z);

                if (!corners.Contains(cellPos)) {
                    aoeSpawnPositions.Add(targetTilemap.GetCellCenterWorld(cellPos));
                }
            }
        }

        // AoE 생성
        foreach (Vector3 worldPos in aoeSpawnPositions) {
            GameObject aoeInstance = Instantiate(aoePrefab, worldPos, Quaternion.identity);
            aoeInstance.transform.localScale = new Vector3(aoeSize, aoeSize, 1f);

            // Snow 패턴은 AoEFollowUpWithSnow를 사용하여, AoE가 사라진 후 데미지를 발생
            StartCoroutine(AoEFollowUpWithSnow(worldPos, effectTriggerDuration, 30));
        }

        float totalAoeDuration = aoeInitialDuration + aoeFadeDuration;
        yield return new WaitForSeconds(totalAoeDuration);
    }

    // AoE가 Initial Duration 이후 Snow 패턴의 데미지 효과를 생성하는 개별 코루틴
    private IEnumerator AoEFollowUpWithSnow(Vector3 spawnWorldPosition, float triggerDelay, int damageAmount) {
        // AoE 경고 시간만큼 대기
        yield return new WaitForSeconds(triggerDelay);
        const float additionalDelay = 1.0f;
        yield return new WaitForSeconds(additionalDelay);
        if (!isSoundPlayedThisPattern) {
            if (audioSource != null && snowSoundClip != null) {
                audioSource.PlayOneShot(snowSoundClip);
                isSoundPlayedThisPattern = true; // 사운드 재생 후 플래그 설정
            }
        }
        Instantiate(snowEffectAttackPrefab, spawnWorldPosition, Quaternion.identity);
    }

    // Snow 패턴 코루틴: 꼭짓점을 제외한 AoE 소환, SnowPrefab 소환, 4초 대기 후 사운드 및 효과 종료.
    public IEnumerator PatternSnow() {
        isSoundPlayedThisPattern = false;
        animator.SetBool("IsSnowPattern", true);

        // Snow 프리팹 소환 (움직임은 프리팹 스크립트에서 알아서 처리)
        if (snowPrefab != null) {
            Instantiate(snowPrefab, transform.position, Quaternion.identity);
        }

        yield return StartCoroutine(ExecuteSnowAoE());

        animator.SetBool("IsSnowPattern", false);
        isSoundPlayedThisPattern = false;
    }

    // 4개의 랜덤 칸에 AoE 경고 후 GiftBox 3개, BoomBox 1개 소환
    public IEnumerator PatternBoxBell() {
        const int totalBoxesToSpawn = 4;
        const int boomBoxCount = 1;

        if (animator != null) {
            animator.SetTrigger("BoxBellTrigger");
        }
        if (audioSource != null && boxBellSoundClip != null) {
            audioSource.PlayOneShot(boxBellSoundClip);
        }

        // AoE 소환 위치 4개
        List<Vector3> spawnPositions = GetUniqueWorldPositions(totalBoxesToSpawn);

        // AoE 패턴 실행 시간 계산
        float totalAoeDuration = aoeInitialDuration + aoeFadeDuration;
        float effectTriggerDuration = aoeInitialDuration;

        for (int i = 0; i < totalBoxesToSpawn; i++) {
            Vector3 worldPos = spawnPositions[i];

            // AoE 생성
            GameObject aoeInstance = Instantiate(aoePrefab, worldPos, Quaternion.identity);
            aoeInstance.transform.localScale = new Vector3(aoeSize, aoeSize, 1f);

            // 소환할 박스 종류 결정 (i=0일 때 BoomBox, 나머지는 GiftBox)
            GameObject boxPrefabToSpawn = (i < boomBoxCount) ? boomBoxPrefab : giftBoxPrefab;

            StartCoroutine(AoEFollowUpWithBox(worldPos, effectTriggerDuration, boxPrefabToSpawn));
        }

        // AoE 대기
        yield return new WaitForSeconds(totalAoeDuration);
    }

    // AoE 이후 지정된 박스 프리팹을 생성하는 개별 코루틴
    private IEnumerator AoEFollowUpWithBox(Vector3 spawnWorldPosition, float triggerDelay, GameObject boxPrefab) {
        // AoE 경고 시간만큼 대기
        yield return new WaitForSeconds(triggerDelay);

        // 박스 프리팹 생성
        if (boxPrefab != null) {
            Instantiate(boxPrefab, spawnWorldPosition, Quaternion.identity);
        }
    }

    // 보따리 패턴
    public IEnumerator PatternBagDrop() {
        // 애니메이션 & 무적 활성화
        if (animator != null) {
            animator.SetBool("IsBagPattern", true);
        }
        isInvulnerable = true;

        float bagDropDelay = 0.5f;

        // 정방향 Bag (오른쪽 끝 열 제외)
        // AoE 소환 위치 획득 (minX ~ maxX-1)
        List<Vector3> aoePositions1 = GetPositionsExcludingColumns(minCellBounds.x, maxCellBounds.x - 1);

        StartCoroutine(ExecuteAoEWarning(aoePositions1));

        yield return new WaitForSeconds(aoeInitialDuration);
        if (audioSource != null && fallingSoundClip != null) {
            audioSource.PlayOneShot(fallingSoundClip);
        }
        yield return new WaitForSeconds(aoeFadeDuration+bagDropDelay);
        SpawnSantaBag(false);
        yield return new WaitForSeconds(1.0f);

        // 역방향 Bag (왼쪽 끝 열 제외)
        // AoE 소환 위치 획득 (minX+1 ~ maxX)
        List<Vector3> aoePositions2 = GetPositionsExcludingColumns(minCellBounds.x + 1, maxCellBounds.x);

        StartCoroutine(ExecuteAoEWarning(aoePositions2));

        yield return new WaitForSeconds(aoeInitialDuration);
        if (audioSource != null && fallingSoundClip != null) {
            audioSource.PlayOneShot(fallingSoundClip);
        }
        yield return new WaitForSeconds(aoeFadeDuration + bagDropDelay);
        SpawnSantaBag(true);
        yield return new WaitForSeconds(1.0f);

        // 애니메이션 & 무적 해제
        if (animator != null) {
            animator.SetBool("IsBagPattern", false);
        }
        isInvulnerable = false;
    }

    /// 지정된 X축 열 범위에 AoE 경고를 표시
    private IEnumerator ExecuteAoEWarning(List<Vector3> spawnPositions) {
        float totalDuration = aoeInitialDuration + aoeFadeDuration;
        float effectTriggerDuration = aoeInitialDuration;

        foreach (Vector3 worldPos in spawnPositions) {
            GameObject aoeInstance = Instantiate(aoePrefab, worldPos, Quaternion.identity);
            aoeInstance.transform.localScale = new Vector3(aoeSize, aoeSize, 1f);
        }

        // AoE 대기
        yield return new WaitForSeconds(totalDuration);
    }

    // 지정된 X축 범위 내 모든 Cell의 World 위치를 반환
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

    /// Santa Bag을 소환하고 이동 시작
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


    // 여섯 번째 패턴: BigGiftBox를 소환하고 애니메이션을 켠 채 대기
    public IEnumerator PatternThrowTheBigBox() {
        const float patternDuration = 3.0f;

        if (animator != null) {
            animator.SetBool("IsSnowPattern", true);
        }

        Vector3 spawnPosition = transform.position;
        spawnPosition.y = 5.57f;

        Instantiate(bigGiftBoxPrefab, spawnPosition, Quaternion.identity);

        // BigGiftBox의 전체 패턴(scale 업, AoE 대기, 이동, 폭발)이 끝날 때까지 대기
        yield return new WaitForSeconds(patternDuration);

        // 애니메이션 종료
        if (animator != null) {
            animator.SetBool("IsSnowPattern", false);
        }
    }
}

