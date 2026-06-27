/*
 * タイトル画面のドロップダウンでげんご変更するクラス
 */

using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageSelector : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private void Start()
    {
        // ドロップダウンの値が変更された時に実行するリスナーを登録
        dropdown.onValueChanged.AddListener(OnLanguageChanged);
        
        // 現在設定されている言語のインデックスを探し、ドロップダウンの初期値を合わせる
        StartCoroutine(SetInitialDropdownValue());
    }

    private IEnumerator SetInitialDropdownValue()
    {
        // Localizationシステムが初期化されるまで待機
        yield return LocalizationSettings.InitializationOperation;

        var activeLocale = LocalizationSettings.SelectedLocale;
        var locales = LocalizationSettings.AvailableLocales.Locales;

        for (int i = 0; i < locales.Count; i++)
        {
            if (locales[i] == activeLocale)
            {
                dropdown.value = i;
                break;
            }
        }
    }

    // ドロップダウンの項目が選択された時に呼び出される関数
    public void OnLanguageChanged(int index)
    {
        // 選択されたインデックスの言語をシステムに反映
        var targetLocale = LocalizationSettings.AvailableLocales.Locales[index];
        LocalizationSettings.SelectedLocale = targetLocale;
        
        Debug.Log($"言語が変更されました: {targetLocale.LocaleName}");
    }
}
