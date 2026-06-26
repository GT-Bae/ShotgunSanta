/*
 * マウスを追従するCrosshairを制御するクラス
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CrosshairUI : MonoBehaviour {
    private RectTransform rectTransform;
    private Canvas canvas;

    void Start() {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Update() {
        Vector2 mousePosition = Input.mousePosition;
        Vector2 localPoint;

        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent.GetComponent<RectTransform>(),
            mousePosition,
            canvas.worldCamera,
            out localPoint
        );

        if (success) {
            rectTransform.localPosition = localPoint;
        }
    }
}