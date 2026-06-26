/*
 * ステージをやり直して、ボス戦を最初からスタートするクラス
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class BossRestart : MonoBehaviour {
    public void bossRestart() {
        SceneManager.LoadScene("BossStage");
    }
}