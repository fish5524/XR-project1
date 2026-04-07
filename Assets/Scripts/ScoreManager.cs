using UnityEngine;
using TMPro; // 如果你有用 TextMeshPro 顯示分數

public class ScoreManager : MonoBehaviour
{
    // 單例模式，方便橡果直接呼叫
    public static ScoreManager instance;

    public int score = 0;
    public TextMeshProUGUI scoreText; // 拖入你的 UI 文字物件

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
    }

    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }
}