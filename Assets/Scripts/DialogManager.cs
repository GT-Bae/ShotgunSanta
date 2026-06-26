using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DialogManager : MonoBehaviour {
    // 대화가 완전히 끝났을 때 외부로 알리는 이벤트
    public static Action OnDialogFinished;

    [Header("UI References")]
    [SerializeField] public GameObject textBoxPrefab;
    [SerializeField] private Transform canvasTransform;

    [Header("Audio")]
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float typingDelay = 0.05f;

    [Header("Dialog Data Source")]
    [SerializeField] private TextAsset dialogCsvFile;

    private Queue<DialogData> dialogQueue = new Queue<DialogData>();
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private GameObject currentTextBox;

    private TextMeshProUGUI dialogText;
    private TextMeshProUGUI characterNameText;

    // 현재 대화가 활성화 상태인지 외부에 알리는 속성
    public bool IsDialogActive => currentTextBox != null;

    // 현재 타이핑이 진행 중인지 외부에 알리는 속성 (외부 시퀀스에서 대기용으로 사용)
    public bool IsTyping => isTyping;
    [HideInInspector] public bool isAutoAdvanceEnabled = true;
    private struct DialogData {
        public int number;
        public string character;
        public float xPos;
        public float yPos;
        public string dialogue;
    }

    private void Start() {
        if (audioSource == null) {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnDestroy() {
        if (typingCoroutine != null) {
            StopCoroutine(typingCoroutine);
        }
    }

    private void Update() {
        // isAutoAdvanceEnabled가 true일 때만 사용자 입력을 통한 자동 진행 처리
        if (isAutoAdvanceEnabled && currentTextBox != null && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))) {
            HandleUserInputForDialogFlow();
        }
    }

    // 사용자 입력이 있을 때 대화 흐름을 제어 (자동 진행)
    private void HandleUserInputForDialogFlow() {
        if (isTyping) {
            // 즉시 완료
            FinishTypingImmediately();
        } else {
            // 타이핑 완료 시 다음 대사로 진행
            AdvanceDialog();
        }
    }

    // 타이핑 중일 때 타이핑을 즉시 완료하고 정지
    private void FinishTypingImmediately() {
        if (typingCoroutine != null) {
            StopCoroutine(typingCoroutine);
        }

        // 현재 대사 전체 출력
        dialogText.text = dialogQueue.Peek().dialogue;
        isTyping = false;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        // 레이아웃 재계산 (ContentSizeFitter 즉시 적용)
        RectTransform rectTransform = currentTextBox.GetComponent<RectTransform>();
        if (rectTransform != null) {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    /// 이전대사 제거 및 다음대사 표시
    public void AdvanceDialog() {
        // 외부에서 호출 시 타이핑 중이었다면 즉시 완료
        if (isTyping) {
            FinishTypingImmediately();
        }

        // 이전 대사 제거
        if (dialogQueue.Count > 0) {
            dialogQueue.Dequeue();
        }

        if (dialogQueue.Count > 0) {
            // 다음 대사 표시 시작
            DialogData nextDialog = dialogQueue.Peek();

            // 이전 텍스트 박스 파괴 및 새 박스 인스턴스화
            if (currentTextBox != null) Destroy(currentTextBox);
            currentTextBox = Instantiate(textBoxPrefab, canvasTransform);

            // 위치 설정
            RectTransform rect = currentTextBox.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(nextDialog.xPos, nextDialog.yPos);

            // TMP 컴포넌트 검색 및 할당
            dialogText = currentTextBox.GetComponentInChildren<TextMeshProUGUI>();

            if (dialogText == null) {
                Debug.LogError("오류: textBoxPrefab에서 TextMeshProUGUI를 찾을 수 없습니다.");
                Destroy(currentTextBox);
                currentTextBox = null;
                return;
            }

            // 타이핑 코루틴 시작
            typingCoroutine = StartCoroutine(TypeDialogue(nextDialog));
        } else {
            // 대화 종료
            Debug.Log("Dialog End. All dialogs consumed.");
            if (currentTextBox != null) Destroy(currentTextBox);
            currentTextBox = null;
            OnDialogFinished?.Invoke();
            Debug.Log("OnDialogFinished 이벤트 발생!");
        }
    }

    private IEnumerator TypeDialogue(DialogData data) {
        isTyping = true;
        dialogText.text = "";
        foreach (char letter in data.dialogue.ToCharArray()) {
            dialogText.text += letter;

            if (audioSource != null && typingSound != null) {
                audioSource.PlayOneShot(typingSound);
            }

            yield return new WaitForSeconds(typingDelay);
        }

        isTyping = false;
    }

    // 할당된 TextAsset으로부터 데이터를 파싱하여 큐에 추가
    private void LoadDialogData() {
        dialogQueue.Clear();

        if (dialogCsvFile == null) {
            Debug.LogError("CSV TextAsset이 인스펙터에 할당되지 않았습니다. 데이터를 로드할 수 없습니다.");
            return;
        }

        try {
            string fileData = dialogCsvFile.text;
            string[] lines = fileData.Split('\n');

            foreach (string line in lines.Skip(1)) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');

                if (values.Length < 5) {
                    Debug.LogWarning($"CSV 파싱 오류: 데이터 항목 수가 부족합니다. (라인: {line}) -> 5개 항목 필요");
                    continue;
                }
                DialogData data = new DialogData();
                data.number = int.Parse(values[0].Trim());
                data.character = values[1].Trim();
                data.xPos = float.Parse(values[2].Trim());
                data.yPos = float.Parse(values[3].Trim());
                // CSV 내 줄바꿈, 따옴표 등을 정리
                data.dialogue = values[4].Trim().Replace("\"", "").Replace("\r", "");
                dialogQueue.Enqueue(data);
            }
            Debug.Log($"CSV 로드 완료. 총 {dialogQueue.Count}개의 대화 데이터를 큐에 추가했습니다.");
        } catch (System.Exception e) {
            Debug.LogError($"CSV 파일 파싱 중 오류 발생: {e.Message}. CSV 파일 형식 또는 데이터 오류.");
        }
    }

    // 외부에서 호출되어 대화 시퀀스를 시작 (CSV 로드 및 첫 대사 준비)
    public void StartDialog() {
        // 초기화
        if (currentTextBox != null) Destroy(currentTextBox);
        currentTextBox = null;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isTyping = false;

        LoadDialogData();

        if (dialogQueue.Count > 0) {
            DialogData firstDialog = dialogQueue.Peek();

            // 텍스트 박스 인스턴스화
            currentTextBox = Instantiate(textBoxPrefab, canvasTransform);
            RectTransform rect = currentTextBox.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(firstDialog.xPos, firstDialog.yPos);
            dialogText = currentTextBox.GetComponentInChildren<TextMeshProUGUI>();

            if (dialogText == null) {
                Debug.LogError("오류: textBoxPrefab에서 TextMeshProUGUI를 찾을 수 없습니다.");
                Destroy(currentTextBox);
                currentTextBox = null;
                return;
            }

            // 타이핑 시작
            typingCoroutine = StartCoroutine(TypeDialogue(firstDialog));
        } else {
            OnDialogFinished?.Invoke();
        }
    }
}