using UnityEngine;

public class AcornGenerate : MonoBehaviour
{
    [Header("配置設定")]
    public GameObject acornPrefab;      // 你的橡果 Prefab
    public int numberOfAcorns = 10;     // N 個橡果
    public float radius = 10f;         // 圓形半徑 10m
    public Transform centerPoint;      // 生成中心 (通常是 forest1 的位置)

    public float scale = 1f;

    void Start()
    {
        // 如果沒有指定中心，就用自己
        if (centerPoint == null) centerPoint = this.transform;

        GenerateAcorns();
    }

    public void GenerateAcorns()
    {
        for (int i = 0; i < numberOfAcorns; i++)
        {
            // 1. 使用極座標隨機公式：為了讓分佈均勻，半徑要開根號
            // 如果不開根號，橡果會全部擠在中心點
            float r = radius * Mathf.Sqrt(Random.Range(0f, 1f));
            float theta = Random.Range(0f, 1f) * 2 * Mathf.PI;

            // 2. 轉換為 Unity 的 X, Z 座標
            float x = r * Mathf.Cos(theta);
            float z = r * Mathf.Sin(theta);
            float y = 0.5f;

            Vector3 spawnPosition = centerPoint.position + new Vector3(x, y, z);

            // 3. 生成橡果
            GameObject newAcorn = Instantiate(acornPrefab, spawnPosition, Quaternion.identity);
            
            // 4. (選配) 讓橡果變成 forest1 的子物件，方便管理
            newAcorn.transform.parent = centerPoint;

            newAcorn.transform.localScale = Vector3.one * scale;
            
            // 5. (選配) 隨機旋轉，看起來比較自然
            newAcorn.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        }
        
        Debug.Log($"[森林生成] 已在 {radius}m 圓內生成 {numberOfAcorns} 個橡果。");
    }
}
