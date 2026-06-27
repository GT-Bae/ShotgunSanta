/*
 * シーンを移動するクラス
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class MoveScene : MonoBehaviour {
    public string sceneName;
    public void moveScene() {
        SceneManager.LoadScene(sceneName);
    }
}