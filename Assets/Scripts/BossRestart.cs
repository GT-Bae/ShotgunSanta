using UnityEngine;
using UnityEngine.SceneManagement;

public class BossRestart : MonoBehaviour {
    public void bossRestart() {
        SceneManager.LoadScene("BossStage");
    }
}
