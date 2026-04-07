using UnityEngine;
using UnityEngine.SceneManagement;

public class InitialSceneLoader : MonoBehaviour
{
    // 在 Inspector 面板中輸入你想載入的場景名稱，例如 "Forest1"
    public string firstSceneName = "Forest1";

    void Start()
    {
        // 檢查 Forest1 是否已經載入，避免重複載入
        if (!SceneManager.GetSceneByName(firstSceneName).isLoaded)
        {
            // 使用 Additive 模式載入，這樣 global 場景（包含 Camera Rig）會繼續存在
            SceneManager.LoadScene(firstSceneName, LoadSceneMode.Additive);
            
            // 選擇性：載入完成後將其設為 Active Scene，這樣新產生的物件會歸類在 Forest1
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == firstSceneName)
        {
            SceneManager.SetActiveScene(scene);
            SceneManager.sceneLoaded -= OnSceneLoaded; // 移除監聽，避免之後切換場景出錯
            Debug.Log(firstSceneName + " 載入完成並已設為活動場景！");
        }
    }
}
