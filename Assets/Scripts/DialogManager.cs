/*
 * CSVから台詞をロードし、台詞の進行を制御するクラス
 */

using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class DialogManager : MonoBehaviour {
    // 会話が完全に終了したときに外部へ通知するイベント
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

    // 現在会話がアクティブ状態かどうかを外部へ知らせるプロパティ
    public bool IsDialogActive => currentTextBox != null;

    // 現在タイピング中かどうかを外部へ知らせるプロパティ（外部シーケンスでの待機用に使用）
    public bool IsTyping => isTyping;
    [HideInInspector] public bool isAutoAdvanceEnabled = true;

    private struct DialogData {
        public int number;
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
        // isAutoAdvanceEnabled が true のときのみ、ユーザー入力による自動進行処理を行う
        if (isAutoAdvanceEnabled && currentTextBox != null && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))) {
            HandleUserInputForDialogFlow();
        }
    }

    // プレイヤー入力があったときに会話の流れを制御する（自動進行）
    private void HandleUserInputForDialogFlow() {
        if (isTyping) {
            // 即時完了
            FinishTypingImmediately();
        } else {
            // タイピング完了時に次の台詞へ進む
            AdvanceDialog();
        }
    }

    // タイピング中の場合は、タイピングを即時完了して停止する
    private void FinishTypingImmediately() {
        if (typingCoroutine != null) {
            StopCoroutine(typingCoroutine);
        }

        // 現在の台詞を全文表示
        dialogText.text = dialogQueue.Peek().dialogue;
        isTyping = false;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        // レイアウトを再計算（ContentSizeFitter を即時反映）
        RectTransform rectTransform = currentTextBox.GetComponent<RectTransform>();
        if (rectTransform != null) {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    // 前の台詞を削除し、次の台詞を表示
    public void AdvanceDialog() {
        // 外部から呼び出されたとき、タイピング中であれば即時完了する
        if (isTyping) {
            FinishTypingImmediately();
        }

        // 前の台詞を削除
        if (dialogQueue.Count > 0) {
            dialogQueue.Dequeue();
        }

        if (dialogQueue.Count > 0) {
            // 次の台詞の表示を開始
            DialogData nextDialog = dialogQueue.Peek();

            // 前のテキストボックスを破棄し、新しいボックスをインスタンス化する
            if (currentTextBox != null) Destroy(currentTextBox);
            currentTextBox = Instantiate(textBoxPrefab, canvasTransform);

            // 位置を設定
            RectTransform rect = currentTextBox.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(nextDialog.xPos, nextDialog.yPos);

            // TMP コンポーネントを検索して割り当て
            dialogText = currentTextBox.GetComponentInChildren<TextMeshProUGUI>();

            if (dialogText == null) {
                Debug.LogError("textBoxPrefab から TextMeshProUGUI が見つかりません。");
                Destroy(currentTextBox);
                currentTextBox = null;
                return;
            }

            // タイピング用コルーチンを開始
            typingCoroutine = StartCoroutine(TypeDialogue(nextDialog));
        } else {
            // 会話終了
            Debug.Log("台詞が全くおわりました。");
            if (currentTextBox != null) Destroy(currentTextBox);
            currentTextBox = null;
            OnDialogFinished?.Invoke();
            Debug.Log("OnDialogFinished イベント発生！");
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

    // 割り当てられた TextAsset からデータを解析し、キューに追加
    private void LoadDialogData() {
        dialogQueue.Clear();

        if (dialogCsvFile == null) {
            Debug.LogError("CSV TextAsset がインスペクターに割り当てられていません。データを読み込めません。");
            return;
        }

        // 現在の言語設定（Locale）を取得
        Locale activeLocale = LocalizationSettings.SelectedLocale;
        if (activeLocale == null) {
            Debug.LogError("SelectedLocale が取得できません。LocalizationSettingsを確認してください。");
            return;
        }

        // 言語コード（"ko" や "ja"）に応じて読み込むCSVの列インデックスを決定
        // csv形式: number(0), x(1), y(2), KO(3), JP(4)
        int dialogueColumnIndex = 3; // デフォルトは韓国語(KO)
        
        string languageCode = activeLocale.Identifier.Code; // "ko", "ja" などが取得できる
        if (languageCode.StartsWith("ja")) {
            dialogueColumnIndex = 4; // 日本語(JP)
        } else if (languageCode.StartsWith("ko")) {
            dialogueColumnIndex = 3; // 韓国語(KO)
        }

        try {
            string fileData = dialogCsvFile.text;
            string[] lines = fileData.Split('\n');

            foreach (string line in lines.Skip(1)) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');

                // 必要最低限の項目数があるかチェック（最大インデックスが dialogueColumnIndex なのでそれ以上必要）
                if (values.Length <= dialogueColumnIndex) {
                    Debug.LogWarning($"CSV 解析エラー: データ項目数が不足しています。（行: {line}）-> 必要なインデックス: {dialogueColumnIndex}");
                    continue;
                }

                DialogData data = new DialogData();
                data.number = int.Parse(values[0].Trim());
                data.xPos = float.Parse(values[1].Trim());
                data.yPos = float.Parse(values[2].Trim());
                
                // 動的に決定したインデックスからセリフを取得
                data.dialogue = values[dialogueColumnIndex].Trim().Replace("\"", "").Replace("\r", "");
                
                dialogQueue.Enqueue(data);
            }
            Debug.Log($"CSV の読み込み完了（言語: {languageCode}）。合計 {dialogQueue.Count} 件の会話データをキューに追加しました。");
        } catch (System.Exception e) {
            Debug.LogError($"CSV ファイル解析中にエラーが発生しました: {e.Message}。CSV ファイル形式またはデータに誤りがあります。");
        }
    }

    // 外部から呼び出されて会話シーケンスを開始（CSV を読み込み、最初の台詞を準備）
    public void StartDialog() {
        // 初期化
        if (currentTextBox != null) Destroy(currentTextBox);
        currentTextBox = null;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isTyping = false;

        LoadDialogData();

        if (dialogQueue.Count > 0) {
            DialogData firstDialog = dialogQueue.Peek();

            // テキストボックスをインスタンス化
            currentTextBox = Instantiate(textBoxPrefab, canvasTransform);
            RectTransform rect = currentTextBox.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(firstDialog.xPos, firstDialog.yPos);
            dialogText = currentTextBox.GetComponentInChildren<TextMeshProUGUI>();

            if (dialogText == null) {
                Debug.LogError("textBoxPrefabからTextMeshProUGUI見つかりません");
                Destroy(currentTextBox);
                currentTextBox = null;
                return;
            }

            // タイピング開始
            typingCoroutine = StartCoroutine(TypeDialogue(firstDialog));
        } else {
            OnDialogFinished?.Invoke();
        }
    }
}