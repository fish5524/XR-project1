using UnityEngine;
using System.Collections;

public class FlyingSceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BirdManager birdManager;
    [SerializeField] private Transform lastAnimationTrajectoryRoot;

    [Header("Settings")]
    [SerializeField] private float delayBeforeSceneSwitch = 5.0f;
    [SerializeField] private float doubleClickThreshold = 0.5f; // 雙擊判定時間

    private float lastBClickTime = 0f;
    private Coroutine switchCoroutine; // 用來儲存協程，以便隨時停止

    void Update()
    {
        // 偵測 B 鍵雙擊跳過
        if (OVRInput.GetDown(OVRInput.RawButton.B) || Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time - lastBClickTime < doubleClickThreshold)
            {
                SkipToNextScene();
            }
            lastBClickTime = Time.time;
        }
    }

    public void PlayHoveringAnimation()
    {
        if (birdManager == null) return;
        birdManager.StartFlock();
    }

    public void PlayLastAnimation()
    {
        if (birdManager == null || lastAnimationTrajectoryRoot == null) return;

        birdManager.StopFlock();
        birdManager.SetTrajectoryRoot(lastAnimationTrajectoryRoot);
        birdManager.StartFlock();

        // 如果已經有在計時，先停止舊的 (防止重複呼叫)
        if (switchCoroutine != null) StopCoroutine(switchCoroutine);
        switchCoroutine = StartCoroutine(WaitAndSwitchScene());
    }

    private void SkipToNextScene()
    {
        Debug.Log("【偵測到雙擊 B】立即跳過動畫並切換場景");
        
        // 停止計時協程
        if (switchCoroutine != null) StopCoroutine(switchCoroutine);
        
        // 直接執行切換
        ExecuteSceneSwitch();
    }

    private IEnumerator WaitAndSwitchScene()
    {
        Debug.Log($"{delayBeforeSceneSwitch} 秒後將自動切換場景...");
        yield return new WaitForSeconds(delayBeforeSceneSwitch);
        ExecuteSceneSwitch();
    }

    private void ExecuteSceneSwitch()
    {
        var globalCtrl = FindFirstObjectByType<GlobalSceneController>();
        if (globalCtrl != null)
        {
            Debug.Log("執行場景切換...");
            globalCtrl.SwitchToNextScene();
        }
        else
        {
            Debug.LogError("找不到 GlobalSceneController，無法切換場景！");
        }
    }
}