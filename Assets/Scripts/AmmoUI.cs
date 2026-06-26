/*
 * AmmoUIを制御するクラス
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AmmoUI : MonoBehaviour {
    public GameObject bulletIconPrefab;
    public Sprite emptyBulletSprite;
    public Transform bulletContainer;
    public HandgunHandler handgunHandler;

    // 生成されたすべての弾丸Imageコンポーネントを格納するリスト
    private List<Image> bulletIcons = new List<Image>();
    public TMPro.TextMeshProUGUI ammoText;

    void Start() {
        InitializeBulletIcons(handgunHandler.maxClipAmmo);
    }

    void InitializeBulletIcons(int maxAmmo) {
        for (int i = 0; i < maxAmmo; i++) {
            // 最大装弾数分、弾丸アイコンを生成
            GameObject iconObj = Instantiate(bulletIconPrefab, bulletContainer);
            Image bulletImage = iconObj.GetComponent<Image>();

            if (bulletImage != null) {
                bulletIcons.Add(bulletImage);
            } else {
                Debug.LogError("Bullet Icon PrefabにImageコンポーネントがありません。");
                break;
            }
        }
    }

    void Update() {
        if (handgunHandler == null) return;

        int currentAmmo = handgunHandler.CurrentAmmo;
        int maxAmmo = handgunHandler.maxClipAmmo;
        bool reloading = handgunHandler.IsReloading;

        UpdateSpriteUI(currentAmmo, maxAmmo);

        if (ammoText != null) { // text UIの更新
            string ammoDisplay = reloading ? "RELOADING..." : $"{currentAmmo} / {maxAmmo}";
            ammoText.text = ammoDisplay;
        }
    }

    // sprite UI の更新
    void UpdateSpriteUI(int current, int max) {
        for (int i = 0; i < max; i++) {
            if (i < current) { // 残弾数分は表示
                bulletIcons[i].sprite = bulletIconPrefab.GetComponent<Image>().sprite;
            } else { // それ以外は非表示
                bulletIcons[i].sprite = emptyBulletSprite;
            }
        }
    }
}