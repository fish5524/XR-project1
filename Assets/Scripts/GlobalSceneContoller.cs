using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.Oculus;
using System.Diagnostics;

using Debug = UnityEngine.Debug;

public class GlobalSceneController : MonoBehaviour
{
    public static GlobalSceneController instance;

    [Header("核心引用")]
    public GameObject ovrCameraRig;
    public GameObject playerBird;

    [Header("過渡設定")]
    public CanvasGroup fadeCanvasGroup; // 拖入剛剛建立的 Canvas Group
    public float fadeDuration = 1.5f;   // 霧化持續時間

    [System.Serializable]
    public struct SceneConfig
    {
        public string sceneName;      // 場景名稱
        public bool isBirdActive;     // 該場景鳥是否開啟
        public bool useFloorLevel;
        public Vector3 cameraPos;     // 相機位置
        public Vector3 cameraRot;     // 相機旋轉
    }

    [Header("場景排程 (依順序排列)")]
    public List<SceneConfig> sceneSequence = new List<SceneConfig>();

    private int currentSequenceIndex = -1; // 目前在第幾個場景
    private bool isSwitching = false;
    private bool isFirst = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; 
            DontDestroyOnLoad(gameObject); // 保護 SceneManager 自己

            // 【關鍵：也要保護相機】
            if (ovrCameraRig != null)
            {
                // 確保它是最頂層物件，否則 DontDestroyOnLoad 會無效
                ovrCameraRig.transform.SetParent(null); 
                DontDestroyOnLoad(ovrCameraRig); 
                Debug.Log("相機已設定跨場景保留");
            }
        }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        // if (sceneSequence.Count > 0)
        // {
        //     currentSequenceIndex = 0;
        //     ApplyConfig(sceneSequence[currentSequenceIndex]);
        // }
    }

    public void StartGameSequence()
    {
        Debug.Log("玩家點擊開始，啟動遊戲流程...");
        SwitchToNextScene();
    }

    public void SetJumpGameMode(bool active)
    {
        var bird = ovrCameraRig.GetComponentInChildren<birdController>();
        if (bird != null)
        {
            bird.isJumpGame = active;
        }
    }

    // --- 核心功能：自動換下一個場景 ---
    public void SwitchToNextScene()
    {
        if(isSwitching)
        {
            Debug.LogWarning("正在切換場景中，請稍候...");
            return;
        }
        currentSequenceIndex++;
        
        if (currentSequenceIndex < sceneSequence.Count)
        {
            SceneConfig nextConfig = sceneSequence[currentSequenceIndex];
            Debug.Log($"準備切換到下一個場景: {nextConfig.sceneName}");
            StartCoroutine(LoadSceneRoutine(nextConfig));
            if (nextConfig.sceneName == "Forest1")
            {
                SetJumpGameMode(true);
            }
            else
            {
                SetJumpGameMode(false);
            }
        }
        else
        {
            Debug.Log("所有場景流程已完成！");
        }
    }

    // --- 核心功能：直接換特定名稱的場景 (保留彈性) ---
    public void SwitchToSceneByName(string name)
    {
        SceneConfig config = sceneSequence.Find(s => s.sceneName == name);
        if (config.sceneName != null)
        {
            StartCoroutine(LoadSceneRoutine(config));
        }
    }

    private IEnumerator LoadSceneRoutine(SceneConfig config)
    {
        isSwitching = true;
        // 1. 開始霧化 (淡入：Alpha 0 -> 1)
        if(isFirst)
        {
            fadeCanvasGroup.alpha = 0;
            isFirst = false;
        }
        else
        {
            yield return StartCoroutine(Fade(1.0f));
        }
        

        Debug.Log($"[Scene] 正在前往: {config.sceneName}");
        
        // 2. 異步載入場景
        AsyncOperation op = SceneManager.LoadSceneAsync(config.sceneName);
        while (!op.isDone) yield return null;

        // 3. 載入完成，套用設定 (包含座標偏移、模式切換)
        ApplyConfig(config);

        // 4. 等待一小段時間確保場景物件都初始化完成 (可選)
        yield return new WaitForSeconds(0.5f);

        // 5. 結束霧化 (淡出：Alpha 1 -> 0)
        if(!isFirst)
        {
            yield return StartCoroutine(Fade(0.0f));
        }

        isSwitching = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void SetTrackingOrigin(bool useFloorLevel)
    {
        if (useFloorLevel)
        {
            // 設定為地面模式：相機高度會加上玩家的真實身高
            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
        }
        else
        {
            // 設定為眼睛模式：相機位置就是你在 Scene 裡擺放的 Transform 座標
            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
        }
        
        // 強制重設視角姿勢以套用變更
        OVRManager.display.RecenterPose();
        Debug.Log($"[VR] 目前追蹤模式設定為: {(useFloorLevel ? "Floor Level" : "Eye Level")}");
    }

    private void ApplyConfig(SceneConfig config)
    {
        if (ovrCameraRig != null)
        {
            ovrCameraRig.SetActive(true);

            // 1. 設定 VR 追蹤模式 (Eye Level vs Floor Level)
            if (OVRManager.instance != null)
            {
                OVRManager.instance.trackingOriginType = config.useFloorLevel ? 
                    OVRManager.TrackingOrigin.FloorLevel : OVRManager.TrackingOrigin.EyeLevel;
                
                // 強制刷新視角 Pose
                OVRManager.display.RecenterPose();
            }

            // 2. 先處理物理 (防止位移時被彈開)
            Rigidbody rb = ovrCameraRig.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; 
            }

            // 3. 設定位置與旋轉
            ovrCameraRig.transform.position = config.cameraPos;
            ovrCameraRig.transform.eulerAngles = config.cameraRot;

            // 4. 強制物理同步
            Physics.SyncTransforms();

            // 5. 根據設定恢復物理與重力
            if (rb != null)
            {
                rb.useGravity = config.isBirdActive;
                rb.isKinematic = !config.isBirdActive;
                
                if (config.isBirdActive)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // 6. 控制器與鳥的開關
            var birdCtrl = ovrCameraRig.GetComponentInChildren<birdController>();
            if (birdCtrl != null) {
                birdCtrl.enabled = config.isBirdActive;
                // birdCtrl.HaltUpdateMovement = !config.isBirdActive;
            }

            if (playerBird != null)
            {
                playerBird.SetActive(config.isBirdActive);
                if (config.sceneName == "Forest1") playerBird.SetActive(false);
            }
            Debug.Log($"[Scene] {config.sceneName} 套用成功！模式: {(config.useFloorLevel ? "Floor" : "Eye")}");
        }
    }
}