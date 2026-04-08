using UnityEngine;

public class AcornCollection : MonoBehaviour
{
    // 當有東西進入橡果的觸發範圍時
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("橡果被撞到了！" + other.name);
        // 1. 檢查撞到我的是不是「玩家」
        // 建議給你的 Camera 或 手把 或 CharacterController 加上 "Player" 標籤 (Tag)
        if (other.CompareTag("Player") || other.name.Contains("Controller"))
        {
            DestroyAcorn();
        }
    }

    void DestroyAcorn()
    {
        Debug.Log("橡果被撞到了！消失！");
        Destroy(gameObject);
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddScore(1);
        }
    }
}