using UnityEngine;
using System.Collections;

public class FlyGameSceneManager : MonoBehaviour
{
    [Header("高度切換設定")]
    [SerializeField] private float exitHeightThreshold = 6.0f; // 到達此高度就換場
    [SerializeField] private bool enableHeightSwitch = true;   // 是否開啟高度偵測

    [Header("鳥模型位置微調")]
    [SerializeField] private Vector3 birdLocalPos = new Vector3(0, -0.5f, 0.8f);
    [SerializeField] private Vector3 birdLocalRot = Vector3.zero;

    private birdController targetBird;
    private bool isTransitioning = false; // 防止重複觸發切換

    private IEnumerator Start()
    {
        // 1. 等待這一幀結束，確保 GlobalSceneController 的 ApplyConfig 已經執行完畢
        yield return new WaitForEndOfFrame();

        // 2. 尋找跨場景的鳥控制器
        targetBird = FindFirstObjectByType<birdController>();
        if (targetBird != null)
        {
            // 開啟飛行遊戲邏輯
            targetBird.isFlyGame = true;
            
            // 再次確保蘑菇頭是禁用的
            targetBird.EnableLinearMovement = false;
            targetBird.EnableRotation = false;

            Debug.Log($"[FlyGame] 控制器已就緒，換場高度設定為: {exitHeightThreshold}m");
        }
        else
        {
            Debug.LogWarning("[FlyGame] 找不到 birdController，請檢查相機是否正確載入");
        }

        // 3. 調整鳥模型相對於相機的位置
        var globalCtrl = FindFirstObjectByType<GlobalSceneController>();
        if (globalCtrl != null && globalCtrl.playerBird != null)
        {
            globalCtrl.playerBird.transform.localPosition = birdLocalPos;
            globalCtrl.playerBird.transform.localEulerAngles = birdLocalRot;
            
            // 確保模型是顯示的
            globalCtrl.playerBird.SetActive(true);
            
            Debug.Log("[FlyGame] 鳥模型位置已重置為: " + birdLocalPos);
        }
    }

    private void Update()
    {
        // 4. 監控高度邏輯
        if (enableHeightSwitch && !isTransitioning && targetBird != null)
        {
            // 這裡抓的是鳥的世界座標高度 (Y 軸)
            if (targetBird.transform.position.y > exitHeightThreshold)
            {
                TriggerSceneSwitch();
            }
        }
    }

    private void TriggerSceneSwitch()
    {
        isTransitioning = true; // 鎖定開關
        Debug.Log($"[FlyGame] 到達高度門檻 {exitHeightThreshold}m，觸發自動換場");

        var globalCtrl = FindFirstObjectByType<GlobalSceneController>();
        if (globalCtrl != null)
        {
            globalCtrl.SwitchToNextScene();
        }
        else
        {
            Debug.LogError("[FlyGame] 找不到 GlobalSceneController，無法切換場景");
            isTransitioning = false; // 若失敗則重置狀態以利再次偵測
        }
    }

    // 當離開此場景或銷毀時，重設狀態
    private void OnDisable()
    {
        if (targetBird != null)
        {
            targetBird.isFlyGame = false;
        }
        isTransitioning = false;
    }
}