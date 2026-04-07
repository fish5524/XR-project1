using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class VRCamFixer : MonoBehaviour
{
    [Header("強制修正高度")]
    public float targetY = 1.0f;

    void Start()
    {
        // 註冊場景載入完成的事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        // 啟動時也跑一次，預防萬一
        StartCoroutine(ForceUpdatePosition());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 當任何新場景（如 Forest1）載入後，強制重置座標
        StartCoroutine(ForceUpdatePosition());
    }

    IEnumerator ForceUpdatePosition()
    {
        // 延遲 1-2 幀，等待 VR SDK 完成初始化與場景物件生成
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        // 強制重新輸入座標
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, targetY, currentPos.z);
        
        Debug.Log("VR Camera 高度已強制校正為: " + targetY);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
