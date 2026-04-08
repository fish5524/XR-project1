using UnityEngine;
using System.Collections;

public class ParrotBornAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float upwardSpeed = 2f;  // 向上速度
    [SerializeField] private float duration = 3f;      // 持續時間

    private Rigidbody rb;

    private void Awake()
    {
        // 如果在 Inspector 沒有分配，自動從同物件取得
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // 取得 Rigidbody
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 開始出生動畫：播放 Parrot_Flap 動畫並給予向上速度
    /// </summary>
    public void StartBorn()
    {
        if (animator == null)
        {
            Debug.LogError("Animator not found!");
            return;
        }

        StartCoroutine(PlayBornAnimation());
    }

    private IEnumerator PlayBornAnimation()
    {
        // 播放 Parrot_Flap 動畫
        animator.Play("AmazonMacaw_Rig:ParrotAnimated|Parrot_Flap", 0, 0f);

        // 給予向上的速度
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, upwardSpeed, rb.linearVelocity.z);
        }
        else
        {
            // 如果沒有 Rigidbody，直接改變位置
            float elapsedTime = 0f;
            Vector3 startPosition = transform.position;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                transform.position = startPosition + Vector3.up * upwardSpeed * elapsedTime;
                yield return null;
            }

            yield break;
        }

        // 持續 t 秒
        yield return new WaitForSeconds(duration);

        // 停止向上移動
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }
    }

    void Update()
    {
        
    }
}
