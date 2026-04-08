using UnityEngine;
using TMPro; // 如果你有用 TextMeshPro 顯示分數

public class ScoreManager : MonoBehaviour
{
    // 單例模式，方便橡果直接呼叫
    public static ScoreManager instance;

    public int score = 0;
    public TextMeshProUGUI scoreText; // 拖入你的 UI 文字物件
    public const int TargetScore = 1;
    private bool hasNotifiedCore = false;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    // 增加分數的函式
    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log("目前分數: " + score);
        UpdateUI();
        if (score >= TargetScore && !hasNotifiedCore)
        {
            hasNotifiedCore = true;
            NotifyGameCore();
        }
    }

    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }

    private void NotifyGameCore()
    {
        Debug.Log("達到 8 分，通知 GameCoreV2 結束互動階段...");
        
        // 使用我們之前寫好的單例模式呼叫
        if (GameCoreV2.Instance != null)
        {
            GameCoreV2.Instance.NotifyInteractionFinished();
        }
        else
        {
            Debug.LogError("找不到 GameCoreV2 Instance！請確認你是從 Global 場景開始的。");
        }
    }
}
