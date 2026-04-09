using UnityEngine;
using UnityEngine.UI; // 必須引用 UI

public class ButtonEvent : MonoBehaviour
{
    [Header("按鈕 1 (Start/A)")]
    public Button targetButton;
    private Vector3 originalScale1;
    private Vector3 currentTargetScale1;

    [Header("按鈕 2 (Quit/B)")]
    public Button targetButton2;
    private Vector3 originalScale2;
    private Vector3 currentTargetScale2;

    [Header("效果設定")]
    private float pressedScaleMultiplier = 1.2f;
    private float animationSpeed = 15f;

    private bool isSceneLoading = false;

    private void Start()
    {
        // 分別初始化兩個按鈕的原始大小
        if (targetButton != null)
        {
            originalScale1 = targetButton.GetComponent<RectTransform>().localScale;
            currentTargetScale1 = originalScale1;
        }
        if (targetButton2 != null)
        {
            originalScale2 = targetButton2.GetComponent<RectTransform>().localScale;
            currentTargetScale2 = originalScale2;
        }
    }

    void Update()
    {
        if (isSceneLoading) return;

        // --- 按鈕 1 偵測 (A/Space) ---
        if (OVRInput.GetDown(OVRInput.RawButton.A) || Input.GetKeyDown(KeyCode.Space))
        {
            UpdateButtonVisuals(targetButton, true, ref currentTargetScale1, originalScale1);
        }
        if (OVRInput.GetUp(OVRInput.RawButton.A) || Input.GetKeyUp(KeyCode.Space))
        {
            UpdateButtonVisuals(targetButton, false, ref currentTargetScale1, originalScale1);
            SwitchScene();
        }

        // --- 按鈕 2 偵測 (B/Esc) ---
        if (OVRInput.GetDown(OVRInput.RawButton.B) || Input.GetKeyDown(KeyCode.Escape))
        {
            UpdateButtonVisuals(targetButton2, true, ref currentTargetScale2, originalScale2);
        }
        if (OVRInput.GetUp(OVRInput.RawButton.B) || Input.GetKeyUp(KeyCode.Escape))
        {
            UpdateButtonVisuals(targetButton2, false, ref currentTargetScale2, originalScale2);
            QuitApplication();
        }

        HandleScaleLerp();
    }

    private void UpdateButtonVisuals(Button btn, bool isPressed, ref Vector3 targetScaleVar, Vector3 origScale)
    {
        if (btn == null) return;

        // 1. 顏色切換
        Color targetColor = isPressed ? btn.colors.pressedColor : btn.colors.normalColor;
        if (btn.targetGraphic != null)
            btn.targetGraphic.CrossFadeColor(targetColor, 0.1f, true, true);

        // 2. 更新該按鈕專屬的目標縮放
        targetScaleVar = isPressed ? (origScale * pressedScaleMultiplier) : origScale;
    }

    private void HandleScaleLerp()
    {
        if (targetButton != null)
        {
            RectTransform rt1 = targetButton.GetComponent<RectTransform>();
            rt1.localScale = Vector3.Lerp(rt1.localScale, currentTargetScale1, Time.deltaTime * animationSpeed);
        }
        if (targetButton2 != null)
        {
            RectTransform rt2 = targetButton2.GetComponent<RectTransform>();
            rt2.localScale = Vector3.Lerp(rt2.localScale, currentTargetScale2, Time.deltaTime * animationSpeed);
        }
    }

    public void SwitchScene()
    {
        if (isSceneLoading) return;
        Debug.Log("[ButtonEvent] 觸發換場");
        var globalCtrl = FindFirstObjectByType<GlobalSceneController>();
        if (globalCtrl != null)
        {
            Debug.Log("[ButtonEvent] 正式執行切換。");
            isSceneLoading = true;
            globalCtrl.SwitchToNextScene(); 
        }
    }

    public void QuitApplication()
    {
        Debug.Log("正在關閉應用程式...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 在編輯器裡停止執行
        #else
            Application.Quit(); // 真正打包後執行的關閉指令
        #endif
    }
}