// using UnityEngine;
// using UnityEngine.SceneManagement; // 必須引用以控制場景

// public class ButtonEvent : MonoBehaviour
// {
//     void Update()
//     {
//         // 偵測手把 A 鍵按下 (如果是右手手把的 A 鍵)
//         // 同時也支援鍵盤 Enter 鍵方便你在電腦測試
//         if (OVRInput.GetDown(OVRInput.RawButton.A) || Input.GetKeyDown(KeyCode.Return))
//         {
//             Debug.Log("[ButtonEvent] 偵測到實體按鍵 A 按下");
//             SwitchScene();
//         }
//     }

//     // 這是按鈕要點擊的方法，必須是 public 且沒有傳回值 (void)
//     public void SwitchScene()
//     {
//         Debug.Log("準備切換場景");

//         // 如果你有 GlobalSceneController，建議呼叫它的切換方法
//         var globalCtrl = FindFirstObjectByType<GlobalSceneController>();
        
//         if (globalCtrl != null)
//         {
//             // 優先使用你的全域管理器來切換，這樣才能確保 Config 被載入
//             globalCtrl.SwitchToNextScene(); 
//         }
//     }
// }

using UnityEngine;
using UnityEngine.UI; // 必須引用 UI

public class ButtonEvent : MonoBehaviour
{
    [Header("綁定 UI 按鈕")]
    public Button targetButton; // 把你想產生視覺效果的按鈕拉進來
    private float pressedScaleMultiplier = 1.2f;
    private float animationSpeed = 10f;
    private Vector3 originalScale; // 紀錄按鈕原本的大小
    private Vector3 targetScale;

    private void Start()
    {
        if (targetButton != null)
        {
            originalScale = targetButton.GetComponent<RectTransform>().localScale;
            targetScale = originalScale;
        }
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.A) || Input.GetKeyDown(KeyCode.Space))
        {
            SetButtonColor(true);
        }

        if (OVRInput.GetUp(OVRInput.RawButton.A) || Input.GetKeyUp(KeyCode.Space))
        {
            SetButtonColor(false);
            SwitchScene();
        }

        if (targetButton != null)
        {
            RectTransform rectTransform = targetButton.GetComponent<RectTransform>();
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * animationSpeed);
        }
    }

    private void SetButtonColor(bool isPressed)
    {
        if (targetButton != null && targetButton.targetGraphic != null)
        {
            // 取得按鈕在 Inspector 中設定好的顏色區塊
            Color targetColor = isPressed ? targetButton.colors.pressedColor : targetButton.colors.normalColor;
            
            // 執行平滑變色 (時間設為 0.1 秒)
            targetButton.targetGraphic.CrossFadeColor(targetColor, 0.1f, true, true);
            targetScale = isPressed ? (originalScale * pressedScaleMultiplier) : originalScale;
        }
    }

    public void SwitchScene()
    {
        Debug.Log("[ButtonEvent] 觸發換場");
        var globalCtrl = FindFirstObjectByType<GlobalSceneController>();
        if (globalCtrl != null)
        {
            globalCtrl.SwitchToNextScene(); 
        }
    }
}