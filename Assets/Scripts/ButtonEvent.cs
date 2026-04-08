using UnityEngine;
using UnityEngine.SceneManagement; // 必須引用以控制場景

public class ButtonEvent : MonoBehaviour
{
    void Update()
    {
        // 偵測手把 A 鍵按下 (如果是右手手把的 A 鍵)
        // 同時也支援鍵盤 Enter 鍵方便你在電腦測試
        if (OVRInput.GetDown(OVRInput.RawButton.A) || Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("[ButtonEvent] 偵測到實體按鍵 A 按下");
            SwitchScene();
        }
    }

    // 這是按鈕要點擊的方法，必須是 public 且沒有傳回值 (void)
    public void SwitchScene()
    {
        Debug.Log("準備切換場景");

        // 如果你有 GlobalSceneController，建議呼叫它的切換方法
        var globalCtrl = FindFirstObjectByType<GlobalSceneController>();
        
        if (globalCtrl != null)
        {
            // 優先使用你的全域管理器來切換，這樣才能確保 Config 被載入
            globalCtrl.SwitchToNextScene(); 
        }
    }
}