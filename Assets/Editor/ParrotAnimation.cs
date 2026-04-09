using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

public class ParrotAnimationByPhysics : MonoBehaviour
{
    [Header("組件綁定")]
    public Animator animator;
    private float lastY;
    public float animationSpeed = 1.0f; // 動畫播放速度倍率

    [Header("觸發設定")]
    public float jumpThreshold = 1.0f; // 向上速度超過多少時觸發動畫
    private bool wasGrounded;

    void Start()
    {
        // 如果你在 Inspector 忘了拉，程式碼會自動在自己身上找
        if (animator == null) animator = GetComponent<Animator>();

        // 如果還是找不到，噴出紅字警告
        if (animator == null) Debug.Log("！！！鸚鵡身上找不到 Animator 組件！！！");

        animator.speed = animationSpeed;
        
        lastY = transform.position.y;

        foreach (var parameter in animator.parameters)
        {
             Debug.Log("找到參數: " + parameter.name);
        }
        
        Debug.Log("--- 請檢查下面這個清單，找到跳躍動作的精確名稱 ---");
        // 注意：這行只能在 Editor 下運作，用來除錯
        UnityEditor.Animations.AnimatorController ac = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        foreach (var layer in ac.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                Debug.Log("【Animator 內的實際名稱】: " + state.state.name);
            }
        }
    }

    void Update()
    {
        if (animator == null) return;

        // 1. 計算這一幀與上一幀的 Y 軸差距
        float currentY = transform.position.y;
        float deltaY = currentY - lastY;

        // 偵測數據：如果想看數值，取消下面這行的註解
        Debug.Log("Y 軸位移: " + deltaY);

        // 2. 判斷是否向上衝 (deltaY 為正值代表上升)
        if (deltaY > jumpThreshold)
        {
            Debug.Log("【座標偵測成功】鸚鵡隨 Camera 上升中，播放動畫！");
            animator.CrossFade("AmazonMacaw_Rig:ParrotAnimated|Parrot_TakeOff", 0.1f);
        }

        // 3. 更新上一幀的座標，供下一幀比對
        lastY = currentY;
    }
}
