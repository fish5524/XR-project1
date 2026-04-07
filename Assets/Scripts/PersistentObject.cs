using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    private static PersistentObject instance;

    void Awake()
    {
        // 確保場景中只有一個 XR Origin，避免重複生成
        if (instance == null)
        {
            instance = this;
            // 關鍵指令：告訴 Unity 在切換場景時不要銷毀這個物件
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 如果回到初始場景發現已經有一個了，就毀掉重複的
            Destroy(gameObject);
        }
    }
}
