/*
 * ゲーム終了を制御するクラス
 */

using UnityEngine;

public class GameOff : MonoBehaviour {
    public void QuitGame() {
        #if UNITY_EDITOR // エディタからプレイモード終了
            UnityEditor.EditorApplication.isPlaying = false;        
        #else // アプリ終了
            Application.Quit();
        #endif
    }
}