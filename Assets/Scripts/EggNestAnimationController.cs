using UnityEngine;
using System.Collections;
using System;

public class EggNestAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ParrotBornAnimationController parrotBornAnimationController;
    [SerializeField] private AudioClip waggleClip;
    [SerializeField] private AudioClip eggCrackClip;

    private const int WAGGLE_COUNT = 5;
    private const float INITIAL_SPEED = 0.3f;
    private const float SPEED_INCREMENT = 0.3f;

    private void Awake()
    {
        // 如果在 Inspector 沒有分配，自動從同物件取得
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (parrotBornAnimationController == null)
        {
            parrotBornAnimationController = GetComponent<ParrotBornAnimationController>();
        }
    }

    /// <summary>
    /// 開始播放動畫序列：
    /// 1. Waggle 狀態重複 5 次，速度逐次加快
    /// 2. Hatch 狀態
    /// 3. Settled 狀態
    /// </summary>
    public void StartAnimation(Action onCompleted = null)
    {
        if (animator == null)
        {
            Debug.LogError("Animator not found!");
            return;
        }

        StartCoroutine(PlayAnimationSequence(onCompleted));
    }

    private IEnumerator PlayAnimationSequence(Action onCompleted)
    {
        // 播放 Waggle 動畫 5 次，每次速度越來越快
        for (int i = 0; i < WAGGLE_COUNT; i++)
        {
            float currentSpeed = INITIAL_SPEED + (i * SPEED_INCREMENT);
            animator.speed = currentSpeed;

            // 直接播放 Waggle 狀態
            PlayClip(waggleClip);
            animator.Play("Waggle", 0, 0f);

            // 等待 Waggle 動畫完成
            yield return WaitForAnimationComplete(0);
        }

        // 重置速度回到 1
        animator.speed = 1f;

        // 直接播放 Hatch 狀態
        PlayClip(eggCrackClip);
        animator.Play("Hatch", 0, 0f);

        // 同時呼叫 ParrotBornAnimationController 的 StartBorn
        if (parrotBornAnimationController != null)
        {
            parrotBornAnimationController.StartBorn();
        }
        else
        {
            Debug.LogWarning("ParrotBornAnimationController not found!");
        }
        yield return WaitForAnimationComplete(0);

        // 直接播放 Settled 狀態
        animator.Play("Settled", 0, 0f);

        // 等待 Settled 動畫完成
        yield return WaitForAnimationComplete(0);

        onCompleted?.Invoke();
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    /// <summary>
    /// 等待動畫狀態完成
    /// </summary>
    private IEnumerator WaitForAnimationComplete(int layerIndex)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
        float animationLength = stateInfo.length;

        yield return new WaitForSeconds(animationLength / animator.speed);
    }
}
