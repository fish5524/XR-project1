using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // 自動抓取場景中的主相機（VR 中的 CenterEyeAnchor）
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    // 使用 LateUpdate 確保在相機移動後才調整 UI 位置，避免抖動
    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // 1. 讓 UI 轉向相機
            transform.LookAt(mainCameraTransform);

            // 2. 修正文字鏡像反向問題
            // 因為 LookAt 會讓物體的「背面」對著目標，
            // 我們需要水平旋轉 180 度讓正面文字轉過來。
            transform.Rotate(0, 180, 0);
        }
        else
        {
            // 如果沒抓到相機，重新抓一次（防止切換場景時失效）
            if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        }
    }
}