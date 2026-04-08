using UnityEngine;

public class GameCoreV2 : MonoBehaviour
{
    public static GameCoreV2 Instance { get; private set; }

    [Header("自動化設定")]
    [Tooltip("勾選後，一執行遊戲就會自動跳過開始畫面進入第一個場景")]
    public bool autoStartOnPlay = false;

    private bool hasStarted = false; // 防止重複啟動

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    private void Start()
    {
        // 如果在 Inspector 勾選了 Auto Start，就在啟動時直接執行
        if (autoStartOnPlay)
        {
            Debug.Log("[GameCore] 偵測到 AutoStart，自動啟動流程...");
            StartFlow();
        }
    }

    // 由「開始按鈕」或 Start() 呼叫
    public void StartFlow()
    {
        if (hasStarted) return; // 保險機制：啟動過就不再啟動
        
        hasStarted = true;
        Debug.Log("[GameCore] 遊戲流程正式啟動");

        if (GlobalSceneController.instance != null)
        {
            GlobalSceneController.instance.SwitchToNextScene();
        }
    }

    // 當動畫結束（Story）或互動結束（Interaction）時呼叫
    public void NotifyInteractionFinished() => MoveToNext();
    public void NotifyStoryFinished() => MoveToNext();

    private void MoveToNext()
    {
        if (GlobalSceneController.instance != null)
        {
            GlobalSceneController.instance.SwitchToNextScene();
        }
    }
}